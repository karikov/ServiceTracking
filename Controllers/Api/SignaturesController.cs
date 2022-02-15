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
    public class SignaturesController : ControllerBase
    {
        private readonly DataContext _context;

        public SignaturesController(DataContext context)
        {
            _context = context;
        }

        // GET: api/Signatures
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Signature>>> GetSignatures(int orderId, int userId)
        {
            if (orderId != 0) return await _context.Signatures.Where(s => s.OrderId == orderId).ToListAsync();
            if (userId != 0) return await _context.Signatures.Where(s => s.UserId == userId).ToListAsync();
            User user = await _context.Users.FirstOrDefaultAsync(u => u.Email == User.Identity.Name);
            return await _context.Signatures.Where(s => s.UserId == user.Id).ToListAsync();
        }

        // GET: api/Signatures/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Signature>> GetSignature(int id, int orderId)
        {
            var signature = await _context.Signatures.Include(i => i.User).ThenInclude(u => u.Position).FirstOrDefaultAsync(s => s.Id == id);

            if (signature == null)
            {
                User user = await _context.Users.FirstOrDefaultAsync(u => u.Email == User.Identity.Name);
                if (orderId != 0) return new Signature(orderId);
                if (orderId == 0) return new Signature();
            }

            return signature;
        }

        // PUT: api/Signatures/5
        // To protect from overposting attacks, enable the specific properties you want to bind to, for
        // more details, see https://go.microsoft.com/fwlink/?linkid=2123754.
        [HttpPut("{id}")]
        public async Task<IActionResult> PutSignature(int id, Signature signature)
        {
            if (id != signature.Id) return BadRequest();

            if (signature.UserId != _context.Users.FirstOrDefault(u => u.Email == User.Identity.Name).Id && signature.Approval == true)
                return BadRequest();
            if (signature.UserId == _context.Users.FirstOrDefault(u => u.Email == User.Identity.Name).Id && signature.Approval == true)
                signature.Submitted = true;
            if (signature.UserId == _context.Users.FirstOrDefault(u => u.Email == User.Identity.Name).Id && signature.Approval == false)
                signature.Submitted = true;

            signature.Date = DateTime.Now;

            _context.Entry(signature).State = EntityState.Modified;

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

        // POST: api/Signatures
        // To protect from overposting attacks, enable the specific properties you want to bind to, for
        // more details, see https://go.microsoft.com/fwlink/?linkid=2123754.
        [HttpPost]
        public async Task<ActionResult<Signature>> PostSignature(Signature signature)
        {
            if (signature.UserId != _context.Users.FirstOrDefault(u => u.Email == User.Identity.Name).Id && signature.Approval == true)
                return BadRequest();
            if (signature.UserId == _context.Users.FirstOrDefault(u => u.Email == User.Identity.Name).Id && signature.Approval == true)
                signature.Submitted = true;

            _context.Signatures.Add(signature);
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
            return CreatedAtAction("GetSignature", new { id = signature.Id }, signature);
        }

        // DELETE: api/Signatures/5
        [HttpDelete("{id}")]
        public async Task<ActionResult<Signature>> DeleteSignature(int id)
        {
            var signature = await _context.Signatures.FindAsync(id);
            if (signature == null)
            {
                return NotFound();
            }

            _context.Signatures.Remove(signature);
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

        //[HttpGet]
        //[Route("Import")]
        //public async Task<string> ImportSignatures()
        //{
        //    User user = await _context.Users.FirstOrDefaultAsync(u => u.Email == User.Identity.Name);
        //    int count = new Order().RecreateSignatures(_context);
        //    return "Импортировано " + count.ToString() + " PO";
        //}


        private bool SignatureExists(int id)
        {
            return _context.Signatures.Any(e => e.Id == id);
        }
    }
}
