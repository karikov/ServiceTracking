using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Service.Data;
using Service.Functions;
using Service.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Service.Controllers.Api
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class OrdersController : ControllerBase
    {
        private readonly DataContext _context;

        public OrdersController(DataContext context)
        {
            _context = context;
        }

        // GET: api/Orders
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Order>>> GetOrders(string query, int agreementId, int orderRegistryId)
        {
            List<Order> orders = new List<Order>();

            if (orderRegistryId == 0)
            {
                orders = await _context.Orders
                    .Include(o => o.Contragent)
                    .Include(o => o.OrderItems)
                    .Include(o => o.Signatures)
                    .OrderByDescending(o => o.Id)
                    .ToListAsync();
            }
            else
            {
                OrderRegistry orderRegistry = await _context.OrderRegistries.FindAsync(orderRegistryId);
                orders = await _context.Orders.Where(o => o.OrderRegistryId != null && o.OrderRegistryId == orderRegistryId).ToListAsync();
            }

            if (agreementId != 0) orders = orders.Where(c => c.AgreementId == agreementId).ToList();
            if (query != null) orders = orders.Where(o => o.Reference.Contains(query) || o.Contragent.Name.Contains(query)).ToList();

            foreach (Order order in orders)
            {
                if (order.Approved != order.GetApproved() || order.Summ != order.GetSumm())
                {
                    order.Summ = Math.Round(order.GetSumm());
                    order.Approved = order.GetApproved();
                    await _context.SaveChangesAsync();
                }
            }

            return orders;
        }


        [HttpGet]
        [Route("WaitApproval")] //For orders, that wait for approval
        public async Task<ActionResult<IEnumerable<Order>>> GetWaitApproval(string query)
        {
            User user = await _context.Users.FirstOrDefaultAsync(u => u.Email == User.Identity.Name);
            if (user == null) return NotFound();
            List<Signature> signatures = await _context.Signatures
                .Where(s => s.Submitted == false && s.UserId == user.Id)
                .ToListAsync();

            List<Order> orders = new List<Order>();

            foreach (Signature signature in signatures)
            {
                orders.Add(await _context.Orders.FindAsync(signature.OrderId));
            }
            if (query != null) orders = orders.Where(o => o.Reference.Contains(query) || o.Contragent.Name.Contains(query)).ToList();

            return orders;
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Order>> GetOrder(int id, int agreementId)
        {
            var order = await _context.Orders.Include(o => o.OrderItems).Include(o => o.Signatures).FirstOrDefaultAsync(o => o.Id == id);

            if (order == null)
            {
                User user = await _context.Users.FirstOrDefaultAsync(u => u.Email == User.Identity.Name);
                if (agreementId != 0) return new Order(_context, agreementId, user.Id);
                if (agreementId == 0) return new Order(user.Id);
            }

            order.Summ = order.GetSumm();
            order.Approved = order.GetApproved();
            await _context.SaveChangesAsync();

            return order;
        }

        [HttpGet]
        [Route("Refresh")] //For data refresh
        public async Task<string> RefreshOrders()
        {
            List<Order> orders = _context.Orders.Include(o => o.Contragent).ToList();

            foreach (Order order in orders)
            {
                order.UserId = order.Contragent.UserId ?? 2;
            }
            await _context.SaveChangesAsync();
            return "ok";
        }



        [HttpGet]
        [Route("Print/{id?}")]
        public async Task<string> PrintOrder(int id)
        {
            Order order = await _context.Orders.Include(o => o.OrderItems)
                                                .Include(o => o.Contragent)
                                                .Include(o => o.User)
                                                .ThenInclude(u => u.Position)
                                                .Include(o => o.Agreement.Currency)
                                                .Include(o => o.Signatures)
                                                .ThenInclude(s => s.User)
                                                .ThenInclude(u => u.Position)
                                                .FirstOrDefaultAsync(o => o.Id == id);
            FileInfo file = Printforms.OrderReport(order);

            return file.Name;
        }

        [HttpGet]
        [Route("Import")]
        public async Task<string> ImportOrders(DateTime startDate, DateTime endDate)
        {
            User user = await _context.Users.FirstOrDefaultAsync(u => u.Email == User.Identity.Name);
            int count = new Order().CreateOrdersFromInvoices(startDate, endDate, user.Id, _context);
            return "Импортировано " + count.ToString() + " PO";
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> PutOrder(int id, Order order)
        {
            if (id != order.Id)
            {
                return BadRequest();
            }

            _context.Entry(order).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (System.Exception e)
            {
                Log.Write(e);
                return BadRequest();
                throw;
            }

            return NoContent();
        }

        [HttpPost]
        public async Task<ActionResult<Order>> PostOrder(Order order)
        {
            _context.Orders.Add(order);
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (System.Exception e)
            {
                Log.Write(e);
                return BadRequest();
                throw;
            }

            _context.Signatures.Add(new Signature
            {
                UserId = order.UserId,
                OrderId = order.Id,
                Approval = true,
                Submitted = true,
                Date = DateTime.Now,
                Comment = "Подписано составителем ордера"
            });
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (System.Exception e)
            {
                Log.Write(e);
                return BadRequest();
                throw;
            }

            return CreatedAtAction("GetOrder", new { id = order.Id }, order);
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult<Order>> DeleteOrder(int id)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order == null)
            {
                return NotFound();
            }

            _context.Orders.Remove(order);
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (System.Exception e)
            {
                Log.Write(e);
                return BadRequest();
                throw;
            }
            return order;
        }

        private bool OrderExists(int id)
        {
            return _context.Orders.Any(e => e.Id == id);
        }
    }
}
