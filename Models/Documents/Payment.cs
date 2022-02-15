using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Service.Models
{
    public class Payment
    {
        public int Id { get; set; }
        public string Description { get; set; }
        public int AgreementId { get; set; }
        public int ContragentId { get; set; }
        public int InvoiceId { get; set; }
        public double Summ { get; set; }
        public int CurrencyId { get; set; }
        public double CurrencyRate { get; set; }
        public DateTime Date { get; set; }
        public string Reference { get; set; }

        public Payment()
        {
            Date = DateTime.Now;
            CurrencyRate = 1;
        }
        public Payment(int agreementId)
        {
            Date = DateTime.Now;
            AgreementId = agreementId;
            CurrencyRate = 1;
        }
    }
}
