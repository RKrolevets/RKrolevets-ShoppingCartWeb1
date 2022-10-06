using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ShoppingCart.DataAccess.Repositories;
using ShoppingCart.DataAccess.ViewModels;
using ShoppingCart.Models;
using ShoppingCart.Utility;
using Stripe;
using Stripe.Checkout;
using System.Security.Claims;

namespace ShoppingCart.Web.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize]
    public class OrderController : Controller
    {
        private IUnitOfWork _unitOfWork;
        public OrderController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        #region APICALL
        public async Task<IActionResult> AllOrders(string status)
        {
            IEnumerable<OrderHeader> orderHeader;
            if (User.IsInRole("Admin") || User.IsInRole("Employee"))
                orderHeader = await _unitOfWork.OrderHeader.GetAllAsync(includeProperties: "ApplicationUser");
            else
            {
                var claimsIdentity = (ClaimsIdentity)User.Identity;
                var claims = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);
                orderHeader = await _unitOfWork.OrderHeader.GetAllAsync(x => x.ApplicationUserId == claims.Value);
            }
            switch (status)
            {
                case "pending":
                    orderHeader = orderHeader.Where(x => x.PaymentStatus == PaymentStatus.StatusPending);
                    break;
                case "approved":
                    orderHeader = orderHeader.Where(x => x.PaymentStatus == PaymentStatus.StatusApproved);
                    break;
                case "underprocess":
                    orderHeader = orderHeader.Where(x => x.OrderStatus == OrderStatus.StatusInProcess);
                    break;
                case "shipped":
                    orderHeader = orderHeader.Where(x => x.OrderStatus == OrderStatus.StatusShipped);
                    break;
                default:
                    break;
            }
            return Json(new { data = orderHeader });
        }
        #endregion

        public IActionResult Index()
        {
            return View();
        }

        public async Task<IActionResult> OrderDetails(int id)
        {
            OrderVM orderVM = new OrderVM()
            {
                OrderHeader = await _unitOfWork.OrderHeader.GetTAsync(x => x.Id == id, includeProperties: "ApplicationUser"),
                OrderDetails = await _unitOfWork.OrderDetail.GetAllAsync(x => x.OrderHeaderId == id, includeProperties: "Product")
            };
            return View(orderVM);
        }

        [Authorize(Roles = WebSiteRole.Role_Admin + "," + WebSiteRole.Role_Employee)]
        [HttpPost]
        public async Task<IActionResult> OrderDetails(OrderVM vm)
        {
            var orderHeader = await _unitOfWork.OrderHeader.GetTAsync(x => x.Id == vm.OrderHeader.Id);
            orderHeader.Name = vm.OrderHeader.Name;
            orderHeader.Phone = vm.OrderHeader.Phone;
            orderHeader.Address = vm.OrderHeader.Address;
            orderHeader.City = vm.OrderHeader.City;
            orderHeader.State = vm.OrderHeader.State;
            orderHeader.PostalCode = vm.OrderHeader.PostalCode;
            if (vm.OrderHeader.Carrier != null)
                orderHeader.Carrier = vm.OrderHeader.Carrier;
            if (vm.OrderHeader.TrackingNumber != null)
                orderHeader.TrackingNumber = vm.OrderHeader.TrackingNumber;
            _unitOfWork.OrderHeader.Update(orderHeader);
            await _unitOfWork.SaveAsync();
            TempData["success"] = "Info Updated";
            return RedirectToAction("OrderDetails", "Order", new { id = vm.OrderHeader.Id });
        }

        [Authorize(Roles = WebSiteRole.Role_Admin + "," + WebSiteRole.Role_Employee)]
        public async Task<IActionResult> InProcess(OrderVM vm)
        {
            await _unitOfWork.OrderHeader.UpdateStatusAsync(vm.OrderHeader.Id, OrderStatus.StatusInProcess);
            await _unitOfWork.SaveAsync();
            TempData["success"] = "Order status updated to Inprocess";
            return RedirectToAction("OrderDetails", "Order", new { id = vm.OrderHeader.Id });
        }

        [Authorize(Roles = WebSiteRole.Role_Admin + "," + WebSiteRole.Role_Employee)]
        public async Task<IActionResult> Shipped(OrderVM vm)
        {
            var orderHeader = await _unitOfWork.OrderHeader.GetTAsync(x => x.Id == vm.OrderHeader.Id);
            orderHeader.Carrier = vm.OrderHeader.Carrier;
            orderHeader.TrackingNumber = vm.OrderHeader.TrackingNumber;
            orderHeader.OrderStatus = OrderStatus.StatusShipped;
            orderHeader.DateOfShipping = DateTime.Now;

            _unitOfWork.OrderHeader.Update(orderHeader);
            await _unitOfWork.SaveAsync();
            TempData["success"] = "Order status updated to Shipped";
            return RedirectToAction("OrderDetails", "Order", new { id = vm.OrderHeader.Id });
        }

        [Authorize(Roles = WebSiteRole.Role_Admin + "," + WebSiteRole.Role_Employee)]
        public async Task<IActionResult> CancelOrder(OrderVM vm)
        {
            var orderHeader = await _unitOfWork.OrderHeader.GetTAsync(x => x.Id == vm.OrderHeader.Id);
            if (orderHeader.PaymentStatus == PaymentStatus.StatusApproved)
            {
                var refundOptions = new RefundCreateOptions()
                {
                    Reason = RefundReasons.RequestedByCustomer,
                    PaymentIntent = orderHeader.PaymentIntentId
                };
                var service = new RefundService();
                Refund refund = service.Create(refundOptions);
                await _unitOfWork.OrderHeader.UpdateStatusAsync(vm.OrderHeader.Id, OrderStatus.StatusCancelled);
            }
            else
            {
                await _unitOfWork.OrderHeader.UpdateStatusAsync(vm.OrderHeader.Id, OrderStatus.StatusCancelled);
            }
            await _unitOfWork.SaveAsync();
            TempData["success"] = "Order cancelled";
            return RedirectToAction("OrderDetails", "Order", new { id = vm.OrderHeader.Id });
        }

        public async Task<IActionResult> PayNow(OrderVM vm)
        {
            var orderheader = await _unitOfWork.OrderHeader.GetTAsync(x => x.Id == vm.OrderHeader.Id, 
                includeProperties:"ApplicationUser");
            var orderDetail = await _unitOfWork.OrderDetail.GetAllAsync(x => x.OrderHeaderId == vm.OrderHeader.Id,
                includeProperties:"Product");
            var domain = "https://Localhost:7191/";
            var options = new SessionCreateOptions
            {
                LineItems = new List<SessionLineItemOptions>(),
                Mode = "payment",
                SuccessUrl = domain + $"customer/cart/OrderSuccess?id={vm.OrderHeader.Id}",
                CancelUrl = domain + $"customer/cart/Index",
            };
            foreach (var item in orderDetail)
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
            Response.Headers.Add("Location", session.Url);
            return new StatusCodeResult(303);
        }
    }
}
