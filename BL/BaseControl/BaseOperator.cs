using ML;
using ML.BaseControl;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Odbc;
using System.Linq;
using System.Net.Http.Headers;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
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
                                    FROM ora_asignacion_operador A, ora_operadores B, ora_ruta C
                                    WHERE A.estatus IN (2,3)
                                    AND B.rfc_ope = A.rfc_ope
                                    AND C.car_sal = A.car_sal
                                    GROUP BY 1,2,3,4,5,6,7
                                    ORDER BY A.estatus DESC";

                    List<ML.Operator.RouteHeader> routes = new List<ML.Operator.RouteHeader>();

                    using(OdbcCommand cmd = new OdbcCommand(query, connection))
                    {
                        using(OdbcDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                ML.Operator.RouteHeader route = new ML.Operator.RouteHeader();

                                route.Estatus = reader.GetString(0);
                                route.Descripcion = reader.GetString(1).Trim();
                                route.PtoAlm = reader.GetString(2).Trim();
                                route.CarSal = reader.GetString(3).Trim();
                                route.FecCar = reader.GetDateTime(4).ToString();
                                route.NomOpe = reader.GetString(5).Trim();
                                route.RfcOpe = reader.GetString(6).Trim();

                                routes.Add(route);
                            }
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

                    string query = $@"SELECT CASE
                                        WHEN(C.fec_act IS NULL) THEN B.fec_car
                                        ELSE C.fec_act 
		                                END AS fec_act,
                                        TRIM(A.car_sal) AS car_sal,
                                        TRIM(B.ord_rel) AS ord_rel,
                                        B.num_scn,
                                        E.cod_pto,
                                        F.cod_cli,
                                        UPPER(TRIM(F.nom_cli)||' '||TRIM(F.ape1_cli)||' '||TRIM(F.ape2_cli)) AS cliente,
                                        CASE
                                        WHEN (C.cod_mot IS NULL) THEN 'Sin marcaje'
                                        ELSE 'Evento Asignado' 
		                                END as estatus_ruta,
                                        CASE
                                        WHEN E.estado = 'P' THEN E.estado||'-Retenido-Transito'
                                        WHEN E.estado = 'T' THEN E.estado||'-Transito'
                                        WHEN E.estado = 'I' THEN E.estado||'-Impreso'
                                        WHEN E.estado = 'X' THEN E.estado||'-Cancelado'
                                        WHEN E.estado = 'C' THEN E.estado||'-Cancelado'
                                        WHEN E.estado = 'E' THEN E.estado||'-Entregado'
                                        WHEN E.estado = 'D' THEN E.estado||'-Devuelto'
                                        WHEN E.estado = 'G' THEN E.estado||'-Generado'
                                        ELSE E.estado||'-Estado desconocido'
                                        END AS estatus_gnx,
                                        'Ruta Trabajandose' AS estatus_rt,
                                        CASE
                                        WHEN (C.cod_mot IS NULL) THEN 'No marcado aun'
                                        ELSE TRIM(D.des_mot) 
		                                END AS des_mot,
                                        CASE
                                        WHEN(G.scn_nvo IS NULL) THEN 'NO'
                                        ELSE 'Es un RDD de cambio' 
		                                END AS rdd_info
                                FROM ora_asignacion_operador A
                                INNER JOIN ora_ruta B
                                         ON B.pto_alm = A.pto_alm
                                        AND B.car_sal = A.car_sal
                                LEFT JOIN ora_ruta_eventos C
                                         ON C.pto_alm = B.pto_alm
                                        AND C.car_sal = B.car_sal
                                        AND C.ord_rel = B.ord_rel
                                        AND C.num_scn = B.num_scn
                                LEFT  JOIN ora_motivos_rt D
                                        ON D.cod_mot = C.cod_mot
                                INNER JOIN edc_cab E
                                         ON E.cod_emp = 1
                                        AND E.num_scn = B.num_scn
                                LEFT JOIN clientes F
                                         ON F.cod_emp = E.cod_emp
                                        AND F.cod_cli = E.cod_cli
                                LEFT JOIN rdd_cab G
                                         ON G.cod_emp = E.cod_emp
                                        AND G.scn_nvo = E.num_scn
                                WHERE A.pto_alm  = {route.PtoAlm}
                                AND A.car_sal = '{route.CarSal}'
                                AND A.rfc_ope = '{route.RfcOpe}'";
                    
                    List<ML.Operator.RouteDetail> routeDetailList = new List<ML.Operator.RouteDetail>();    

                    using(OdbcCommand cmd = new OdbcCommand(query, connection))
                    {
                        using(OdbcDataReader reader = cmd.ExecuteReader())
                        {
                           while (reader.Read())
                           {
                                ML.Operator.RouteDetail routeDetail = new ML.Operator.RouteDetail();

                                routeDetail.FecAct = reader.GetDateTime(0).ToString();
                                routeDetail.CarSal = reader.GetString(1);
                                routeDetail.OrdRel = reader.GetString(2);
                                routeDetail.NumScn = reader.GetString(3);
                                routeDetail.CodPto = reader.GetString(4);
                                routeDetail.CodCli = reader.GetString(5);
                                routeDetail.Cliente = reader.GetString(6);
                                routeDetail.EstatusRuta = reader.GetString(7).Trim();
                                routeDetail.EstatusGnx = reader.GetString(8).Trim();
                                routeDetail.EstatusRT = reader.GetString(9);
                                routeDetail.Motivo = reader.GetString(10);
                                routeDetail.RddInfo = reader.GetString(11).Trim();
                                //routeDetail.Dirreccion = reader.GetString(12).Trim();

                                routeDetailList.Add(routeDetail);
                           }
                        }
                    }

                    result.Correct = true;
                    result.Object = routeDetailList;
                }
            }
            catch (Exception ex)
            {
                result.Correct = false;
                result.Message = $@"Se obtuvo un error al consultar el detalle de la ruta: {ex.Message}";
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
         *                  NOTA: a Multiconfirmation solo llegaran rutas que tengan estatus finalizado, no deben llegar otras rutas
         */
        public static async Task<ML.Result> MultiConfirmation(ML.Operator.RouteHeader route,string user, string mode)
        {
            ML.Result result = new ML.Result();
            try
            {
                //GetConfirmations
                ML.Result resultGetConfirmations = GetConfirmations(route.CarSal, route.PtoAlm, user, mode);
                if (!resultGetConfirmations.Correct)
                {
                    throw new Exception($@"{resultGetConfirmations.Message}");
                }
                List<ML.BaseControl.Confirmation> confirmationList = (List<ML.BaseControl.Confirmation>)resultGetConfirmations.Object;

                List<ML.BaseControl.Interface> interList = new List<ML.BaseControl.Interface>();

                //Confirmation
                foreach (ML.BaseControl.Confirmation confirmation  in confirmationList)
                {
                    confirmation.DeliveryReason = confirmation.DeliveryReason == "0" ? "ENTREGA_OK" : confirmation.DeliveryReason;
                    //GetData
                    ML.Result resultGetDetails = await GetDetails(confirmation.OrdRel, mode);
                    if (!resultGetDetails.Correct)
                    {
                        throw resultGetDetails.Ex;
                    }
                    List<OrderResult> orderDetails = (List<OrderResult>)resultGetDetails.Object;

                    //BuildDetail
                    ML.Result resultBuildData = BuildData(orderDetails, confirmation, mode);
                    if (!resultBuildData.Correct)
                    {
                        throw resultBuildData.Ex;
                    }
                    ML.BaseControl.Interface inter = (ML.BaseControl.Interface)resultBuildData.Object;

                    interList.Add(inter);
                }

                foreach(ML.BaseControl.Interface inter in  interList)
                {
                    ML.Result resultCreateFile = CreateFile(inter, user, mode);
                    if (!resultCreateFile.Correct)
                    {
                        throw resultCreateFile.Ex;
                    }
                    Thread.Sleep(500);
                }

                ML.Result resultUpdateRoute = UpdateRoute(route.CarSal, mode);

                ML.Result resultAcceptRoute = AcceptRoute(route, mode);
                if (!resultAcceptRoute.Correct)
                {
                    throw new Exception($@"{result.Message}");
                }

                result.Correct = true;
                result.Message = $@"Se crearon {confirmationList.Count()} interfaces y bloqueo la ruta.";
            }
            catch (Exception ex)
            {
                result.Correct = false;
                result.Message = $@"Error al confirmar las rutas {ex.Message}";
            }
            return result;
        }
        private static ML.Result GetConfirmations(string carSal, string ptoAlm,string user, string mode)
        {
            ML.Result result = new ML.Result();
            try
            {
                List<ML.BaseControl.Confirmation> confirmationList = new List<ML.BaseControl.Confirmation>();

                using (OdbcConnection connection = new OdbcConnection(DL.Connection.GetConnectionStringGen(mode)))
                {
                    connection.Open();

                    string query = $@"SELECT TRIM(A.ord_rel) AS ord_rel, cod_mot
                                    FROM ora_ruta A
                                    INNER JOIN ora_ruta_eventos B
                                             ON B.pto_alm = A.pto_alm
                                            AND B.car_sal = A.car_sal
                                            AND B.ord_rel = A.ord_rel
                                            AND B.num_scn = A.num_scn
                                    WHERE A.pto_alm = {ptoAlm}
                                    AND A.estatus = 0
                                    AND A.car_sal = '{carSal}'";

                    using (OdbcCommand command = new OdbcCommand(query, connection))
                    {
                        using (OdbcDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                ML.BaseControl.Confirmation confirmation = new ML.BaseControl.Confirmation();

                                confirmation.OrdRel = reader.GetString(0);
                                confirmation.Shipment = carSal;
                                confirmation.DeliveryReason = reader.GetString(1);
                                confirmation.User = user;

                                confirmationList.Add(confirmation);
                            }
                        }
                    }
                }

                if(confirmationList.Count() < 1)
                {
                    throw new Exception($@"No se encontraron eventos a marcar ({confirmationList.Count()})");
                }

                result.Correct = true;
                result.Message = $@"Se encontraron {confirmationList.Count()} eventos";
                result.Object = confirmationList;
            }
            catch (Exception ex)
            {
                result.Correct = false;
                result.Ex = ex;
                result.Message = $@"Error al obtener confirmaciones: {ex.Message}";
            }
            return result;
        }
        private static async Task<ML.Result> GetDetails(string ordRel, string mode)
        {
            ML.Result result = new ML.Result();
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    client.Timeout = TimeSpan.FromSeconds(5);

                    var byteArray = Encoding.ASCII.GetBytes($"{DL.ApiOracle.GetOracleUsr(mode)}:{DL.ApiOracle.GetOraclePwd(mode)}");
                    client.DefaultRequestHeaders.Accept.Clear();
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", $"{Convert.ToBase64String(byteArray)}");

                    Uri uri = new Uri($@"{DL.ApiOracle.GetEndpoint(mode)}".Replace("{########}", ordRel));

                    HttpResponseMessage response = await client.GetAsync(uri);

                    if (response.IsSuccessStatusCode)
                    {

                        string jsonString = await response.Content.ReadAsStringAsync();

                        ML.BaseControl.ApiResponse apiResponse = JsonSerializer.Deserialize<ML.BaseControl.ApiResponse>(jsonString);

                        result.Correct = true;
                        result.Object = apiResponse.Results.Where(x => x.OrderStatus == "Shipped").ToList();
                    }
                    else
                    {
                        Console.WriteLine($"Error al hacer la petición: {(int)response.StatusCode} - {response.ReasonPhrase}");
                        result.Correct = false;
                    }
                }
            }
            catch (TaskCanceledException)
            {
                Console.WriteLine($"Se obtuvo un error al consultar los detalles de envio: tiempo exedido.");
                result.Correct = false;
            }
            catch (Exception ex)
            {
                result.Correct = false;
                result.Message = result.Message = $@"Se obtuvo un error al consultar los detalles de envio.";
                result.Ex = ex;
            }
            return result;
        }
        private static ML.Result BuildData(List<OrderResult> orderDetails, ML.BaseControl.Confirmation confirmation, string mode)
        {
            Result result = new Result();
            try
            {
                ML.BaseControl.Interface inter = new ML.BaseControl.Interface();

                inter.Control = new ML.BaseControl.InterfaceControl();

                inter.Control.OrdRel = confirmation.OrdRel;
                inter.Control.User = confirmation.User;
                inter.Control.Reason = confirmation.DeliveryReason;

                inter.Header = new ML.BaseControl.InterfaceHeader();
                inter.Header.Shipment = confirmation.Shipment;

                inter.Details = BuildDetail(orderDetails, confirmation);

                result.Correct = true;
                result.Object = inter;
            }
            catch (Exception ex)
            {
                result.Correct = false;
                result.Message = $@"Error al construir data {confirmation.OrdRel}: {ex.Message}";
                Console.WriteLine(result.Message);
            }
            return result;
        }
        private static List<InterfaceDetail> BuildDetail(List<OrderResult> orderDetails, ML.BaseControl.Confirmation confirmation)
        {
            List<InterfaceDetail> interDetails = new List<InterfaceDetail>();

            foreach (OrderResult orderResult in orderDetails)
            {
                InterfaceDetail interDetail = new InterfaceDetail();

                interDetail.Reason = confirmation.DeliveryReason;
                interDetail.Shipment = confirmation.Shipment;

                interDetail.Olpn = orderResult.Olpn;
                interDetail.Sku = orderResult.Sku.Replace("SRS", "");
                interDetail.Cantidad = orderResult.Quantity.ToString();
                interDetail.Cantidad2 = interDetail.Cantidad;

                interDetails.Add(interDetail);
            }
            return interDetails;
        }
        private static Result CreateFile(ML.BaseControl.Interface inter, string user, string mode)
        {
            Result result = new Result();
            string dateTime = DateTime.Now.ToString("yyyyMMddHHmmssfff");
            string fileName = $@"SE_LEGACY_ENTREGA_X_{user}_{dateTime}.csv";
            //string filePath = Path.Combine(DL.Directory.GetOutputPath(mode), fileName);
            string filePath = Path.Combine("C:\\Users\\Sistemas piso6 3\\Documents\\Alan Oran\\Temp", fileName);
            try
            {
                string C = string.Join("|", typeof(ML.BaseControl.InterfaceControl).GetProperties().Select(p => p.GetValue(inter.Control)).ToArray());
                string H = string.Join("|", typeof(ML.BaseControl.InterfaceHeader).GetProperties().Select(p => p.GetValue(inter.Header)).ToArray());

                List<string> D = new List<string>();

                foreach (ML.BaseControl.InterfaceDetail detail in inter.Details)
                {
                    string deta = string.Join("|", typeof(ML.BaseControl.InterfaceDetail).GetProperties().Select(p => p.GetValue(detail)).ToArray());
                    D.Add(deta);
                }

                using (StreamWriter writer = new StreamWriter(filePath))
                {
                    writer.WriteLine(C);
                    writer.WriteLine(H);
                    foreach (string d in D)
                    {
                        writer.WriteLine(d);
                    }
                }

                result.Correct = true;
                result.Object = filePath;
                //Console.WriteLine("Se creo el archivo " + filePath);
            }
            catch (Exception ex)
            {
                result.Correct = false;
                result.Message = "Error al crear un archivo" + fileName + ": " + ex.Message;
                //Console.WriteLine(result.Message);
            }
            return result;
        }
        private static ML.Result UpdateRoute(string carSal, string mode)
        {
            ML.Result result = new ML.Result();
            try
            {
                using (OdbcConnection connection = new OdbcConnection(DL.Connection.GetConnectionStringGen(mode)))
                {
                    connection.Open();

                    string query = $@"UPDATE
                                            ora_ruta
                                        SET estatus = 2,
                                            fec_act = CURRENT
                                        WHERE car_sal = '{carSal}'
                                        ";

                    List<ML.BaseControl.Reason> reasonList = new List<ML.BaseControl.Reason>();

                    using (OdbcCommand cmd = new OdbcCommand(query, connection))
                    {
                        int rowsAffected = cmd.ExecuteNonQuery();
                        if (rowsAffected < 1)
                        {

                        }
                    }

                    result.Correct = true;
                    result.Message = $@"Se bloqueo el registro";
                }
            }
            catch (Exception ex)
            {
                result.Correct = false;
                result.Message = $@"No pudo bloquearse el registro";
            }
            return result;
        }
        private static ML.Result AcceptRoute(ML.Operator.RouteHeader route, string mode)
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
                            throw new Exception($@"No se pudo aceptar la ruta");
                        }
                    }
                    result.Correct = true;
                    result.Message = $@"Ruta aceptada";
                }
            }
            catch (Exception ex)
            {
                result.Correct = false;
                result.Ex = ex;
                result.Message = $@"Error al aceptar la ruta se formaron las interfaces, reintentar sin modificar eventos";
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
                                        SET estatus = 2
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

                    result.Correct = true;
                    result.Message = $@"Ruta rechazada";
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
