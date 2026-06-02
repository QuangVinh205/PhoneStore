using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using PhoneStore.DB;
using PhoneStore.Dtos;
using PhoneStore.Models;
using System.Globalization;

namespace PhoneStore.Controllers
{
    public class ProductController : Controller
    {
        //Key để lưu chuỗi json vào session
        public string CARTKEY = "cart";

        readonly PhoneStoreDbContext _ctx;
        
        public ProductController(PhoneStoreDbContext ctx)
        {
            _ctx = ctx;
        }
        

        // Lấy cart từ Session (danh sách CartItem)
        List<CartDto> GetCartItems()
        {

            var session = HttpContext.Session;
            string? jsoncart = session.GetString(CARTKEY);
            if (jsoncart != null)
            {
                var result = JsonConvert.DeserializeObject<List<CartDto>>(jsoncart);
                if(result != null)
                {
                    return result;
                }
            }
            return new List<CartDto>();
        }

        // Xóa cart khỏi session
        void ClearCart()
        {
            var session = HttpContext.Session;
            session.Remove(CARTKEY);
        }

        // Lưu Cart (Danh sách CartItem) vào session
        void SaveCartSession(List<CartDto> ls)
        {
            var session = HttpContext.Session;
            string jsoncart = JsonConvert.SerializeObject(ls);
            session.SetString(CARTKEY, jsoncart);
        }


        public async Task <IActionResult> Index()
        {
            var prods = await _ctx.Products.ToListAsync();
            return View(prods);
        }

        [Route("/Cart/AddToCart")]
        public async Task<IActionResult> AddToCart(int pid, int? quantity)
        {
            int q = quantity ?? 1; // Nếu quantity là null, mặc định là 1
            Product? prod = await _ctx.Products.FirstOrDefaultAsync(p => p.Id == pid);
            if (prod != null) 
            {
                CartDto dto = new CartDto
                {
                    Item = prod,
                    Quantity = q
                };

                List<CartDto>? ls = GetCartItems();
                ls.Add(dto);
                SaveCartSession(ls);
            }
            return RedirectToAction("Index", "Home");
        }

        [Route("/Product/{slug}")]
        public async Task<IActionResult> Details(string slug)
        {
            var prod = await _ctx.Products
                .Include(p=> p.Category)
                .SingleOrDefaultAsync(p=>p.Slug == slug);
           return View(prod);
        }

        [Route("/ViewCart")]
        public IActionResult ViewCart()
        {
            return View();
        }


    }
}
