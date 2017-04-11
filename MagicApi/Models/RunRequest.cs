using System;

namespace MagicApi.Models
{
    public class RunRequest
    {
        public Guid? Id { get; set; }
        public string Name { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
    }
}