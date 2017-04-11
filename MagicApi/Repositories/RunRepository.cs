using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MagicApi.Models;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;

namespace MagicApi.Repositories
{
    public class RunRepository
    {
        private readonly string _runTableName = "Run";
        private readonly string _runPointTableName = "RunPoint";
        private readonly CloudTableClient _tableClient;

        public RunRepository(string accountName, string accountKey)
        {
            if (string.IsNullOrWhiteSpace(accountName))
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(accountName));
            if (string.IsNullOrWhiteSpace(accountKey))
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(accountKey));

            var connString = $"DefaultEndpointsProtocol=https;AccountName={accountName};AccountKey={accountKey};EndpointSuffix=core.windows.net";

            var storageAccount = CloudStorageAccount.Parse(connString);
            _tableClient = storageAccount.CreateCloudTableClient();

            _tableClient.GetTableReference(_runTableName).CreateIfNotExists();
            _tableClient.GetTableReference(_runPointTableName).CreateIfNotExists();
        }

        public async Task SavePointAsync(RunPoint point)
        {
            if (point == null) throw new ArgumentNullException(nameof(point));

            var table = _tableClient.GetTableReference(_runPointTableName);
            var insertOperation = TableOperation.Insert(point.AsRunPointEntity());
            await table.ExecuteAsync(insertOperation);
        }

        public Task<IEnumerable<Run>> GetRunsAsync()
        {
            var table = _tableClient.GetTableReference(_runTableName);

            TableContinuationToken token = null;
            var runs = new List<Run>();

            do
            {
                var queryResult = table.ExecuteQuerySegmented(new TableQuery<RunEntity>(), token);

                runs.AddRange(queryResult.Results.Select(runEntity => runEntity.AsRun()));
                token = queryResult.ContinuationToken;
            } while (token != null);

            return Task.FromResult(runs.AsEnumerable());
        }

        public async Task SaveRunAsync(Run run)
        {
            if (run == null) throw new ArgumentNullException(nameof(run));

            var table = _tableClient.GetTableReference(_runTableName);
            var insertOperation = TableOperation.Insert(run.AsRunEntity());
            await table.ExecuteAsync(insertOperation);
        }

        public async Task<Run> GetRunAsync(Guid runId)
        {
            var table = _tableClient.GetTableReference(_runTableName);
            var retrieveOperation = TableOperation.Retrieve<RunEntity>(RunEntityExtensions.RunPartitionKey, runId.ToString());
            var retrievedResult = await table.ExecuteAsync(retrieveOperation);

            var runEntity = (RunEntity)retrievedResult.Result;
            return runEntity.AsRun();
        }

        public async Task EndRunAsync(Guid runId, Uri imageUri)
        {
            if (imageUri == null) throw new ArgumentNullException(nameof(imageUri));

            var table = _tableClient.GetTableReference(_runTableName);

            var retrieveOperation = TableOperation.Retrieve<RunEntity>(RunEntityExtensions.RunPartitionKey, runId.ToString());
            var retrievedResult = await table.ExecuteAsync(retrieveOperation);

            var runEntity = (RunEntity) retrievedResult.Result;
            runEntity.ImageUri = imageUri.AbsoluteUri;
            runEntity.EndedAt = DateTime.UtcNow;

            var replaceOperation = TableOperation.Replace(runEntity);
            await table.ExecuteAsync(replaceOperation);
        }

        public async Task<RunPoint> GetRunPointAsync(Guid runId, Guid runPointId)
        {
            var table = _tableClient.GetTableReference(_runPointTableName);
            var retrieveOperation = TableOperation.Retrieve<RunEntity>(runId.ToString(), runPointId.ToString());
            var retrievedResult = await table.ExecuteAsync(retrieveOperation);

            var runPointEntity = (RunPointEntity)retrievedResult.Result;
            return runPointEntity.AsRunPoint();
        }

        public void ForEachRunPoint(Guid runId, Action<int, int> runPointAction)
        {
            var table = _tableClient.GetTableReference(_runPointTableName);
            var query = new TableQuery<RunPointEntity>().Where(TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, runId.ToString()));
            TableContinuationToken token = null;

            do
            {
                var queryResult = table.ExecuteQuerySegmented(query, token);

                foreach (var pointEntity in queryResult.Results)
                {
                    var point = pointEntity.AsRunPoint();
                    runPointAction(point.X, point.Y);
                }
                token = queryResult.ContinuationToken;
            } while (token != null);
        }
    }
}
