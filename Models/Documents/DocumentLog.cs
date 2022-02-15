using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Service.Models
{
    public class DocumentLog
    {
        public int Id { get; set; }
        // Type of logs: 1 - Comment, 2 - Event
        public int Type { get; set; }
        public DateTime dateTime { get; set; }
        public string Content { get; set; }
    }
}

