using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Service.Models
{
    public class Position
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public bool DefaultApprover { get; set; }
        public Position()
        {
            Name = "";
            DefaultApprover = false;
        }
    }
}
