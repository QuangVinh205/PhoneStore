using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using PhoneStore.DB;
using PhoneStore.Models;

namespace PhoneStore.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class ProductController : Controller
    {
        private PhoneStoreDbContext db;

        public ProductController(PhoneStoreDbContext db) { this.db = db; }
        public async Task<IActionResult> Index()
        {
            var products = await db.Products.Include(p => p.Category).ToListAsync();
            return View(products);
        }

        public async Task<IActionResult> Create()
        {
            var cats = await db.Categories.Select(c=> new SelectListItem(
                c.Name,c.Id.ToString())).ToListAsync();
            ViewBag.CategoryId = cats;
            return View();
        }
        [HttpPost]
        public async Task<IActionResult> Create(Product p)
        {
            if(!ModelState.IsValid)
            {
                return View(p);
            }

            await db.Products.AddAsync(p);
            await db.SaveChangesAsync();

            return RedirectToAction("Index");
        }
    }
}