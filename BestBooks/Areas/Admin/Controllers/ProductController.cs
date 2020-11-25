using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using BestBooks.DataAccess.Repository.IRepository;
using BestBooks.Models;
using BestBooks.Models.ViewModels;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace BestBooks.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class ProductController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IWebHostEnvironment _host;
        public ProductController(IUnitOfWork unitOfWork, IWebHostEnvironment host)
        {
            _unitOfWork = unitOfWork;
            _host = host;
        }

        public IActionResult Index()
        {
            return View();
        }        
        
        public IActionResult Upsert(int? id)
        {

            // Product View Model
            ProductVM productVM = new ProductVM()
            {
                Product = new Product(),
                CategoryList = _unitOfWork.Category.GetAll().Select(c => new SelectListItem {
                    Text = c.Name,
                    Value = c.Id.ToString()
                }),
                CoverTypeList = _unitOfWork.CoverType.GetAll().Select(c => new SelectListItem
                {
                    Text = c.Name,
                    Value = c.Id.ToString()
                })
            };

            // create
            if (id == null)
            {
                return View(productVM);
            }

            // edit
            productVM.Product = _unitOfWork.Product.Get(id.GetValueOrDefault());
            if (productVM.Product == null)
            {
                return NotFound();
            }

            return View(productVM);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Upsert(ProductVM productVM)
        {
            // if model validation is valid
            if (ModelState.IsValid)
            {
                // get the wwwroot path
                string webRootPath = _host.WebRootPath;
                // retrieve all the files that were uploaded
                var files = HttpContext.Request.Form.Files;

                // This means the file was uploaded
                if (files.Count > 0)
                {
                    // all the file names for their images will have a new Guid
                    string fileName = Guid.NewGuid().ToString();
                    //  navigate to the path of images and products
                    // We combine the web root path with our folder
                    var uploads = Path.Combine(webRootPath, @"images\products");
                    // Get the file extension so that it will be the same
                    var extension = Path.GetExtension(files[0].FileName);

                    //  check if product image URL is not null.
                    if (productVM.Product.ImageUrl != null)
                    {
                        // we are in edit mode, so we need to remove old image
                        var imagePath = Path.Combine(webRootPath, productVM.Product.ImageUrl.TrimStart('\\'));

                        // if image path exists then we have to remove that.
                        if (System.IO.File.Exists(imagePath))
                        {
                            System.IO.File.Delete(imagePath);
                        }
                    }

                    // After the file is deleted we will have to upload the new file
                    using (var filesStreams = new FileStream(Path.Combine(uploads, fileName=extension), FileMode.Create))
                    {
                        files[0].CopyTo(filesStreams);
                    }
                    // update the product view model
                    productVM.Product.ImageUrl = @"\images\products\" + fileName + extension;
                }
                else
                {
                    // update when the image is not changed
                    if (productVM.Product.Id != 0)
                    {
                        Product objFromDb = _unitOfWork.Product.Get(productVM.Product.Id);
                        productVM.Product.ImageUrl = objFromDb.ImageUrl;
                    }
                }


                if (productVM.Product.Id == 0)
                {
                    _unitOfWork.Product.Add(productVM.Product);
                } else
                {
                    _unitOfWork.Product.Update(productVM.Product);
                }
                
                _unitOfWork.Save();

                return RedirectToAction(nameof(Index)); // or RedirectToAction("Index");
            }
            else
            {
                productVM.CategoryList = _unitOfWork.Category.GetAll().Select(c => new SelectListItem
                {
                    Text = c.Name,
                    Value = c.Id.ToString()
                });
                productVM.CoverTypeList = _unitOfWork.CoverType.GetAll().Select(c => new SelectListItem
                {
                    Text = c.Name,
                    Value = c.Id.ToString()
                });

                if (productVM.Product.Id != 0)
                {
                    productVM.Product = _unitOfWork.Product.Get(productVM.Product.Id);
                }
            }

            return View(productVM);
        }

        #region API Calls

        [HttpGet]
        public IActionResult GetAll() 
        {
            // retrieve all the categories
            var allObj = _unitOfWork.Product.GetAll(includeProperties:"Category,CoverType");

            //  return in a json format
            return Json(new { data = allObj });
        }

        [HttpDelete]
        public IActionResult Delete(int id)
        {
            var objFromDb = _unitOfWork.Product.Get(id);

            if (objFromDb == null)
            {
                return Json(new { success=false, message="Error while deleting"});
            }
            // get the wwwroot path
            string webRootPath = _host.WebRootPath;

            // we are in edit mode, so we need to remove old image
            var imagePath = Path.Combine(webRootPath, objFromDb.ImageUrl.TrimStart('\\'));

            // if image path exists then we have to remove that.
            if (System.IO.File.Exists(imagePath))
            {
                System.IO.File.Delete(imagePath);
            }

            _unitOfWork.Product.Remove(objFromDb);
            _unitOfWork.Save();

            return Json(new { success=true, message="Deleted Successfully"});
        }

        #endregion

    }
}
