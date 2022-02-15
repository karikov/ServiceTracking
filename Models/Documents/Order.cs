using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace Service.Models
{
    public class Order
    {
        public Order(DataContext context, int agreementId, int userId)
        {
            UserId = userId;
            Date = DateTime.Now;
            AgreementId = agreementId;
            ContragentId = context.Agreements.FirstOrDefault(a => a.Id == agreementId).ContragentId;
            OrderItems = new List<OrderItem>();
            Signatures = new List<Signature>();
        }
        public Order(int userId)
        {
            UserId = userId;
            Date = DateTime.Now;
            OrderItems = new List<OrderItem>();
            Signatures = new List<Signature>();
        }
        public Order()
        {
            Date = DateTime.Now;
            OrderItems = new List<OrderItem>();
            Signatures = new List<Signature>();
        }
        public int Id { get; set; }
        public DateTime Date { get; set; }
        public string Reference { get; set; }
        [JsonIgnore]
        public Agreement Agreement { get; set; }
        public int AgreementId { get; set; }
        [JsonIgnore]
        public Contragent Contragent { get; set; }
        public int ContragentId { get; set; }
        public int InvoiceId { get; set; }
        [JsonIgnore]
        public Invoice Invoice { get; set; }
        public int UserId { get; set; }
        public User User { get; set; }
        public string Justification { get; set; }
        public bool Approved { get; set; }
        public double Summ { get; set; }
        public List<OrderItem> OrderItems { get; set; }
        public int? OrderRegistryId { get; set; }
        [JsonIgnore]
        public OrderRegistry OrderRegistry { get; set; }
        public List<Signature> Signatures { get; set; }

        public double GetSumm()
        {
            double summ = 0;
            foreach (OrderItem item in OrderItems)
            {
                summ += item.Price * item.Qty;
            }
            return summ;
        }
        public bool GetApproved()
        {
            if (Signatures.Count > 0)
            {
                int approves = 0;
                foreach (Signature signature in Signatures)
                {
                    if (signature.Approval) approves++;
                }
                if (approves >= 2) return true;
            }
            return false;
        }


        public int CreateOrdersFromInvoices(DateTime startDate, DateTime endDate, int userId, DataContext context)
        {
            int counter = 0;
            List<User> users = context.Users.ToList();
            List<Order> orders = new List<Order>();
            List<Invoice> invoices = context.Invoices
                .Include(i => i.InvoiceItems)
                .Include(i => i.Contragent)
                .Where(i => i.Date <= endDate && i.Date >= startDate && i.Contragent.UseForReport == true)
                .OrderBy(i => i.Date).ToList();
            foreach (Invoice invoice in invoices)
            {
                if (context.Orders.FirstOrDefault(o => o.InvoiceId == invoice.Id) != null) continue;

                Order order = new Order()
                {
                    Date = invoice.Date.AddMonths(-1),
                    Reference = "",
                    AgreementId = invoice.AgreementId,
                    ContragentId = invoice.ContragentId,
                    InvoiceId = invoice.Id,
                    Justification = "Производственная необходимость",
                    Summ = invoice.Summ,
                    UserId = invoice.Contragent.UserId ?? userId
                };
                context.Orders.Add(order);
                context.SaveChanges();

                List<User> SignUsers = new List<User>();
                User accountant = users.FirstOrDefault(u => u.Id == 5);
                User ceo = users.FirstOrDefault(u => u.Id == 7);
                User init = new User();

                SignUsers.Add(ceo);
                SignUsers.Add(accountant);

                if (order.Contragent.UserId != accountant.Id && order.Contragent.UserId != null)
                {
                    init = users.FirstOrDefault(u => u.Id == order.Contragent.UserId);
                    if (init.Id == 12 && order.Date < DateTime.ParseExact("13.07.2021", "dd.MM.yyyy", null)) init = users.FirstOrDefault(u => u.Id == 15);
                    if (init != null) SignUsers.Add(init);
                }

                foreach (User user in SignUsers)
                {
                    order.Signatures.Add(new Signature()
                    {
                        Approval = true,
                        Date = order.Date,
                        OrderId = order.Id,
                        UserId = user.Id,
                        Submitted = true
                    });
                }

                foreach (InvoiceItem invoiceItem in invoice.InvoiceItems)
                {
                    order.OrderItems.Add(new OrderItem()
                    {
                        DeliveryDate = invoiceItem.DeliveryDate,
                        Description = invoiceItem.Description,
                        OrderId = order.Id,
                        InvoiceId = invoice.Id,
                        Qty = invoiceItem.Qty,
                        Price = invoiceItem.Price
                    });
                }
                context.SaveChanges();

                counter++;
            }

            return counter;
        }

    }
}