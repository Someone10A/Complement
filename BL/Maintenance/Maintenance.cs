using DocumentFormat.OpenXml.Wordprocessing;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data.Odbc;
using System.Linq;
using System.Net.Http.Headers;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;
using System.Transactions;
using System.Xml;

namespace BL.Maintenance
{
    public class Maintenance
    {
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
	                                    CASE WHEN (K.num_scn IS NULL)
	                                    THEN
		                                    'NO'
	                                    ELSE
		                                    'SI'
	                                    END AS is_rdd,
	                                    CASE WHEN (K.num_scn IS NULL)
	                                    THEN
			                                    'NO'
	                                    ELSE
			                                    'SI'
	                                    END AS is_rdd_send,
	                                    K.num_rdd,
                                        CASE WHEN(M.num_scn IS NULL)
                                        THEN
                                            'NO'
                                        ELSE
                                            TO_CHAR(M.fec_con, '%Y-%m-%d %H:%M:%S')
                                        END AS is_confirmed,
                                        CASE WHEN(O.num_scn IS NULL)
                                        THEN
                                            'NO'
                                        ELSE
                                            'SI'
                                        END AS in_plan,
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
	                                    OUTER ora_ruta N,
                                        OUTER ora_confirmacion M,
                                        OUTER ora_drouting O
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
                                    AND N.estatus = 0
                                    AND M.pto_alm = A.pto_alm
                                    AND M.num_scn = A.num_scn
                                    AND M.estatus IN (0,1)
                                    AND O.cod_emp = A.cod_emp
                                    AND O.pto_alm = A.pto_alm
                                    AND O.num_scn = A.num_scn
                                    AND O.fec_ent >= TODAY";

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

                                    FecEnt = reader["fec_ent"]?.ToString(),
                                    FecCli = reader["fec_cli"]?.ToString(),
                                    FecEntR = reader["fec_ent_r"]?.ToString(),

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
                                    ColoniaOriginal = reader["colonia"]?.ToString(),
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

                                    UltConfirm = reader["is_confirmed"].ToString(),
                                    IsConfirmed = reader["is_confirmed"].ToString() == "NO" ? false : true,
                                    InPlan = reader["in_plan"].ToString(),
                                    InRoute = reader["in_route"].ToString()
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

                    scnInfo.ApiRequestWMS = GetOracleWMSInfo(scnInfo.OrdRel, mode);

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
        private static ML.Maintenance.ApiRequestWMS GetOracleWMSInfo(string orderNumber, string mode)
        {
            ML.Maintenance.ApiRequestWMS result = new ML.Maintenance.ApiRequestWMS();

            try
            {
                using (HttpClient client = new HttpClient())
                {
                    client.Timeout = TimeSpan.FromSeconds(10);

                    string username = DL.ApiOracle.GetOracleUsr(mode);
                    string password = DL.ApiOracle.GetOraclePwd(mode);
                    var byteArray = Encoding.ASCII.GetBytes($"{username}:{password}");

                    client.DefaultRequestHeaders.Accept.Clear();
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    client.DefaultRequestHeaders.Authorization =
                        new AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));

                    string url = DL.ApiOracle.GetEndpointOrder(mode).Replace("{########}", orderNumber);
                    Uri uri = new Uri(url);

                    HttpResponseMessage response = client.GetAsync(uri).Result;

                    if (response.IsSuccessStatusCode)
                    {
                        string json = response.Content.ReadAsStringAsync().Result;
                        var jsonObject = JsonConvert.DeserializeObject<JObject>(json);

                        if (jsonObject["results"] != null)
                        {
                            var item = jsonObject["results"].FirstOrDefault();
                            if (item != null)
                            {
                                result.Successes = true;
                                result.OrderNumber = item["OrderNumber"]?.ToString();
                                result.IdEstatus = item["IdEstatus"]?.ToObject<int>();
                                result.EnvioBloqueado = item["EnvioBloqueado"]?.ToObject<bool>() ?? false;

                                var statusDict = new Dictionary<int, string>
                                    {
                                        { 0, "Creado" },
                                        { 10, "Parcialmente asignado" },
                                        { 20, "Asignado" },
                                        { 25, "En recolección" },
                                        { 27, "Preparado" },
                                        { 30, "En empaquetado" },
                                        { 40, "Empaquetado" },
                                        { 50, "Cargado" },
                                        { 90, "Enviado" },
                                        { 99, "Cancelado" }
                                    };

                                if (result.IdEstatus.HasValue && statusDict.ContainsKey(result.IdEstatus.Value))
                                    result.DescEstatus = statusDict[result.IdEstatus.Value];
                                else
                                    result.DescEstatus = "Desconocido";
                            }
                        }
                        else
                        {
                            result.Successes = false;
                            result.OrderNumber = orderNumber;
                            result.DescEstatus = "No encontrado";
                        }
                    }
                    else
                    {
                        result.Successes = false;
                        result.DescEstatus = $"Error HTTP {(int)response.StatusCode}";
                    }
                }
            }
            catch (Exception ex)
            {
                result.Successes = false;
                result.DescEstatus = $"Error: {ex.Message}";
            }

