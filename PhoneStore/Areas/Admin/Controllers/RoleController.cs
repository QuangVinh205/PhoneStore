using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PhoneStore.DB;
using PhoneStore.Models;

namespace PhoneStore.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class RoleController : Controller
    {
        private readonly PhoneStoreDbContext _db;
        public RoleController(PhoneStoreDbContext db) => _db = db;

        public async Task<IActionResult> Index()
        {
            var roles = await _db.Roles.ToListAsync();
            return View(roles);
        }

        public IActionResult Create() => View(new Roles());

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Roles model)
        {
            if (!ModelState.IsValid) return View(model);
            _db.Roles.Add(model);
            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int id)
        {
            var r = await _db.Roles.FindAsync(id);
            if (r == null) return NotFound();
            return View(r);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Roles model)
        {
            if (id != model.Id) return BadRequest();
            if (!ModelState.IsValid) return View(model);
            _db.Roles.Update(model);
            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Details(int id)
        {
            var r = await _db.Roles.FindAsync(id);
            if (r == null) return NotFound();
            return View(r);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var r = await _db.Roles.FindAsync(id);
            if (r == null) return NotFound();
            _db.Roles.Remove(r);
            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
    }
}
