using System;
using System.Collections.Generic;
using System.Text;

namespace VerwijderWKUsers.Models
{
    public class EmailLoginToken
    {
        public string Emailadres { get; set; }
        public byte WebsiteId { get; set; }
        public string Token { get; set; }
        public DateTime Datum { get; set; }
    }
}
