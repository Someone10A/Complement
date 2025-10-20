using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ML.Maintenance
{
    public class ConfirmedInfoByScn
    {
        public string NumScn { get; set; }
        public string PtoAlm { get; set; }
        public string CodPto { get; set; }
        public string NumEdc { get; set; }
        public bool IsConfirmed { get; set; }
        public bool IsPosfec { get; set; }
        public string? FecEnt { get; set; }

        public string UsuCon { get; set; }
        public string RolUsu { get; set; }
        public bool IsCrb { get; set; }

        public bool IsRdd { get; set; }
        
        public string CodCli { get; set; }
        public string CodDir { get; set; }

        public string NumInt { get; set; }
        public string NumExt { get; set; }
        public string Calle { get; set; }
        public string Colonia { get; set; }
        public string Municipio { get; set; }
        public string Estado { get; set; }
        public string CodPos { get; set; }
        public string Referencias { get; set; }
        public string Observaciones { get; set; }
        public string Panel { get; set; }
        public string Volado { get; set; }
        public string MasGen { get; set; }
        public string Longitud { get; set; }
        public string Latitud { get; set; }
    }
}
