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

    }
}
