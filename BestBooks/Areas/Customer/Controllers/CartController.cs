using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using BestBooks.DataAccess.Repository.IRepository;
using BestBooks.Models;
using BestBooks.Models.ViewModels;
using BestBooks.Utility;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;
using Stripe;
using Twilio;
using Twilio.Rest.Api.V2010.Account;

namespace BestBooks.Areas.Customer.Controllers
{
    [Area("Customer")]
    public class CartController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IEmailSender _emailSender;
        private readonly UserManager<IdentityUser> _userManager;
        private TwilioSettings _twilioOptions { get; set;  }

        [BindProperty]
        public ShoppingCartVM ShoppingCartVM { get; set; }

        public CartController(IUnitOfWork unitOfWork, 
                            IEmailSender emailSender, 
                            UserManager<IdentityUser> userManager,
                            IOptions<TwilioSettings> twilioOptions)
        {
            _unitOfWork = unitOfWork;
            _emailSender = emailSender;
            _userManager = userManager;
            _twilioOptions = twilioOptions.Value;
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
                HttpContext.Session.SetInt32(SD.ssShoppingCart, cnt - 1);
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
            HttpContext.Session.SetInt32(SD.ssShoppingCart, cnt - 1);
            
            return RedirectToAction(nameof(Index));
        }

        public IActionResult Summary() 
        {
            // get currently logged in user
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);

            // populate the cart from the database
            ShoppingCartVM = new ShoppingCartVM()
            {
                OrderHeader = new Models.OrderHeader(),
                ListCart = _unitOfWork.ShoppingCart
                    .GetAll(c => c.ApplicationUserId == claim.Value, includeProperties: "Product")
            };

            // populate the application user inside order header
            ShoppingCartVM.OrderHeader.ApplicationUser = _unitOfWork.ApplicationUser
                .GetFirstOrDefault(c => c.Id == claim.Value, includeProperties: "Company");

            // iterate through all of the items inside the ListCart to calculate price
            foreach (var list in ShoppingCartVM.ListCart)
            {
                // get price of the product based on the quantity
                list.Price = SD.GetPriceBasedOnQuantity(list.Count, list.Product.Price,
                                                        list.Product.Price50, list.Product.Price100);

                // set the order total to the recieved price and quantity
                ShoppingCartVM.OrderHeader.OrderTotal += (list.Price * list.Count);
            }

            // populate the order header property from the application user.
            ShoppingCartVM.OrderHeader.Name = ShoppingCartVM.OrderHeader.ApplicationUser.Name;
            ShoppingCartVM.OrderHeader.PhoneNumber = ShoppingCartVM.OrderHeader.ApplicationUser.PhoneNumber;
            ShoppingCartVM.OrderHeader.StreetAddress = ShoppingCartVM.OrderHeader.ApplicationUser.StreetAddress;
            ShoppingCartVM.OrderHeader.City = ShoppingCartVM.OrderHeader.ApplicationUser.City;
            ShoppingCartVM.OrderHeader.Province = ShoppingCartVM.OrderHeader.ApplicationUser.Province;
            ShoppingCartVM.OrderHeader.PostalCode = ShoppingCartVM.OrderHeader.ApplicationUser.PostalCode;

