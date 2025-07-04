using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AcfunApi
{
    public class LoginInformation
    {
        public string? securityKey { get; set; }
        public long? userId { get; set; }
        public string? serviceToken { get; set; }

        public string? _did { get; set; }
        public string? serviceTokenName { get; set; }
    }
}