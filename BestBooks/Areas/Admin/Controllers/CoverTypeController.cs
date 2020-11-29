using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BestBooks.DataAccess.Repository.IRepository;
using BestBooks.Models;
using BestBooks.Utility;
using Dapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BestBooks.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = SD.Role_Admin)]
    public class CoverTypeController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        public CoverTypeController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Upsert(int? id)
        {

            CoverType coverType = new CoverType();

            // create
            if (id == null)
            {
                return View(coverType);
            }

            // stored procedure
            // var parameter = new DynamicParameters();
            // parameter.Add("@Id", id);
            // coverType = _unitOfWork.SP_Call.OneRecord<CoverType>(SD.Proc_CoverType_Get, parameter);

            // edit
            coverType = _unitOfWork.CoverType.Get(id.GetValueOrDefault());
            if (coverType == null)
            {
                return NotFound();
            }

            return View(coverType);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Upsert(CoverType coverType)
        {
            // if model validation is valid
            if (ModelState.IsValid)
            {
                // stored procedure
                // var parameter = new DynamicParameters();
                // parameter.Add("@Name", coverType.Name);

                if (coverType.Id == 0)
                {
                    // _unitOfWork.SP_Call.Execute(SD.Proc_CoverType_Create, parameter);
                    _unitOfWork.CoverType.Add(coverType);
                }
                else
                {
                    // parameter.Add("@Id", coverType.Id);
                    // _unitOfWork.SP_Call.Execute(SD.Proc_CoverType_Update, parameter);
                    _unitOfWork.CoverType.Update(coverType);
                }
                _unitOfWork.Save();
                return RedirectToAction(nameof(Index)); // or RedirectToAction("Index");
            }

            return View(coverType);

        }

        #region API Calls

        [HttpGet]
        public IActionResult GetAll()
        {
            // retrieve all the categories
            var allObj = _unitOfWork.CoverType.GetAll();

            // stored procedure
            // var allObj2 = _unitOfWork.SP_Call.List<CoverType>(SD.Proc_CoverType_GetAll, null);

            //  return in a json format
            return Json(new { data = allObj });
        }

        [HttpDelete]
        public IActionResult Delete(int id)
        {
            // stored procedure
            // var parameter = new DynamicParameters();
            // parameter.Add("@Id", id);
            // var objFromDb2 = _unitOfWork.SP_Call.OneRecord<CoverType>(SD.Proc_CoverType_Get, parameter);

            var objFromDb = _unitOfWork.CoverType.Get(id);

            if (objFromDb == null)
            {
                return Json(new { success = false, message = "Error while deleting" });
            }

            // _unitOfWork.SP_Call.Execute(SD.Proc_CoverType_Delete, parameter);

            _unitOfWork.CoverType.Remove(objFromDb);
            _unitOfWork.Save();

            return Json(new { success = true, message = "Deleted Successfully" });
        }

        #endregion

    }
}