            return View(ShoppingCartVM);
        }

        [HttpPost]
        [ActionName("Summary")]
        [ValidateAntiForgeryToken]
        public IActionResult SummaryPost(string stripeToken)
        {
            // get currently logged in user
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);

            // populate User with claim
            ShoppingCartVM.OrderHeader.ApplicationUser = _unitOfWork.ApplicationUser
                .GetFirstOrDefault(c => c.Id == claim.Value, includeProperties: "Company");

            // populate cart using user id
            ShoppingCartVM.ListCart = _unitOfWork.ShoppingCart
                .GetAll(c => c.ApplicationUserId == claim.Value, includeProperties: "Product");

            // set OrderHeader properties
            ShoppingCartVM.OrderHeader.PaymentStatus = SD.PaymentStatusPending;
            ShoppingCartVM.OrderHeader.OrderStatus = SD.StatusPending;
            ShoppingCartVM.OrderHeader.ApplicationUserId = claim.Value;
            ShoppingCartVM.OrderHeader.OrderDate = DateTime.Now;

            // add Order header to db
            _unitOfWork.OrderHeader.Add(ShoppingCartVM.OrderHeader);

            _unitOfWork.Save();

            // add items to db via OrderDetails
            foreach(var item in ShoppingCartVM.ListCart)
            {
                // get price of item
                item.Price = SD.GetPriceBasedOnQuantity(item.Count, item.Product.Price, item.Product.Price50, item.Product.Price100);

                OrderDetails orderDetails = new OrderDetails()
                {
                    ProductId = item.ProductId,
                    OrderId = ShoppingCartVM.OrderHeader.Id,
                    Price = item.Price,
                    Count = item.Count
                };

                // update the order total in order header
                ShoppingCartVM.OrderHeader.OrderTotal += orderDetails.Count * orderDetails.Price;

                // add Order details to db
                _unitOfWork.OrderDetails.Add(orderDetails);
            }

            // Remove all of the items from shopping cart
            _unitOfWork.ShoppingCart.RemoveRange(ShoppingCartVM.ListCart);
            _unitOfWork.Save();

            // update the session
            HttpContext.Session.SetInt32(SD.ssShoppingCart, 0);

            // token received from stripe
            if(stripeToken == null)
            {
                // authorized user who is placing an order and they can make the payment later on.
                // order will be created for delayed payment for authorized company
                ShoppingCartVM.OrderHeader.PaymentDueDate = DateTime.Now.AddDays(30);
                ShoppingCartVM.OrderHeader.PaymentStatus = SD.PaymentStatusDelayedPayment;
                ShoppingCartVM.OrderHeader.OrderStatus = SD.StatusApproved;
            }
            else
            {
                // process the payment
                var options = new ChargeCreateOptions
                {
                    Amount = Convert.ToInt32(ShoppingCartVM.OrderHeader.OrderTotal * 100),
                    Currency = "cad",
                    Description = "Order Id: " + ShoppingCartVM.OrderHeader.Id,
                    Source = stripeToken
                };

                var service = new ChargeService();
                // This will make the call to create the transaction on the credit card
                Charge charge = service.Create(options);

                // BalanceTransactionId is returned once a transaction is made.
                if (charge.BalanceTransactionId == null)
                {
                    ShoppingCartVM.OrderHeader.PaymentStatus = SD.PaymentStatusRejected;
                }
                else
                {
                    ShoppingCartVM.OrderHeader.TransactionId = charge.BalanceTransactionId;
                }

                // status of the charge
                if(charge.Status.ToLower() =="succeded")
                {
                    ShoppingCartVM.OrderHeader.PaymentStatus = SD.PaymentStatusApproved;
                    ShoppingCartVM.OrderHeader.OrderStatus = SD.StatusApproved;
                    ShoppingCartVM.OrderHeader.PaymentDate = DateTime.Now;
                }
            }

            _unitOfWork.Save();

            // redirect to OrderConfirmation view, passing along OrderHeader Id
            return RedirectToAction("OrderConfirmation", "Cart", new { id = ShoppingCartVM.OrderHeader.Id });
        }

        public IActionResult OrderConfirmation(int id)
        {
            OrderHeader orderHeader = _unitOfWork.OrderHeader.GetFirstOrDefault(u => u.Id == id);

            TwilioClient.Init(_twilioOptions.AccountSid, _twilioOptions.AuthToken);

            try
            {
                var message = MessageResource.Create(
                        body: "Order Placed on Best Books. Your Order ID: " + id,
                        from: new Twilio.Types.PhoneNumber(_twilioOptions.PhoneNumber),
                        to: new Twilio.Types.PhoneNumber(orderHeader.PhoneNumber)
                        );
            }
            catch(Exception ex)
            {
                
            }


            return View(id);
        }

    }
}
