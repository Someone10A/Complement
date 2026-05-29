using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
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

        public static string GetConnectionStringSig(string mode) 
        {
            if (mode == "PRO")
            {
                return GetConSigPro(mode);
            }
            else
            {
                return GetConSigDev(mode);
            }
        }
        protected static string? GetConSigPro(string mode)
        {
            return Environment.GetEnvironmentVariable("CON_SIG_PRO2");
        }
        protected static string? GetConSigDev(string mode)
        {
            return Environment.GetEnvironmentVariable("CON_SIG_PRO2");
        }


        public static string? GetInterfacePath(string mode)
        {
            try
            {
                return Environment.GetEnvironmentVariable("INTERPATH");
            }
            catch (Exception ex)
            {
                return null;
            }
        }
        public static string? GetLegacyIp(string mode)
        {
            try
            {
                return Environment.GetEnvironmentVariable("LEGACYIP");
            }
            catch (Exception ex)
            {
                return null;
            }
        }
        public static string? GetLegacyUser(string mode)
        {
            try
            {
                return Environment.GetEnvironmentVariable("LEGACYUSER");
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        public static string? GetLegacyPwd(string mode)
        {
            try
            {
                return Environment.GetEnvironmentVariable("LEGACYPWD");
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        public static string? GetLegacyPort(string mode)
        {
            try
            {
                return Environment.GetEnvironmentVariable("LEGACYPORT");
            }
            catch (Exception ex)
            {
                return null;
            }
        }
    }
}
