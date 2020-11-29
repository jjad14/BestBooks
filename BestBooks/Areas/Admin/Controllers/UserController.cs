using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BestBooks.DataAccess.Data;
using BestBooks.DataAccess.Repository.IRepository;
using BestBooks.Models;
using BestBooks.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BestBooks.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = SD.Role_Admin + "," + SD.Role_Employee)]
    public class UserController : Controller
    {

        private readonly ApplicationDbContext _db;
        public UserController(ApplicationDbContext db)
        {
            _db = db;
        }

        public IActionResult Index()
        {
            return View();
        }        
       

        #region API Calls

        [HttpGet]
        public IActionResult GetAll() 
        {
            // retrieve all the users
            var userList = _db.ApplicationUsers.Include(u => u.Company).ToList();
            // mapping of user list with that role
            var userRole = _db.UserRoles.ToList();
            // retrieve all the roles
            var roles = _db.Roles.ToList();

            foreach(var user in userList)
            {
                // get role userid that matches the user's id
                var roleId = userRole.FirstOrDefault(u => u.UserId == user.Id).RoleId;
                // set the users role, to the role name that is found
                user.Role = roles.FirstOrDefault(u => u.Id == roleId).Name;

                // if user is not affiliated with a compnay, set default Name
                if (user.Company == null)
                {
                    user.Company = new Company()
                    {
                        Name = ""
                    };
                }
            }

            //  return in a json format
            return Json(new { data = userList });
        }

        [HttpPost]
        public IActionResult LockUnlock([FromBody] string id) 
        {
            // get user
            var objFromDb = _db.ApplicationUsers.FirstOrDefault(u => u.Id == id);

            // user not found
            if (objFromDb == null)
            {
                return Json(new { success = false, message = "Error while Locking/Unlocking" });
            }

            if (objFromDb.LockoutEnd != null && objFromDb.LockoutEnd > DateTime.Now)
            {
                // user is currently locked - unlock user
                objFromDb.LockoutEnd = DateTime.Now;
            } 
            else
            {
                // user is not locked - lock for 30 days
                objFromDb.LockoutEnd = DateTime.Now.AddDays(30);
            }

            _db.SaveChanges();

            return Json(new { success = true, message = "Operation Successful" });
        }

        #endregion

    }
}
