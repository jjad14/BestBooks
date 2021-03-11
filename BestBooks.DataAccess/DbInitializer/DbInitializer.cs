using BestBooks.DataAccess.Data;
using BestBooks.Models;
using BestBooks.Utility;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BestBooks.DataAccess.DbInitializer
{
    public class DbInitializer : IDbInitializer
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ILogger<DbInitializer> _logger;

        private IConfiguration _configuration { get; }

        public DbInitializer(ApplicationDbContext db, 
                            UserManager<IdentityUser> userManager, 
                            RoleManager<IdentityRole> roleManager,
                            IConfiguration configuration,
                            ILogger<DbInitializer> logger)
        {
            _db = db;
            _userManager = userManager;
            _roleManager = roleManager;
            _configuration = configuration;
            _logger = logger;
        }

        public void InitalizeDb()
        {
            try
            {
                // If there are pending migrations, then we want to migrate them
                if (_db.Database.GetPendingMigrations().Count() > 0)
                {
                    // push all of the pending migrations to the database
                    _db.Database.Migrate();
                }

            }
            catch (Exception ex) 
            {
                _logger.LogError(ex.ToString());
            }

            // everytime app starts it will call the initalizer, which we do not want
            // so we check if role admin exists, if one does then so do the others
            if (_db.Roles.Any(r => r.Name == SD.Role_Admin)) 
            {
                // we return nothing, because we do not want to create the roles again
                return;
            }

            // Create roles
            // execute commands before proceeding further because it is an async method
            _roleManager.CreateAsync(new IdentityRole(SD.Role_Admin)).GetAwaiter().GetResult();
            _roleManager.CreateAsync(new IdentityRole(SD.Role_Employee)).GetAwaiter().GetResult();
            _roleManager.CreateAsync(new IdentityRole(SD.Role_User_Comp)).GetAwaiter().GetResult();
            _roleManager.CreateAsync(new IdentityRole(SD.Role_User_Indi)).GetAwaiter().GetResult();

            // create user
            _userManager.CreateAsync(new ApplicationUser
            {
                UserName = _configuration.GetSection("AdminUser")["Email"],
                Email = _configuration.GetSection("AdminUser")["Email"],
                EmailConfirmed = true,
                Name = "Administrator"
            }, _configuration.GetSection("AdminUser")["Password"]).GetAwaiter().GetResult();

            try
            {
                // get user
                ApplicationUser user = _db.ApplicationUsers.Where(u => u.Email == "admin@gmail.com").FirstOrDefault();

                // assign administrator roles to user
                _userManager.AddToRoleAsync(user, SD.Role_Admin).GetAwaiter().GetResult();
            }
            catch (Exception ex) 
            {
                Console.WriteLine(ex);
            }
        }
    }
}
