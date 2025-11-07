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
                return "DSN=lga_prod;Uid=lgprod01;Pwd=L0gcdt22;";
            }
            else
            {
                return "DSN=lga_clon;Uid=lgprod01;Pwd=L0gcdt22;";
            }
        }

        public static string GetConnectionStringGnx(string mode)
        {
            if (mode == "PRO")
            {
                return "DSN=gnx_prod;Uid=desa;Pwd=Desa0615;";
            }
            else
            {
                return "DSN=gnx_clon;Uid=desa;Pwd=Desa0615;";
            }
        }
    }
}
