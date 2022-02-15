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

    public class ContragentsController : ControllerBase
    {
        private readonly DataContext _context;

        public ContragentsController(DataContext context)
        {
            _context = context;
        }

        // GET: api/Contragents
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Contragent>>> GetContragents(string query)
        {
            if (query == null) return await _context.Contragents.OrderBy(c => c.Name).Take(50).ToListAsync();
            return await _context.Contragents.Where(c => c.Name.Contains(query)).OrderBy(c => c.Name).Take(50).ToListAsync();
        }

        // GET: api/Contragents/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Contragent>> GetContragent(int id)
        {
            if (id == 0) 
            {
                return new Contragent();
            }
            else
            {
                var contragent = await _context.Contragents.FindAsync(id);
                if (contragent == null)
                {
                    return NotFound();
                }
                return contragent;
            }
        }

        // PUT: api/Contragents/5
        // To protect from overposting attacks, enable the specific properties you want to bind to, for
        // more details, see https://go.microsoft.com/fwlink/?linkid=2123754.
        [HttpPut("{id}")]
        public async Task<IActionResult> PutContragent(int id, Contragent contragent)
        {
            if (id != contragent.Id)
            {
                return BadRequest();
            }

            _context.Entry(contragent).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ContragentExists(id))
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

        // POST: api/Contragents
        // To protect from overposting attacks, enable the specific properties you want to bind to, for
        // more details, see https://go.microsoft.com/fwlink/?linkid=2123754.
        [HttpPost]
        public async Task<ActionResult<Contragent>> PostContragent(Contragent contragent)
        {
            _context.Contragents.Add(contragent);
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (System.Exception e)
            {
                Log.Write(e);
                throw;
            }

            return CreatedAtAction("GetContragent", new { id = contragent.Id }, contragent);
        }

        // DELETE: api/Contragents/5
        [HttpDelete("{id}")]
        public async Task<ActionResult<string>> DeleteContragent(int id)
        {
            var contragent = await _context.Contragents.FindAsync(id);
            if (contragent == null)
            {
                return NotFound();
            }
            try
            {
                _context.Contragents.Remove(contragent);
                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (System.Exception e)
                {
                    Log.Write(e);
                    throw;
                }
            }
            catch (Exception e)
            {
                Log.Write(e);
                return BadRequest();
            }
            return "ok";
        }

        private bool ContragentExists(int id)
        {
            return _context.Contragents.Any(e => e.Id == id);
        }
    }
}
