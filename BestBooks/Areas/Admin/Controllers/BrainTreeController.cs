using BestBooks.Utility;
using Braintree;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BestBooks.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class BrainTreeController : Controller
    {
        public IBrainTree BrainTree { get; set; }

        private readonly ILogger<BrainTreeController> _logger;

        public BrainTreeController(IBrainTree brainTree, ILogger<BrainTreeController> logger)
        {
            BrainTree = brainTree;
            _logger = logger;
        }

        public IActionResult Index()
        {
            // get the gateway
            var gateway = BrainTree.GetGateway();
            // create client token
            var clientToken = gateway.ClientToken.Generate();

            // pass token to view
            // ViewBag can be used to pass some data from the controller to the view
            // alternative is to create a ViewModel
            ViewBag.ClientToken = clientToken;

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Index(IFormCollection collection) 
        {
            // access nonce token
            // Whatever value will be posted, it will be inside the collection

            Random rnd = new Random();
            string nonceFromClient = collection["payment_method_nonce"];
            var request = new TransactionRequest
            {
                Amount = rnd.Next(1, 100),
                PaymentMethodNonce = nonceFromClient,
                CurrencyIsoCode = "CAD",
                // random for now, will actuall come from OrderHeader
                OrderId = "123456789",
                Options = new TransactionOptionsRequest
                {
                    // In case of payments, you need to settle what it will be pending
                    // so we will directly send the payment for settlement
                    SubmitForSettlement = true
                }
            };

            // Once we have that request object, we will get a result once we do the transaction
            var gateway = BrainTree.GetGateway();
            // Make the call to Braintree and it will process the transaction and return the result
            Result<Transaction> result = gateway.Transaction.Sale(request);

            // check if the result was successful or not
            if (result.Target.ProcessorResponseText == "Approved")
            {
                // the transaction went through

                TempData["Success"] = "The Transaction was a success," +
                                      " Your Transaction ID: " + result.Target.Id +
                                      ", Amount Charged: $" + result.Target.Amount;

            }
            else 
            {
                TempData["Error"] = "There was a problem processing your credit card; please double check your payment information and try again";

                // Log any failed transactions
                _logger.LogInformation("OrderId: " + result.Target.Id +
                                       "\nResponse: " + result.Target.ProcessorResponseCode +
                                       "\nResponseType: " + result.Target.ProcessorResponseType +
                                       "\nResponseText: " + result.Target.ProcessorResponseText);
            }

            return RedirectToAction("Index");

        }
    }
}
