using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using PhoneStore.DB;
using PhoneStore.Dtos.Product;
using PhoneStore.Models;

namespace PhoneStore.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class ProductController : Controller
    {
        private PhoneStoreDbContext db;
        private IWebHostEnvironment env; //enviroment variable controls foder

        public ProductController(PhoneStoreDbContext db, IWebHostEnvironment env) 
        { 
            this.db = db; this.env = env; 
        }
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
        public async Task<IActionResult> Create(CreateProductDtos p)
        {
            if (!ModelState.IsValid)
            {
                return View(p);
            }
            string? imgFileName = string.Empty;
            if (p.Photo != null && p.Photo.Length > 0)
            {
                try
                {
                    var imgFolder = Path.Combine(env.WebRootPath = "img");
                    //Create img folder if not exist
                    if (!Directory.Exists(imgFolder))
                    {
                        Directory.CreateDirectory(imgFolder);
                    }

                    string? imgPath = Path.Combine(imgFolder, p.Photo.FileName);

                    using (var fs = new FileStream(imgPath, FileMode.Create))
                    {
                        await p.Photo.CopyToAsync(fs);
                    }
                    imgFileName = p.Photo.FileName;
                }

                catch(Exception ex) 
                {
                        ViewBag.Message = $"Error uploading image:" + ex.Message;
                        return View(p);
                }
            }

            Category? cate = null;
            if (p.CategoryId.HasValue)
            {
                cate = await db.Categories.FindAsync(p.CategoryId.Value);
            }

            var product = new Product
            {
                Name = p.Name,
                Description = p.Description,
                Price = p.Price,
                PriceSale = p.PriceSale,
                Category = cate,
                Photo = imgFileName
            };

            await db.Products.AddAsync(product);
            await db.SaveChangesAsync();

            return RedirectToAction("Index");
        }
    }
}