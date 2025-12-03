using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Odbc;
using System.Linq;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace BL.BaseControl
{
    public class BaseOperator
    {
        /*
         *0 Ruta Asignada -Es posible cambiar otro chofer
         *1 Ruta Cerrada - Ya no se puede hacer nada
         *2 Ruta Abierta - El chofer acepto la ruta y esta en marcancion de eventos
         *3 Ruta Finalizada - el chofer finalizo ruta, y puede ser aprobado o rechazado por Base
         */

        /*
         * Método:      .
         * Vista:       .
         * Descripción: .
         * Entrada:     .
         * Salida:      .
         * Proceso:     .
         */
        public static ML.Result x(string mode)
        {
            ML.Result result = new ML.Result();
            try
            {

            }
            catch (Exception ex)
            {
                result.Correct = false;
                result.Message = $@"";
            }
            return result;
        }
        /*
         * Método:      .GetOpenRoutes
         * Vista:       .BaseOperator.cshtml
         * Descripción: .Visualiza las rutas abiertas y finalizadas 
         * Entrada:     .mode
         * Salida:      .ML.Result y en el object Lista de rutas parecido al GetOrdersPerRoute de BaseControl.BaseControl
         * Proceso:     .Obtener todo lo de ora_asignacion_operador en estatus 2 y 3
         */
        public static ML.Result GetOpenRoutes(string mode)
        {
            ML.Result result = new ML.Result();
            try
            {
                using(OdbcConnection connection = new OdbcConnection(DL.Connection.GetConnectionStringGen(mode)))
                {
                    connection.Open();

                    string query = $@"SELECT  A.estatus,
                                    CASE
                                    WHEN (A.estatus = 0) THEN 'Ruta asignada'
                                    WHEN (A.estatus = 1) THEN 'Ruta Cerrada'
                                    WHEN (A.estatus = 2) THEN 'Ruta Abierta'
                                    WHEN (A.estatus = 3) THEN 'Ruta Finalizada'
                                    ELSE 'No Listado'
                                    END AS descripcion,
                                    A.pto_alm,
                                    TRIM(A.car_sal) AS car_sal,
                                    C.fec_car,
                                    TRIM(B.nom_ope) AS nom_ope,
                                    TRIM(A.rfc_ope) AS rfc_ope
                            FROM ora_asignacion_operador A, ora_operadores B
                            WHERE A.estatus IN (2,3)
                            AND B.rfc_ope = A.rfc_ope
                            AND C.car_sal = A.car_sal
                            GROUP BY 1,2,3,4,5,6,7";

                    List<ML.Operator.RouteHeader> routes = new List<ML.Operator.RouteHeader>();

                    using(OdbcCommand cmd = new OdbcCommand(query, connection))
                    {
                        using(OdbcDataReader reader = cmd.ExecuteReader())
                        {
                            ML.Operator.RouteHeader route = new ML.Operator.RouteHeader();

                            route.Estatus = reader.GetString(0);
                            route.Descripcion = reader.GetString(1);
                            route.PtoAlm = reader.GetString(2);
                            route.CarSal = reader.GetString(3);
                            route.FecCar = reader.GetDateTime(4).ToString("ddMMyyyy");
                            route.NomOpe = reader.GetString(5);
                            route.RfcOpe = reader.GetString(6);

                            routes.Add(route);
                        }
                    }

                    result.Correct = true;
                    result.Object = routes;
                }
            }
            catch (Exception ex)
            {
                result.Correct = false;
                result.Message = $@"Error al obtener las rutas abiertas";
            }
            return result;
        }

        /*
         * Método:      .GetOrdersPerRoute
         * Vista:       .BaseOperator.cshtml
         * Descripción: .Obtener el detalle de ruta
         * Entrada:     ."Cabecera de ruta de chofer" y mode
         * Salida:      .ML.Result y en el object Lista de ordenes
         * Proceso:     .Obtener de ora_asignacion_operador y ora_ruta y ora_ruta_eventos
         */
        public static ML.Result GetOrdersPerRoute(ML.Operator.RouteHeader route, string mode)
        {
            ML.Result result = new ML.Result();
            try
            {
                using (OdbcConnection connection = new OdbcConnection(DL.Connection.GetConnectionStringGen(mode)))
                {
                    connection.Open();

                    result.Correct = true;
                    //result.Object = orderList;
                }
            }
            catch (Exception ex)
            {
                result.Correct = false;
                result.Message = $@"Se obtuvo un error al consultar los viajes.";
            }
            return result;
        }

        /*
         * Método:      .MultiConfirmation
         * Vista:       .BaseOperator.cshtml
         * Descripción: .Mudar los eventos de entrega y no entrega
         * Entrada:     ."Cabecera de ruta de chofer" y mode
         * Salida:      .ML.Result y Mensaje de confirmacion
         * Proceso:     .Por cada registro de ora_ruta_eventos, 
         *                  generar una interfaz contemplar diseño en BaseControl.BaseControl
         *                  Confirmation,BuilData,BuildDetail,CreateFile,UpdateRuta
         *                  Ademas de cerrar ruta ora_asignacion_operador a 1
         */
        public static ML.Result MultiConfirmation(ML.Operator.RouteHeader route, string mode)
        {
            ML.Result result = new ML.Result();
            try
            {

            }
            catch (Exception ex)
            {
                result.Correct = false;
                result.Message = $@"Error al confirmar las ruta";
            }
            return result;
        }

        /*
         *Método:       .RejectRoute
         *Vista:        .BaseOperator.cshtml
         *Descripción:  .Rechazar el procesamiento de ruta del chofer
         *Entrada:      ."Cabecera de ruta de chofer"  y mode
         *Salida:       .ML.Result y Mensaje en result.Message
         *Proceso:      .Actualizar ora_asignacion_operador a un estatus Ruta abierta siempre y cuando sea ruta finalizada desde la vista
       */
        public static ML.Result RejectRoute(ML.Operator.RouteHeader route, string mode)
        {
            ML.Result result = new ML.Result();
            try
            {
                using (OdbcConnection connection = new OdbcConnection(DL.Connection.GetConnectionStringGen(mode)))
                {
                    connection.Open();

                    string query = $@"UPDATE
                                             ora_asignacion_operador
                                        SET estatus = 1
                                        WHERE cod_emp = 1
                                        AND pto_alm = {route.PtoAlm}
                                        AND car_sal = '{route.CarSal}'
                                        AND rfc_ope = '{route.RfcOpe}'
                                        AND estatus = 3
                                        ";

                    using (OdbcCommand cmd = new OdbcCommand(query, connection))
                    {
                        int rowsAffected = cmd.ExecuteNonQuery();

                        if (rowsAffected < 1)
                        {
                            throw new Exception($@"No se pudo rechazar la ruta");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                result.Correct = false;
                result.Message = $@"Error al rechazar la ruta";
            }
            return result;
        }
    }
}
