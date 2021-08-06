using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using WamBot.Twitch.Data;

namespace WamBot.Twitch.Controllers
{
    public class ChannelController : Controller
    {
        private readonly BotDbContext _context;

        public ChannelController(BotDbContext context)
        {
            _context = context;
        }

        // GET: Channels
        public async Task<IActionResult> Index()
        {
            var channels = await _context.DbChannels.ToListAsync();
            foreach (var channel in channels)
            {
                channel.TotalUsers = await _context.DbChannelUsers.CountAsync(c => c.ChannelId == channel.Id);
            }

            return View(channels);
        }

        // GET: Channels/Details/5
        public async Task<IActionResult> Details(long id)
        {
            if (id == 0)
            {
                return NotFound();
            }

            var dbChannel = await _context.DbChannels
                .FirstOrDefaultAsync(m => m.Id == id);
            if (dbChannel == null)
            {
                return NotFound();
            }

            return View(dbChannel);
        }

        // GET: Channels/Edit/5
        public async Task<IActionResult> Edit(long id)
        {
            if (id == 0)
            {
                return NotFound();
            }

            var dbChannel = await _context.DbChannels.FindAsync(id);
            if (dbChannel == null)
            {
                return NotFound();
            }
            return View(dbChannel);
        }

        // POST: Channels/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(long id, [Bind("Name,LastStreamId")] DbChannel dbChannel)
        {
            if (id != dbChannel.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(dbChannel);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!DbChannelExists(dbChannel.Id))
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
            return View(dbChannel);
        }

        // GET: Channels/Delete/5
        public async Task<IActionResult> Delete(long id)
        {
            if (id == 0)
            {
                return NotFound();
            }

            var dbChannel = await _context.DbChannels
                .FirstOrDefaultAsync(m => m.Id == id);
            if (dbChannel == null)
            {
                return NotFound();
            }

            return View(dbChannel);
        }

        // POST: Channels/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(long id)
        {
            var dbChannel = await _context.DbChannels.FindAsync(id);
            _context.DbChannels.Remove(dbChannel);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool DbChannelExists(long id)
        {
            return _context.DbChannels.Any(e => e.Id == id);
        }
    }
}
