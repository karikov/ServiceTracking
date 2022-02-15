using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace Service.Models
{
    public class OrderRegistry
    {
        public int Id { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public List<Order> Orders { get; set; }
        public OrderRegistry()
        {
            StartDate = DateTime.Now.AddMonths(-1);
            EndDate = DateTime.Now;
            Orders = new List<Order>();
        }

        public void Refill(DataContext context)
        {
            List<Order> orders = context.Orders.Where(o => o.Date <= this.EndDate 
                && o.Date >= this.StartDate 
                && o.Contragent.UseForReport == true 
                && o.Approved == true).OrderBy(o => o.Date).ToList();
            int counter = 1;
            foreach(Order order in orders)
            {
                order.Reference = counter.ToString();
                context.SaveChanges();
                counter++;
            }
            this.Orders.AddRange(orders);
            context.SaveChanges();
        }

    }
}
