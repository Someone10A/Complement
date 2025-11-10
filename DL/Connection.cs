using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DL
{
    public class Connection
    {
        public static string GetConnectionStringGen(string mode)
        {
            if (mode == "PRO")
            {
                return "Dsn=gnx_prod;uid=desa";
            }
            else
            {
                return "Dsn=gnx_clon;uid=desa";
            }
        }
        public static string GetConnectionStringLga(string mode)
        {
            if (mode == "PRO")
            {
                return "Dsn=lga_prod;uid=lgprod01;";
            }
            else
            {
                return "Dsn=lga_clon;uid=lgprod01;";
            }
        }

        public static string GetConnectionSAT(string mode) 
        {
            return Environment.GetEnvironmentVariable("CON_CAT");
        }
    }
}
