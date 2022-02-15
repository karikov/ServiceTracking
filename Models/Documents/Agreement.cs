using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Service.Models
{
    public class Agreement
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Number { get; set; }
        public int ContragentId { get; set; }
        [JsonIgnore]
        public Contragent Contragent { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public bool LongTime { get; set; }
        public int CurrencyId { get; set; }
        public Currency Currency { get; set; }
        public double Summ { get; set; }
        public double InvoiceSumm { get; set; }
        public double PayedSumm { get; set; }
        public double RestSumm { get; set; }
        public List<Invoice> Invoices { get; set; }
        public List<Order> Orders { get; set; }
        public List<Payment> Payments { get; set; }
        public string Reference { get; set; }
        [NotMapped]
        public string TempReference { get; set; }

        public Agreement()
        {
            Invoices = new List<Invoice>();
            Orders = new List<Order>();
            Payments = new List<Payment>();
        }
        public Agreement(int contragentId)
        {
            StartDate = DateTime.Now;
            EndDate = DateTime.Now.AddYears(1);
            ContragentId = contragentId;
            Invoices = new List<Invoice>();
            Orders = new List<Order>();
            Payments = new List<Payment>();
        }
        public double GetPayedSumm(DataContext context)
        {
            List<CurrencyRate> currencyRates = context.CurrencyRates.ToList();
            double summ = 0;

            foreach (Payment payment in Payments)
            {
                if (payment.CurrencyId == this.CurrencyId) summ += payment.Summ;
                if (payment.CurrencyId > this.CurrencyId)
                {
                    CurrencyRate currencyRate = currencyRates.FirstOrDefault(cr => cr.CurrencyId == payment.CurrencyId && cr.Date == payment.Date);
                    payment.Summ += payment.Summ * currencyRate.Multiplexor;
                }
                if (payment.CurrencyId < this.CurrencyId)
                {
                    CurrencyRate currencyRate = currencyRates.FirstOrDefault(cr => cr.CurrencyId == this.CurrencyId && cr.Date == payment.Date);
                    payment.Summ += payment.Summ / currencyRate.Multiplexor;
                }
            }
            return Math.Round(summ, 2);
        }

        public double GetInvoiceSumm(DataContext context)
        {
            List<CurrencyRate> currencyRates = context.CurrencyRates.ToList();
            double summ = 0;

            foreach (Invoice invoice in Invoices)
            {
                if (invoice.CurrencyId == this.CurrencyId) summ += invoice.Summ;
                if (invoice.CurrencyId > this.CurrencyId)
                {
                    CurrencyRate currencyRate = currencyRates.FirstOrDefault(cr => cr.CurrencyId == invoice.CurrencyId && cr.Date == invoice.Date);
                    invoice.Summ += invoice.Summ * currencyRate.Multiplexor;
                }
                if (invoice.CurrencyId < this.CurrencyId)
                {
                    CurrencyRate currencyRate = currencyRates.FirstOrDefault(cr => cr.CurrencyId == this.CurrencyId && cr.Date == invoice.Date);
                    invoice.Summ += invoice.Summ / currencyRate.Multiplexor;
                }
            }
            return Math.Round(summ, 2);
        }
        public double GetRestSumm(DataContext context)
        {
            if (Summ > 0) return Summ - GetPayedSumm(context);
            return Math.Round(GetInvoiceSumm(context) - GetPayedSumm(context), 2);
        }
    }
}
