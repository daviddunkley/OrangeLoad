using System;
using Microsoft.WindowsAzure.Storage.Table;

namespace MagicApi.Repositories
{
    public class RunEntity : TableEntity
    {
        public Guid Id => Guid.Parse(RowKey);
        public string Name { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public DateTime StartedAt { get; set; }
        public DateTime? EndedAt { get; set; }
        public string ImageUri { get; set; }
    }
}
