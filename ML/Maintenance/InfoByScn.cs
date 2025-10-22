using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ML.Maintenance
{
    public class InfoByScn
    {
        public string NumScn { get; set; }
        public string PtoAlm { get; set; }
        public string CodPto { get; set; }
        public string NumEdc { get; set; }
        public string OrdRel { get; set; }
        public string Estado1 { get; set; }//edc_cab
        public string Estado2 { get; set; }//ordedc_cab
        public string? FecEnt { get; set; }
        public string? FecCli { get; set; }
        public string? FecEntR { get; set; }
        public string CodCli { get; set; }
        public string CodDir { get; set; }
        public string? TelCli { get; set; }
        public string? TelCli1 { get; set; }
        public string? TelCli2 { get; set; }
        public string Maintenance { get; set; }
        public string? NomCli { get; set; }
        public string? Ape1Cli { get; set; }
        public string? Ape2Cli { get; set; }
        public string? NumInt { get; set; }
        public string? NumExt { get; set; }
        public string? Calle { get; set; }
        public string? Colonia { get; set; }
        public string? Municipio { get; set; }
        public string? Estado { get; set; }
        public string? CodPos { get; set; }
        public string? Referencias { get; set; }
        public string? Observaciones { get; set; }
        public string? Panel { get; set; }
        public string? Volado { get; set; }
        public string? MasGen { get; set; }
        public string Longitud { get; set; }
        public string Latitud { get; set; }
        public string IsRdd { get; set; }
        public string IsRddSend { get; set; }
        public string? NumRdd { get; set; }
        public string IsConfirmed { get; set; }
        public string InPlan { get; set; }
        public string InRoute { get; set; }

        public List<ML.Maintenance.Detail> Details { get; set; }
    }
}
