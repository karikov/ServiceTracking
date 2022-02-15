using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Service.Models
{
    public class Signature
    {
        public Signature()
        {
            Date = DateTime.Now;
            Submitted = false;
        }
        public Signature(int orderId)
        {
            Date = DateTime.Now;
            OrderId = orderId;
            Submitted = false;
        }
        public int Id { get; set; }
        [JsonIgnore]
        public User User { get; set; }
        public int UserId { get; set; }
        public int OrderId { get; set; }
        public string Comment { get; set; }
        public bool Approval { get; set; }
        public bool Submitted { get; set; }
        public DateTime Date { get; set; }
    }
}
