using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ShoppingCart.DataAccess.Repositories;
using ShoppingCart.DataAccess.ViewModels;

namespace ShoppingCart.Web.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles ="Admin")]
    public class CategoryController : Controller
    {
        private IUnitOfWork _unitofWork;

        public CategoryController (IUnitOfWork unitofWork)
        {
            _unitofWork = unitofWork;
        }

        public async Task<IActionResult> Index()
        {
            var categoryVM = new CategoryVM();
            categoryVM.Categories = await _unitofWork.Category.GetAllAsync();
            return View(categoryVM);
        }

        [HttpGet]
        public async Task<IActionResult> CreateUpdate(int? id)
        {
            var vm = new CategoryVM();
            if (id == null || id==0)
                return View(vm);
            else
            {
                vm.Category = await _unitofWork.Category.GetTAsync(x => x.Id == id);
                if (vm.Category == null)
                    return NotFound();
                else
                    return View(vm);
            }
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateUpdate (CategoryVM vm)
        {
            if (ModelState.IsValid)
            {
                if (vm.Category.Id == 0)
                {
                    await _unitofWork.Category.AddAsync(vm.Category);
                    TempData["success"] = "Category Created Done";
                }
                else
                {
                    await _unitofWork.Category.UpdateAsync(vm.Category);
                    TempData["success"] = "Category Updated Done";
                }
                await _unitofWork.SaveAsync();
                return RedirectToAction("Index");
            }
            return RedirectToAction("Index");
        }

        [HttpGet]
        public async Task<IActionResult> Delete (int? id)
        {
            if (id == null || id == 0)
                return NotFound();
            var category = await _unitofWork.Category.GetTAsync(x => x.Id == id);
            if (category == null)
                return NotFound();
            return View(category);
        }

        [HttpPost,ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteData (int? id)
        {
            var category = await _unitofWork.Category.GetTAsync(x => x.Id == id);
            if (category == null)
                return NotFound();
            _unitofWork.Category.Delete(category);
            await _unitofWork.SaveAsync();
            TempData["success"] = "Category Deleted Done";
            return RedirectToAction("Index");
        }
    }
}
