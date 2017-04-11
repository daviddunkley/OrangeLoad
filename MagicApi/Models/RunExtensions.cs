using System;

namespace MagicApi.Models
{
    public static class RunExtensions
    {
        public static Run AsRun(this RunRequest runRequest)
        {
            return new Run()
            {
                Id = runRequest.Id??Guid.NewGuid(),
                Name = runRequest.Name,
                StartedAt = DateTime.UtcNow,
                Height = runRequest.Height,
                Width = runRequest.Width,
                EndedAt = null,
                ImageUri = default(Uri),
            };
        }

        public static RunPoint AsRunPoint(this RunPointRequest runPointRequest, Guid runId)
        {
            return new RunPoint()
            {
                Id = runPointRequest.Id ?? Guid.NewGuid(),
                RunId = runId,
                X = runPointRequest.X,
                Y = runPointRequest.Y,
            };
        }
    }
}
