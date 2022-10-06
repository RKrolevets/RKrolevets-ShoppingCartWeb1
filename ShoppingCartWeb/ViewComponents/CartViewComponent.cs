using Microsoft.AspNetCore.Mvc;
using ShoppingCart.DataAccess.Repositories;
using System.Security.Claims;

namespace ShoppingCart.Web.ViewComponents
{
    public class CartViewComponent : ViewComponent    
    {
        private readonly IUnitOfWork _unitOfWork;

        public CartViewComponent (IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var claims = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);
            if (claims != null)
            {
                if (HttpContext.Session.GetInt32("SessionCart") != null)
                    return View(HttpContext.Session.GetInt32("SessionCart"));
                else
                {
                    HttpContext.Session.SetInt32("SessionCart", (await _unitOfWork
                        .Cart.GetAllAsync(x => x.ApplicationUserId == claims.Value)).ToList().Count);
                    return View(HttpContext.Session.GetInt32("SessionCart"));
                }
            }
            else
            {
                HttpContext.Session.Clear();
                return View(0);
            }
        }
    }
}
