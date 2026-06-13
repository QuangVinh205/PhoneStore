using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PhoneStore.DB;
using PhoneStore.Models;

namespace PhoneStore.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class UserController : Controller
    {
        private readonly PhoneStoreDbContext _db;
        public UserController(PhoneStoreDbContext db) => _db = db;

        public async Task<IActionResult> Index()
        {
            var users = await _db.Users.Include(u => u.Role).ToListAsync();
            return View(users);
        }

        public async Task<IActionResult> Edit(int id)
        {
            var u = await _db.Users.FindAsync(id);
            if (u == null) return NotFound();
            ViewBag.Roles = await _db.Roles.Select(r => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem(r.Name, r.Id.ToString())).ToListAsync();
            return View(u);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Users model)
        {
            if (id != model.Id) return BadRequest();
            if (!ModelState.IsValid) return View(model);
            _db.Users.Update(model);
            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Details(int id)
        {
            var u = await _db.Users.Include(x => x.Role).FirstOrDefaultAsync(x => x.Id == id);
            if (u == null) return NotFound();
            return View(u);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var u = await _db.Users.FindAsync(id);
            if (u == null) return NotFound();
            _db.Users.Remove(u);
            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
    }
}
