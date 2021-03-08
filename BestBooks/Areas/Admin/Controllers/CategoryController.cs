using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BestBooks.DataAccess.Repository.IRepository;
using BestBooks.Models;
using BestBooks.Models.ViewModels;
using BestBooks.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BestBooks.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = SD.Role_Admin)]
    public class CategoryController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        public CategoryController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<IActionResult> Index(int productPage = 1)
        {
            CategoryVM categoryVM = new CategoryVM()
            {
                Categories = await _unitOfWork.Category.GetAllAsync()
            };

            var count = categoryVM.Categories.Count();

            categoryVM.Categories = categoryVM.Categories.OrderBy(p => p.Name)
                .Skip((productPage - 1) * 2)
                .Take(2)
                .ToList();

            categoryVM.PagingInfo = new PagingInfo
            {
                CurrentPage = productPage,
                ItemsPerPage = 2,
                TotalItem = count,
                urlParam = "/Admin/Category/Index?productPage=:"
            };

            return View(categoryVM);
        }        
        
        public async Task<IActionResult> Upsert(int? id)
        {

            Category category = new Category();

            // create
            if (id == null)
            {
                return View(category);
            }

            // edit
            category = await _unitOfWork.Category.GetAsync(id.GetValueOrDefault());
            if (category == null)
            {
                return NotFound();
            }

            return View(category);
        }

        // MVCs AntiForgery support writes a unique value to an Http only cookie and then the same value is written to the form 
        // when the page is submitted an error is raised If the cookie value does not match with the form value 
        // it is important to note that this feature prevents cross site request forgery.
        // Which is a form from another site that posts to your site in an attempt to submit hidden content using an authenticated users credential.
        // But with Asp.Net core in form, anti forgery token is already included.

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Upsert(Category category)
        {
            // if model validation is valid
            if (ModelState.IsValid)
            {
                if (category.Id == 0)
                {
                    await _unitOfWork.Category.AddAsync(category);
                } else
                {
                    _unitOfWork.Category.Update(category);
                }
                _unitOfWork.Save();

                return RedirectToAction(nameof(Index)); // or RedirectToAction("Index");
            }

            return View(category);

        }

        #region API Calls

        // IActionResult defines a contract that represents the result of an action method.
        // The IActionResult return type is appropriate when multiple ActionResult return types are possible in an action.
        // The ActionResult type represents various HTTP status codes 
        // any non abstract class deriving from ActionResult qualifies as a valid return type

        // IActionResult is an interface we can create custom responses as a result when you use action result
        // you can only return the predefined ones for returning a view or a resource.

        // ActionResult is an implementation of the interface
        // ActionResult is an abstract class and ActionResults like view results partial view results, json results derive from the action result.

        [HttpGet]
        public async Task<IActionResult> GetAll() 
        {
            // retrieve all the categories
            var allObj = await _unitOfWork.Category.GetAllAsync();

            //  return in a json format
            return Json(new { data = allObj });
        }

        [HttpDelete]
        public async Task<IActionResult> Delete(int id)
        {
            var objFromDb = await _unitOfWork.Category.GetAsync(id);

            if (objFromDb == null)
            {
                // tempdata will hold a value for just one request
                TempData["Error"] = "Error Deleting the Category";
                return Json(new { success=false, message= "Error Deleting the Category" });
            }

            await _unitOfWork.Category.RemoveAsync(objFromDb);
            _unitOfWork.Save();

            TempData["Success"] = "Category Successfully Deleted";
            return Json(new { success=true, message= "Category Successfully Deleted" });
        }

        #endregion

    }
}
