using System;
using MagicApi.Models;
using Microsoft.WindowsAzure.Storage.Table;

namespace MagicApi.Repositories
{
    public class RunPointEntity : TableEntity
    {
        public Guid RunId => Guid.Parse(PartitionKey);
        public Guid Id => Guid.Parse(RowKey);
        public int X { get; set; }
        public int Y { get; set; }

        public RunPoint AsRunPoint()
        {
            return new RunPoint()
            {
                Id = Id,
                RunId = RunId,
                X = X,
                Y = Y,
            };
        }
    }
}
