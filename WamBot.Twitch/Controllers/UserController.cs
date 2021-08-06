using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using TwitchLib.Api;
using WamBot.Twitch.Data;

namespace WamBot.Twitch.Controllers
{
    public class UserController : Controller
    {
        private readonly BotDbContext _context;
        private readonly TwitchAPI _api;

        public UserController(BotDbContext context, TwitchAPI api)
        {
            _context = context;
            _api = api;
        }

        // GET: User
        public async Task<IActionResult> Index(string sortBy = "Name", string sortOrder = "Ascending", int page = 1, int pageCount = 100)
        {
            DbUser AddPenisSize(DbUser user)
            {
                if (user.PenisSize == null)
                    user.PenisSize = PenisUtils.CalculatePenisSize(user, out _);
                return user;
            }

            var users = _context.DbUsers.Include(u => u.DbChannelUsers);
            var count = await users.CountAsync();

            ViewData["NameTargetSortOrder"] = sortBy == "Name" && sortOrder == "Ascending" ? "Descending" : "Ascending";
            ViewData["OnyxPointsTargetSortOrder"] = sortBy == "OnyxPoints" && sortOrder == "Ascending" ? "Descending" : "Ascending";
            ViewData["PenisOffsetTargetSortOrder"] = sortBy == "PenisOffset" && sortOrder == "Ascending" ? "Descending" : "Ascending";
            ViewData["PenisTypeTargetSortOrder"] = sortBy == "PenisType" && sortOrder == "Ascending" ? "Descending" : "Ascending";
            ViewData["TotalBalanceTargetSortOrder"] = sortBy == "TotalBalance" && sortOrder == "Ascending" ? "Descending" : "Ascending";
            ViewData["PenisSizeTargetSortOrder"] = sortBy == "PenisSize" && sortOrder == "Ascending" ? "Descending" : "Ascending";

            IEnumerable<DbUser> sortedUsers = (sortBy, sortOrder) switch
            {
                ("Name", "Descending") => users.OrderByDescending(u => u.Name),
                ("Name", _) => users.OrderBy(u => u.Name),
                ("OnyxPoints", "Descending") => users.OrderByDescending(u => u.OnyxPoints),
                ("OnyxPoints", _) => users.OrderBy(u => u.OnyxPoints),
                ("PenisOffset", "Descending") => users.OrderByDescending(u => u.PenisOffset),
                ("PenisOffset", _) => users.OrderBy(u => u.PenisOffset),
                ("PenisType", "Descending") => users.OrderByDescending(u => u.PenisType),
                ("PenisType", _) => users.OrderBy(u => u.PenisType),
                ("TotalBalance", "Descending") => (await users.ToListAsync()).OrderByDescending(u => u.TotalBalance),
                ("TotalBalance", _) => (await users.ToListAsync()).OrderBy(u => u.TotalBalance),
                ("PenisSize", "Descending") => (await users.ToListAsync()).Select(AddPenisSize).OrderByDescending(u => u.PenisSize),
                ("PenisSize", _) => (await users.ToListAsync()).Select(AddPenisSize).OrderBy(u => u.PenisSize),
                (_, _) => users
            };

            ViewData["CurrentSort"] = sortBy;
            ViewData["CurrentOrder"] = sortOrder;

            var items = sortedUsers.Skip((page - 1) * pageCount).Take(pageCount).Select(AddPenisSize);
            return View(new IndexModel(items, page, (int)Math.Ceiling(count / (double)pageCount), count));
        }

        // GET: User/Details/5
        public async Task<IActionResult> Details(long id)
        {
            if (id == 0)
            {
                return NotFound();
            }

            var dbUser = await _context.DbUsers
                .Include(d => d.DbChannelUsers)
                .ThenInclude(d => d.DbChannel)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (dbUser == null)
            {
                return NotFound();
            }

            dbUser.PenisSize = PenisUtils.CalculatePenisSize(dbUser, out _);
            return View(dbUser);
        }

        // GET: User/Edit/5
        public async Task<IActionResult> Edit(long id)
        {
            if (id == 0)
            {
                return NotFound();
            }

            var dbUser = await _context.DbUsers.FindAsync(id);
            if (dbUser == null)
            {
                return NotFound();
            }
            return View(dbUser);
        }

        // POST: User/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(long id, [Bind("Name,OnyxPoints,PenisOffset,PenisType")] DbUser dbUser)
        {
            if (id != dbUser.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(dbUser);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!DbUserExists(dbUser.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(dbUser);
        }

        // GET: User/Delete/5
        public async Task<IActionResult> Delete(long id)
        {
            if (id == 0)
            {
                return NotFound();
            }

            var dbUser = await _context.DbUsers
                .FirstOrDefaultAsync(m => m.Id == id);
            if (dbUser == null)
            {
                return NotFound();
            }

            return View(dbUser);
        }

        // POST: User/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(long id)
        {
            var dbUser = await _context.DbUsers.FindAsync(id);
            _context.DbUsers.Remove(dbUser);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool DbUserExists(long id)
        {
            return _context.DbUsers.Any(e => e.Id == id);
        }
    }
}
