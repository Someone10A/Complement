using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ML.CartaPorte
{
    public class EmailConfiguration
    {
        public string SmtpServer { get; set; }
        public string FromEmail { get; set; }
        public string DefaultEmail { get; set; }
        public List<string> CopyEmails { get; set; }
        public Dictionary<string, string> Operators { get; set; }
    }
}