            return result;
        }
        public static ML.Result GetColByCodPos(string codPos, string mode)
        {
            ML.Result result = new ML.Result();
            try
            {
                ML.Maintenance.DireccionInfo direccionInfo = new ML.Maintenance.DireccionInfo();

                using (OdbcConnection connection = new OdbcConnection(DL.Connection.GetConnectionStringGen(mode)))
                {
                    connection.Open();

                    string queryEstado = $@"SELECT TRIM(B.nom_est)
                                    FROM cat_est2 A, cat_est B
                                    WHERE A.cod_postal = '{codPos}'
                                    AND B.cve_est = A.cve_est";

                    using (OdbcCommand cmd = new OdbcCommand(queryEstado, connection))
                    {
                        using(OdbcDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.Read()) 
                            {
                                direccionInfo.Estado = reader.GetString(0);
                            }
                        }
                    }

                    string queryMunicipio= $@"SELECT TRIM(desc)
                                                FROM cat_mun2
                                                WHERE cod_postal = '{codPos}'";

                    using (OdbcCommand cmd = new OdbcCommand(queryMunicipio, connection))
                    {
                        using (OdbcDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                direccionInfo.Municipio = reader.GetString(0);
                            }
                        }
                    }

                    string queryColonias = $@"SELECT TRIM(nom_col)
                                            FROM cat_cp
                                            WHERE cod_postal = '{codPos}'";

                    using (OdbcCommand cmd = new OdbcCommand(queryColonias, connection))
                    {
                        using(OdbcDataReader reader = cmd.ExecuteReader())
                        {
                            direccionInfo.Colonias = new List<string>();

                            while (reader.Read()) 
                            {
                                string col = reader.GetString(0);

                                direccionInfo.Colonias.Add(col);
                            }
                        }
                    }
                }


                result.Correct = true;
                result.Object = direccionInfo;
            }
            catch (Exception ex)
            {
                result.Correct = false;
                result.Ex = ex;
                System.Console.WriteLine($@"{ex.Message}");
            }
            return result;
        }
        public static ML.Result GetTope(string date, string ent, string mode)
        {
            ML.Result result = new ML.Result();
            try
            {
                using (OdbcConnection connection = new OdbcConnection(DL.Connection.GetConnectionStringGen(mode)))
                {
                    connection.Open();

                    int tope = 0;
                    int count = 0;

                    string queryTope = $@"SELECT num_tope
                                        FROM edc_tope
                                        WHERE cod_emp = 1
                                        AND pto_alm = 870
                                        AND ent = '{ent}'
                                        ";

                    using (OdbcCommand cmd = new OdbcCommand(queryTope, connection))
                    {
                        using (OdbcDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                tope = reader.GetInt32(0);
                            }
                        }
                    }

                    string queryCount = $@"SELECT COUNT(*)
                                        FROM ora_confirmacion
                                        WHERE cod_emp = 1
                                        AND pto_alm = 870
                                        AND estatus = 0
                                        AND tip_ent IN('L','C')
                                        AND fec_ent = '{date}'
                                        ";

                    using (OdbcCommand cmd = new OdbcCommand(queryCount, connection))
                    {
                        using (OdbcDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                count = reader.GetInt32(0);
                            }
                        }
                    }
                    result.Correct = true;

                    if (tope > count)
                    {
                        result.Object = (true, $@"{count}/{tope} confirmaciones para la fecha");
                    }
                    else
                    {
                        result.Object = (false, $@"{count}/{tope} confirmaciones para la fecha");
                    }
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

        public static ML.Result UpdateScnInfo(ML.Maintenance.ConfirmedInfoByScn confirmedInfo, string mode)
        {
            ML.Result result = new ML.Result();
            try
            {
                using (OdbcConnection connection = new OdbcConnection(DL.Connection.GetConnectionStringGen(mode)))
                {
                    connection.Open();

                    string query = $@"SET ISOLATION TO DIRTY READ;";

                    using (OdbcCommand cmd = new OdbcCommand(query, connection))
                    {
                        int rowsaffected = cmd.ExecuteNonQuery();
                    }

                    if (confirmedInfo.IsConfirmed)
                    {
                        ML.Result resultGetTope = GetTope(confirmedInfo.FecEnt, "L", mode);
                        if (!resultGetTope.Correct)
                        {
                            throw resultGetTope.Ex;
                        }

                        var (ok, mensaje) = ((bool, string))result.Object;
                        if (!ok)
                        {
                            throw new Exception($"{mensaje}");
                        }
                    }

                    if (confirmedInfo.IsRdd)
                    {
                        UpdateOraIntegraEnvio(connection, confirmedInfo.NumScn);
                    }

                    UpdateCliCoord(connection, confirmedInfo.CodCli, confirmedInfo.Longitud, confirmedInfo.Latitud);
                    UpdateCliDireccion(connection, confirmedInfo);
                    UpdateLgaEnt(connection, confirmedInfo);

                    if (confirmedInfo.IsPosfec)
                    {
                        UpdateDate(connection, confirmedInfo.FecEnt, confirmedInfo);
                        UpdateLgahventa(connection, confirmedInfo);
                        UpdateLgaposfecha(connection, confirmedInfo);
                    }

                    if (confirmedInfo.IsConfirmed)
                    {
                        UpdateOraMantenimiento(connection, confirmedInfo.CodPto, confirmedInfo.NumEdc);
                        UpdateOraConfirmacion(connection, confirmedInfo);
                    }

                    result.Correct = true;
                    result.Object = "Cambios guardados";
                }
            }
            catch (Exception ex)
            {
                result.Correct = false;
                result.Message = $@"Error al actualizar la información del SCN {confirmedInfo.NumScn}: {ex.Message}";
                result.Ex = ex;
            }
            return result;
        }

        private static void UpdateLgaEnt(OdbcConnection connection, ML.Maintenance.ConfirmedInfoByScn confirmedInfo)
        {
            char[] linea = new string(' ', 50).ToCharArray();

            string num_int = confirmedInfo.NumInt.PadRight(10);
            string num_ext = confirmedInfo.NumExt.PadRight(10);

            Array.Copy(num_int.ToCharArray(), 0, linea, 29, 10);
            Array.Copy(num_ext.ToCharArray(), 0, linea, 39, 10);

            string nums  = new string(linea);

            string updateQuery = $@"UPDATE dblga@lga_prod:lgaent 
                                   SET f_entrega = '{confirmedInfo.FecEnt}',
                                       direc_cte = '{confirmedInfo.Calle}',
                                       direc_cte1 = '{nums}',
                                       col_poblacion = '{confirmedInfo.Colonia}',
                                       del_municipio = '{confirmedInfo.Municipio}',
                                       cod_postal = '{confirmedInfo.CodPos}',
                                       observ = '{confirmedInfo.Observaciones}',
                                       f_act = CURRENT,
                                       usuario = '{confirmedInfo.UsuCon}'
                                   WHERE cod_empresa = 1 
                                   AND cd_id = {confirmedInfo.PtoAlm}
                                   AND no_transf = '{confirmedInfo.CodPto}{confirmedInfo.NumEdc}'";

            using (OdbcCommand cmd = new OdbcCommand(updateQuery, connection))
            {
                int rowsAffected = cmd.ExecuteNonQuery();

                //if (rowsAffected < 1)
                //{
                //    throw new Exception($"UpdateLgaEnt - No rows affected");
                //}
            }
        }
        private static void UpdateLgahventa(OdbcConnection connection, ML.Maintenance.ConfirmedInfoByScn confirmedInfo)
        {
            string updateQuery = $@"UPDATE dblga@lga_prod:lgahventa 
                                   SET f_entrega = '{confirmedInfo.FecEnt}',
                                       volar = '{confirmedInfo.Volado}',
                                       panel = '{confirmedInfo.Panel}',
                                       mas_gente = '{confirmedInfo.MasGen}'
                                   WHERE cod_empresa = 1 
                                   AND cd_id = {confirmedInfo.PtoAlm}
                                   AND no_transf = '{confirmedInfo.CodPto}{confirmedInfo.NumEdc}'";

            using (OdbcCommand cmd = new OdbcCommand(updateQuery, connection))
            {
                int rowsAffected = cmd.ExecuteNonQuery();

                //if (rowsAffected < 1)
                //{
                //    throw new Exception($"[Warning] UpdateLgahventa - No rows affected");
                //}
            }
        }
        private static void UpdateLgaposfecha(OdbcConnection connection, ML.Maintenance.ConfirmedInfoByScn confirmedInfo)
        {
            string updateQuery = $@"UPDATE dblga@lga_prod:lgaposfecha 
                                   SET f_ent_nueva = '{confirmedInfo.FecEnt}',
                                       f_act = TODAY
                                   WHERE cod_empresa = 1 
                                   AND cd_id = {confirmedInfo.PtoAlm}
                                   AND sales_check = '{confirmedInfo.NumScn}'";

            using (OdbcCommand cmd = new OdbcCommand(updateQuery, connection))
            {
                int rowsAffected = cmd.ExecuteNonQuery();

                //if (rowsAffected < 1)
                //{
                //    throw new Exception($"[Warning] UpdateLgaposfecha - No rows affected");
                //}
            }
        }

        private static void UpdateOraIntegraEnvio(OdbcConnection connection, string num_scn)
        {
            string updateQuery = $@"UPDATE ora_integra_envio 
                                   SET status = NULL
                                   WHERE tipo = 'RD'
                                   AND num_scn = '{num_scn}'";

            using (OdbcCommand cmd = new OdbcCommand(updateQuery, connection))
            {
                int rowsAffected = cmd.ExecuteNonQuery();

                if (rowsAffected < 1)
                {
                    //throw new Exception();
                }
            }
        }
        private static void UpdateCliCoord(OdbcConnection connection, string codCli, string longitud, string latitud)
        {
            string checkExists = $@"SELECT COUNT(*) 
                                    FROM cli_coord 
                                    WHERE cod_emp = 1 
                                    AND cod_cli = '{codCli}'";

            int exists = 0;
            using (OdbcCommand cmd = new OdbcCommand(checkExists, connection))
            {
                exists = Convert.ToInt32(cmd.ExecuteScalar());
            }

            if (exists > 0)
            {
                string updateQuery = $@"UPDATE cli_coord 
                                       SET longitud = '{longitud}', 
                                           latitud = '{latitud}'
                                       WHERE cod_emp = 1 
                                       AND cod_cli = '{codCli}'";

                using (OdbcCommand cmd = new OdbcCommand(updateQuery, connection))
                {
                    int rowsAffected = cmd.ExecuteNonQuery();
                    //System.Console.WriteLine($"[BL] UpdateCliCoord - Updated existing record, rows affected: {rowsAffected}");

                    if (rowsAffected < 1)
                    {
                        throw new Exception("No rows affected in cli_coord update");
                    }
                }
            }
            else
            {
                string insertQuery = $@"INSERT INTO cli_coord (cod_emp, cod_cli, longitud, latitud)
                                       VALUES (1, '{codCli}', '{longitud}', '{latitud}')";

                using (OdbcCommand cmd = new OdbcCommand(insertQuery, connection))
                {
                    int rowsAffected = cmd.ExecuteNonQuery();

                    if (rowsAffected <1)
                    {
                        throw new Exception("No rows affected in cli_coord insert");
                    }
                }
            }
        }
        private static void UpdateCliDireccion(OdbcConnection connection, ML.Maintenance.ConfirmedInfoByScn confirmedInfo)
        {
            string getCveEstQuery = $@"SELECT TRIM(cve_est)
                                            FROM cat_est
                                            WHERE nom_est = '{confirmedInfo.Estado}'
                                            ";

            using (OdbcCommand cmd = new OdbcCommand(getCveEstQuery, connection))
            {
                using (OdbcDataReader reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        confirmedInfo.Estado = reader.GetString(0);
                    }
                    else
                    {
                        throw new Exception($@"No se encontro el estado {confirmedInfo.Estado}");
                    }
                }
            }

            string updateQuery = $@"UPDATE cli_direccion 
                                       SET dir_cli = '{confirmedInfo.Calle}', 
                                           col_cli = '{confirmedInfo.Colonia}',
                                           cp_cli = '{confirmedInfo.CodPos}',
                                           pob_cli = '{confirmedInfo.Municipio}',
                                           pro_cli = '{confirmedInfo.Estado}',
                                           ent_calles = '{confirmedInfo.Referencias}',
                                           panel = '{confirmedInfo.Panel}',
                                           volado = '{confirmedInfo.Volado}',
                                           mas_gen = '{confirmedInfo.MasGen}',
                                           ent_calles2 = '{confirmedInfo.Observaciones}',
                                           num_int = '{confirmedInfo.NumInt}',
                                           num_ext = '{confirmedInfo.NumExt}'
                                        WHERE cod_emp = 1 
                                        AND cod_dir = '{confirmedInfo.CodDir}'";

            using (OdbcCommand cmd = new OdbcCommand(updateQuery, connection))
            {
                int rowsAffected = cmd.ExecuteNonQuery();

                if (rowsAffected < 1)
                {
                    throw new Exception("No rows affected in cli_direccion update");
                }
            }
        }
        private static void UpdateDate(OdbcConnection connection, string fecEnt, ML.Maintenance.ConfirmedInfoByScn confirmedInfo)
        {
            string updateEdcCab = $@"UPDATE edc_cab 
                                    SET fec_cli = '{confirmedInfo.FecEnt}'
                                    WHERE cod_emp = 1 
                                    AND num_scn = '{confirmedInfo.NumScn}'";

            using (OdbcCommand cmd = new OdbcCommand(updateEdcCab, connection))
            {
                int rowsAffected = cmd.ExecuteNonQuery();

                if (rowsAffected < 1)
                {
                    throw new Exception("No se actualizo fecha genesix 1");
                }
            }

            string updateOrdEdcCab = $@"UPDATE ordedc_cab 
                                    SET fec_cli = '{fecEnt}'
                                    WHERE cod_emp = 1 
                                    AND cod_pto = {confirmedInfo.CodPto}
                                    AND num_edc = {confirmedInfo.NumEdc}";

            using (OdbcCommand cmd = new OdbcCommand(updateEdcCab, connection))
            {
                int rowsAffected = cmd.ExecuteNonQuery();

                if (rowsAffected < 1)
                {
                    throw new Exception("No se actualizo fecha genesix 2");
                }
            }
        }
        private static void UpdateOraMantenimiento(OdbcConnection connection, string codPto, string numEdc)
        {
            string updateQuery = $@"UPDATE ora_mantenimiento 
                                       SET estatus = 2
                                       WHERE cod_emp = 1 
                                       AND cod_pto = {codPto}
                                       AND num_edc = {numEdc}";

            using (OdbcCommand cmd = new OdbcCommand(updateQuery, connection))
            {
                int rowsAffected = cmd.ExecuteNonQuery();

                if (rowsAffected < 1)
                {
                    throw new Exception("No rows affected in ora_mantenimiento update");
                }
            }
        }
        private static void UpdateOraConfirmacion(OdbcConnection connection, ML.Maintenance.ConfirmedInfoByScn confirmedInfo)
        {
            string checkQuery = $@"SELECT *
                                    FROM ora_confirmacion 
                                    WHERE estatus IN (0)
                                    AND num_scn = '{confirmedInfo.NumScn}'";
            bool exists = false;
            using (OdbcCommand cmd = new OdbcCommand(checkQuery, connection))
            {
                using (OdbcDataReader reader = cmd.ExecuteReader())
                {
                    exists = reader.Read();
                }
            }

            string tipEnt = confirmedInfo.IsCrb ? "C" : "L";

            if (exists)
            {
                string updateQuery = $@"UPDATE ora_confirmacion 
                                        SET estatus = 100
                                        WHERE cod_emp = 1
                                        AND num_scn = '{confirmedInfo.NumScn}'";

                using (OdbcCommand cmd = new OdbcCommand(updateQuery, connection))
                {
                    int rowsAffected = cmd.ExecuteNonQuery();

                    if (rowsAffected < 1)
                    {
                        throw new Exception($@"No se pudo cancelar la fecha anterior");
                    }
                }
            }

            string insertQuery = $@"INSERT INTO ora_confirmacion (cod_emp, pto_alm, num_scn, estatus, fec_ent, fec_con, usu_con, tip_ent)
                                   VALUES (1, {confirmedInfo.PtoAlm ?? "0"}, '{confirmedInfo.NumScn}', 0, '{confirmedInfo.FecEnt}', CURRENT YEAR TO MINUTE, {confirmedInfo.UsuCon}, '{tipEnt}')";

            using (OdbcCommand cmd = new OdbcCommand(insertQuery, connection))
            {
                int rowsAffected = cmd.ExecuteNonQuery();

                if (rowsAffected < 1)
                {
                    throw new Exception($"No rows affected in ora_confirmacion insert for num_scn={confirmedInfo.NumScn}");
                }
            }
        }
        
        /*----------------------------------------------------------------------------*/

        public static ML.Result GetToConfirm(string mode)
        {
            ML.Result result = new ML.Result();
            try
            {
                using (OdbcConnection connection = new OdbcConnection(DL.Connection.GetConnectionStringGen(mode)))
                {
                    connection.Open();

                    string query = $@"SELECT  UNIQUE
                                            B.num_scn,B.pto_alm,B.cod_pto,B.num_edc,B.estado||C.estado AS estado,
                                            CASE WHEN (B.fec_cli IS NULL)
                                            THEN
                                                    CASE WHEN (C.fec_ent_r IS NULL)
                                                    THEN
                                                            B.fec_ent
                                                    ELSE
                                                            C.fec_ent_r
                                                    END
                                            ELSE
                                                    B.fec_cli
                                            END AS fec_ent,
                                            CASE WHEN (D.intentos IS NULL)
                                            THEN
                                                    A.cod_pto||A.num_edc||'000'
                                            ELSE
                                                    A.cod_pto||A.num_edc||LPAD(TO_CHAR(D.intentos), 3, '0')
                                            END AS ord_rel,
		                                    CASE WHEN (D.intentos IS NULL)
                                            THEN
                                                    'NO'
                                            ELSE
                                                    'SI'
                                            END AS is_rete
                                    FROM ora_mantenimiento A, edc_cab B, ordedc_cab C, OUTER ora_rt_envio D
                                    WHERE A.estatus IN (0,1,2)
                                    AND B.cod_emp = A.cod_emp
                                    AND B.cod_pto = A.cod_pto
                                    AND B.num_edc = A.num_edc
                                    AND B.pto_alm = 870
                                    AND B.tip_ent = 1
                                    AND B.estado IN ('I','P')
                                    AND B.num_scn NOT IN (SELECT num_scn FROM ora_excluye)
                                    AND B.num_scn NOT IN (SELECT num_scn FROM ora_ruta WHERE estatus IN (0,2))
                                    AND B.num_scn NOT IN (SELECT num_scn FROM ora_confirmacion WHERE cod_emp = 1 AND estatus = 0 AND fec_ent >= TODAY)
                                    AND B.num_scn NOT IN (SELECT num_scn FROM ora_drouting WHERE cod_emp = 1 AND fec_ent >= TODAY)
                                    AND C.cod_emp = A.cod_emp
                                    AND C.cod_pto = A.cod_pto
                                    AND C.num_edc = A.num_edc
                                    AND D.cod_pto = B.cod_pto
                                    AND D.num_edc = B.num_edc
                                    AND D.num_scn = B.num_scn
                                    AND D.num_scn = B.num_scn
                                    ORDER BY 6 ASC
                                    ";

                    List<ML.Maintenance.ScnToConfirm> scnToConfirmList = new List<ML.Maintenance.ScnToConfirm>();

                    using (OdbcCommand cmd = new OdbcCommand(query, connection))
                    {
                        using (OdbcDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                ML.Maintenance.ScnToConfirm scnToConfirm = new ML.Maintenance.ScnToConfirm();

                                scnToConfirm.num_scn = reader.GetString(0);
                                scnToConfirm.pto_alm = reader.GetString(1);
                                scnToConfirm.cod_pto = reader.GetString(2);
                                scnToConfirm.num_edc = reader.GetString(3);
                                scnToConfirm.estado = reader.GetString(4);
                                scnToConfirm.fec_ent = reader.GetString(5);
                                scnToConfirm.ord_rel = reader.GetString(6);
                                scnToConfirm.is_rete = reader.GetString(7);

                                scnToConfirmList.Add(scnToConfirm);
                            }
                        }
                    }

                    result.Correct = true;
                    result.Object = scnToConfirmList;
                }
            }
            catch (Exception ex)
            {
                result.Correct = false;
                result.Message = $@"Error al obtener los SCN a confirmar: {ex.Message}";
                result.Ex = ex;
            }
            return result;
        }
    }
}
