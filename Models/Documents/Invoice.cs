using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace Service.Models
{
    public class Invoice
    {
        public int Id { get; set; }
        public string Number { get; set; }
        public string Type { get; set; }
        public DateTime Date { get; set; }
        public double Summ { get; set; }
        public double CurrencyRate { get; set; }
        public int CurrencyId { get; set; }
        public int ContragentId { get; set; }
        public Contragent Contragent { get; set; }
        public int AgreementId { get; set; }
        public List<InvoiceItem> InvoiceItems{ get; set; }
        public string Reference { get; set; }
        public Invoice()
        {
            InvoiceItems = new List<InvoiceItem>();
            CurrencyRate = 1;
        }
    }
}
