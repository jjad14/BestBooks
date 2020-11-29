using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BestBooks.DataAccess.Repository.IRepository;
using BestBooks.Models;
using BestBooks.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BestBooks.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = SD.Role_Admin + "," + SD.Role_Employee)]
    public class CompanyController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        public CompanyController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public IActionResult Index()
        {
            return View();
        }        
        
        public IActionResult Upsert(int? id)
        {

            Company company = new Company();

            // create
            if (id == null)
            {
                return View(company);
            }

            // edit
            company = _unitOfWork.Company.Get(id.GetValueOrDefault());
            if (company == null)
            {
                return NotFound();
            }

            return View(company);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Upsert(Company company)
        {
            // if model validation is valid
            if (ModelState.IsValid)
            {
                if (company.Id == 0)
                {
                    _unitOfWork.Company.Add(company);
                } else
                {
                    _unitOfWork.Company.Update(company);
                }
                    _unitOfWork.Save();
                return RedirectToAction(nameof(Index)); // or RedirectToAction("Index");
            }

            return View(company);

        }

        #region API Calls

        [HttpGet]
        public IActionResult GetAll() 
        {
            // retrieve all the companies
            var allObj = _unitOfWork.Company.GetAll();

            //  return in a json format
            return Json(new { data = allObj });
        }

        [HttpDelete]
        public IActionResult Delete(int id)
        {
            var objFromDb = _unitOfWork.Company.Get(id);

            if (objFromDb == null)
            {
                return Json(new { success=false, message="Error while deleting"});
            }

            _unitOfWork.Company.Remove(objFromDb);
            _unitOfWork.Save();

            return Json(new { success=true, message="Deleted Successfully"});
        }

        #endregion

    }
}
