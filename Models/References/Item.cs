using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace Service.Models
{
    public class Item
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Reference { get; set; }
        [NotMapped]
        public string TempReference { get; set; }

    }
}
