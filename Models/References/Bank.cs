using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace Service.Models
{
    public class Bank
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Code { get; set; }
        public string Adress { get; set; }
        public string Description { get; set; }
        public Bank()
        {
            Name = "";
            Code = "";
            Adress = "";
            Description = "";
        }
    }
}
