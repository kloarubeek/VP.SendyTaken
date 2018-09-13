using System;
using System.Collections.Generic;
using System.Text;

namespace VerwijderWKUsers.Models
{
    public class SendyApiSettings
    {
        public string ApiKey { get; set; }
        public string Endpoint { get; set; }
        public string HuidigeDeelnemersLijstId { get; set; }
        public string VerplaatsNaarLijstId { get; set; }
    }
}
