using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using BestBooks.DataAccess.Repository.IRepository;
using BestBooks.Models.ViewModels;
using BestBooks.Utility;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;

namespace BestBooks.Areas.Customer.Controllers
{
    [Area("Customer")]
    public class CartController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IEmailSender _emailSender;
        private readonly UserManager<IdentityUser> _userManager;

        public ShoppingCartVM ShoppingCartVM { get; set; }

        public CartController(IUnitOfWork unitOfWork, IEmailSender emailSender, UserManager<IdentityUser> userManager)
        {
            _unitOfWork = unitOfWork;
            _emailSender = emailSender;
            _userManager = userManager;
        }

        public IActionResult Index()
        {
            // get currently logged in user
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);

            // init new shopping card viewmodel
            // creating a new order header and populating ListCart with the users shopping cart from the db
            ShoppingCartVM = new ShoppingCartVM()
            {
                OrderHeader = new Models.OrderHeader(),
                ListCart = _unitOfWork.ShoppingCart
                    .GetAll(u => u.ApplicationUserId == claim.Value, includeProperties: "Product")
            };

            // initial value of properties
            ShoppingCartVM.OrderHeader.OrderTotal = 0;
            ShoppingCartVM.OrderHeader.ApplicationUser = _unitOfWork.ApplicationUser
                .GetFirstOrDefault(u => u.Id == claim.Value, includeProperties: "Company");

            // iterate through all of the items inside the ListCart to calculate price
            foreach(var list in ShoppingCartVM.ListCart)
            {
                // get price of the product based on the quantity
                list.Price = SD.GetPriceBasedOnQuantity(list.Count, list.Product.Price, 
                                                        list.Product.Price50, list.Product.Price100);

                // set the order total to the recieved price and quantity
                ShoppingCartVM.OrderHeader.OrderTotal += (list.Price * list.Count);

                // convert description to Html
                list.Product.Description = SD.ConvertToRawHtml(list.Product.Description);

                // we only want to show 100 characters of the description
                if (list.Product.Description.Length > 100)
                {
                    list.Product.Description = list.Product.Description.Substring(0, 99) + "...";
                }
            }

            return View(ShoppingCartVM);
        }

        [HttpPost]
        [ActionName("Index")]
        public async Task<IActionResult> IndexPOST() 
        {
            // get currently logged in user
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);
            var user = _unitOfWork.ApplicationUser.GetFirstOrDefault(u => u.Id == claim.Value);

            if (user == null)
            {
                ModelState.AddModelError(string.Empty, "Verfication email is empty");
            }

            var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
            var callbackUrl = Url.Page(
                "/Account/ConfirmEmail",
                pageHandler: null,
                values: new { area = "Identity", userId = user.Id, code = code },
                protocol: Request.Scheme);

            await _emailSender.SendEmailAsync(user.Email, "Confirm your email",
                $"Please confirm your account by <a href='{HtmlEncoder.Default.Encode(callbackUrl)}'>clicking here</a>.");

            ModelState.AddModelError(string.Empty, "Verfication email sent. Please check your email.");

            return RedirectToAction("Index");
        }

        public IActionResult Plus(int cartId)
        {
            // get cart record
            var cart = _unitOfWork.ShoppingCart
                    .GetFirstOrDefault(c => c.Id == cartId, includeProperties: "Product");

            // increment quantity
            cart.Count += 1;
            // update pricing
            cart.Price = SD.GetPriceBasedOnQuantity(cart.Count, cart.Product.Price, 
                                                    cart.Product.Price50, cart.Product.Price100);

            _unitOfWork.Save();

            return RedirectToAction(nameof(Index));
        }

        public IActionResult Minus(int cartId)
        {
            // get shopping cart
            var cart = _unitOfWork.ShoppingCart
                    .GetFirstOrDefault(c => c.Id == cartId, includeProperties: "Product");

            // if cart quantity is 1, then we have to remove it from the shopping cart
            if (cart.Count == 1)
            {
                // get shopping cart length
                var cnt = _unitOfWork.ShoppingCart
                    .GetAll(u => u.ApplicationUserId == cart.ApplicationUserId)
                    .ToList().Count();

                // remove cart record from shopping cart
                _unitOfWork.ShoppingCart.Remove(cart);
                _unitOfWork.Save();

                // update session
                HttpContext.Session.SetInt32(SD.ssShopingCart, cnt - 1);
            }
            else
            {
                // decrement quantity
                cart.Count -= 1;
                // update pricing
                cart.Price = SD.GetPriceBasedOnQuantity(cart.Count, cart.Product.Price,
                                                        cart.Product.Price50, cart.Product.Price100);

                _unitOfWork.Save();
            }

            return RedirectToAction(nameof(Index));
        }

        public IActionResult Remove(int cartId)
        {
            // get shopping cart
            var cart = _unitOfWork.ShoppingCart
                    .GetFirstOrDefault(c => c.Id == cartId, includeProperties: "Product");

            // get shopping cart length
            var cnt = _unitOfWork.ShoppingCart
                .GetAll(u => u.ApplicationUserId == cart.ApplicationUserId)
                .ToList().Count();

            // remove cart record from shopping cart
            _unitOfWork.ShoppingCart.Remove(cart);
            _unitOfWork.Save();

            // update session
            HttpContext.Session.SetInt32(SD.ssShopingCart, cnt - 1);
            
            return RedirectToAction(nameof(Index));
        }
    }
}
