using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Service.Functions;
using Service.Models;

namespace Service.Controllers.Api
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class InvoiceItemsController : ControllerBase
    {
        private readonly DataContext _context;

        public InvoiceItemsController(DataContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<InvoiceItem>>> GetInvoiceItems(string query, int invoiceId)
        {
            if (invoiceId != 0) {
                return await _context.InvoiceItems.Where(ii => ii.InvoiceId == invoiceId).ToListAsync();
            }
            else
            {
            if (query != null) return await _context.InvoiceItems.Where(ii => ii.Description.Contains(query)).OrderBy(ii => ii.Description).ToListAsync();
            }
            return await _context.InvoiceItems.OrderBy(ii => ii.Description).ToListAsync();
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<InvoiceItem>> GetInvoiceItem(int id, int invoiceId)
        {
            var invoiceItem = await _context.InvoiceItems.FindAsync(id);

            if (invoiceItem == null)
            {
                if (invoiceId != 0) return new InvoiceItem(invoiceId); 
                if (invoiceId == 0) return new InvoiceItem(); 
            }

            return invoiceItem;
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> PutInvoiceItem(int id, InvoiceItem invoiceItem)
        {
            if (id != invoiceItem.Id)
            {
                return BadRequest();
            }

            _context.Entry(invoiceItem).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!InvoiceItemExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        [HttpPost]
        public async Task<ActionResult<InvoiceItem>> PostInvoiceItem(InvoiceItem invoiceItem)
        {
            _context.InvoiceItems.Add(invoiceItem);
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (System.Exception e)
            {
                Log.Write(e);
                throw;
            }

            return CreatedAtAction("GetInvoiceItem", new { id = invoiceItem.Id }, invoiceItem);
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult<InvoiceItem>> DeleteInvoiceItem(int id)
        {
            var invoiceItem = await _context.InvoiceItems.FindAsync(id);
            if (invoiceItem == null)
            {
                return NotFound();
            }

            _context.InvoiceItems.Remove(invoiceItem);
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (System.Exception e)
            {
                Log.Write(e);
                throw;
            }

            return invoiceItem;
        }

        private bool InvoiceItemExists(int id)
        {
            return _context.InvoiceItems.Any(e => e.Id == id);
        }
    }
}
