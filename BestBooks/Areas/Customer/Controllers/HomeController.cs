using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using BestBooks.Models.ViewModels;
using BestBooks.DataAccess.Repository.IRepository;
using BestBooks.Models;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using BestBooks.Utility;
using Microsoft.AspNetCore.Http;

namespace BestBooks.Areas.Customer.Controllers
{
    [Area("Customer")]
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IUnitOfWork _unitOfWork;

        public HomeController(ILogger<HomeController> logger, IUnitOfWork unitOfWork)
        {
            _logger = logger;
            _unitOfWork = unitOfWork;
        }

        public IActionResult Index()
        {
            // check if user is logged in, only then we retrieve the shopping cart from the database.

            IEnumerable<Product> productList = _unitOfWork.Product.GetAll(includeProperties: "Category,CoverType");

            // get the id of the currently logged in user
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);

            // will be null if user is not logged in
            if (claim != null)
            {
                var count = _unitOfWork.ShoppingCart
                    .GetAll(c => c.ApplicationUserId == claim.Value)
                    .ToList()
                    .Count();

                // set the session
                HttpContext.Session.SetInt32(SD.ssShoppingCart, count);
            }

            return View(productList);
        }

        public IActionResult Details(int id)
        {
            var productFromDb = _unitOfWork.Product.GetFirstOrDefault(u => u.Id == id, includeProperties:"Category,CoverType");

            ShoppingCart cart = new ShoppingCart()
            {
                Product = productFromDb,
                ProductId = productFromDb.Id
            };
            
            return View(cart);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public IActionResult Details(ShoppingCart CartObj)
        {
            CartObj.Id = 0;

            if (ModelState.IsValid)
            {
                // add product to cart

                // get the id of the currently logged in user
                var claimsIdentity = (ClaimsIdentity)User.Identity;
                var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);
                CartObj.ApplicationUserId = claim.Value;

                // retrieve shopping cart from db based on the user id and product id
                ShoppingCart cartFromDb = _unitOfWork.ShoppingCart
                    .GetFirstOrDefault(
                    u => u.ApplicationUserId == CartObj.ApplicationUserId &&
                    u.ProductId == CartObj.ProductId,
                    includeProperties: "Product");

                // No record exists in the database for that product for this user. So we have to add one
                if (cartFromDb == null)
                {
                    _unitOfWork.ShoppingCart.Add(CartObj);
                }
                else
                {
                    // a shopping cart exists for this user and product, update quantity
                    cartFromDb.Count += CartObj.Count;
                    // asp.net core and ef core automatically tracks that this is a shopping cart 
                    // that you just retrieved from the database
                    // any changes will result in an automatic update so the update call is not necessary
                    // just make sure to save
                    _unitOfWork.ShoppingCart.Update(cartFromDb);
                }
                    _unitOfWork.Save();

                var count = _unitOfWork.ShoppingCart
                    .GetAll(c => c.ApplicationUserId == CartObj.ApplicationUserId)
                    .ToList()
                    .Count();

                // add to session
                // HttpContext.Session.SetObject(SD.ssShopingCart, CartObj);

                // alternative
                HttpContext.Session.SetInt32(SD.ssShoppingCart, count);

                // get session obj
                // var obj = HttpContext.Session.GetObject<ShoppingCart>(SD.ssShopingCart);

                return RedirectToAction(nameof(Index));
            } 
            else
            {
                var productFromDb = _unitOfWork.Product
                    .GetFirstOrDefault(u => u.Id == CartObj.ProductId, includeProperties: "Category,CoverType");

                ShoppingCart cart = new ShoppingCart()
                {
                    Product = productFromDb,
                    ProductId = productFromDb.Id
                };

                return View(cart);
            }
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
