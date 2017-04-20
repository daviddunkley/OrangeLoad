using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using MagicConsole.Models;
using RestSharp;

namespace MagicConsole
{
    public class Program
    {
        static void Main(string[] args)
        {
            var width = Convert.ToInt32(args[0]);
            var height = Convert.ToInt32(args[1]);
            var level = args[2];
            var magicApiEndpoint = new Uri(ConfigurationManager.AppSettings["MagicApiEndpoint"]);
            var client = new RestClient(magicApiEndpoint);
            client.AddDefaultHeader("X-Level", level);

            var sw = new Stopwatch();
            sw.Start();

            var run = CreateRun(client, width, height);
            Console.WriteLine($"{sw.ElapsedMilliseconds:D6}: Created the Run.");

            var tasks = CreatePoints(client, run);
            Console.WriteLine($"{sw.ElapsedMilliseconds:D6}: Created the {run.Width * run.Height} points.");

            var responseCodes = WaitForPoints(tasks);
            Console.WriteLine($"{sw.ElapsedMilliseconds:D6}: Waited for the {run.Width * run.Height} points to return.");

            Console.WriteLine($"    Create Point returned with the following Response Codes");

            foreach (var responseCode in responseCodes)
            {
                Console.WriteLine($"        {responseCode.Key}: {responseCode.Value}");
            }

            var imageUrl = EndRun(client, run);

            Console.WriteLine($"{sw.ElapsedMilliseconds:D6}: Ended the Run.");
            Console.WriteLine($"    ImageUri: {imageUrl}");
        }

        private static Run CreateRun(IRestClient client, int width, int height)
        {
            var runId = Guid.NewGuid();
            var run = new Run()
            {
                Id = runId,
                Name = $"Run {runId}",
                Width = width,
                Height = height
            };
            
            var request = new RestRequest("runs", Method.POST)
                {
                    RequestFormat = DataFormat.Json
                }
                .AddBody(run);

            var response = client.Execute(request);

            if (response.StatusCode == HttpStatusCode.Created)
            {
                // OK
                return run;
            }

            // NOK
            Console.WriteLine($"Unexpected Response {response.StatusCode} ({response.StatusDescription}) returned when Created Run '{run.Name}'.");
            throw new Exception();
        }

        private static string EndRun(IRestClient client, Run run)
        {
            var request = new RestRequest($"/runs/{run.Id}/end/", Method.POST);

            var response = client.Execute(request);

            if (response.StatusCode == HttpStatusCode.Created)
            {
                // OK
                var imageUri = response.Headers.FirstOrDefault(h => h.Name == "Location")?.Value.ToString();

                var proc = new Process
                {
                    StartInfo =
                    {
                        FileName = @"C:\Program Files (x86)\Google\Chrome\Application\chrome.exe",
                        Arguments = $"{imageUri} --incognito",
                    }
                };
                proc.Start();

                return imageUri;
            }
            else
            {
                // NOK
                Console.WriteLine($"Unexpected Response {response.StatusCode} ({response.StatusDescription}) returned when Ending Run '{run.Name}'.");
                throw new Exception();
            }
        }

        private static List<Task> CreatePoints(IRestClient client, Run run)
        {
            var tasks = new List<Task>();

            for (var x = 0; x < run.Width; x++)
            {
                for (var y = 0; y < run.Height; y++)
                {
                    var runPoint = new RunPoint()
                    {
                        X = x,
                        Y = y,
                    };

                    var request = new RestRequest($"/runs/{run.Id}/point/", Method.POST)
                        {
                            RequestFormat = DataFormat.Json,
                        }
                        .AddBody(runPoint);

                    tasks.Add(client.ExecuteTaskAsync(request));
                }
            }

            return tasks;
        }

        private static Dictionary<string, long> WaitForPoints(List<Task> tasks)
        {
            Task.WaitAll(tasks.ToArray());

            var responseCodes = new Dictionary<string, long>();
            foreach (var task in tasks)
            {
                var restResponse = (task as Task<IRestResponse>)?.Result;

                if (restResponse == null)
                    continue;

                var key = $"{(int)restResponse.StatusCode} ({restResponse.StatusDescription})";

                if (!responseCodes.ContainsKey(key))
                {
                    responseCodes.Add(key, 0);
                }
                responseCodes[key]++;
            }

            return responseCodes;
        }
    }
}
