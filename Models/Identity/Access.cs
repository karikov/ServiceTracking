using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Service.Models
{
    public class Access
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string ApiKey { get; set; }
        public List<User> Users { get; set; }
        public Access()
        {
            Name = "";
            ApiKey = "";
            Users = new List<User>();
        }
    }
}
