using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using ShoppingCart.DataAccess.Repositories;
using ShoppingCart.DataAccess.ViewModels;
using ShoppingCart.Models;

namespace ShoppingCart.Web.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class ProductController : Controller
    {
        private IUnitOfWork _unitOfWork;
        IWebHostEnvironment _hostingEnvironment;

        public ProductController(IUnitOfWork unitOfWork, IWebHostEnvironment webHostEnvironment)
        {
            _unitOfWork = unitOfWork;
            _hostingEnvironment = webHostEnvironment;
        }

        #region APICALL
        public async Task<IActionResult> AllProducts()
        {
            var products = await _unitOfWork.Product.GetAllAsync(includeProperties: "Category");
            return Json(new { data = products });
        }
        #endregion

        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> CreateUpdate(int? id)
        {
            var a = _unitOfWork.Category.GetAllAsync();
            ProductVM vm = new ()
            {
                Product = new(),
                Categories = _unitOfWork.Category.GetAllAsync().Result.Select(x =>
                new SelectListItem()
                {
                    Text = x.Name,
                    Value = x.Id.ToString()
                })
            };
            if (id == null || id == 0)
                return View(vm);
            else
            {
                vm.Product = await _unitOfWork.Product.GetTAsync(x => x.Id == id);
                if (vm.Product == null)
                    return NotFound();
                else
                    return View(vm);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateUpdate(ProductVM vm, IFormFile? file)
        {
            if(ModelState.IsValid)
            {
                string fileName = string.Empty;  
                if (file != null)
                {
                    string uploadDir = Path.Combine(_hostingEnvironment.WebRootPath, "ProductImage");
                    fileName = Guid.NewGuid().ToString() + "-" + file.FileName;
                    string filePath = Path.Combine(uploadDir, fileName);
                    if (vm.Product.ImageUrl != null)
                    {
                        var oldImagePath = Path.Combine(_hostingEnvironment.WebRootPath, 
                            vm.Product.ImageUrl.TrimStart('\\'));
                        if (System.IO.File.Exists(oldImagePath))
                            System.IO.File.Delete(oldImagePath);
                    }
                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        file.CopyTo(fileStream);
                    }
                    vm.Product.ImageUrl = @"\ProductImage\" + fileName;
                }
                if (vm.Product.Id == 0)
                {
                    await _unitOfWork.Product.AddAsync(vm.Product);
                    TempData["success"] = "Product Create Done";
                }
                else
                {
                    await _unitOfWork.Product.UpdateAsync(vm.Product);
                    TempData["success"] = "Product Create Done";
                }
                await _unitOfWork.SaveAsync();
                return RedirectToAction("Index");
            }
            return RedirectToAction("Index");
        }

        #region DeleteAPICALL
        [HttpDelete]
        public async Task<IActionResult> Delete (int? id)
        {
            var product = await _unitOfWork.Product.GetTAsync(x => x.Id == id);
            if (product == null)
                return Json(new { success = false, message = "Error in Fetching Data" });
            else
            {
                var oldImagePath = Path.Combine(_hostingEnvironment.WebRootPath, product.ImageUrl.TrimStart('\\'));
                if (System.IO.File.Exists(oldImagePath))
                    System.IO.File.Delete(oldImagePath);
                _unitOfWork.Product.Delete(product);
                await _unitOfWork.SaveAsync();
                return Json(new { success = true, message = "Product Deleted" });
            }
        }
        #endregion
    }
}
