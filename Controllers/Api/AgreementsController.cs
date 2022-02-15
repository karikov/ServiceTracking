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
    public class AgreementsController : ControllerBase
    {
        private readonly DataContext _context;

        public AgreementsController(DataContext context)
        {
            _context = context;
        }

        // GET: api/Agreements
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Agreement>>> GetAgreements(string query, int contragentId)
        {
            var agreements = await _context.Agreements
                .Include(a => a.Contragent)
                .Include(a => a.Payments)
                .Include(a => a.Currency)
                .Include(a => a.Invoices).ToListAsync();

            foreach (Agreement agreement in agreements)
            {
                agreement.InvoiceSumm = Math.Round(agreement.GetInvoiceSumm(_context), 2);
                agreement.PayedSumm = Math.Round(agreement.GetPayedSumm(_context), 2);
                agreement.RestSumm = Math.Round(agreement.GetRestSumm(_context), 2);
            }
            if (contragentId != 0)
                agreements = agreements.Where(c => c.ContragentId == contragentId).OrderBy(a => a.Name).ToList();
            if (query != null)
                agreements = agreements.Where(c => c.Name.Contains(query) || c.Contragent.Name.Contains(query)).OrderBy(a => a.Name).ToList();
 
            return agreements;
        }

        [HttpGet]
        [Route("Expired")] //For agreements, that expires

        public async Task<ActionResult<IEnumerable<Agreement>>> GetAgreementsExpired(string query)
        {
            var agreements = await _context.Agreements
                .Include(a => a.Contragent)
                .Include(a => a.Payments)
                .Where(a => a.EndDate.AddMonths(-1) <= DateTime.Now && a.LongTime == false).ToListAsync();

            foreach (Agreement agreement in agreements)
            {
                agreement.InvoiceSumm = Math.Round(agreement.GetInvoiceSumm(_context), 2);
                agreement.PayedSumm = Math.Round(agreement.GetPayedSumm(_context), 2);
                agreement.RestSumm = Math.Round(agreement.GetRestSumm(_context), 2);
            }
            
            if (query != null)
                agreements = agreements.Where(c => c.Name.Contains(query) || c.Contragent.Name.Contains(query)).OrderBy(a => a.Name).ToList();

            return agreements;
        }

        [HttpGet]
        [Route("Overpriced")] //For agreements, that expires

        public async Task<ActionResult<IEnumerable<Agreement>>> GetAgreementsOverpriced(string query)
        {
            var agreements = await _context.Agreements
                .Include(a => a.Contragent)
                .Include(a => a.Payments)
                .ToListAsync();

            foreach(Agreement agreement in agreements)
            {
                agreement.InvoiceSumm = Math.Round(agreement.GetInvoiceSumm(_context), 2);
                agreement.PayedSumm = Math.Round(agreement.GetPayedSumm(_context), 2);
                agreement.RestSumm = Math.Round(agreement.GetRestSumm(_context), 2);
            }
            agreements = agreements.Where(a => a.PayedSumm >= a.Summ * 0.95).ToList(); 

            if (query != null)
                agreements = agreements.Where(c => c.Name.Contains(query) || c.Contragent.Name.Contains(query)).OrderBy(a => a.Name).ToList();

            return agreements;
        }


        // GET: api/Agreements/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Agreement>> GetAgreement(int id, int contragentId)
        {
            var agreement = await _context.Agreements.Include(a => a.Payments).Include(a => a.Currency).Include(a => a.Invoices).FirstOrDefaultAsync(a => a.Id == id);

            if (agreement == null)
            {
                if (contragentId != 0) return new Agreement(contragentId);
                if (contragentId == 0) return new Agreement();
            }
            agreement.PayedSumm = Math.Round(agreement.GetPayedSumm(_context), 2);
            agreement.InvoiceSumm = Math.Round(agreement.GetInvoiceSumm(_context), 2);
            agreement.RestSumm = Math.Round(agreement.GetRestSumm(_context), 2);

            return agreement;
        }

        // PUT: api/Agreements/5
        // To protect from overposting attacks, enable the specific properties you want to bind to, for
        // more details, see https://go.microsoft.com/fwlink/?linkid=2123754.
        [HttpPut("{id}")]
        public async Task<IActionResult> PutAgreement(int id, Agreement agreement)
        {
            if (id != agreement.Id)
            {
                return BadRequest();
            }

            _context.Entry(agreement).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!AgreementExists(id))
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

        // POST: api/Agreements
        // To protect from overposting attacks, enable the specific properties you want to bind to, for
        // more details, see https://go.microsoft.com/fwlink/?linkid=2123754.
        [HttpPost]
        public async Task<ActionResult<Agreement>> PostAgreement(Agreement agreement)
        {
            _context.Agreements.Add(agreement);
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (System.Exception e)
            {
                Log.Write(e);
                throw;
            }

            return CreatedAtAction("GetAgreement", new { id = agreement.Id }, agreement);
        }

        // DELETE: api/Agreements/5
        [HttpDelete("{id}")]
        public async Task<ActionResult<Agreement>> DeleteAgreement(int id)
        {
            var agreement = await _context.Agreements.FindAsync(id);
            if (agreement == null)
            {
                return NotFound();
            }

            _context.Agreements.Remove(agreement);
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (System.Exception e)
            {
                Log.Write(e);
                throw;
            }

            return agreement;
        }

        private bool AgreementExists(int id)
        {
            return _context.Agreements.Any(e => e.Id == id);
        }
    }
}
