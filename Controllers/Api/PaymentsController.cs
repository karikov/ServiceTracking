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
    public class PaymentsController : ControllerBase
    {
        private readonly DataContext _context;

        public PaymentsController(DataContext context)
        {
            _context = context;
        }

        // GET: api/Payments
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Payment>>> GetPayments(int agreementId, int invoiceId)
        {
            if (invoiceId != 0) {
                return await _context.Payments.Where(c => c.InvoiceId == invoiceId).Take(50).ToListAsync();
            } else
            {
                if (agreementId != 0) return await _context.Payments.Where(c => c.AgreementId == agreementId).ToListAsync();
            }
            return await _context.Payments.ToListAsync();
        }

        // GET: api/Payments/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Payment>> GetPayment(int id, int agreementId)
        {
            var payment = await _context.Payments.FindAsync(id);

            if (payment == null)
            {
                if (agreementId != 0) return new Payment(agreementId);
                if (agreementId == 0) return new Payment();
            }

            return payment;
        }

        // PUT: api/Payments/5
        // To protect from overposting attacks, enable the specific properties you want to bind to, for
        // more details, see https://go.microsoft.com/fwlink/?linkid=2123754.
        [HttpPut("{id}")]
        public async Task<IActionResult> PutPayment(int id, Payment payment)
        {
            if (id != payment.Id)
            {
                return BadRequest();
            }

            _context.Entry(payment).State = EntityState.Modified;

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

        // POST: api/Payments
        // To protect from overposting attacks, enable the specific properties you want to bind to, for
        // more details, see https://go.microsoft.com/fwlink/?linkid=2123754.
        [HttpPost]
        public async Task<ActionResult<Payment>> PostPayment(Payment payment)
        {
            if (payment.ContragentId == 0) payment.ContragentId = _context.Agreements.Find(payment.AgreementId).ContragentId;
            if (payment.CurrencyId == 0) payment.CurrencyId = _context.Agreements.Find(payment.AgreementId).CurrencyId;
            _context.Payments.Add(payment);
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
            return CreatedAtAction("GetPayment", new { id = payment.Id }, payment);
        }

        // DELETE: api/Payments/5
        [HttpDelete("{id}")]
        public async Task<ActionResult<Payment>> DeletePayment(int id)
        {
            var payment = await _context.Payments.FindAsync(id);
            if (payment == null)
            {
                return NotFound();
            }

            _context.Payments.Remove(payment);
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
            return payment;
        }

        private bool PaymentExists(int id)
        {
            return _context.Payments.Any(e => e.Id == id);
        }
    }
}
