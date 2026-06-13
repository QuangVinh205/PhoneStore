using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using PhoneStore.DB;
using PhoneStore.Dtos;
using PhoneStore.Models;
using PhoneStore.Service;
using System.Globalization;

namespace PhoneStore.Controllers
{
    public class ProductController : Controller
    {
        //Key để lưu chuỗi json vào session
        public const string CARTKEY = "cart";

        readonly PhoneStoreDbContext _ctx;
        readonly IPayPalService _payPalService;

        public ProductController(PhoneStoreDbContext ctx, IPayPalService payPalService)
        {
            _ctx = ctx;
            _payPalService = payPalService;
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
            List<CartDto> ls = GetCartItems();
            return View(ls);
        }
        [HttpPost]
        [Route("/UpdateCart")]
        public IActionResult UpdateCart(int pid, int quantity)
        {
            List<CartDto> ls = GetCartItems();
            CartDto? p = null;
            foreach (var item in ls)
            {
                if (item.Item!.Id == pid)
                {
                    p = item;
                }
            }
            if (p != null)
            {
                p.Quantity = quantity;
               
            }
            SaveCartSession(ls);
            return View("ViewCart", ls);
        }

        [HttpPost]
        [Route("/RemoveCart")]
        public IActionResult RemoveCart(int pid)
        {
            List<CartDto> ls = GetCartItems();
            CartDto? p = null;
            foreach (var item in ls)
            {
                if (item.Item!.Id == pid)
                {
                    p = item;
                }
            }
            if (p != null)
            {
                ls.Remove(p);
            }
            SaveCartSession(ls);
            return View("ViewCart", ls);
        }
        [Route("/CheckOut")]
        public IActionResult CheckOut()
        {
            List<CartDto> ls = GetCartItems();
            return View(ls);
        }

        [HttpPost]
public async Task<IActionResult> CreatePaymentUrl(PaymentInformation model)
        {
            List<CartDto> items = GetCartItems()
                .Where(o => o.Item != null && o.Quantity > 0)
                .ToList();

            decimal amount = 0;
            foreach (var item in items)
            {
                amount += item.Item!.PriceSale!.Value * item.Quantity;
            }
            model.Amount = amount;

            List<OrdersDetails> details = new List<OrdersDetails>();
            foreach (var item in items)
            {
                OrdersDetails od = new OrdersDetails
                {
                    ProductId = item.Item!.Id,
                    Price = item.Item!.PriceSale!.Value,
                    Quantity = item.Quantity
                };
                details.Add(od);
            }

            Orders ord = new Orders
            {
                CustomerName = model.FullName,
                CustomerPhone = model.Phone,
                CustomerAddress = model.Address,
                OrderDate = DateTime.Now,
                Details = details
            };

            try
            {
                _ctx.Orders.Add(ord);
                await _ctx.SaveChangesAsync();

                var url = await _payPalService.CreatePaymentUrl(model, HttpContext);
                if (string.IsNullOrEmpty(url))
                {
                    TempData["PaymentMessage"] = "Paypal do not response approval URL";
                    return RedirectToAction(nameof(CheckOut));
                }

                return Redirect(url);
            }
            catch (Exception ex)
            {
                TempData["PaymentMessage"] = $"Can not create payment with Paypal: {ex.Message}";
                return RedirectToAction(nameof(CheckOut));
            }
        }

        public async Task<IActionResult> PaymentCallback()
        {
            var response = await _payPalService.PaymentExecute(Request.Query);
            if (response.Success)
            {
                ClearCart();
            }

            return Json(response);
        }
    }
}
