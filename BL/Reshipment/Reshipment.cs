using ML.AsnHelper;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data.Odbc;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace BL.Reshipment
{
    public class Reshipment
    {
        //1
        public static ML.Result GetReshipments(string ptoAlm, string mode)
        {
            ML.Result result = new ML.Result();
            try
            {
                ML.Result resultGetOracleCode = GetOracleCode(ptoAlm, mode);
                if (!resultGetOracleCode.Correct)
                {
                    throw new Exception($@"{resultGetOracleCode.Message}");
                }
                string facility = (string)resultGetOracleCode.Object;

                using (var client = new HttpClient())
                {
                    var byteArray = Encoding.ASCII.GetBytes($@"{DL.ApiOracle.GetOracleUsr(mode)}:{DL.ApiOracle.GetOraclePwd(mode)}");
                    client.DefaultRequestHeaders.Authorization =
                        new AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));

                    string baseUrl = $"{DL.ApiOracle.GetLoads(mode).Replace("{facility}", facility)}";

                    List<ML.Reshipment.Reshipment> allReshipments = new List<ML.Reshipment.Reshipment>();

                    string currentUrl = baseUrl;
                    int pageCount = 0;

                    do
                    {
                        var response = client.GetAsync(currentUrl).Result;
                        var json = response.Content.ReadAsStringAsync().Result;

                        var pageData = Newtonsoft.Json.JsonConvert.DeserializeObject<ApiResponse>(json);

                        if (pageData != null && pageData.results != null && pageData.results.Count > 0)
                        {
                            foreach (var item in pageData.results)
                            {
                                System.Diagnostics.Debug.WriteLine($"IdCarga: {item.IdCarga}, IdEstatusCarga recibido: {item.IdEstatusCarga}");
                                var reshipment = new ML.Reshipment.Reshipment
                                {   
                                    Facility = facility,
                                    IdCarga = item.IdCarga.ToString(),
                                    CargaSalida = item.CargaSalida,
                                    IdEstatusCarga = item.IdEstatusCarga.ToString(),
                                    EstatusCarga = GetStatusDescription(item.IdEstatusCarga),
                                    SigAlmacen = item.SigAlmacen ?? ""
                                };
                                allReshipments.Add(reshipment);
                            }

                            pageCount++;
                            currentUrl = pageData.next_page;
                        }
                        else
                        {
                            break; // No hay más datos
                        }

                    } while (!string.IsNullOrEmpty(currentUrl));

                    result.Object = allReshipments;
                }
                result.Correct = true;
            }
            catch (Exception ex)
            {
                result.Correct = false;
                result.Message = $@"Error al obtener cargas {ex.Message}";
            }
            return result;
        }
        private static ML.Result GetOracleCode(string cod_pto, string mode) 
        {
            ML.Result result = new ML.Result();
            try
            {
                using (OdbcConnection connection = new OdbcConnection(DL.Connection.GetConnectionStringGen(mode)))
                {
                    connection.Open();

                    string query = $@"SELECT CASE WHEN (B.facility IS NULL)
                                            THEN
                                                    'SRS'||A.cod_pto
                                            ELSE
                                                    TRIM(B.facility)
                                            END
                                    FROM puntos A,OUTER ora_fac_go B
                                    WHERE A.cod_emp = 1
                                    AND A.cod_pto = {cod_pto}
                                    AND B.cen_pto = A.cod_pto
                                    AND B.is_fac = 'T'";

                    using (OdbcCommand cmd = new OdbcCommand(query, connection))
                    {
                        using (OdbcDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                string originCode = reader.GetString(0);
                                result.Object = originCode;
                            }
                            else
                            {
                                throw new Exception("No se leyó el origin code");
                            }
                        }
                    }
                    result.Correct = true;
                }
            }
            catch (Exception ex)
            {
                result.Correct = false;
                result.Message = "Error al obtener el origin code " + ex.Message;
                result.Ex = ex;
            }
            return result;
        }
        private static string GetStatusDescription(int statusId)
        {
            switch (statusId)
            {
                case 0:
                    return "Creada";
                case 10:
                    return "Comienzo registrado";
                case 30:
                    return "Carga iniciada";
                case 40:
                    return "Cierre de carga en progreso";
                case 50:
                    return "X-Cargada";
                case 80:
                    return "Registrada salida";
                case 85:
                    return "Carga en progreso";
                case 90:
                    return "Enviada";
                case 99:
                    return "Cancelada";
                default:
                    return $"Desconocido ({statusId})";
            }
        }
        //2
        public static ML.Result GetFacilities(string facility, string mode)
        {
            ML.Result result = new ML.Result();
            try
            {
                using (OdbcConnection connection = new OdbcConnection(DL.Connection.GetConnectionStringGen(mode)))
                {
                    connection.Open();

                    string query = $@"SELECT TRIM(A.facility),TRIM(B.des_pto)
                                    FROM ora_fac_go A, puntos B
                                    WHERE A.facility NOT IN ('{facility}')
                                    AND B.cod_emp = 1
                                    AND B.cod_pto = A.cen_pto
                                    ";

                    List<(string facility, string desc)> facilityList = new List<(string facility, string desc)>();

                    using (OdbcCommand cmd = new OdbcCommand(query, connection))
                    {
                        using (OdbcDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                string fac = reader.GetString(0);
                                string des = reader.GetString(1);

                                facilityList.Add((fac, des));
                            }
                        }
                    }

                    var facilitiesList = facilityList.Select(f => new {
                        facility = f.facility,
                        desc = f.desc
                    }).ToList();

                    result.Correct = true;
                    result.Message = $@"Se recuperaron almacenes";
                    result.Object = facilitiesList;
                }
            }
            catch (Exception ex)
            {
                result.Correct = false;
                result.Message = $@"Error al obtener cargas {ex.Message}";
            }
            return result;
        }
        //3
        public static async Task<ML.Result> PatchLoadById(ML.Reshipment.Reshipment reshipment, string mode)
        {
            ML.Result result = new ML.Result();

            try
            {
                using (HttpClient client = new HttpClient())
                {
                    byte[] byteArray = Encoding.ASCII.GetBytes($"{DL.ApiOracle.GetOracleUsr(mode)}:{DL.ApiOracle.GetOraclePwd(mode)}");

                    client.DefaultRequestHeaders.Authorization =
                        new AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));

                    string url = $"{DL.ApiOracle.PatchLoadById(mode).Replace("{id}", reshipment.IdCarga)}";

                    var body = new
                    {
                        fields = new
                        {
                            cust_field_2 = reshipment.SigAlmacen
                        }
                    };

                    var json = JsonConvert.SerializeObject(body);

                    HttpRequestMessage request = new HttpRequestMessage(new HttpMethod("PATCH"), url)
                    {
                        Content = new StringContent(json, Encoding.UTF8, "application/json")
                    };

                    var response = await client.SendAsync(request);

                    if (!response.IsSuccessStatusCode)
                    {
                        result.Correct = false;
                        string messageResponse = await response.Content.ReadAsStringAsync();
                        throw new Exception(messageResponse);
                    }
                }

                result.Correct = true;
                result.Message = $@"Se actualizó el siguiente almacen de la carga {reshipment.CargaSalida}";
            }
            catch (Exception ex)
            {
                result.Correct = false;
                result.Message = $@"Error al actualizar campos en WMS para carga {reshipment.CargaSalida}: {ex.Message}";
            }

            return result;
        }        
        //4
        public static async Task<ML.Result> ShipReshipment(ML.Reshipment.Reshipment reshipment, string user, string mode)
        {
            ML.Result result = new ML.Result();

            try
            {
                ML.Result resultCheckLoadStatus = CheckLoadStatus(reshipment.Facility, reshipment.CargaSalida, mode);
                if (!resultCheckLoadStatus.Correct)
                {
                    throw new Exception($@"{resultCheckLoadStatus.Message}");
                }

                using (HttpClient client = new HttpClient())
                {
                    byte[] byteArray = Encoding.ASCII.GetBytes($"{DL.ApiOracle.GetOracleUsr(mode)}:{DL.ApiOracle.GetOraclePwd(mode)}");

                    client.DefaultRequestHeaders.Authorization =
                        new AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));

                    string url = $"{DL.ApiOracle.ShipReshipment(mode).Replace("{id}", reshipment.IdCarga)}";

                    var body = new
                    {
                        parameters = new
                        {
                            facility_id__code = reshipment.Facility,
                            company_id__code = "GPOSAN",
                            load_nbr = reshipment.CargaSalida
                        }
                    };

                    var json = JsonConvert.SerializeObject(body);

                    HttpRequestMessage request = new HttpRequestMessage(new HttpMethod("POST"), url)
                    {
                        Content = new StringContent(json, Encoding.UTF8, "application/json")
                    };

                    var response = await client.SendAsync(request);

                    if (!response.IsSuccessStatusCode)
                    {
                        result.Correct = false;
                        string messageResponse = await response.Content.ReadAsStringAsync();
                        throw new Exception(messageResponse);
                    }
                }

                ML.Result resultTrackingShip = TrackingShip(reshipment, user, mode);
                if (!resultTrackingShip.Correct)
                {
                    throw new Exception($@"{resultTrackingShip.Message}");
                }
                
                result.Correct = true;
                result.Message = $@"Se envio exitosamente la carga {reshipment.CargaSalida}";
            }
            catch (Exception ex)
            {
                result.Correct = false;
                result.Message = $@"Error al enviar Carga {reshipment.CargaSalida}: {ex.Message}";
            }

            return result;
        }
        private static ML.Result CheckLoadStatus(string facility, string loadNbr, string mode)
        {
            ML.Result result = new ML.Result();
            try
            {
                using (var client = new HttpClient())
                {
                    var byteArray = Encoding.ASCII.GetBytes($"{DL.ApiOracle.GetOracleUsr(mode)}:{DL.ApiOracle.GetOraclePwd(mode)}");
                    client.DefaultRequestHeaders.Authorization =
                        new AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));

                    
                    string baseUrl = $"{DL.ApiOracle.GetIdStatusByLoad(mode).Replace("{facility}", facility).Replace("{load}", loadNbr)}";

                    var response = client.GetAsync(baseUrl).Result;
                    var json = response.Content.ReadAsStringAsync().Result;

                    dynamic data = Newtonsoft.Json.JsonConvert.DeserializeObject(json);
                    if (data?.results != null && data.results.Count > 0)
                    {
                        int statusId = data.results[0].IdEstatusCarga;
                        if (statusId == 50)
                        {
                            result.Correct = true;
                            result.Message = "La carga tiene estatus Cargada (50).";
                            return result;
                        }
                        else
                        {
                            result.Correct = false;
                            result.Message = $"La carga tiene estatus {statusId} ({GetStatusDescription(statusId)}). Se requiere estatus 50 (Cargada).";
                            return result;
                        }
                    }
                    else
                    {
                        result.Correct = false;
                        result.Message = "No se encontró la carga o la respuesta no contiene resultados.";
                        return result;
                    }
                }
            }
            catch (Exception ex)
            {
                result.Correct = false;
                result.Message = $"Error al verificar estatus de carga: {ex.Message}";
                return result;
            }
        }
        private static ML.Result TrackingShip(ML.Reshipment.Reshipment reshipment, string user, string mode)
        {
            ML.Result result = new ML.Result();
            try
            {
                using (OdbcConnection connection = new OdbcConnection(DL.Connection.GetConnectionStringGen(mode)))
                {
                    connection.Open();

                    string query = $@"INSERT INTO hermes_reshipment(car_sal,facility_ori,facility_des,usu_id,fec_send)
	                                    VALUES ('{reshipment.CargaSalida}','{reshipment.Facility}','{reshipment.SigAlmacen}',{user},CURRENT)
                                    ";


                    using (OdbcCommand cmd = new OdbcCommand(query, connection))
                    {
                        int rowsAffected = cmd.ExecuteNonQuery();
                        if(rowsAffected < 1)
                        {
                            throw new Exception($@"Sin registros afectados");
                        }
                    }

                    result.Correct = true;
                    result.Message = $@"Se Inserto el seguimiento";
                }
            }
            catch (Exception ex)
            {
                result.Correct = false;
                result.Message = $@"Error al Insertar seguimiento {ex.Message}";
            }
            return result;
        }
    }
}
