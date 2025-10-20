using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace DL
{
    public class Directory
    {
        public static string? GetOutputPath(string mode)
        {
            try
            {
                return Environment.GetEnvironmentVariable("OUTPUT46");
            }
            catch (Exception ex)
            {
                return null;
            }
        }
    }
}
