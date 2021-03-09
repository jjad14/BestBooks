using Braintree;
using System;
using System.Collections.Generic;
using System.Text;

namespace BestBooks.Utility
{
    public interface IBrainTree
    {
        IBraintreeGateway CreateGateway();
        IBraintreeGateway GetGateway();

    }
}
