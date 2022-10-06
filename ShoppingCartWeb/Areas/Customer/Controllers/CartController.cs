using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ShoppingCart.DataAccess.Repositories;
using ShoppingCart.DataAccess.ViewModels;
using ShoppingCart.Models;
using ShoppingCart.Utility;
using Stripe.Checkout;
using System.Security.Claims;

namespace ShoppingCart.Web.Areas.Customer.Controllers
{
    [Area("Customer")]
    [Authorize]
    public class CartController : Controller
    {
        private IUnitOfWork _unitOfWork;
        public CartVM vm { get; set; }

        public CartController (IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<IActionResult> Index()
        {
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var claims = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);
            vm = new CartVM()
            {
                ListOfCart = await _unitOfWork.Cart.GetAllAsync(x => x.ApplicationUserId == claims.Value, 
                includeProperties: "Product"),
                OrderHeader = new OrderHeader()
            };
            foreach (var item in vm.ListOfCart)
            {
                vm.OrderHeader.OrderTotal += (item.Product.Price * item.Count);
            }
            return View(vm);
        }

        public async Task<IActionResult> Summary()
        {
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var claims = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);
            vm = new CartVM()
            {
                ListOfCart = await _unitOfWork.Cart.GetAllAsync(x => x.ApplicationUserId == claims.Value, includeProperties: "Product"),
                OrderHeader = new OrderHeader()
            };
            vm.OrderHeader.ApplicationUser = await _unitOfWork.ApplicationUser.GetTAsync(x => x.Id == claims.Value);
            vm.OrderHeader.Name = vm.OrderHeader.ApplicationUser.Name;
            vm.OrderHeader.Phone = vm.OrderHeader.ApplicationUser.PhoneNumber;
            vm.OrderHeader.Address = vm.OrderHeader.ApplicationUser.Address;
            vm.OrderHeader.City = vm.OrderHeader.ApplicationUser.City;
            vm.OrderHeader.State = vm.OrderHeader.ApplicationUser.State;
            vm.OrderHeader.PostalCode = vm.OrderHeader.ApplicationUser.PinCode;
            foreach (var item in vm.ListOfCart)
            {
                vm.OrderHeader.OrderTotal += item.Product.Price * item.Count;
            }
            return View(vm);
        }

        [HttpPost]
        public async Task<IActionResult> Summary(CartVM vm)
        {
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var claims = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);
            vm.ListOfCart = await _unitOfWork.Cart.GetAllAsync(x => x.ApplicationUserId == claims.Value, 
                includeProperties: "Product");
            vm.OrderHeader.OrderStatus = OrderStatus.StatusPending;
            vm.OrderHeader.PaymentStatus = PaymentStatus.StatusPending;
            vm.OrderHeader.DateOfOrder = DateTime.Now;
            vm.OrderHeader.ApplicationUserId = claims.Value;
            foreach (var item in vm.ListOfCart)
            {
                vm.OrderHeader.OrderTotal += item.Product.Price * item.Count;
            }
            await _unitOfWork.OrderHeader.AddAsync(vm.OrderHeader);
            await _unitOfWork.SaveAsync();
            foreach (var item in vm.ListOfCart)
            {
                OrderDetail orderDetail = new OrderDetail()
                {
                    ProductId = item.ProductId,
                    OrderHeaderId = vm.OrderHeader.Id,
                    Count = item.Count,
                    Price = item.Product.Price
                };
                await _unitOfWork.OrderDetail.AddAsync(orderDetail);
                await _unitOfWork.SaveAsync();
            }
            var domain = "https://Localhost:7191/";
            var options = new SessionCreateOptions
            {
                LineItems = new List<SessionLineItemOptions>(),
                Mode = "payment",
                SuccessUrl = domain + $"customer/cart/OrderSuccess?id={vm.OrderHeader.Id}",
                CancelUrl = domain + $"customer/cart/Index",
            };
            foreach (var item in vm.ListOfCart)
            {
                var lineItemsOptions = new SessionLineItemOptions
                {
                    PriceData = new SessionLineItemPriceDataOptions
                    {
                        UnitAmount = (long)(item.Product.Price * 100),
                        Currency = "USD",
                        ProductData = new SessionLineItemPriceDataProductDataOptions
                        {
                            Name = item.Product.Name,
                        },
                    },
                    Quantity = item.Count,
                };
                options.LineItems.Add(lineItemsOptions);
            }
            var service = new SessionService();
            Session session = service.Create(options);
            await _unitOfWork.OrderHeader.PaymentStatusAsync(vm.OrderHeader.Id, session.Id, session.PaymentIntentId);
            await _unitOfWork.SaveAsync();
            _unitOfWork.Cart.DeleteRange(vm.ListOfCart);
            await _unitOfWork.SaveAsync();
            Response.Headers.Add("Location", session.Url);
            return new StatusCodeResult(303);
        }

        [HttpGet]
        public async Task<IActionResult> OrderSuccess(int id)
        {
            var orderHeader = await _unitOfWork.OrderHeader.GetTAsync(x => x.Id == id);
            var service = new SessionService();
            Session session = service.Get(orderHeader.SessionId);
            if (session.PaymentStatus.ToLower() == "paid")
                await _unitOfWork.OrderHeader.UpdateStatusAsync(id, OrderStatus.StatusApproved, PaymentStatus.StatusApproved);
            List<Cart> cart = (await _unitOfWork.Cart.GetAllAsync(x => x.ApplicationUserId == orderHeader.ApplicationUserId)).ToList();
            _unitOfWork.Cart.DeleteRange(cart);
            await _unitOfWork.SaveAsync();
            return View(id);
        }

        [HttpGet]
        public async Task<IActionResult> Plus(int id)
        {
            var cart = await _unitOfWork.Cart.GetTAsync(x => x.Id == id);
            await _unitOfWork.Cart.IncrementCartItemAsync(cart, 1);
            await _unitOfWork.SaveAsync();
            return RedirectToAction(nameof(Index));
        }

        public async Task<ActionResult> Minus(int id)
        {
            var cart = _unitOfWork.Cart.GetTAsync(x => x.Id == id).Result;
            if (cart.Count <= 1)
            {
                _unitOfWork.Cart.Delete(cart);
                var count = (await _unitOfWork.Cart.GetAllAsync(x => x.ApplicationUserId == cart.ApplicationUserId))
                .ToList().Count-1;
                HttpContext.Session.SetInt32("SessionCart", count);
            }
            else
            {
                await _unitOfWork.Cart.DecrementCartItemAsync(cart, 1);
            }
            await _unitOfWork.SaveAsync();
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Delete(int id)
        {
            var cart = _unitOfWork.Cart.GetTAsync(x => x.Id == id).Result;
            _unitOfWork.Cart.Delete(cart);
            await _unitOfWork.SaveAsync();
            var count = (await _unitOfWork.Cart.GetAllAsync(x => x.ApplicationUserId == cart.ApplicationUserId))
                .ToList().Count;
            HttpContext.Session.SetInt32("SessionCart", count);
            return RedirectToAction(nameof(Index));
        }
    }
}
