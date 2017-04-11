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
            var requestCount = Convert.ToInt64(args[2]);
            var magicApiEndpoint = new Uri(ConfigurationManager.AppSettings["MagicApiEndpoint"]);
            var sw = new Stopwatch();
            sw.Start();

            var run = CreateRun(magicApiEndpoint, width, height);
            Console.WriteLine($"{sw.ElapsedMilliseconds:D6}: Created the Run.");

            var responseCodes = CreatePoints(magicApiEndpoint, run, requestCount);

            Console.WriteLine($"{sw.ElapsedMilliseconds:D6}: Created the {requestCount} points.");
            Console.WriteLine($"    Create Point returned with the following Response Codes");

            foreach (var responseCode in responseCodes)
            {
                Console.WriteLine($"        {responseCode.Key}: {responseCode.Value}");
            }

            var imageUrl = EndRun(magicApiEndpoint, run);

            Console.WriteLine($"{sw.ElapsedMilliseconds:D6}: Ended the Run.");
            Console.WriteLine($"    ImageUri: {imageUrl}");
            Console.ReadLine();
        }

        private static Run CreateRun(Uri apiEndpoint, int width, int height)
        {
            var runId = Guid.NewGuid();
            var run = new Run()
            {
                Id = runId,
                Name = $"Run {runId}",
                Width = width,
                Height = height
            };
            

            var client = new RestClient(apiEndpoint);
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

        private static string EndRun(Uri apiEndpoint, Run run)
        {
            var client = new RestClient(apiEndpoint);
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
                proc.WaitForExit();
                proc.Close();
                return imageUri;
            }
            else
            {
                // NOK
                Console.WriteLine($"Unexpected Response {response.StatusCode} ({response.StatusDescription}) returned when Ending Run '{run.Name}'.");
                throw new Exception();
            }
        }

        private static Dictionary<string, long> CreatePoints(Uri apiEndpoint, Run run, long requestCount)
        {
            var rnd = new Random();
            var client = new RestClient(apiEndpoint);
            var responseCodes = new Dictionary<string, long>();
            var tasks = new List<Task>();

            for (var i = 0; i < requestCount; i++)
            {
                var runPoint = new RunPoint()
                {
                    X = rnd.Next(0, run.Width),
                    Y = rnd.Next(0, run.Height),
                };

                var request = new RestRequest($"/runs/{run.Id}/point/", Method.POST)
                {
                    RequestFormat = DataFormat.Json,
                }
                    .AddBody(runPoint);

                tasks.Add(client.ExecuteTaskAsync(request));
            }

            Task.WaitAll(tasks.ToArray());

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
