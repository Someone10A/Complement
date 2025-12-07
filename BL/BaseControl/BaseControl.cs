using DocumentFormat.OpenXml.EMMA;
using ML;
using ML.BaseControl;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Odbc;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace BL.BaseControl
{
    public class BaseControl
    {
        public static ML.Result GetInfoByScn(string num_scn,string cod_pto, string mode)
        {
            ML.Result result = new ML.Result();
            try
            {
                using (OdbcConnection connection = new OdbcConnection(DL.Connection.GetConnectionStringGen(mode)))
                {
                    connection.Open();

                    string query = $@"SELECT TRIM(car_sal) AS car_sal,
                                            TO_CHAR(fec_car) AS fec_car,
                                            pto_alm
                                        FROM ora_ruta
                                        WHERE pto_alm = {cod_pto}
                                        AND estatus = 0
                                        AND DATE(fec_car) > '03102025'
                                        GROUP BY 1,2,3
                                        ORDER BY 2
                                        ";

                    List<ML.BaseControl.OutboundShipment> outboundShipmentList = new List<ML.BaseControl.OutboundShipment>();

                    using (OdbcCommand cmd = new OdbcCommand(query, connection))
                    {
                        using (OdbcDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                ML.BaseControl.OutboundShipment outboundShipment = new ML.BaseControl.OutboundShipment();

                                outboundShipment.car_sal = reader.GetString(0);
                                outboundShipment.fec_car = reader.GetString(1);
                                outboundShipment.pto_alm = reader.GetInt32(2).ToString();

                                outboundShipmentList.Add(outboundShipment);
                            }
                        }
                    }

                    result.Correct = true;
                    result.Object = outboundShipmentList;
                }
            }
            catch (Exception ex)
            {
                result.Correct = false;
                result.Message = $@"Se obtuvo un error al consultar las cargas pendientes.";
            }
            return result;
        }
        public static ML.Result GetOpenRoutes(string cod_pto, string mode)
        {
            ML.Result result = new ML.Result();
            try
            {
                using (OdbcConnection connection = new OdbcConnection(DL.Connection.GetConnectionStringGen(mode)))
                {
                    connection.Open();

                    string query = $@"SELECT TRIM(car_sal) AS car_sal,
                                            TO_CHAR(fec_car) AS fec_car,
                                            pto_alm
                                        FROM ora_ruta
                                        WHERE pto_alm = {cod_pto}
                                        AND estatus = 0
                                        AND DATE(fec_car) > '03102025'
                                        GROUP BY 1,2,3
                                        ORDER BY 2
                                        ";

                    List<ML.BaseControl.OutboundShipment> outboundShipmentList = new List<ML.BaseControl.OutboundShipment>();

                    using (OdbcCommand cmd = new OdbcCommand(query, connection))
                    {
                        using (OdbcDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                ML.BaseControl.OutboundShipment outboundShipment = new ML.BaseControl.OutboundShipment();

                                outboundShipment.car_sal = reader.GetString(0);
                                outboundShipment.fec_car = reader.GetString(1);
                                outboundShipment.pto_alm = reader.GetInt32(2).ToString();

                                outboundShipmentList.Add(outboundShipment);
                            }
                        }
                    }

                    result.Correct = true;
                    result.Object = outboundShipmentList;
                }
            }
            catch (Exception ex)
            {
                result.Correct = false;
                result.Message = $@"Se obtuvo un error al consultar las cargas pendientes.";
            }
            return result;
        }
        public static ML.Result GetOpenRoutesPast(string cod_pto, string mode)
        {
            ML.Result result = new ML.Result();
            try
            {
                using (OdbcConnection connection = new OdbcConnection(DL.Connection.GetConnectionStringGen(mode)))
                {
                    connection.Open();

                    string query = $@"SELECT TRIM(car_sal) AS car_sal,
                                            TO_CHAR(fec_car) AS fec_car,
                                            pto_alm
                                        FROM ora_ruta
                                        WHERE pto_alm = {cod_pto}
                                        AND estatus = 0
                                        AND DATE(fec_car) < '04102025'
                                        GROUP BY 1,2,3
                                        ORDER BY 2
                                        ";

                    List<ML.BaseControl.OutboundShipment> outboundShipmentList = new List<ML.BaseControl.OutboundShipment>();

                    using (OdbcCommand cmd = new OdbcCommand(query, connection))
                    {
                        using (OdbcDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                ML.BaseControl.OutboundShipment outboundShipment = new ML.BaseControl.OutboundShipment();

                                outboundShipment.car_sal = reader.GetString(0);
                                outboundShipment.fec_car = reader.GetString(1);
                                outboundShipment.pto_alm = reader.GetInt32(2).ToString();

                                outboundShipmentList.Add(outboundShipment);
                            }
                        }
                    }

                    result.Correct = true;
                    result.Object = outboundShipmentList;
                }
            }
            catch (Exception ex)
            {
                result.Correct = false;
                result.Message = $@"Se obtuvo un error al consultar las cargas pendientes.";
            }
            return result;
        }
        public static ML.Result GetOrdersPerRoute(ML.BaseControl.OutboundShipment outboundShipment, string mode)
        {
            ML.Result result = new ML.Result();
            try
            {
                using (OdbcConnection connection = new OdbcConnection(DL.Connection.GetConnectionStringGen(mode)))
                {
                    connection.Open();

                    string query = $@"SELECT
	                                    TRIM(A.car_sal) AS car_sal,
	                                    A.fec_act,
	                                    TRIM(TO_CHAR(F.cod_cli)||REPLACE(F.nom_cli, ' ', ''))  as cod_cli,
                                        UPPER(TRIM(F.nom_cli)||' '||TRIM(F.ape1_cli)||' '||TRIM(F.ape2_cli)) AS cliente,
	                                    TRIM(A.ord_rel) AS ord_rel,B.num_scn,B.cod_pto,
                                    CASE
	                                    WHEN A.estatus = 0 THEN A.estatus||'-Pendiente'
	                                    WHEN A.estatus = 1 THEN A.estatus||'-Cerrado'
	                                    WHEN A.estatus = 2 THEN A.estatus||'-Creando evento'
	                                    ELSE 'Estado No listado.'
                                    END AS estatus_ruta,
                                    CASE
	                                    WHEN B.estado = 'P' THEN B.estado||C.estado||'-Retenido-Transito'				
	                                    WHEN B.estado = 'T' THEN B.estado||C.estado||'-Transito'
	                                    WHEN B.estado = 'I' THEN B.estado||C.estado||'-Impreso'
	                                    WHEN B.estado = 'X' THEN B.estado||C.estado||'-Cancelado'
	                                    WHEN B.estado = 'C' THEN B.estado||C.estado||'-Cancelado'
	                                    WHEN B.estado = 'E' THEN B.estado||C.estado||'-Entregado'
	                                    WHEN B.estado = 'D' THEN B.estado||C.estado||'-Devuelto'
	                                    WHEN B.estado = 'G' THEN B.estado||C.estado||'-Generado'
	                                    ELSE B.estado||C.estado||'-Estado desconocido'
                                    END AS estatus_gnx,
                                    CASE
	                                    WHEN D.rt_stat IS NULL THEN 'X-Esperando interfaz de evento'
	                                    WHEN D.rt_stat = 1 THEN '1-Interfaz ENTREGA para Genesix generada'
	                                    WHEN D.rt_stat = 2 THEN '2-Generando ASN para WMS'
	                                    WHEN D.rt_stat = 3 THEN '3-ASN Generado'
	                                    WHEN D.rt_stat = 4 THEN '4-Interfaz Retención para Genesix generada'
	                                    WHEN D.rt_stat = 5 THEN '5-Evento marcado por base-Generando transmision'
	                                    ELSE 'Estado No listado.'
                                    END AS estatus_rt,
                                    CASE WHEN (E.des_mot IS NULL)
                                    THEN
	                                    'X-Evento no registrado aun'
                                    ELSE
	                                    E.cod_mot||'-'||TRIM(E.des_mot)
                                    END AS motivo
                                    FROM ora_ruta A
                                    JOIN edc_cab B ON B.cod_emp = 1 AND B.num_scn = A.num_scn
                                    JOIN ordedc_cab C ON C.cod_emp = B.cod_emp AND C.cod_pto = B.cod_pto AND C.num_edc = B.num_edc
                                    LEFT JOIN ora_rt D ON D.cod_emp = B.cod_emp AND D.ord_rel = A.ord_rel
                                    LEFT JOIN ora_motivos_rt E ON E.cod_mot = D.cod_mot
                                    INNER JOIN clientes F ON F.cod_emp = B.cod_emp AND F.cod_cli = B.cod_cli
                                    WHERE A.pto_alm = {outboundShipment.pto_alm}
                                    AND A.car_sal = '{outboundShipment.car_sal}'
                                    ORDER BY A.estatus ASC, D.cod_mot ASC";

                    List<ML.BaseControl.Order> orderList = new List<ML.BaseControl.Order>();

                    OdbcDataAdapter adapter = new OdbcDataAdapter(query, connection);

                    DataTable table = new DataTable();

                    adapter.Fill(table);


                    foreach (DataRow dataRow in table.Rows)
                    {
                        ML.BaseControl.Order order = new ML.BaseControl.Order
                        {
                            car_sal = dataRow["car_sal"].ToString(),
                            fec_act = dataRow["fec_act"].ToString(),
                            cod_cli = dataRow["cod_cli"].ToString(),
                            cliente = dataRow["cliente"].ToString(),
                            ord_rel = dataRow["ord_rel"].ToString(),
                            num_scn = dataRow["num_scn"].ToString(),
                            cod_pto = dataRow["cod_pto"].ToString(),
                            estatus_ruta = dataRow["estatus_ruta"].ToString(),
                            estatus_gnx = dataRow["estatus_gnx"].ToString(),
                            estatus_rt = dataRow["estatus_rt"].ToString(),
                            motivo = dataRow["motivo"].ToString()
                        };

                        orderList.Add(order);
                    }

                    result.Correct = true;
                    result.Object = orderList;
                }
            }
            catch (Exception ex)
            {
                result.Correct = false;
                result.Message = $@"Se obtuvo un error al consultar los viajes.";
            }
            return result;
        }
        public static ML.Result GetOrdersPerData(ML.BaseControl.QueryInfo queryInfo, string mode)   
        {
            ML.Result result = new ML.Result();
            try
            {
                using (OdbcConnection connection = new OdbcConnection(DL.Connection.GetConnectionStringGen(mode)))
                {
                    connection.Open();

                    string[] values = queryInfo.data_info
                            .Split(',', StringSplitOptions.RemoveEmptyEntries)
                            .Select(v => $"'{v.Trim()}'")
                            .ToArray();

                    string inClause = string.Join(",", values);

                    string query = $@"SELECT
	                                    TRIM(A.car_sal) AS car_sal,
	                                    A.fec_act,
	                                    TRIM(TO_CHAR(F.cod_cli)||REPLACE(F.nom_cli, ' ', ''))  as cod_cli,
                                        UPPER(TRIM(F.nom_cli)||' '||TRIM(F.ape1_cli)||' '||TRIM(F.ape2_cli)) AS cliente,
	                                    TRIM(A.ord_rel) AS ord_rel,B.num_scn,B.cod_pto,
                                    CASE
	                                    WHEN A.estatus = 0 THEN A.estatus||'-Pendiente'
	                                    WHEN A.estatus = 1 THEN A.estatus||'-Cerrado'
	                                    WHEN A.estatus = 2 THEN A.estatus||'-Creando evento'
	                                    ELSE 'Estado No listado.'
                                    END AS estatus_ruta,
                                    CASE
	                                    WHEN B.estado = 'P' THEN B.estado||C.estado||'-Retenido-Transito'				
	                                    WHEN B.estado = 'T' THEN B.estado||C.estado||'-Transito'
	                                    WHEN B.estado = 'I' THEN B.estado||C.estado||'-Impreso'
	                                    WHEN B.estado = 'X' THEN B.estado||C.estado||'-Cancelado'
	                                    WHEN B.estado = 'E' THEN B.estado||C.estado||'-Entregado'
	                                    WHEN B.estado = 'D' THEN B.estado||C.estado||'-Devolucion'
	                                    WHEN B.estado = 'G' THEN B.estado||C.estado||'-Generado'
	                                    ELSE B.estado||C.estado||'-Estado desconocido'
                                    END AS estatus_gnx,
                                    CASE
	                                    WHEN D.rt_stat IS NULL THEN 'X-Esperando interfaz de evento'
	                                    WHEN D.rt_stat = 1 THEN '1-Interfaz ENTREGA para Genesix generada'
	                                    WHEN D.rt_stat = 2 THEN '2-Generando ASN para WMS'
	                                    WHEN D.rt_stat = 3 THEN '3-ASN Generado'
	                                    WHEN D.rt_stat = 4 THEN '4-Interfaz Retención para Genesix generada'
	                                    WHEN D.rt_stat = 5 THEN '5-Evento marcado por base-Generando transmision'
	                                    ELSE 'Estado No listado.'
                                    END AS estatus_rt,
                                    CASE WHEN (E.des_mot IS NULL)
                                    THEN
	                                    'X-Evento no registrado aun'
                                    ELSE
	                                    E.cod_mot||'-'||TRIM(E.des_mot)
                                    END AS motivo
                                    FROM ora_ruta A
                                    JOIN edc_cab B ON B.cod_emp = 1 AND B.num_scn = A.num_scn
                                    JOIN ordedc_cab C ON C.cod_emp = B.cod_emp AND C.cod_pto = B.cod_pto AND C.num_edc = B.num_edc
                                    LEFT JOIN ora_rt D ON D.cod_emp = B.cod_emp AND D.ord_rel = A.ord_rel
                                    LEFT JOIN ora_motivos_rt E ON E.cod_mot = D.cod_mot
                                    INNER JOIN clientes F ON F.cod_emp = B.cod_emp AND F.cod_cli = B.cod_cli
                                    WHERE A.pto_alm = {queryInfo.pto_alm}
                                    AND A.{queryInfo.data_type} IN ({inClause})
                                    ORDER BY A.estatus ASC, A.fec_car, A.num_scn";

                    List<ML.BaseControl.Order> orderList = new List<ML.BaseControl.Order>();

                    OdbcDataAdapter adapter = new OdbcDataAdapter(query, connection);

                    DataTable table = new DataTable();

                    adapter.Fill(table);


                    foreach (DataRow dataRow in table.Rows)
                    {
                        ML.BaseControl.Order order = new ML.BaseControl.Order
                        {
                            car_sal = dataRow["car_sal"].ToString(),
                            fec_act = dataRow["fec_act"].ToString(),
                            cod_cli = dataRow["cod_cli"].ToString(),
                            cliente = dataRow["cliente"].ToString(),
                            ord_rel = dataRow["ord_rel"].ToString(),
                            num_scn = dataRow["num_scn"].ToString(),
                            cod_pto = dataRow["cod_pto"].ToString(),
                            estatus_ruta = dataRow["estatus_ruta"].ToString(),
                            estatus_gnx = dataRow["estatus_gnx"].ToString(),
                            estatus_rt = dataRow["estatus_rt"].ToString(),
                            motivo = dataRow["motivo"].ToString()
                        };

                        orderList.Add(order);
                    }

                    result.Correct = true;
                    result.Object = orderList;
                }
            }
            catch (Exception ex)
            {
                result.Correct = false;
                result.Message = $@"Se obtuvo un error al consultar los viajes.";
            }
            return result;
        }
        public static async Task<ML.Result> GetDetails(string ord_rel, string mode)
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

                    Uri uri = new Uri($@"{DL.ApiOracle.GetEndpoint(mode)}".Replace("{########}", ord_rel));

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

        public static async Task<ML.Result> Confirmation(ML.BaseControl.Confirmation confirmation, string mode)
        {
            ML.Result result = new ML.Result();
            try
            {
                confirmation.DeliveryReason = confirmation.DeliveryReason == "0" ? "ENTREGA_OK" : confirmation.DeliveryReason;

                ML.Result resultGetDetails = await GetDetails(confirmation.OrdRel, mode);
                if (!resultGetDetails.Correct)
                {
                    throw resultGetDetails.Ex;
                }
                List<OrderResult> orderDetails = (List<OrderResult>)resultGetDetails.Object;

                ML.Result resultBuildData = BuildData(orderDetails, confirmation, mode);
                if (!resultBuildData.Correct)
                {
                    throw resultBuildData.Ex;
                }
                ML.BaseControl.Interface inter = (ML.BaseControl.Interface)resultBuildData.Object;

                ML.Result resultCreateFile = CreateFile(inter, confirmation.User , mode);
                if (!resultCreateFile.Correct)
                {
                    throw resultCreateFile.Ex;
                }

                ML.Result resultUpdateRuta = UpdateRuta(confirmation.OrdRel, mode);
                if (!resultCreateFile.Correct)
                {
                    //throw resultCreateFile.Ex;
                }

                result.Correct = true;
                result.Message = $@"Interface creada, favor de esperar. {resultUpdateRuta.Message}";
            }
            catch (Exception ex)
            {
                result.Correct = false;
                result.Ex = ex;
                result.Message = $@"Error al generar interface de confirmacion {ex.Message}";
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
                interDetail.Sku = orderResult.Sku.Replace("SRS","");
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
            string fileName = $@"SE_LEGACY_ENTREGA_{user}_{dateTime}.csv";
            string filePath = Path.Combine(DL.Directory.GetOutputPath(mode), fileName);
            //string filePath = Path.Combine("C:\\Users\\Sistemas piso6 3\\Downloads", fileName);
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
                result.Message = "Error al crear el archivo " + fileName + ": " + ex.Message;
                //Console.WriteLine(result.Message);
            }
            return result;
        }
        private static ML.Result UpdateRuta(string ord_rel, string mode)
        {
            ML.Result result = new ML.Result();
            try
            {
                using (OdbcConnection connection = new OdbcConnection(DL.Connection.GetConnectionStringGen(mode)))
                {
                    connection.Open();

                    string query = $@"UPDATE
                                            ora_ruta
                                        SET estatus = 2
                                        WHERE ord_rel = '{ord_rel}'
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

        public static ML.Result GetReasons(string mode)
        {
            ML.Result result = new ML.Result();
            try
            {
                using (OdbcConnection connection = new OdbcConnection(DL.Connection.GetConnectionStringGen(mode)))
                {
                    connection.Open();

                    string query = $@"SELECT cod_mot,
                                                TRIM(asn_tpe) AS asn_tpe,
                                                TRIM(des_mot) AS des_mot
                                        FROM ora_motivos_rt
                                        ";

                    List<ML.BaseControl.Reason> reasonList = new List<ML.BaseControl.Reason>();

                    using (OdbcCommand cmd = new OdbcCommand(query, connection))
                    {
                        using (OdbcDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                ML.BaseControl.Reason reason = new ML.BaseControl.Reason();

                                reason.cod_mot = reader.GetInt32(0).ToString(); 
                                reason.asn_tpe = reader.GetString(1);
                                reason.des_mot = reader.GetString(2);

                                reasonList.Add(reason);
                            }
                        }
                    }

                    result.Correct = true;
                    result.Object = reasonList;
                }
            }
            catch (Exception ex)
            {
                result.Correct = false;
                result.Message = $@"No se pudieron recuperar los motivos";
            }
            return result;
        }

        public static async Task<ML.Result> MultiConfirmationsOk(string user, string shipment, string cod_pto, string mode)
        {
            ML.Result result = new ML.Result();
            try
            {

                ML.Result resultGetConfirmations = await GetConfirmationsOK(user, shipment, cod_pto, mode);
                if (!resultGetConfirmations.Correct)
                {
                    throw new Exception(resultGetConfirmations.Message);
                }
                List<ML.BaseControl.Confirmation> confirmationList = (List<ML.BaseControl.Confirmation>)resultGetConfirmations.Object;

                int interfaCount = 0;
                foreach(ML.BaseControl.Confirmation confirmation in confirmationList)
                {
                    Thread.Sleep(20);

                    ML.Result resultConfirmationOK = await ConfirmationOK(confirmation, mode);
                    if (!resultConfirmationOK.Correct)
                    {
                        throw resultConfirmationOK.Ex;
                    }
                    interfaCount++;
                }

                result.Correct = true;
                result.Message = $@"{interfaCount} Interfaces creadas, favor de esperar";
            }
            catch (Exception ex)
            {
                result.Correct = false;
                result.Ex = ex;
                result.Message = $@"Error mientras se generaban multiples interfaces {ex.Message}";
            }
            return result;
        }

        private static async Task<ML.Result> GetConfirmationsOK(string user, string shipment, string cod_pto, string mode)
        {
            ML.Result result = new ML.Result();
            try
            {
                List<ML.BaseControl.Confirmation> confirmationList = new List<ML.BaseControl.Confirmation>();

                using (OdbcConnection connection = new OdbcConnection(DL.Connection.GetConnectionStringGen(mode)))
                {
                    connection.Open();

                    string query = $@"SELECT TRIM(ord_rel)
                                        FROM ora_ruta
                                        WHERE pto_alm = {cod_pto}
                                        AND estatus = 0";

                    using (OdbcCommand command = new OdbcCommand(query, connection))
                    {
                        using(OdbcDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                ML.BaseControl.Confirmation confirmation = new ML.BaseControl.Confirmation();

                                confirmation.OrdRel = reader.GetString(0);
                                confirmation.Shipment = shipment;
                                confirmation.DeliveryReason = "0";
                                confirmation.User = user;

                                confirmationList.Add(confirmation);
                            }
                        }
                    }
                }

                result.Correct = true;
                result.Message = $@"";
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
        private static async Task<ML.Result> ConfirmationOK(ML.BaseControl.Confirmation confirmation, string mode)
        {
            ML.Result result = new ML.Result();
            try
            {
                ML.Result resultGetDetails = await GetDetails(confirmation.OrdRel, mode);
                if (!resultGetDetails.Correct)
                {
                    throw resultGetDetails.Ex;
                }
                List<OrderResult> orderDetails = (List<OrderResult>)resultGetDetails.Object;

                ML.Result resultBuildData = BuildData(orderDetails, confirmation, mode);
                if (!resultBuildData.Correct)
                {
                    throw resultBuildData.Ex;
                }
                ML.BaseControl.Interface inter = (ML.BaseControl.Interface)resultBuildData.Object;

                ML.Result resultCreateFile = CreateFile(inter, confirmation.User, mode);
                if (!resultCreateFile.Correct)
                {
                    throw resultCreateFile.Ex;
                }

                ML.Result resultUpdateRuta = UpdateRuta(confirmation.OrdRel, mode);
                if (!resultCreateFile.Correct)
                {
                    //throw resultCreateFile.Ex;
                }

                result.Correct = true;
                result.Message = $@"Interface creada, favor de esperar. {resultUpdateRuta.Message}";
            }
            catch (Exception ex)
            {
                result.Correct = false;
                result.Ex = ex;
                result.Message = $@"Error al generar interface de confirmacion de {confirmation.OrdRel}: {ex.Message}";
            }
            return result;
        }
    }
}
