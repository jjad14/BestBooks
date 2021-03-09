using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using BestBooks.DataAccess.Repository.IRepository;
using BestBooks.Models;
using BestBooks.Models.ViewModels;
using BestBooks.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Stripe;

namespace BestBooks.Areas.Admin.Controllers
{
    [Authorize]
    [Area("Admin")]
    public class OrderController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;

        [BindProperty]
        public OrderDetailsVM OrderVm { get; set; }

        public OrderController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Details(int id)
        {
            // populate viewmodel
            OrderVm = new OrderDetailsVM()
            {
                OrderHeader = _unitOfWork.OrderHeader
                    .GetFirstOrDefault(u => u.Id == id, includeProperties: "ApplicationUser"),
                OrderDetails = _unitOfWork.OrderDetails
                    .GetAll(o => o.OrderId == id, includeProperties: "Product")
            };

            return View(OrderVm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [ActionName("Details")]
        public IActionResult Details(string stripeToken)
        {
            OrderHeader orderHeader = _unitOfWork.OrderHeader
                .GetFirstOrDefault(u => u.Id == OrderVm.OrderHeader.Id, includeProperties: "ApplicationUser");

            if (stripeToken != null)
            {
                // process the payment
                var options = new ChargeCreateOptions
                {
                    Amount = Convert.ToInt32(orderHeader.OrderTotal * 100),
                    Currency = "cad",
                    Description = "Order Id: " + orderHeader.Id,
                    Source = stripeToken
                };

                var service = new ChargeService();
                // This will make the call to create the transaction on the credit card
                Charge charge = service.Create(options);

                // BalanceTransactionId is returned once a transaction is made.
                if (charge.BalanceTransactionId == null)
                {
                    orderHeader.PaymentStatus = SD.PaymentStatusRejected;
                }
                else
                {
                    orderHeader.TransactionId = charge.BalanceTransactionId;
                }

                // status of the charge
                if (charge.Status.ToLower() == "succeded")
                {
                    orderHeader.PaymentStatus = SD.PaymentStatusApproved;
                    orderHeader.PaymentDate = DateTime.Now;
                }

                _unitOfWork.Save();
            }
            
            return RedirectToAction("Details", "Order", new { id = orderHeader.Id });
        }

        [Authorize(Roles = SD.Role_Admin + "," + SD.Role_Employee)]
        public IActionResult StartProcessing(int id)
        {
            // set order status to StatusInProcess
            OrderHeader orderHeader = _unitOfWork.OrderHeader.GetFirstOrDefault(u => u.Id ==id);
            orderHeader.OrderStatus = SD.StatusInProcess;

            _unitOfWork.Save();

            return RedirectToAction("Index");
        }

        [HttpPost]
        [Authorize(Roles = SD.Role_Admin + "," + SD.Role_Employee)]
        public IActionResult ShipOrder()
        {
            // get orderHeader and configure properties in line with the shipping
            OrderHeader orderHeader = _unitOfWork.OrderHeader.GetFirstOrDefault(u => u.Id == OrderVm.OrderHeader.Id);
            orderHeader.TrackingNumber = OrderVm.OrderHeader.TrackingNumber;
            orderHeader.Carrier = OrderVm.OrderHeader.Carrier;
            orderHeader.OrderStatus = SD.StatusShipped;
            orderHeader.ShippingDate = DateTime.Now;

            _unitOfWork.Save();

            return RedirectToAction("Index");
        }

        [Authorize(Roles = SD.Role_Admin + "," + SD.Role_Employee)]
        public IActionResult CancelOrder(int id)
        {
            // get order header
            OrderHeader orderHeader = _unitOfWork.OrderHeader.GetFirstOrDefault(u => u.Id == id);

            // if the order has been approved then it will need to be refunded
            if(orderHeader.PaymentStatus == SD.StatusApproved)
            {
                // create refund options
                var options = new RefundCreateOptions
                {
                    Amount = Convert.ToInt32(orderHeader.OrderTotal * 100),
                    Reason = RefundReasons.RequestedByCustomer,
                    Charge = orderHeader.TransactionId
                };

                var service = new RefundService();
                Refund refund = service.Create(options);

                // set orderHeader properties in line with a refund
                orderHeader.OrderStatus = SD.StatusRefunded;
                orderHeader.PaymentStatus = SD.StatusRefunded;
            }
            else
            {
                // order was not approved so it will be cancelled
                orderHeader.OrderStatus = SD.StatusCancelled;
                orderHeader.PaymentStatus = SD.StatusCancelled;
            }

            _unitOfWork.Save();

            return RedirectToAction("Index");
        }

        public IActionResult UpdateOrderDetails() 
        {
            // get OrderHeader by Id
            var orderHEaderFromDb = _unitOfWork.OrderHeader.GetFirstOrDefault(u => u.Id == OrderVm.OrderHeader.Id);

            // update all possible fields that might have been updated
            orderHEaderFromDb.Name = OrderVm.OrderHeader.Name;
            orderHEaderFromDb.PhoneNumber = OrderVm.OrderHeader.PhoneNumber;
            orderHEaderFromDb.StreetAddress = OrderVm.OrderHeader.StreetAddress;
            orderHEaderFromDb.City = OrderVm.OrderHeader.City;
            orderHEaderFromDb.Province = OrderVm.OrderHeader.Province;
            orderHEaderFromDb.PostalCode = OrderVm.OrderHeader.PostalCode;

            // make sure carrier is not null
            if (OrderVm.OrderHeader.Carrier != null)
            {
                orderHEaderFromDb.Carrier = OrderVm.OrderHeader.Carrier;
            
            }
            
            // make sure tracking number is not null
            if (OrderVm.OrderHeader.TrackingNumber != null)
            {
                orderHEaderFromDb.TrackingNumber = OrderVm.OrderHeader.TrackingNumber;
            }

            // save changes
            _unitOfWork.Save();

            TempData["Success"] = "Order Details Updated Successfully.";
            
            return RedirectToAction("Details", "Order");
        }

        #region API CALLS

        [HttpGet]
        public IActionResult GetOrderList(string status)
        {
            // get currently logged in user
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);

            IEnumerable<OrderHeader> orderHeaderList;

            // check users role
            if (User.IsInRole(SD.Role_Admin) || User.IsInRole(SD.Role_Employee))
            {
                // if admin or employee then get all the orders
                orderHeaderList = _unitOfWork.OrderHeader
                        .GetAll(includeProperties: "ApplicationUser");
            }
            else
            {
                // get all orders related to user
                orderHeaderList = _unitOfWork.OrderHeader
                        .GetAll(u => u.ApplicationUserId == claim.Value,
                                includeProperties: "ApplicationUser");
            }

            // return specific orders based on status
            switch (status)
            {
                case "inprocess":
                    orderHeaderList = orderHeaderList.Where(o => o.PaymentStatus == SD.PaymentStatusDelayedPayment);
                    break;
                case "pending":
                    orderHeaderList = orderHeaderList.Where(o => 
                            o.OrderStatus == SD.StatusApproved || 
                            o.OrderStatus == SD.StatusInProcess ||
                            o.OrderStatus == SD.StatusPending);
                    break;
                case "completed":
                    orderHeaderList = orderHeaderList.Where(o => o.OrderStatus == SD.StatusShipped);
                    break;
                case "rejected":
                    orderHeaderList = orderHeaderList.Where(o =>
                            o.OrderStatus == SD.StatusCancelled ||
                            o.OrderStatus == SD.StatusRefunded ||
                            o.OrderStatus == SD.PaymentStatusRejected);
                    break;
                default:
                    break;
            }


            return Json(new { data = orderHeaderList });
        }

        #endregion
    }
}
