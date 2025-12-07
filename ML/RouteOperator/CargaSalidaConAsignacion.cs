using System;

namespace ML.RouteOperator
{
    public class CargaSalidaConAsignacion
    {
        public string CarSal { get; set; }
        public bool TieneAsignacion { get; set; }
        public ML.RouteOperator.AsignacionOperador Asignacion { get; set; }
    }
}

