using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ShoppingCart.DataAccess.Repositories;

using ShoppingCart.Models;
using System.Diagnostics;
using System.Security.Claims;

namespace ShoppingCart.Web.Areas.Customer.Controllers
{
    [Area("Customer")]
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private IUnitOfWork _unitOfWork;

        public HomeController(ILogger<HomeController> logger, IUnitOfWork unitOfWork)
        {
            _logger = logger;
            _unitOfWork = unitOfWork;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            IEnumerable<Product> products = await _unitOfWork.Product.GetAllAsync(includeProperties: "Category");
            var claimsIdentity = (ClaimsIdentity?)User.Identity;
            var claims = claimsIdentity?.FindFirst(ClaimTypes.NameIdentifier);
            if (claims != null)
                HttpContext.Session.SetInt32("SessionCart", (await _unitOfWork
                            .Cart.GetAllAsync(x => x.ApplicationUserId == claims.Value)).ToList().Count);
            return View(products);
        }

        [HttpGet]
        public async Task<IActionResult> Details (int? productId)
        {
            Cart cart = new()
            {
                Product = await _unitOfWork.Product.GetTAsync(x => x.Id == productId,
                    includeProperties:"Category"),
                Count = 1,
                ProductId = (int)productId
            };
            return View(cart);
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Details(Cart cart)
        {
            if (ModelState.IsValid)
            {
                var claimsIdentity = (ClaimsIdentity)User.Identity;
                var claims = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);
                cart.ApplicationUserId = claims.Value;
                var cartItem = await _unitOfWork.Cart.GetTAsync(x => x.ProductId == cart.ProductId &&
                x.ApplicationUserId == claims.Value);

                if (cartItem == null)
                {
                    await _unitOfWork.Cart.AddAsync(cart);
                    await _unitOfWork.SaveAsync();
                }
                else
                {
                    await _unitOfWork.Cart.IncrementCartItemAsync(cartItem, cart.Count);
                    await _unitOfWork.SaveAsync();
                }
            }
            return RedirectToAction("Index");
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}