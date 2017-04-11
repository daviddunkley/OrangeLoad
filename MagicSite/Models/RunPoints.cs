using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Web;

namespace MagicSite.Models
{
    public class RunPoints
    {
        public Guid RunId { get; set; }
        public long RequestCount { get; set; }
    }
}