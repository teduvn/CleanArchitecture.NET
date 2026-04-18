using System;
using System.Collections.Generic;
using System.Text;

namespace OrderManagement.Infrastructure.Payment
{
    public class StripeSettings
    {
        public string SecretKey { get; set; } = null!;
    }
}
