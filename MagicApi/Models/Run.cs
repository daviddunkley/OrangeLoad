using System;

namespace MagicApi.Models
{
    public class Run : RunRequest
    {
        public new Guid Id { get; set; }
        public DateTime StartedAt { get; set; }
        public DateTime? EndedAt { get; set; }
        public Uri ImageUri { get; set; }

        public static Run FakeRun(Guid? id = null)
        {
            var runId = id ?? Guid.NewGuid();

            return new Run()
            {
                Id = runId,
                Height = 1000,
                Width = 1000,
                Name = "Made Up Run",
                StartedAt = DateTime.UtcNow.AddMinutes(-2),
            };
        }
    }
}
