using Braintree;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Text;

namespace BestBooks.Utility
{
    public class BrainTree : IBrainTree
    {
        public BrainTreeSettings Options { get; set; }

        private IBraintreeGateway BraintreeGateway { get; set; }

        public BrainTree(IOptions<BrainTreeSettings> options)
        {
            Options = options.Value;
        }

        public IBraintreeGateway CreateGateway()
        {
            return new BraintreeGateway(Options.Environment, Options.MerchantID, Options.PublicKey, Options.PrivateKey);
        }

        public IBraintreeGateway GetGateway()
        {
            if (BraintreeGateway == null) {
                BraintreeGateway = CreateGateway();
            }

            return BraintreeGateway;
        }
    }
}
