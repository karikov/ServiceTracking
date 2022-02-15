using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Service.Models
{
    public class CurrencyRate
    {
        public int Id { get; set; }
        public Currency Currency { get; set; }
        public int CurrencyId { get; set; }
        public double Multiplexor { get; set; }
        public DateTime Date { get; set; }
    }
}
