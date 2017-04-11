using System;
using MagicApi.Models;

namespace MagicApi.Repositories
{
    public static class RunEntityExtensions
    {
        public const string RunPartitionKey = "RunPartitionKey";
        public static Run AsRun(this RunEntity runEntity)
        {
            return new Run()
            {
                Id = Guid.Parse(runEntity.RowKey),
                EndedAt = runEntity.EndedAt,
                Height = runEntity.Height,
                ImageUri = runEntity.ImageUri == null ? default(Uri) : new Uri(runEntity.ImageUri),
                Name = runEntity.Name,
                StartedAt = runEntity.StartedAt,
                Width = runEntity.Width,
            };
        }

        public static RunEntity AsRunEntity(this Run run)
        {
            return new RunEntity()
            {
                PartitionKey = RunPartitionKey,
                RowKey = run.Id.ToString(),
                EndedAt = run.EndedAt,
                Height = run.Height,
                Name = run.Name,
                ImageUri = run.ImageUri?.AbsoluteUri,
                StartedAt = run.StartedAt,
                Width = run.Width,
            };
        }

        public static RunPoint AsRunPoint(this RunPointEntity runPointEntity)
        {
            return new RunPoint()
            {
                Id = runPointEntity.Id,
                RunId = runPointEntity.RunId,
                X = runPointEntity.X,
                Y = runPointEntity.Y,
            };
        }

        public static RunPointEntity AsRunPointEntity(this RunPoint runPoint)
        {
            return new RunPointEntity()
            {
                PartitionKey = runPoint.RunId.ToString(),
                RowKey = runPoint.Id.ToString(),
                X = runPoint.X,
                Y = runPoint.Y
            };
        }
    }
}
