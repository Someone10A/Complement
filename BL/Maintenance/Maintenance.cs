using System;
using System.Collections.Generic;
using System.Data.Odbc;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Xml;

namespace BL.Maintenance
{
    public class Maintenance
    {
        /*
        SELECT FIRST 1 C.cod_fam2
                            FROM edc_det A, arti C
                            WHERE EXISTS(
                                            SELECT 1
                                            FROM edc_cab B
                                            WHERE B.cod_emp = A.cod_emp
                                            AND B.cod_pto = A.cod_pto
                                            AND B.num_edc = A.num_edc
                                            AND B.tip_ent = 1
                                            AND B.num_scn = '0106003763170807'
                            )
                            AND A.cod_emp = 1
                            AND C.cod_emp = A.cod_emp
                            AND C.int_art = A.int_art
        */
        public static ML.Result GetScnInfo(string numScn, string mode)
        {
            ML.Result result = new ML.Result();
            try
            {
                using (OdbcConnection connection = new OdbcConnection(DL.Connection.GetConnectionStringGen(mode)))
                {
                    connection.Open();

                    string query = $@"SELECT FIRST 1
	                                    A.num_scn,
	                                    A.pto_alm,
	                                    A.cod_pto,
	                                    A.num_edc,
	                                    CASE WHEN (G.intentos IS NULL)
	                                    THEN
		                                    A.cod_pto||A.num_edc||'000'
	                                    ELSE
		                                    A.cod_pto||A.num_edc||LPAD(TO_CHAR(G.intentos), 3, '0')
	                                    END AS ord_rel,
	                                    A.estado AS estado1,
	                                    B.estado AS estado2,
	                                    A.fec_ent,
	                                    A.fec_cli,
	                                    B.fec_ent_r,
	                                    A.cod_cli,
	                                    A.cod_dir,
	                                    A.tel_cli,
	                                    F.tel_cli1,
	                                    F.tel_cli2,
	                                    CASE
		                                    WHEN (C.estatus IS NULL) THEN 'NO DISPONIBLE'
		                                    WHEN (C.estatus = 0) THEN 'DISPONIBLE'
		                                    WHEN (C.estatus = 1) THEN 'ENVIADO'
		                                    WHEN (C.estatus = 2) THEN 'EN PROCESO DE ENVIO'
		                                    ELSE 'ESTATUS NO LISTADO '||C.estatus
	                                    END AS maintenance,
	                                    TRIM(D.nom_cli) AS nom_cli,
	                                    TRIM(D.ape1_cli) AS ap1_cli,
	                                    TRIM(D.ape2_cli) AS ap2_cli,
	                                    TRIM(F.num_int) AS num_int,
	                                    TRIM(F.num_ext) AS num_ext,
	                                    TRIM(F.dir_cli) AS calle,
	                                    TRIM(F.col_cli) AS colonia,
	                                    TRIM(F.pob_cli) AS municipio,
	                                    CASE WHEN (H.nom_est IS NULL) 
	                                    THEN
		                                    TRIM(F.pro_cli)
	                                    ELSE
		                                    TRIM(H.nom_est)
	                                    END AS estado,
	                                    F.cp_cli AS cod_pos,
	                                    TRIM(F.ent_calles) AS referencias,
	                                    TRIM(F.ent_calles2) AS observaciones,
	                                    F.panel,
	                                    F.volado,
	                                    F.mas_gen,
	                                    CASE WHEN(I.longitud IS NULL)
	                                    THEN
		                                    0
	                                    ELSE
		                                    I.longitud
	                                    END AS longitud,
	                                    CASE WHEN (I.latitud IS NULL)
	                                    THEN
		                                    0
	                                    ELSE
		                                    I.latitud
	                                    END AS latitud,
	                                    CASE WHEN (J.num_scn IS NULL)
	                                    THEN
		                                    'NO'
	                                    ELSE
		                                    'SI'
	                                    END AS is_rdd,
	                                    CASE WHEN (J.status IS NULL)
	                                    THEN
			                                    'NO'
	                                    ELSE
			                                    'SI'
	                                    END AS is_rdd_send,
	                                    K.num_rdd,
                                        'NO' AS is_confirmed,
                                        'NO' AS in_plan,
	                                    CASE WHEN (N.num_scn IS NULL)
	                                    THEN
		                                    'NO'
	                                    ELSE
		                                    TRIM(N.car_sal)
	                                    END AS in_route
                                    FROM edc_cab A, 
	                                    ordedc_cab B,
	                                    OUTER ora_mantenimiento C,
	                                    clientes D,
	                                    cli_dir E, 
	                                    cli_direccion F,
	                                    OUTER ora_rt_envio G,
	                                    OUTER cat_est H,
	                                    OUTER cli_coord I,
	                                    OUTER ora_integra_envio J,
	                                    OUTER rdd_cab K,
	                                    OUTER ora_ruta N
                                    WHERE A.cod_emp = 1
                                    AND A.tip_ent = 1
                                    AND A.num_scn = '{numScn}'
                                    AND B.cod_emp = A.cod_emp
                                    AND B.cod_pto = A.cod_pto
                                    AND B.num_edc = A.num_edc
                                    AND C.cod_emp = A.cod_emp
                                    AND C.cod_pto = A.cod_pto
                                    AND C.num_edc = A.num_edc
                                    AND D.cod_emp = A.cod_emp
                                    AND D.cod_cli = A.cod_cli
                                    AND E.cod_emp = A.cod_emp
                                    AND E.cod_cli = A.cod_cli
                                    AND F.cod_emp = A.cod_emp
                                    AND F.cod_dir = E.cod_dir
                                    AND F.cod_dir = A.cod_dir
                                    AND G.cod_pto = A.cod_pto
                                    AND G.num_edc = A.num_edc
                                    AND H.cve_est = F.pro_cli
                                    AND I.cod_emp = A.cod_emp
                                    AND I.cod_cli = A.cod_cli
                                    AND J.tipo = 'RD'
                                    AND J.num_scn = A.num_scn
                                    AND K.cod_emp = A.cod_emp
                                    AND K.pto_alm = A.pto_alm
                                    AND K.scn_nvo = A.num_scn
                                    AND N.pto_alm = A.pto_alm
                                    AND N.num_scn = A.num_scn
                                    AND N.estatus = 0";

                    ML.Maintenance.InfoByScn scnInfo = new ML.Maintenance.InfoByScn();

                    using (OdbcCommand cmd = new OdbcCommand(query, connection))
                    {
                        using (OdbcDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                scnInfo = new ML.Maintenance.InfoByScn
                                {
                                    NumScn = reader["num_scn"].ToString(),
                                    PtoAlm = reader["pto_alm"].ToString(),
                                    CodPto = reader["cod_pto"].ToString(),
                                    NumEdc = reader["num_edc"].ToString(),
                                    OrdRel = reader["ord_rel"].ToString(),

                                    Estado1 = reader["estado1"].ToString(),
                                    Estado2 = reader["estado2"].ToString(),

                                    FecEnt = reader["fec_ent"] != DBNull.Value ? Convert.ToDateTime(reader["fec_ent"]).ToString("dd/MM/yyyy") : null,
                                    FecCli = reader["fec_cli"] != DBNull.Value ? Convert.ToDateTime(reader["fec_cli"]).ToString("dd/MM/yyyy") : null,
                                    FecEntR = reader["fec_ent_r"] != DBNull.Value ? Convert.ToDateTime(reader["fec_ent_r"]).ToString("dd/MM/yyyy") : null,

                                    CodCli = reader["cod_cli"].ToString(),
                                    CodDir = reader["cod_dir"].ToString(),

                                    TelCli = reader["tel_cli"]?.ToString(),
                                    TelCli1 = reader["tel_cli1"]?.ToString(),
                                    TelCli2 = reader["tel_cli2"]?.ToString(),

                                    Maintenance = reader["maintenance"].ToString(),

                                    NomCli = reader["nom_cli"]?.ToString(),
                                    Ape1Cli = reader["ap1_cli"]?.ToString(),
                                    Ape2Cli = reader["ap2_cli"]?.ToString(),

                                    NumInt = reader["num_int"]?.ToString(),
                                    NumExt = reader["num_ext"]?.ToString(),
                                    Calle = reader["calle"]?.ToString(),
                                    Colonia = reader["colonia"]?.ToString(),
                                    Municipio = reader["municipio"]?.ToString(),
                                    Estado = reader["estado"]?.ToString(),
                                    CodPos = reader["cod_pos"]?.ToString(),

                                    Referencias = reader["referencias"]?.ToString(),
                                    Observaciones = reader["observaciones"]?.ToString(),

                                    Panel = reader["panel"]?.ToString(),
                                    Volado = reader["volado"]?.ToString(),
                                    MasGen = reader["mas_gen"]?.ToString(),

                                    Longitud = reader["longitud"].ToString(),
                                    Latitud = reader["latitud"].ToString(),

                                    IsRdd = reader["is_rdd"].ToString(),
                                    IsRddSend = reader["is_rdd_send"].ToString(),
                                    NumRdd = reader["num_rdd"]?.ToString(),

                                    IsConfirmed = reader["is_confirmed"].ToString(),
                                    InPlan = reader["in_plan"].ToString(),
                                    InRoute = reader["in_route"].ToString(),
                                    ApiRequestWMS = null
                                };
                            }
                            else
                            {
                                throw new Exception($@"Sin registros leidos");
                            }
                        }
                    }

                    ML.Result resultGetScnDetail = GetScnDetail(numScn, connection);
                    if (!resultGetScnDetail.Correct)
                    {
                        throw new Exception($@"{resultGetScnDetail.Message}");
                    }
                    scnInfo.Details = (List<ML.Maintenance.Detail>)resultGetScnDetail.Object;

                    // Obtener información de Oracle WMS si hay OrdRel
                    if (!string.IsNullOrEmpty(scnInfo.OrdRel))
                    {
                        scnInfo.ApiRequestWMS = GetOracleWMSInfo(scnInfo.OrdRel);
                    }

                    result.Correct = true;
                    result.Object = scnInfo;
                }
            }
            catch (Exception ex)
            {
                result.Correct = false;
                result.Message = $@"Error al obtener la información del SCN {numScn}: {ex.Message}";
                result.Ex = ex;
            }
            return result;
        }
        private static ML.Result GetScnDetail(string numScn, OdbcConnection connection)
        {
            ML.Result result = new ML.Result();
            try
            {
                string query = $@"SELECT TRIM(A.int_art) AS int_art,
                                                TRIM(C.des_art) AS des_art,
                                                TO_CHAR(A.uni_mov) AS uni_mov
                                        FROM edc_det A, arti C
                                        WHERE EXISTS (
                                                SELECT 1
                                                FROM edc_cab B
                                                WHERE B.cod_emp = A.cod_emp
                                                AND B.cod_pto = A.cod_pto
                                                AND B.num_edc = A.num_edc
                                                AND B.tip_ent = 1
                                                AND B.num_scn = '{numScn}'
                                        )
                                        AND A.cod_emp = 1
                                        AND C.cod_emp = A.cod_emp
                                        AND C.int_art = A.int_art
                                        ";

                List<ML.Maintenance.Detail> details = new List<ML.Maintenance.Detail>();

                using (OdbcCommand cmd = new OdbcCommand(query, connection))
                {
                    using (OdbcDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            ML.Maintenance.Detail detail = new ML.Maintenance.Detail();

                            detail.Sku = reader.GetString(0);
                            detail.Descripcion = reader.GetString(1);
                            detail.Piezas = reader.GetString(2);

                            details.Add(detail);
                        }
                    }
                }

                if(details.Count == 0)
                {
                    throw new Exception($@"No se encontraron detalles en el SCN");
                }

                result.Correct = true;
                result.Object = details;
            }
            catch (Exception ex)
            {
                result.Correct = false;
                result.Message = $@"Error al obtener la info del detalle: {ex.Message}";
                result.Ex = ex;
            }
            return result;
        }

        private static ML.Maintenance.ApiRequestWMS GetOracleWMSInfo(string orderNumber)
        {
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    client.Timeout = TimeSpan.FromSeconds(10);

                    // Usar credenciales siempre en PRO
                    string username = DL.ApiOracle.GetOracleUsr("PRO");
                    string password = DL.ApiOracle.GetOraclePwd("PRO");
                    var byteArray = Encoding.ASCII.GetBytes($"{username}:{password}");
                    client.DefaultRequestHeaders.Accept.Clear();
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/xml"));
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));

                    // Construir URL con número de orden, siempre en PRO
                    string url = DL.ApiOracle.GetInfoWMSPro(orderNumber);
                    Uri uri = new Uri(url);

                    System.Console.WriteLine($"[BL] GetOracleWMSInfo - Consultando Oracle WMS para orden: {orderNumber}");
                    System.Console.WriteLine($"[BL] GetOracleWMSInfo - URL: {url}");

                    HttpResponseMessage response = client.GetAsync(uri).Result;

                    System.Console.WriteLine($"[BL] GetOracleWMSInfo - Status Code: {(int)response.StatusCode} - {response.StatusCode}");

                    if (response.IsSuccessStatusCode)
                    {
                        string xmlString = response.Content.ReadAsStringAsync().Result;
                        System.Console.WriteLine($"[BL] GetOracleWMSInfo - Respuesta XML recibida (longitud: {xmlString.Length})");
                        
                        // Parsear XML para extraer información
                        XmlDocument xmlDoc = new XmlDocument();
                        xmlDoc.LoadXml(xmlString);

                        ML.Maintenance.ApiRequestWMS apiRequest = new ML.Maintenance.ApiRequestWMS();
                        apiRequest.Successes = true;

                        // Los datos están dentro de list-item dentro de results
                        XmlNodeList listItemNodes = xmlDoc.SelectNodes("//list-item");
                        if (listItemNodes != null && listItemNodes.Count > 0)
                        {
                            XmlNode listItem = listItemNodes[0];

                            XmlNode orderNumberNode = listItem.SelectSingleNode("OrderNumber");
                            if (orderNumberNode != null)
                            {
                                apiRequest.OrderNumber = orderNumberNode.InnerText;
                            }

                            XmlNode idEstatusNode = listItem.SelectSingleNode("IdEstatus");
                            if (idEstatusNode != null)
                            {
                                if (int.TryParse(idEstatusNode.InnerText, out int idEstatus))
                                {
                                    apiRequest.IdEstatus = idEstatus;
                                    System.Console.WriteLine($"[BL] GetOracleWMSInfo - IdEstatus extraído: {idEstatus}");
                                }
                            }

                            XmlNode descEstatusNode = listItem.SelectSingleNode("DescEstatus");
                            if (descEstatusNode != null)
                            {
                                apiRequest.DescEstatus = descEstatusNode.InnerText;
                            }

                            XmlNode envioBloqueadoNode = listItem.SelectSingleNode("EnvioBloqueado");
                            if (envioBloqueadoNode != null)
                            {
                                if (bool.TryParse(envioBloqueadoNode.InnerText, out bool envioBloqueado))
                                {
                                    apiRequest.EnvioBloqueado = envioBloqueado;
                                }
                            }
                        }

                        return apiRequest;
                    }
                    else
                    {
                        System.Console.WriteLine($"[BL] GetOracleWMSInfo - Error al hacer la petición: {(int)response.StatusCode} - {response.ReasonPhrase}");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"[BL] GetOracleWMSInfo - Error al obtener información de Oracle WMS: {ex.Message}");
            }
            return null;
        }

        public static ML.Result GetTope(string date, string ent, string mode)
        {
            ML.Result result = new ML.Result();
            try
            {
                using (OdbcConnection connection = new OdbcConnection(DL.Connection.GetConnectionStringGen(mode)))
                {
                    connection.Open();

                    string query = $@"SELECT num_tope
                                        FROM edc_tope
                                        WHERE cod_emp = 1
                                        AND pto_alm = 870
                                        AND ent = 'L'
                                        ";

                    ML.Maintenance.InfoByScn scnInfo = new ML.Maintenance.InfoByScn();

                    using (OdbcCommand cmd = new OdbcCommand(query, connection))
                    {
                        using (OdbcDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {

                            }
                        }
                    }

                    result.Correct = true;
                    result.Object = scnInfo;
                }
            }
            catch (Exception ex)
            {
                result.Correct = false;
                result.Message = $@"Error al obtener el tope: {ex.Message}";
                result.Ex = ex;
            }
            return result;
        }

        public static ML.Result CheckOraMantenimiento(string codPto, string numEdc, string numScn, string mode)
        {
            ML.Result result = new ML.Result();
            try
            {
                using (OdbcConnection connection = new OdbcConnection(DL.Connection.GetConnectionStringGen(mode)))
                {
                    connection.Open();

                    string query = $@"SELECT * FROM ora_mantenimiento 
                                      WHERE cod_pto = '{codPto}' 
                                      AND num_edc = '{numEdc}'";

                    using (OdbcCommand cmd = new OdbcCommand(query, connection))
                    {
                        using (OdbcDataReader reader = cmd.ExecuteReader())
                        {
                            bool exists = reader.Read();
                            System.Console.WriteLine($"\n[BL] CheckOraMantenimiento - SCN {numScn} exists: {exists}\n");
                            
                            result.Correct = true;
                            result.Object = exists;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                result.Correct = false;
                result.Message = $@"Error al verificar ora_mantenimiento: {ex.Message}";
                result.Ex = ex;
                System.Console.WriteLine($"[BL] CheckOraMantenimiento - Error: {ex.Message}");
            }
            return result;
        }

        public static ML.Result UpdateScnInfo(ML.Maintenance.ConfirmedInfoByScn confirmedInfo, ML.Maintenance.InfoByScn originalInfo, string mode, string usuId = "")
        {
            ML.Result result = new ML.Result();
            OdbcTransaction transaction = null;
            
            try
            {
                // Validar que se recibió la información original
                if (originalInfo == null || string.IsNullOrEmpty(originalInfo.NumScn))
                {
                    throw new Exception("La información original del SCN no fue proporcionada");
                }
                
                // Obtener OrdRel de InfoByScn y quitar los últimos 3 dígitos para uso en tablas LGA
                string ordRelValue = originalInfo.OrdRel ?? "";
                string ordRelWithoutLastThree = ordRelValue.Length > 3 ? ordRelValue.Substring(0, ordRelValue.Length - 3) : ordRelValue;
                
                // Determinar escenario basado en cambios reales
                int scenario = DetermineScenario(confirmedInfo, originalInfo);
                System.Console.WriteLine($"[BL] UpdateScnInfo - Escenario determinado: {scenario} para SCN: {confirmedInfo.NumScn}");
                
                using (OdbcConnection connection = new OdbcConnection(DL.Connection.GetConnectionStringGen(mode)))
                {
                    connection.Open();
                    transaction = connection.BeginTransaction();

                    try
                    {
                        // Ejecutar actualizaciones basadas en el escenario
                        switch (scenario)
                        {
                            case 1: // posfechado
                                ExecuteScenario1(confirmedInfo, connection, transaction);
                                break;
                            case 2: // cambio de dirección con posfechado
                                ExecuteScenario2(confirmedInfo, connection, transaction);
                                break;
                            case 3: // cambio de dirección
                                ExecuteScenario3(confirmedInfo, connection, transaction);
                                break;
                        }

                        // Actualizar ora_mantenimiento.estatus = 2 solo si IsConfirmed es true
                        if (confirmedInfo.IsConfirmed)
                        {
                            UpdateOraMantenimientoEstatus(originalInfo, connection, transaction);
                        }

                        // Actualizar ora_integra_envio si el InfoByScn original indica RDD
                        if (!string.IsNullOrEmpty(originalInfo.IsRdd) && originalInfo.IsRdd.Equals("SI", StringComparison.OrdinalIgnoreCase))
                        {
                            UpdateOraIntegraEnvio(confirmedInfo, connection, transaction);
                        }

                        // Verificar y actualizar/insertar ora_confirmacion solo si IsConfirmed es true
                        if (confirmedInfo.IsConfirmed)
                        {
                            CheckAndUpdateOraConfirmacion(confirmedInfo, originalInfo, connection, transaction, usuId);
                        }

                        transaction.Commit();
                        System.Console.WriteLine($"[BL] UpdateScnInfo - Transacción confirmada exitosamente para SCN: {confirmedInfo.NumScn}");

                        // Ejecutar actualizaciones LGA (fuera de transacción según requisitos)
                        ExecuteLgaUpdates(confirmedInfo, connection, scenario, ordRelWithoutLastThree);

                        result.Correct = true;
                        result.Object = $"SCN {confirmedInfo.NumScn} updated successfully";
                    }
                    catch (Exception ex)
                    {
                        if (transaction != null)
                        {
                            transaction.Rollback();
                            System.Console.WriteLine($"[BL] UpdateScnInfo - Transaction rolled back for SCN: {confirmedInfo.NumScn}");
                        }
                        throw;
                    }
                }
            }
            catch (Exception ex)
            {
                result.Correct = false;
                result.Message = $@"Error al actualizar la información del SCN {confirmedInfo.NumScn}: {ex.Message}";
                result.Ex = ex;
                System.Console.WriteLine($"[BL] UpdateScnInfo - Error: {ex.Message}");
            }
            return result;
        }

        private static int DetermineScenario(ML.Maintenance.ConfirmedInfoByScn confirmedInfo, ML.Maintenance.InfoByScn originalInfo)
        {
            bool hasDateChange = HasDateChange(confirmedInfo, originalInfo);
            bool hasAddressChange = HasAddressChanges(confirmedInfo, originalInfo);
            
            System.Console.WriteLine($"[BL] DetermineScenario - Date change: {hasDateChange}, Address change: {hasAddressChange}");
            
            // Escenario 1: posfechado (solo cambio de fecha)
            if (hasDateChange && !hasAddressChange)
            {
                return 1;
            }
            // Escenario 2: cambio de dirección con posfechado (dirección + fecha)
            else if (hasDateChange && hasAddressChange)
            {
                return 2;
            }
            // Escenario 3: cambio de dirección (solo cambio de dirección)
            else if (!hasDateChange && hasAddressChange)
            {
                return 3;
            }
            else
            {
                return 1; // Por defecto usar escenario 1
            }
        }

        private static bool HasDateChange(ML.Maintenance.ConfirmedInfoByScn confirmedInfo, ML.Maintenance.InfoByScn originalInfo)
        {
            if (string.IsNullOrEmpty(confirmedInfo.FecEnt))
            {
                return false;
            }
            string originalDate = originalInfo.FecEnt ?? "";
            string newDate = confirmedInfo.FecEnt;

            if (string.IsNullOrEmpty(originalDate))
            {
                return !string.IsNullOrEmpty(newDate);
            }
            return !originalDate.Equals(newDate, StringComparison.OrdinalIgnoreCase);
        }

        private static bool HasAddressChanges(ML.Maintenance.ConfirmedInfoByScn confirmedInfo, ML.Maintenance.InfoByScn originalInfo)
        {
            // Comparar cada campo de dirección con los valores originales
            bool calleChanged = !string.IsNullOrEmpty(confirmedInfo.Calle) && 
                               !confirmedInfo.Calle.Equals(originalInfo.Calle ?? "", StringComparison.OrdinalIgnoreCase);
            
            bool coloniaChanged = !string.IsNullOrEmpty(confirmedInfo.Colonia) && 
                                 !confirmedInfo.Colonia.Equals(originalInfo.Colonia ?? "", StringComparison.OrdinalIgnoreCase);
            
            bool municipioChanged = !string.IsNullOrEmpty(confirmedInfo.Municipio) && 
                                   !confirmedInfo.Municipio.Equals(originalInfo.Municipio ?? "", StringComparison.OrdinalIgnoreCase);
            
            bool estadoChanged = !string.IsNullOrEmpty(confirmedInfo.Estado) && 
                                !confirmedInfo.Estado.Equals(originalInfo.Estado ?? "", StringComparison.OrdinalIgnoreCase);
            
            bool codPosChanged = !string.IsNullOrEmpty(confirmedInfo.CodPos) && 
                                !confirmedInfo.CodPos.Equals(originalInfo.CodPos ?? "", StringComparison.OrdinalIgnoreCase);
            
            bool numIntChanged = !string.IsNullOrEmpty(confirmedInfo.NumInt) && 
                                !confirmedInfo.NumInt.Equals(originalInfo.NumInt ?? "", StringComparison.OrdinalIgnoreCase);
            
            bool numExtChanged = !string.IsNullOrEmpty(confirmedInfo.NumExt) && 
                                !confirmedInfo.NumExt.Equals(originalInfo.NumExt ?? "", StringComparison.OrdinalIgnoreCase);
            
            bool longitudChanged = !string.IsNullOrEmpty(confirmedInfo.Longitud) && 
                                  !confirmedInfo.Longitud.Equals(originalInfo.Longitud ?? "", StringComparison.OrdinalIgnoreCase);
            
            bool latitudChanged = !string.IsNullOrEmpty(confirmedInfo.Latitud) && 
                                 !confirmedInfo.Latitud.Equals(originalInfo.Latitud ?? "", StringComparison.OrdinalIgnoreCase);
            
            bool referenciasChanged = !string.IsNullOrEmpty(confirmedInfo.Referencias) && 
                                     !confirmedInfo.Referencias.Equals(originalInfo.Referencias ?? "", StringComparison.OrdinalIgnoreCase);
            
            bool observacionesChanged = !string.IsNullOrEmpty(confirmedInfo.Observaciones) && 
                                       !confirmedInfo.Observaciones.Equals(originalInfo.Observaciones ?? "", StringComparison.OrdinalIgnoreCase);
            
            bool panelChanged = !string.IsNullOrEmpty(confirmedInfo.Panel) && 
                               !confirmedInfo.Panel.Equals(originalInfo.Panel ?? "", StringComparison.OrdinalIgnoreCase);
            
            bool voladoChanged = !string.IsNullOrEmpty(confirmedInfo.Volado) && 
                                !confirmedInfo.Volado.Equals(originalInfo.Volado ?? "", StringComparison.OrdinalIgnoreCase);
            
            bool masGenChanged = !string.IsNullOrEmpty(confirmedInfo.MasGen) && 
                                !confirmedInfo.MasGen.Equals(originalInfo.MasGen ?? "", StringComparison.OrdinalIgnoreCase);
            
            return calleChanged || coloniaChanged || municipioChanged || estadoChanged || 
                   codPosChanged || numIntChanged || numExtChanged || longitudChanged || 
                   latitudChanged || referenciasChanged || observacionesChanged || 
                   panelChanged || voladoChanged || masGenChanged;
        }

        private static void ExecuteScenario1(ML.Maintenance.ConfirmedInfoByScn confirmedInfo, OdbcConnection connection, OdbcTransaction transaction)
        {
            System.Console.WriteLine($"[BL] ExecuteScenario1 - Actualizando edc_cab para SCN: {confirmedInfo.NumScn}");
            
            string fechaEnt = !string.IsNullOrEmpty(confirmedInfo.FecEnt) ? confirmedInfo.FecEnt : "30/03/2000";
            
            System.Console.WriteLine($"[BL] ExecuteScenario1 - Actualizando fec_ent a: {fechaEnt}");
            
            string updateEdcCab = $@"UPDATE edc_cab 
                                    SET fec_ent = '{fechaEnt}'
                                    WHERE cod_emp = 1 
                                    AND num_scn = '{confirmedInfo.NumScn}'";

            using (OdbcCommand cmd = new OdbcCommand(updateEdcCab, connection, transaction))
            {
                int rowsAffected = cmd.ExecuteNonQuery();
                System.Console.WriteLine($"[BL] ExecuteScenario1 - edc_cab rows affected: {rowsAffected}");
                
                if (rowsAffected == 0)
                {
                    throw new Exception("No rows affected in edc_cab update");
                }
            }
        }

        private static void ExecuteScenario2(ML.Maintenance.ConfirmedInfoByScn confirmedInfo, OdbcConnection connection, OdbcTransaction transaction)
        {
            System.Console.WriteLine($"[BL] ExecuteScenario2 - Actualizando cli_direccion, cli_coord, edc_cab para SCN: {confirmedInfo.NumScn}");
            
            // Actualizar cli_coord
            UpdateCliCoord(confirmedInfo, connection, transaction);
            
            // Actualizar cli_direccion
            UpdateCliDireccion(confirmedInfo, connection, transaction);
            
            string fechaEnt = !string.IsNullOrEmpty(confirmedInfo.FecEnt) ? confirmedInfo.FecEnt : "30/03/2000";
            
            System.Console.WriteLine($"[BL] ExecuteScenario2 - Actualizando fec_ent a: {fechaEnt}");
            
            string updateEdcCab = $@"UPDATE edc_cab 
                                    SET fec_ent = '{fechaEnt}'
                                    WHERE cod_emp = 1 
                                    AND num_scn = '{confirmedInfo.NumScn}'";

            using (OdbcCommand cmd = new OdbcCommand(updateEdcCab, connection, transaction))
            {
                int rowsAffected = cmd.ExecuteNonQuery();
                System.Console.WriteLine($"[BL] ExecuteScenario2 - edc_cab rows affected: {rowsAffected}");
                
                if (rowsAffected == 0)
                {
                    throw new Exception("No rows affected in edc_cab update");
                }
            }
        }

        private static void ExecuteScenario3(ML.Maintenance.ConfirmedInfoByScn confirmedInfo, OdbcConnection connection, OdbcTransaction transaction)
        {
            
            UpdateCliCoord(confirmedInfo, connection, transaction);
            UpdateCliDireccion(confirmedInfo, connection, transaction);
        }

        private static void UpdateCliCoord(ML.Maintenance.ConfirmedInfoByScn confirmedInfo, OdbcConnection connection, OdbcTransaction transaction)
        {
            System.Console.WriteLine($"[BL] UpdateCliCoord - Actualizando cli_coord: {confirmedInfo.CodCli}");
            
            string checkExists = $@"SELECT COUNT(*) FROM cli_coord 
                                   WHERE cod_emp = 1 
                                   AND cod_cli = '{confirmedInfo.CodCli}'";

            int exists = 0;
            using (OdbcCommand cmd = new OdbcCommand(checkExists, connection, transaction))
            {
                exists = Convert.ToInt32(cmd.ExecuteScalar());
            }

            if (exists > 0)
            {
                string updateQuery = $@"UPDATE cli_coord 
                                       SET longitud = '{confirmedInfo.Longitud}', 
                                           latitud = '{confirmedInfo.Latitud}'
                                       WHERE cod_emp = 1 
                                       AND cod_cli = '{confirmedInfo.CodCli}'";

                using (OdbcCommand cmd = new OdbcCommand(updateQuery, connection, transaction))
                {
                    int rowsAffected = cmd.ExecuteNonQuery();
                    System.Console.WriteLine($"[BL] UpdateCliCoord - Updated existing record, rows affected: {rowsAffected}");
                    
                    if (rowsAffected == 0)
                    {
                        throw new Exception("No rows affected in cli_coord update");
                    }
                }
            }
            else
            {
                string insertQuery = $@"INSERT INTO cli_coord (cod_emp, cod_cli, longitud, latitud)
                                       VALUES (1, '{confirmedInfo.CodCli}', '{confirmedInfo.Longitud}', '{confirmedInfo.Latitud}')";

                using (OdbcCommand cmd = new OdbcCommand(insertQuery, connection, transaction))
                {
                    int rowsAffected = cmd.ExecuteNonQuery();
                    System.Console.WriteLine($"[BL] UpdateCliCoord - Inserted new record, rows affected: {rowsAffected}");
                    
                    if (rowsAffected == 0)
                    {
                        throw new Exception("No rows affected in cli_coord insert");
                    }
                }
            }
        }

        private static void UpdateCliDireccion(ML.Maintenance.ConfirmedInfoByScn confirmedInfo, OdbcConnection connection, OdbcTransaction transaction)
        {
            System.Console.WriteLine($"[BL] UpdateCliDireccion - Actualizando cli_direccion: {confirmedInfo.CodDir}");
            
            // Obtener cve_est basado en el código postal
            string cveEst = confirmedInfo.Estado; // Valor por defecto si no se encuentra
            if (!string.IsNullOrEmpty(confirmedInfo.CodPos))
            {
                string getCveEstQuery = $@"SELECT cve_est
                                           FROM cat_cp A, cat_est2 B
                                           WHERE A.cod_postal = '{confirmedInfo.CodPos}'
                                           AND B.cod_postal = A.cod_postal
                                           LIMIT 1";

                using (OdbcCommand cmd = new OdbcCommand(getCveEstQuery, connection, transaction))
                {
                    object result = cmd.ExecuteScalar();
                    if (result != null && result != DBNull.Value)
                    {
                        cveEst = result.ToString();
                        System.Console.WriteLine($"[BL] UpdateCliDireccion - Obtenido cve_est: {cveEst} para código postal: {confirmedInfo.CodPos}");
                    }
                    else
                    {
                        System.Console.WriteLine($"[BL] UpdateCliDireccion - No se encontró cve_est para código postal: {confirmedInfo.CodPos}, usando valor por defecto: {cveEst}");
                    }
                }
            }

            // Obtener municipio de cat_mun usando cve_est y el campo Delegación/Municipio
            string municipioValue = confirmedInfo.Municipio; // Valor por defecto
            if (!string.IsNullOrEmpty(cveEst) && !string.IsNullOrEmpty(confirmedInfo.Municipio))
            {
                string municipioSearch = confirmedInfo.Municipio.Trim().Replace("'", "''");
                string municipioSearchUpper = municipioSearch.ToUpper();
                string getMunicipioQuery = $@"SELECT desc
                                               FROM cat_mun
                                               WHERE cve_est = '{cveEst}'
                                               AND UPPER(desc) LIKE '%{municipioSearchUpper}%'";

                System.Console.WriteLine($"[BL] UpdateCliDireccion - Parámetros: cve_est={cveEst}, municipioSearch={municipioSearch}");

                using (OdbcCommand cmd = new OdbcCommand(getMunicipioQuery, connection, transaction))
                {
                    using (OdbcDataReader reader = cmd.ExecuteReader())
                    {
                        string bestMatch = null;
                        int bestMatchScore = -1;

                        while (reader.Read())
                        {
                            string descValue = reader.GetString(0).Trim();
                            string descUpper = descValue.ToUpper();
                            
                            int score = 0;
                            if (descUpper == municipioSearchUpper)
                            {
                                score = 100; // Coincidencia exacta
                            }
                            else if (descUpper.StartsWith(municipioSearchUpper))
                            {
                                score = 80; // Empieza con el valor
                            }
                            else if (descUpper.Contains(municipioSearchUpper))
                            {
                                score = 60; // Contiene el valor
                            }

                            if (score > bestMatchScore)
                            {
                                bestMatch = descValue;
                                bestMatchScore = score;
                            }
                        }

                        if (bestMatch != null)
                        {
                            municipioValue = bestMatch;
                            System.Console.WriteLine($"[BL] UpdateCliDireccion - Obtenido municipio: {municipioValue} para cve_est: {cveEst} y municipio: {confirmedInfo.Municipio} (score: {bestMatchScore})");
                        }
                        else
                        {
                            System.Console.WriteLine($"[BL] UpdateCliDireccion - No se encontró municipio en cat_mun para cve_est: {cveEst} y municipio: '{confirmedInfo.Municipio}', usando valor por defecto: {municipioValue}");
                        }
                    }
                }
            }

            string checkExists = $@"SELECT COUNT(*) FROM cli_direccion 
                                   WHERE cod_dir = '{confirmedInfo.CodDir}'";

            int exists = 0;
            using (OdbcCommand cmd = new OdbcCommand(checkExists, connection, transaction))
            {
                exists = Convert.ToInt32(cmd.ExecuteScalar());
            }

            if (exists > 0)
            {
                string updateQuery = $@"UPDATE cli_direccion 
                                       SET dir_cli = '{confirmedInfo.Calle}', 
                                           col_cli = '{confirmedInfo.Colonia}',
                                           cp_cli = '{confirmedInfo.CodPos}',
                                           pob_cli = '{municipioValue.Replace("'", "''")}',
                                           pro_cli = '{cveEst}',
                                           ent_calles = '{confirmedInfo.Referencias}',
                                           panel = '{confirmedInfo.Panel}',
                                           volado = '{confirmedInfo.Volado}',
                                           mas_gen = '{confirmedInfo.MasGen}',
                                           ent_calles2 = '{confirmedInfo.Observaciones}',
                                           num_int = '{confirmedInfo.NumInt}',
                                           num_ext = '{confirmedInfo.NumExt}'
                                       WHERE cod_dir = '{confirmedInfo.CodDir}'";

                using (OdbcCommand cmd = new OdbcCommand(updateQuery, connection, transaction))
                {
                    int rowsAffected = cmd.ExecuteNonQuery();
                    System.Console.WriteLine($"[BL] UpdateCliDireccion - Updated existing record, rows affected: {rowsAffected}");
                    
                    if (rowsAffected == 0)
                    {
                        throw new Exception("No rows affected in cli_direccion update");
                    }
                }
            }
            else
            {
                string insertQuery = $@"INSERT INTO cli_direccion (cod_dir, dir_cli, col_cli, cp_cli, pob_cli, pro_cli, ent_calles, panel, volado, mas_gen, ent_calles2, num_int, num_ext)
                                       VALUES ('{confirmedInfo.CodDir}', '{confirmedInfo.Calle}', '{confirmedInfo.Colonia}', '{confirmedInfo.CodPos}', '{municipioValue.Replace("'", "''")}', '{cveEst}', '{confirmedInfo.Referencias}', '{confirmedInfo.Panel}', '{confirmedInfo.Volado}', '{confirmedInfo.MasGen}', '{confirmedInfo.Observaciones}', '{confirmedInfo.NumInt}', '{confirmedInfo.NumExt}')";

                using (OdbcCommand cmd = new OdbcCommand(insertQuery, connection, transaction))
                {
                    int rowsAffected = cmd.ExecuteNonQuery();
                    System.Console.WriteLine($"[BL] UpdateCliDireccion - Inserted new record, rows affected: {rowsAffected}");
                    
                    if (rowsAffected == 0)
                    {
                        throw new Exception("No rows affected in cli_direccion insert");
                    }
                }
            }
        }

        private static void UpdateOraMantenimiento(ML.Maintenance.ConfirmedInfoByScn confirmedInfo, OdbcConnection connection, OdbcTransaction transaction)
        {
            System.Console.WriteLine($"[BL] UpdateOraMantenimiento - Actualizando/Insertando estatus a 2 para SCN: {confirmedInfo.NumScn}");
            
            string checkExists = $@"SELECT COUNT(*) FROM ora_mantenimiento 
                                   WHERE cod_emp = 1 
                                   AND cod_pto = '{confirmedInfo.CodPto}'
                                   AND num_edc = '{confirmedInfo.NumEdc}'";

            int exists = 0;
            using (OdbcCommand cmd = new OdbcCommand(checkExists, connection, transaction))
            {
                exists = Convert.ToInt32(cmd.ExecuteScalar());
            }

            if (exists > 0)
            {
                string updateQuery = $@"UPDATE ora_mantenimiento 
                                       SET estatus = 2
                                       WHERE cod_emp = 1 
                                       AND cod_pto = '{confirmedInfo.CodPto}'
                                       AND num_edc = '{confirmedInfo.NumEdc}'";

                using (OdbcCommand cmd = new OdbcCommand(updateQuery, connection, transaction))
                {
                    int rowsAffected = cmd.ExecuteNonQuery();
                    System.Console.WriteLine($"[BL] UpdateOraMantenimiento - Updated existing record, rows affected: {rowsAffected}");
                    
                    if (rowsAffected == 0)
                    {
                        throw new Exception("No rows affected in ora_mantenimiento update");
                    }
                }
            }
            else
            {
                string insertQuery = $@"INSERT INTO ora_mantenimiento (cod_emp, cod_pto, num_edc, estatus)
                                       VALUES (1, '{confirmedInfo.CodPto}', '{confirmedInfo.NumEdc}', 2)";

                using (OdbcCommand cmd = new OdbcCommand(insertQuery, connection, transaction))
                {
                    int rowsAffected = cmd.ExecuteNonQuery();
                    System.Console.WriteLine($"[BL] UpdateOraMantenimiento - Inserted new record, rows affected: {rowsAffected}");
                    
                    if (rowsAffected == 0)
                    {
                        throw new Exception("No rows affected in ora_mantenimiento insert");
                    }
                }
            }
        }

        private static void UpdateOraIntegraEnvio(ML.Maintenance.ConfirmedInfoByScn confirmedInfo, OdbcConnection connection, OdbcTransaction transaction)
        {
            System.Console.WriteLine($"[BL] UpdateOraIntegraEnvio - Actualizando estatus a null para SCN: {confirmedInfo.NumScn}");
            
            string updateQuery = $@"UPDATE ora_integra_envio 
                                   SET status = NULL
                                   WHERE num_scn = '{confirmedInfo.NumScn}'";

            using (OdbcCommand cmd = new OdbcCommand(updateQuery, connection, transaction))
            {
                int rowsAffected = cmd.ExecuteNonQuery();
                System.Console.WriteLine($"[BL] UpdateOraIntegraEnvio - rows affected: {rowsAffected}");
                
                if (rowsAffected == 0)
                {
                    System.Console.WriteLine($"[Warning] UpdateOraIntegraEnvio - No rows affected");
                }
            }
        }

        private static void UpdateOraMantenimientoEstatus(ML.Maintenance.InfoByScn originalInfo, OdbcConnection connection, OdbcTransaction transaction)
        {
            System.Console.WriteLine($"[BL] UpdateOraMantenimientoEstatus - Actualizando estatus a 2 para cod_pto={originalInfo.CodPto}, num_edc={originalInfo.NumEdc}");
            
            string updateQuery = $@"UPDATE ora_mantenimiento 
                                   SET estatus = 2 
                                   WHERE cod_pto = '{originalInfo.CodPto}' 
                                   AND num_edc = '{originalInfo.NumEdc}'";

            using (OdbcCommand cmd = new OdbcCommand(updateQuery, connection, transaction))
            {
                int rowsAffected = cmd.ExecuteNonQuery();
                System.Console.WriteLine($"[BL] UpdateOraMantenimientoEstatus - rows affected: {rowsAffected}");
                
                if (rowsAffected == 0)
                {
                    throw new Exception($"No rows affected in ora_mantenimiento update for cod_pto={originalInfo.CodPto}, num_edc={originalInfo.NumEdc}");
                }
            }
        }

        private static void CheckAndUpdateOraConfirmacion(ML.Maintenance.ConfirmedInfoByScn confirmedInfo, ML.Maintenance.InfoByScn originalInfo, OdbcConnection connection, OdbcTransaction transaction, string usuId)
        {
            System.Console.WriteLine($"[BL] CheckAndUpdateOraConfirmacion - Verificando registro para num_scn={originalInfo.NumScn}");
            
            string checkQuery = $@"SELECT * FROM ora_confirmacion WHERE num_scn = '{originalInfo.NumScn}'";
            bool exists = false;
            using (OdbcCommand cmd = new OdbcCommand(checkQuery, connection, transaction))
            {
                using (OdbcDataReader reader = cmd.ExecuteReader())
                {
                    exists = reader.Read();
                    System.Console.WriteLine($"[BL] CheckAndUpdateOraConfirmacion - Record exists: {exists}");
                }
            }

            // Convertir fecha de formulario DD/MM/YYYY a formato date
            string fecEnt = "30/03/2000";
            if (!string.IsNullOrEmpty(confirmedInfo.FecEnt))
            {
                fecEnt = confirmedInfo.FecEnt;
            }

            // tip_ent según IsCrb: 'C' si es true, 'L' si es false
            string tipEnt = confirmedInfo.IsCrb ? "C" : "L";

            if (exists)
            {
                // Actualizar estatus a 100 para todos los registros existentes (sin cambiar tip_ent)
                string updateQuery = $@"UPDATE ora_confirmacion 
                                       SET estatus = 100,
                                           fec_con = CURRENT YEAR TO MINUTE
                                       WHERE num_scn = '{originalInfo.NumScn}'";
                
                System.Console.WriteLine($"[BL] CheckAndUpdateOraConfirmacion - Update Parameters: estatus=100, fec_con=CURRENT YEAR TO MINUTE, num_scn={originalInfo.NumScn}");
                using (OdbcCommand cmd = new OdbcCommand(updateQuery, connection, transaction))
                {
                    int rowsAffected = cmd.ExecuteNonQuery();
                    System.Console.WriteLine($"[BL] CheckAndUpdateOraConfirmacion - Update rows affected: {rowsAffected}");
                }
            }

            string insertQuery = $@"INSERT INTO ora_confirmacion (cod_emp, pto_alm, num_scn, estatus, fec_ent, fec_con, usu_con, tip_ent)
                                   VALUES (1, {originalInfo.PtoAlm ?? "0"}, '{originalInfo.NumScn}', 0, '{fecEnt}', CURRENT YEAR TO MINUTE, {usuId ?? "0"}, '{tipEnt}')";

            System.Console.WriteLine($"[BL] CheckAndUpdateOraConfirmacion - Insert Parameters: cod_emp=1, pto_alm={originalInfo.PtoAlm}, num_scn={originalInfo.NumScn}, estatus=0, fec_ent={fecEnt}, fec_con=CURRENT YEAR TO MINUTE, usu_con={usuId}, tip_ent={tipEnt}");
            using (OdbcCommand cmd = new OdbcCommand(insertQuery, connection, transaction))
            {
                int rowsAffected = cmd.ExecuteNonQuery();
                System.Console.WriteLine($"[BL] CheckAndUpdateOraConfirmacion - Insert rows affected: {rowsAffected}");
                
                if (rowsAffected == 0)
                {
                    throw new Exception($"No rows affected in ora_confirmacion insert for num_scn={originalInfo.NumScn}");
                }
            }
        }

        private static void ExecuteLgaUpdates(ML.Maintenance.ConfirmedInfoByScn confirmedInfo, OdbcConnection connection, int scenario, string ordRelWithoutLastThree)
        {
            System.Console.WriteLine($"[BL] ExecuteLgaUpdates - Ejecutando actualizaciones LGA para escenario: {scenario}");
            
            try
            {
                // Actualizar lgaent
                UpdateLgaEnt(confirmedInfo, connection, ordRelWithoutLastThree);

                // Actualizar lgahventa y lgaposfecha para escenarios 2 y 3
                if (scenario == 2 || scenario == 3)
                {
                    UpdateLgahventa(confirmedInfo, connection, ordRelWithoutLastThree);
                    UpdateLgaposfecha(confirmedInfo, connection, ordRelWithoutLastThree);
                }
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"[BL] ExecuteLgaUpdates - Error en actualizaciones LGA: {ex.Message}");
            }
        }

        private static void UpdateLgaEnt(ML.Maintenance.ConfirmedInfoByScn confirmedInfo, OdbcConnection connection, string ordRelWithoutLastThree)
        {
            System.Console.WriteLine($"[BL] UpdateLgaEnt - Actualizando tabla lgaent");
            
            string updateQuery = $@"UPDATE dblga@lga_prod:lgaent 
                                   SET f_entrega = '{confirmedInfo.FecEnt}',
                                       direc_cte = '{confirmedInfo.Calle}',
                                       direc_cte1 = '{confirmedInfo.Calle}',
                                       col_poblacion = '{confirmedInfo.Colonia}',
                                       del_municipio = '{confirmedInfo.Municipio}',
                                       cod_postal = '{confirmedInfo.CodPos}',
                                       observ = '{confirmedInfo.Observaciones}',
                                       f_act = '27/10/2025',
                                       usuario = '{confirmedInfo.UsuCon}'
                                   WHERE cod_empresa = 1 
                                   AND cd_id = 870
                                   AND no_transf = '{ordRelWithoutLastThree}'";

            using (OdbcCommand cmd = new OdbcCommand(updateQuery, connection))
            {
                int rowsAffected = cmd.ExecuteNonQuery();
                System.Console.WriteLine($"[BL] UpdateLgaEnt - rows affected: {rowsAffected}");
                
                if (rowsAffected == 0)
                {
                    System.Console.WriteLine($"[Warning] UpdateLgaEnt - No rows affected");
                }
            }
        }

        private static void UpdateLgahventa(ML.Maintenance.ConfirmedInfoByScn confirmedInfo, OdbcConnection connection, string ordRelWithoutLastThree)
        {
            System.Console.WriteLine($"[BL] UpdateLgahventa - Actualizando tabla lgahventa");
            
            string updateQuery = $@"UPDATE dblga@lga_prod:lgahventa 
                                   SET f_entrega = '{confirmedInfo.FecEnt}',
                                       volar = '{confirmedInfo.Volado}',
                                       panel = '{confirmedInfo.Panel}',
                                       mas_gente = '{confirmedInfo.MasGen}'
                                   WHERE cod_empresa = 1 
                                   AND cd_id = 870
                                   AND no_transf = '{ordRelWithoutLastThree}'";

            using (OdbcCommand cmd = new OdbcCommand(updateQuery, connection))
            {
                int rowsAffected = cmd.ExecuteNonQuery();
                System.Console.WriteLine($"[BL] UpdateLgahventa - rows affected: {rowsAffected}");
                
                if (rowsAffected == 0)
                {
                    System.Console.WriteLine($"[Warning] UpdateLgahventa - No rows affected");
                }
            }
        }

        private static void UpdateLgaposfecha(ML.Maintenance.ConfirmedInfoByScn confirmedInfo, OdbcConnection connection, string ordRelWithoutLastThree)
        {
            System.Console.WriteLine($"[BL] UpdateLgaposfecha - Actualizando tabla lgaposfecha");
            
            string updateQuery = $@"UPDATE dblga@lga_prod:lgaposfecha 
                                   SET f_ent_nueva = '{confirmedInfo.FecEnt}',
                                       f_act = '28/10/2025'
                                   WHERE cod_empresa = 1 
                                   AND cd_id = 870
                                   AND no_transf = '{ordRelWithoutLastThree}'";

            using (OdbcCommand cmd = new OdbcCommand(updateQuery, connection))
            {
                int rowsAffected = cmd.ExecuteNonQuery();
                System.Console.WriteLine($"[BL] UpdateLgaposfecha - rows affected: {rowsAffected}");
                
                if (rowsAffected == 0)
                {
                    System.Console.WriteLine($"[Warning] UpdateLgaposfecha - No rows affected");
                }
            }
        }
    }
}
