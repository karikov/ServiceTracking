using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Service.Models
{
    public class User
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
        public string Role { get; set; }
        public Position Position { get; set; }
        public int PositionId { get; set; }
        public List<Access> Accesses { get; set; }
        public List<Signature> Signatures { get; set; }
        public User()
        {
            Name = "";
            Email = "";
            Password = "";
            Role = "";
            Accesses = new List<Access>();
            Signatures = new List<Signature>();
        }
    }
}
