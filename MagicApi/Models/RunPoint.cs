using System;

namespace MagicApi.Models
{
    public class RunPoint : RunPointRequest
    {
        public new Guid Id { get; set; }
        public Guid RunId { get; set; }

        public static RunPoint FakePoint(Guid runId, Guid? id = null)
        {
            return new RunPoint()
            {
                Id = id??Guid.NewGuid(),
                RunId = runId,
                X = 143,
                Y = 54,
            };
        }
    }
}
