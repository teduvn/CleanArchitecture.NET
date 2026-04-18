using System;
using System.Collections.Generic;
using System.Text;

namespace OrderManagement.Infrastructure.Email
{
    public class SendGridSettings
    {
        public string ApiKey { get; set; }
        public string FromEmail { get; set; }
    }
}
