using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Service.Models;
using Microsoft.EntityFrameworkCore;
using Service.Data;
using System.IO;
using Service.Functions;

namespace Service.Controllers.Api
{
    [Route("api/[controller]")]
    [ApiController]
    public class OrderRegistriesController : ControllerBase
    {
        private readonly DataContext _context;

        public OrderRegistriesController(DataContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<OrderRegistry>>> GetOrderRegistries(int fromId)
        {

            return await _context.OrderRegistries.Where(or => or.Id >= fromId).OrderBy(or => or.Id).ToListAsync();
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<OrderRegistry>> GetOrderRegistry(int id)
        {
            var orderRegistry = await _context.OrderRegistries.FirstOrDefaultAsync(or => or.Id == id);

            if (orderRegistry == null)
                return new OrderRegistry();

            return orderRegistry;
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> PutOrderRegistry(int id, OrderRegistry orderRegistry)
        {
            if (id != orderRegistry.Id)
            {
                return BadRequest();
            }

            _context.Entry(orderRegistry).State = EntityState.Modified;

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

            orderRegistry.Refill(_context);

            return NoContent();
        }

        [HttpPost]
        public async Task<ActionResult<OrderRegistry>> PostOrderRegistry(OrderRegistry orderRegistry)
        {
            _context.OrderRegistries.Add(orderRegistry);
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
            orderRegistry.Refill(_context);
            return orderRegistry;
        }


        [HttpDelete("{id}")]
        public async Task<ActionResult<OrderRegistry>> DeleteOrderRegistry(int id)
        {
            var orderRegistry = await _context.OrderRegistries.FindAsync(id);
            if (orderRegistry == null)
            {
                return NotFound();
            }

            _context.OrderRegistries.Remove(orderRegistry);
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
            return orderRegistry;
        }

        [HttpGet]
        [Route("Print/{id?}")]
        public async Task<string> PrintOrderRegistry(int id)
        {
            OrderRegistry orderRegistry = await _context.OrderRegistries
                .Include(or => or.Orders)
                .ThenInclude(o => o.OrderItems)
                .Include(or => or.Orders)
                .ThenInclude(o => o.Contragent)
                .Include(or => or.Orders)
                .ThenInclude(o => o.Invoice)
                .ThenInclude(i => i.InvoiceItems)
                .FirstOrDefaultAsync(o => o.Id == id);
    
            FileInfo file = Printforms.OrderRegistry(orderRegistry);

            return file.Name;
        }


        private bool OrderRegistryExists(int id)
        {
            return _context.OrderRegistries.Any(e => e.Id == id);
        }


    }
}
