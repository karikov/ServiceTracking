using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Service.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Service.Functions;

namespace Service.Controllers.Api
{
    [Route("api/[controller]")]
    [ApiController]
    public class AccessesController : ControllerBase
    {
        private readonly DataContext _context;

        public AccessesController(DataContext context)
        {
            _context = context;
        }

        [HttpGet]
        [Authorize]
        public async Task<IEnumerable<Access>> GetAccessesAsync(int userId)
        {
            User user = null;
            if (userId != 0)
            {
                user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            } else {
                user = await _context.Users.FirstOrDefaultAsync(u => u.Email == User.Identity.Name);
            }
            if (user.Role == "admin") return await _context.Accesses.ToListAsync();
            return await _context.Accesses.Where(a => a.Users.Contains(user)).ToListAsync();
        }

        [HttpGet("{id}")]
        [Authorize]
        [Authorize(Roles = "admin")]
        public async Task<ActionResult<Access>> GetAccessAsync(int id, int addUserId, int removeUserId)
        {
            if (id == 0) return new Access();
            if (addUserId != 0 && id != 0)
            {
                User user = await _context.Users.FindAsync(addUserId);
                Access access = await _context.Accesses.FindAsync(id);
                try
                {
                    access.Users.Add(user);
                    await _context.SaveChangesAsync();
                }
                catch (System.Exception e)
                {
                    Log.Write(e);
                    throw;
                }
                return null;
            }
            if (removeUserId != 0 && id != 0)
            {
                User user = await _context.Users.FindAsync(removeUserId);
                Access access = await _context.Accesses.Include(a => a.Users).FirstOrDefaultAsync(a => a.Id == id);
                access.Users.Remove(user);

                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (System.Exception e)
                {
                    Log.Write(e);
                    throw;
                }
                return null;
            }

            return await _context.Accesses.FindAsync(id);
        }

        [HttpPost]
        [Authorize(Roles = "admin")]
        public async Task<ActionResult<Access>> PostAccess(Access access)
        {
            _context.Accesses.Add(access);
            await _context.SaveChangesAsync();
            
            return CreatedAtAction("GetAccess", new { id = access.Id }, access);
        }


    }
}
