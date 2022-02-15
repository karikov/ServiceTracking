using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace Service.Models
{
    public class InvoiceItem
    {
        public int Id { get; set; }
        public int InvoiceId { get; set; }
        public string Brand { get; set; }
        public string Description { get; set; }
        public double Qty { get; set; }
        public double Price { get; set; }
        public DateTime DeliveryDate { get; set; }
        public string Reference { get; set; }
        [NotMapped]
        public string TempReference { get; set; }

        public InvoiceItem(int invoiceId)
        {
            InvoiceId = invoiceId;
            Brand = "";
            Description = "";
            Qty = 1;
            Price = 0;
            DeliveryDate = DateTime.Now;
        }
        public InvoiceItem()
        {
            Brand = "";
            Description = "";
            Qty = 1;
            Price = 0;
            DeliveryDate = DateTime.Now;
        }
    }
}
