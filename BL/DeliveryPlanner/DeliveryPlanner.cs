using ML.DeliveryPlanner;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data.Odbc;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Transactions;

namespace BL.DeliveryPlanner
{
    public class DeliveryPlanner
    {
        /*GetReadyOrdersPerDate*/
        public static ML.Result GetReadyOrdersPerDate(ML.DeliveryPlanner.ReadyQuery readyQuery,string mode)
        {
            ML.Result result = new ML.Result();
            try
            {
                string query = $@"SELECT A.num_scn,A.pto_alm,TRIM(C.pro_cli) edo_cli,TRIM(C.pob_cli) mun_cli,
                                    CASE WHEN (D.sector IS NULL)
                                    THEN
                                        'XXX'
                                    ELSE
                                        TRIM(D.sector)
                                    END AS sector
                                    ,C.cp_cli,
                                    C.panel,C.volado,C.mas_gen, A.tip_ent,
                                    CASE WHEN (E.intentos IS NULL)
                                    THEN
                                            B.cod_pto||B.num_edc||'000'
                                    ELSE
                                            B.cod_pto||B.num_edc||LPAD(TO_CHAR(E.intentos),3,'0')
                                    END AS ord_rel, A.fec_ent
                            FROM ora_confirmacion A,edc_cab B, cli_direccion C, OUTER ora_sectores D,
                            OUTER ora_rt_envio E
                            WHERE A.cod_emp = 1
                            AND A.pto_alm = {readyQuery.PtoAlm}
                            AND A.fec_ent = '{readyQuery.FecEnt}'
                            AND A.tip_ent = '{readyQuery.TipEnt}'
                            AND A.estatus = 0
                            AND B.cod_emp = A.cod_emp
                            AND B.pto_alm = A.pto_alm
                            AND B.num_scn = A.num_scn
                            AND C.cod_emp = B.cod_emp
                            AND C.cod_dir = B.cod_dir
                            AND D.cod_emp = A.cod_emp
                            AND D.pto_alm = A.pto_alm
                            AND D.cod_postal = C.cp_cli
                            AND E.cod_pto = B.cod_pto
                            AND E.num_edc = B.num_edc
                            AND E.num_scn = B.num_scn           ";

                List<ML.DeliveryPlanner.ReadyInfo> readyInfoList = new List<ML.DeliveryPlanner.ReadyInfo>();

                using(OdbcConnection connection = new OdbcConnection(DL.Connection.GetConnectionStringGen(mode)))
                {
                    connection.Open();

                    using (OdbcCommand command = new OdbcCommand(query, connection))
                    {
                        using (OdbcDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read()) 
                            {
                                ML.DeliveryPlanner.ReadyInfo readyInfo = new ML.DeliveryPlanner.ReadyInfo();

                                readyInfo.NumScn = reader.GetString(0);
                                readyInfo.PtoAlm = reader.GetString(1);
                                readyInfo.EdoCli = reader.GetString(2);
                                readyInfo.MunCli = reader.GetString(3);
                                readyInfo.Sector = reader.GetString(4);
                                readyInfo.CpCli = reader.GetString(5);
                                readyInfo.Panel = reader.GetString(6);
                                readyInfo.Volado = reader.GetString(7);
                                readyInfo.MasGen = reader.GetString(8);
                                readyInfo.TipEnt = reader.GetString(9);
                                readyInfo.OrdRel = reader.GetString(10);
                                readyInfo.FecEnt = reader.GetDateTime(11).ToString("ddMMyyyy");

                                readyInfo.IsReady = readyInfo.Sector == "XXX" && readyInfo.TipEnt == "L" ? false : true;

                                readyInfoList.Add(readyInfo);
                            }
                        }
                    }
                }


                foreach(ML.DeliveryPlanner.ReadyInfo readyInfo in readyInfoList)
                {
                    ML.DeliveryPlanner.ApiRequestWMS apiRequestWMS = GetOracleWMSInfo(readyInfo.OrdRel, 1, mode);

                    readyInfo.IsReady = apiRequestWMS.IdEstatus == 0 ? readyInfo.IsReady : false;
                    readyInfo.WmsEst = apiRequestWMS.DescEstatus;
                }

                result.Correct = true;
                result.Object = readyInfoList;
            }
            catch (Exception ex)
            {
                result.Correct = false;
                result.Message = $@"Error al obtener Ordenes aptas {ex.Message}";
            }
            return result;
        }
        private static ML.DeliveryPlanner.ApiRequestWMS GetOracleWMSInfo(string orderNumber, int numTry, string mode)
        {
            ML.DeliveryPlanner.ApiRequestWMS result = new ML.DeliveryPlanner.ApiRequestWMS();
            if (numTry <= 3)
            {
                if (numTry > 1)
                {
                    Thread.Sleep(1000);
                }
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
                                result.DescEstatus = "No existe";
                            }
                        }
                        else
                        {
                            //Recursivo
                            result = GetOracleWMSInfo(orderNumber, numTry + 1, mode);
                        }
                    }
                }
                catch (Exception ex)
                {
                    result.Successes = false;
                    result.DescEstatus = $"Error: {ex.Message}";
                }
            }
            else
            {
                result.Successes = false;
                result.OrderNumber = orderNumber;
                result.DescEstatus = "No existe";
            }
                

            return result;
        }
        private static ML.Result x()
        {
            ML.Result result = new ML.Result();
            try
            {
                result.Correct = true;
            }
            catch (Exception ex)
            {
                result.Correct = false;
                result.Message = $@"Error al ";
            }
            return result;
        }

        /*OrderFreeze*/
        public static ML.Result OrderFreeze(List<ML.DeliveryPlanner.ReadyInfo> readyInfoList, string mode)
        {
            ML.Result result = new ML.Result();
            int successes = 0;
            try
            {

                foreach (ML.DeliveryPlanner.ReadyInfo readyInfo in readyInfoList) 
                {
                    using (OdbcConnection connection = new OdbcConnection(DL.Connection.GetConnectionStringGen(mode)))
                    {
                        connection.Open();

                        ML.Result resultGetDivision = GetDivision(connection, readyInfo.NumScn);
                        if (!resultGetDivision.Correct)
                        {
                            throw new Exception(resultGetDivision.Message);
                        }
                        string div = (string)resultGetDivision.Object;

                        using(OdbcTransaction transaction = connection.BeginTransaction(System.Data.IsolationLevel.ReadUncommitted))
                        {
                            try
                            {
                                //Console.WriteLine($"{readyInfo.NumScn} inicia");
                                ML.Result resultUpdateConfirma = UpdateConfirma(connection, transaction, readyInfo);
                                if (!resultUpdateConfirma.Correct)
                                {
                                    throw new Exception(resultUpdateConfirma.Message);
                                }

                                ML.Result resultInsertRouting = InsertRouting(connection, transaction, readyInfo, div);
                                if (!resultInsertRouting.Correct)
                                {
                                    throw new Exception(resultInsertRouting.Message);
                                }
                                transaction.Commit();
                                successes++;
                            }
                            catch (Exception ex)
                            {
                                transaction.Rollback();
                                throw ex;
                            }
                        }
                    }
                }

                result.Correct = true;
                result.Message = $@"{successes}/{readyInfoList.Count} Ordenes liberadas satisfactoriamente";
            }
            catch (Exception ex)
            {
                result.Correct = false;
                result.Message = $@"{successes}/{readyInfoList.Count} Ordenes liberadas satisfactoriamente. Excepcion liberando ordenes a planeación {ex.Message}";
            }
            return result;
        }
        private static ML.Result GetDivision(OdbcConnection connection, string NumScn)
        {
            ML.Result result = new ML.Result();
            try
            {
                string div = string.Empty;

                string query = $@"SELECT FIRST 1 C.cod_fam2
                                    FROM edc_det A, arti C
                                    WHERE EXISTS(
				                                    SELECT 1
				                                    FROM edc_cab B
				                                    WHERE B.cod_emp = A.cod_emp
				                                    AND B.cod_pto = A.cod_pto
				                                    AND B.num_edc = A.num_edc
				                                    AND B.tip_ent = 1
				                                    AND B.num_scn = '{NumScn}'
                                    )
                                    AND A.cod_emp = 1
                                    AND C.cod_emp = A.cod_emp
                                    AND C.int_art = A.int_art
                                    AND C.cod_fam2 NOT IN (192, 187)
                                    ;
                                    ";

                using(OdbcCommand cmd = new OdbcCommand(query, connection))
                {
                    using(OdbcDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            div = reader.GetString(0);
                        }
                        else
                        {
                            throw new Exception($@"SCN sin division");
                        }
                    }
                }

                result.Correct = true;
                result.Object = div;
            }
            catch (Exception ex)
            {
                result.Correct = false;
                result.Message = $@"Error al consultar la división del SCN {NumScn}: {ex.Message}";
            }
            return result;
        }
        private static ML.Result UpdateConfirma(OdbcConnection connection, OdbcTransaction transaction, ML.DeliveryPlanner.ReadyInfo readyInfo)
        {
            ML.Result result = new ML.Result();
            try
            {
                string query = $@"UPDATE
                                     ora_confirmacion
                                SET estatus = 99
                                WHERE cod_emp = 1
                                AND estatus = 0
                                AND pto_alm = {readyInfo.PtoAlm}
                                AND tip_ent = '{readyInfo.TipEnt}'
                                AND num_scn = '{readyInfo.NumScn}'";

                using (OdbcCommand cmd = new OdbcCommand(query, connection, transaction))
                {
                    int rowsAffected = cmd.ExecuteNonQuery();
                    if (rowsAffected < 1)
                    {
                        throw new Exception($@"No se actualizo ora_confirmacion");
                    }
                }

                result.Correct = true;
            }
            catch (Exception ex)
            {
                result.Correct = false;
                result.Ex = ex;
                result.Message = $@"Error en UpdateConfirma SCN {readyInfo.NumScn}: {ex.Message}";
            }
            return result;
        }
        private static ML.Result InsertRouting(OdbcConnection connection, OdbcTransaction transaction, ML.DeliveryPlanner.ReadyInfo readyInfo, string div)
        {
            ML.Result result = new ML.Result();
            try
            {
                string query = $@"INSERT INTO 
	                                ora_drouting (cod_emp,pto_alm,estatus,num_scn,ord_rel,fec_ent,tip_ent,div_ent)
	                                VALUES (1,{readyInfo.PtoAlm},0,'{readyInfo.NumScn}','{readyInfo.OrdRel}','{readyInfo.FecEnt}','{readyInfo.TipEnt}','{div}')";

                using (OdbcCommand cmd = new OdbcCommand(query, connection, transaction))
                {
                    int rowsAffected = cmd.ExecuteNonQuery();
                    if (rowsAffected < 1)
                    {
                        throw new Exception($@"No se inserto ora_drouting");
                    }
                }

                result.Correct = true;
            }
            catch (Exception ex)
            {
                result.Correct = false;
                result.Ex = ex;
                result.Message = $@"Error en InsertRouting SCN {readyInfo.NumScn}: {ex.Message}";
            }
            return result;
        }

        /*CreateRouting*/
        public static ML.Result GetOrdersPerDate(ML.DeliveryPlanner.PlanQuery planQuery, string mode)
        {
            ML.Result result = new ML.Result();
            try
            {
                string query = $@"SELECT  A.tip_ent,A.pto_alm,B.cod_pto,A.num_scn,
                                            CASE WHEN (E.intentos IS NULL)
                                            THEN
                                                    B.cod_pto||B.num_edc||'000'
                                            ELSE
                                                    B.cod_pto||B.num_edc||LPAD(TO_CHAR(E.intentos),3,'0')
                                            END AS ord_rel,
                                            CASE 
                                                WHEN (B.estado = 'G') THEN 'Generado'
                                                WHEN (B.estado = 'I') THEN 'Impreso'
                                                WHEN (B.estado = 'P') THEN 'Transito-Retenido'
                                                WHEN (B.estado = 'X') THEN 'Cancelado'
                                                WHEN (B.estado = 'C') THEN 'PreCancelado'
                                                WHEN (B.estado = 'D') THEN 'Devuelto'
                                                WHEN (B.estado = 'N') THEN 'Devueltos'
                                            ELSE 'Estatus no clasificado '||B.estado END AS gnx_est,
                                            A.div_ent,
                                            TRIM(C.pro_cli) edo_cli,
                                            TRIM(C.pob_cli) mun_cli,TRIM(D.sector) AS sector,C.cp_cli,
                                            TRIM(C.col_cli) as col_cli,
                                            TRIM(F.nom_cli)||' '||TRIM(F.ape1_cli)||' '||TRIM(F.ape2_cli) as cli,
                                            C.panel,C.volado,C.mas_gen,
                                            A.fec_ent, G.longitud,G.latitud
                                    FROM ora_drouting A,edc_cab B, cli_direccion C, ora_sectores D,
			                                    OUTER ora_rt_envio E, clientes F, cli_coord G
                                    WHERE A.cod_emp = 1
                                    AND A.pto_alm = {planQuery.PtoAlm}
                                    AND A.fec_ent = '{planQuery.FecEnt}'
                                    AND A.tip_ent = '{planQuery.TipEnt}'
                                    AND A.estatus = 0
                                    AND B.cod_emp = A.cod_emp
                                    AND B.pto_alm = A.pto_alm
                                    AND B.num_scn = A.num_scn
                                    AND C.cod_emp = B.cod_emp
                                    AND C.cod_dir = B.cod_dir
                                    AND D.cod_emp = A.cod_emp
                                    AND D.pto_alm = A.pto_alm
                                    AND D.cod_postal = C.cp_cli
                                    AND E.cod_pto = B.cod_pto
                                    AND E.num_edc = B.num_edc
                                    AND E.num_scn = B.num_scn
                                    AND F.cod_emp = B.cod_emp
                                    AND F.cod_cli = B.cod_cli
                                    AND G.cod_emp = B.cod_emp
                                    AND G.cod_cli = B.cod_cli
                                    ";

                List<ML.DeliveryPlanner.PlanInfo> planInfoList = new List<ML.DeliveryPlanner.PlanInfo>();

                using (OdbcConnection connection = new OdbcConnection(DL.Connection.GetConnectionStringGen(mode)))
                {
                    connection.Open();

                    using (OdbcCommand command = new OdbcCommand(query, connection))
                    {
                        using (OdbcDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                ML.DeliveryPlanner.PlanInfo planInfo = new ML.DeliveryPlanner.PlanInfo();

                                planInfo.IsReady = true;
                                planInfo.TipEnt = reader.GetString(0);
                                planInfo.PtoAlm = reader.GetString(1);
                                planInfo.CodPto = reader.GetString(2);
                                planInfo.NumScn = reader.GetString(3);
                                planInfo.OrdRel = reader.GetString(4);
                                planInfo.GnxEst = reader.GetString(5);
                                planInfo.Division = reader.GetString(6);
                                planInfo.EdoCli = reader.GetString(7);
                                planInfo.MunCli = reader.GetString(8);
                                planInfo.Sector = reader.GetString(9);
                                planInfo.CpCli = reader.GetString(10);
                                planInfo.ColCli = reader.GetString(11);
                                planInfo.NomCli = reader.GetString(12);
                                planInfo.Panel = reader.GetString(13);
                                planInfo.Volado = reader.GetString(14);
                                planInfo.MasGen = reader.GetString(15);
                                planInfo.FecEnt = reader.GetDateTime(16).ToString("ddMMyyyy");

                                planInfoList.Add(planInfo); 
                            }
                        }
                    }
                }

                foreach (ML.DeliveryPlanner.PlanInfo planInfo in planInfoList)
                {
                    ML.DeliveryPlanner.ApiRequestWMS apiRequestWMS = GetOracleWMSInfo(planInfo.OrdRel, 1, mode);

                    planInfo.IsReady = apiRequestWMS.IdEstatus == 0 ? planInfo.IsReady : false;
                    planInfo.WmsEst = apiRequestWMS.DescEstatus;
                }

                result.Correct = true;
                result.Message = $@"{planInfoList.Count} Ordenes recuperadas";
                result.Object = planInfoList;
            }
            catch (Exception ex)
            {
                result.Correct = false;
                result.Message = $@"Error al obtener Ordenes para planear {ex.Message}";
            }
            return result;
        }
        /*Cambia de listo para asignar  a PreAsignado*/
        public static ML.Result ChangeInRoute(PlanSchema planSchema, string mode)
        {
            ML.Result result = new ML.Result();
            try
            {
                string tipEnt = planSchema.planInfoList.First().TipEnt;
                string ptoAlm = planSchema.planInfoList.First().PtoAlm;
                string fecEnt = planSchema.planInfoList.First().FecEnt;
                int scnCan = planSchema.planInfoList.Count;
                string idUni = DateTime.Now.ToString("yyyyMMddHHmm");

                string allOrd = string.Join(",",
                    planSchema.planInfoList.Select(x => $"'{x.OrdRel}'"));

                string queryIsolation = $@"SET ISOLATION TO DIRTY READ;";
                string queryInsert = $"INSERT INTO ora_crouting VALUES(1,{ptoAlm},0,'{fecEnt}','{tipEnt}','{idUni}',{scnCan},{planSchema.routes})";
                string queryUpdate = $@"UPDATE
                                             ora_drouting
                                        SET estatus = 2,
                                             pre_fol = '{idUni}'
                                        WHERE cod_emp = 1
                                        AND estatus = 0
                                        AND pto_alm = {ptoAlm}
                                        AND tip_ent = '{tipEnt}'
                                        AND fec_ent = '{fecEnt}'
                                        AND ord_rel IN ({allOrd})
                                        ";

                using (OdbcConnection connection = new OdbcConnection(DL.Connection.GetConnectionStringGen(mode)))
                {
                    connection.Open();

                    using (OdbcTransaction transaction = connection.BeginTransaction())
                    {
                        try
                        {
                            using (OdbcCommand cmd = new OdbcCommand(queryIsolation, connection, transaction))
                            {
                                int execute = cmd.ExecuteNonQuery();
                            }

                            using (OdbcCommand cmd = new OdbcCommand(queryUpdate, connection, transaction))
                            {
                                int rowsAffected = cmd.ExecuteNonQuery();
                                if (rowsAffected < 1)
                                {
                                    throw new Exception($@"No se actualizaron los registros");
                                }
                            }

                            using (OdbcCommand cmd = new OdbcCommand(queryInsert, connection, transaction))
                            {
                                int rowsAffected = cmd.ExecuteNonQuery();
                                if (rowsAffected < 1)
                                {
                                    throw new Exception($@"No se inserto el seguimiento");
                                }
                            }

                            result.Correct = true;
                            result.Object = idUni;
                            transaction.Commit();
                        }
                        catch (Exception ex)
                        {
                            transaction.Rollback();
                            throw;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                result.Correct = false;
                result.Message = $@"{ex.Message}";
            }
            return result;
        }
        /*Obtener las cosas para asignar*/
        public static ML.Result GetSchemas(string mode)
        {
            ML.Result result = new ML.Result();
            try
            {
                using (OdbcConnection connection = new OdbcConnection(DL.Connection.GetConnectionStringGen(mode)))
                {
                    connection.Open();

                    string query = $@"SELECT pto_alm, estatus, fec_ent, tip_ent, TRIM(pre_fol), scn_can, unidades
                                    FROM ora_crouting
                                    ORDER BY 
                                        CASE WHEN estatus = 1 THEN 1 ELSE 0 END,
                                        estatus ASC";

                    List<ML.DeliveryPlanner.Schema> schemaList = new List<ML.DeliveryPlanner.Schema>();

                    using(OdbcCommand cmd =  new OdbcCommand(query, connection))
                    {
                        using (OdbcDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read()) 
                            {
                                ML.DeliveryPlanner.Schema schema = new ML.DeliveryPlanner.Schema();

                                schema.PtoAlm = reader.GetString(0);
                                schema.Estatus = reader.GetString(1);
                                schema.FecEnt = reader.GetDateTime(2).ToString("ddMMyyyy"); ;
                                schema.TipEnt = reader.GetString(3);
                                schema.PreFol = reader.GetString(4);
                                schema.ScnCan = reader.GetString(5);
                                schema.Unidades = reader.GetString(6);

                                schemaList.Add(schema);
                            }
                        }
                    }

                    result.Correct = true;
                    result.Object = schemaList;
                }
            }
            catch (Exception ex)
            {
                result.Correct = false;
                result.Message = $@"Error al obtener las planeaciones {ex.Message}";
            }
            return result;
        }

        /*Cambia de PreAsignado a asignado*/
        public static ML.Result CreateRouting(ML.DeliveryPlanner.Schema schema, string mode)
        {
            ML.Result result = new ML.Result();
            try
            {
                ML.Result resultGetSchemaRoute = GetSchemaRoute(schema, mode);
                if (!resultGetSchemaRoute.Correct)
                {
                    throw new Exception($@"{resultGetSchemaRoute.Message}");
                }
                ML.DeliveryPlanner.RouteSchema routeSchema = (ML.DeliveryPlanner.RouteSchema)resultGetSchemaRoute.Object;

                List<List<RouteLines>> routesGenerated = GenerateRoutes(routeSchema,schema.Unidades, mode);

                ML.Result resultAssignRoutes = AssignRoutes(routesGenerated, mode);
                if (!resultAssignRoutes.Correct)
                {
                    throw new Exception($@"{resultAssignRoutes.Message}");
                }

                result.Correct = true;
            }
            catch (Exception ex)
            {
                result.Correct = false;
                result.Message = $@"Error al crear ruta: {ex.Message}";
            }
            return result;
        }
        private static ML.Result GetSchemaRoute(ML.DeliveryPlanner.Schema schema, string mode)
        {
            ML.Result result = new ML.Result();
            try
            {
                ML.DeliveryPlanner.RouteSchema routeSchema = new ML.DeliveryPlanner.RouteSchema();
                routeSchema.ptoAlm = schema.PtoAlm;
                routeSchema.tipEnt = schema.TipEnt;
                routeSchema.preFol = schema.PreFol;
                routeSchema.routeCount = int.Parse(schema.ScnCan);
                routeSchema.Orders = new List<ML.DeliveryPlanner.RouteLines>();

                string queryIsolation = $@"SET ISOLATION TO DIRTY READ;";

                using (OdbcConnection connection = new OdbcConnection(DL.Connection.GetConnectionStringGen(mode)))
                {
                    connection.Open();

                    using (OdbcCommand cmd = new OdbcCommand(queryIsolation, connection))
                    {
                        int execute = cmd.ExecuteNonQuery();
                    }

                    string queryGetScn = $@"SELECT TRIM(A.pre_fol),A.pto_alm,A.num_scn,TRIM(A.ord_rel) AS ord_rel,
                                                A.fec_ent,A.tip_ent,A.div_ent,
                                                TRIM(Z.des_fam) AS des_fam,
                                                TRIM(C.pro_cli) AS edo_cli,
                                                TRIM(C.pob_cli) AS mun_cli,
                                                TRIM(D.sector) AS sector,
                                                C.cp_cli AS cod_pos,
                                                TRIM(C.col_cli) as col_cli,
                                                CASE WHEN (C.panel = 'N') THEN 'NO' ELSE 'SI' END AS panel,
                                                CASE WHEN (C.volado = 'N') THEN 'NO' ELSE 'SI' END AS volado,
                                                CASE WHEN (C.mas_gen = 'N') THEN 'NO' ELSE 'SI' END AS mas_gen,
                                                F.longitud,F.latitud
                                        FROM ora_drouting A, famil Z, edc_cab B,cli_direccion C, ora_sectores D,
                                                cli_coord F
                                        WHERE A.cod_emp = 1
                                        AND A.estatus = 2
                                        AND A.pto_alm = {routeSchema.ptoAlm}
                                        AND A.pre_fol = '{routeSchema.preFol}'
                                        AND A.tip_ent = '{routeSchema.tipEnt}'
                                        AND Z.cod_fam2 = A.div_ent
                                        AND Z.cod_fam3 = ' '
                                        AND Z.cod_fam4 = ' '
                                        AND Z.cod_fam5 = ' '
                                        AND B.cod_emp = A.cod_emp
                                        AND B.pto_alm = A.pto_alm
                                        AND B.num_scn = A.num_scn
                                        AND C.cod_emp = B.cod_emp
                                        AND C.cod_dir = B.cod_dir
                                        AND D.cod_emp = A.cod_emp
                                        AND D.pto_alm = A.pto_alm
                                        AND D.cod_postal = C.cp_cli
                                        AND F.cod_emp = B.cod_emp
                                        AND F.cod_cli = B.cod_cli
                                        ORDER BY 8,9,10,11,12,6,13,14,15";

                    using (OdbcCommand cmd = new OdbcCommand(queryGetScn, connection))
                    {
                        using (OdbcDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                ML.DeliveryPlanner.RouteLines order = new ML.DeliveryPlanner.RouteLines();

                                order.PreFol = reader.GetString(0);
                                order.PtoAlm = reader.GetString(1);
                                order.NumScn = reader.GetString(2);
                                order.OrdRel = reader.GetString(3);
                                order.FecEnt = reader.GetDateTime(4).ToString("ddMMyyyy");
                                order.TipEnt = reader.GetString(5);
                                order.DivEnt = reader.GetString(6);
                                order.DesFam = reader.GetString(7);
                                order.EdoCli = reader.GetString(8);
                                order.MunCli = reader.GetString(9);
                                order.Sector = reader.GetString(10);
                                order.CodPos = reader.GetString(11);
                                order.ColCli = reader.GetString(12);
                                order.Panel = reader.GetString(13);
                                order.Volado = reader.GetString(14);
                                order.MasGen = reader.GetString(15);
                                order.Longitud = reader.GetString(16);
                                order.Latitud = reader.GetString(17);

                                routeSchema.Orders.Add(order);
                            }
                        }
                    }
                }
                result.Correct = true;
                result.Object = routeSchema;
            }
            catch (Exception ex)
            {
                result.Correct = false;
                result.Message = $@"Error al obtener los datos de ruta {ex.Message}";
            }
            return result;
        }
        public static List<List<RouteLines>> GenerateRoutes(RouteSchema routeSchema, string unidades, string mode)
        {
            Func<RouteLines, object>[] priority =
            {
                o => o.Sector,
                o => o.CodPos,
                o => o.ColCli,
                o => o.Panel,
                o => o.Volado,
                o => o.MasGen,
                o => o.DivEnt
            };

            var orders = routeSchema.Orders.ToList();

            IOrderedEnumerable<RouteLines> ordered = orders.OrderBy(priority[0]);
            for (int i = 1; i < priority.Length; i++)
                ordered = ordered.ThenBy(priority[i]);

            var sortedOrders = ordered.ToList();

            int totalRoutes = int.Parse(unidades);
            int min = routeSchema.minOrdersPerRoute;
            int target = routeSchema.targetOrdersPerRoute;
            int max = routeSchema.maxOrdersPerRoute;

            List<List<RouteLines>> routes = new List<List<RouteLines>>(totalRoutes);

            for (int r = 0; r < totalRoutes; r++)
                routes.Add(new List<RouteLines>());

            int index = 0;

            for (int r = 0; r < totalRoutes; r++)
            {
                int remainingOrders = sortedOrders.Count - index;
                int remainingRoutes = totalRoutes - r;

                int expectedRemaining = remainingOrders - ((remainingRoutes - 1) * min);
                if (expectedRemaining < 0)
                    expectedRemaining = 0;

                int take = Math.Min(target, expectedRemaining);

                if (take < min)
                    take = min;

                if (take > max)
                    take = max;

                if (r == totalRoutes - 1)
                    take = remainingOrders;

                routes[r].AddRange(sortedOrders.Skip(index).Take(take));
                index += take;
            }

            foreach (var route in routes)
            {
                if (route.Count == 0)
                    continue;

                string originalPreFol = route[0].PreFol;
                string fecEnt = route[0].FecEnt;

                string consecutivo = GetConsecutivo(fecEnt, mode);
                string newPreFol = $"{originalPreFol}-{consecutivo}";

                foreach (var order in route)
                    order.PreFol = newPreFol;
            }

            return routes;
        }

        private static List<List<RouteLines>> GenerateRoutesFail(RouteSchema routeSchema, string mode)
        {
            // Prioridad de agrupamiento (editable)
            Func<RouteLines, object>[] priority =
            {
                o => o.Sector,
                o => o.CodPos,
                o => o.ColCli,
                o => o.Panel,
                o => o.Volado,
                o => o.MasGen,
                o => o.DivEnt
            };

            var orders = routeSchema.Orders.ToList();

            // 1. Ordenar según prioridades
            IOrderedEnumerable<RouteLines> ordered = orders.OrderBy(priority[0]);
            for (int i = 1; i < priority.Length; i++)
                ordered = ordered.ThenBy(priority[i]);

            var sortedOrders = ordered.ToList();

            int totalRoutes = routeSchema.routeCount;
            int target = routeSchema.targetOrdersPerRoute;

            List<List<RouteLines>> routes = new List<List<RouteLines>>(totalRoutes);

            // 2. Crear lista vacía por cada ruta
            for (int r = 0; r < totalRoutes; r++)
                routes.Add(new List<RouteLines>());

            // 3. Distribución de órdenes respetando límites
            int index = 0;

            for (int r = 0; r < totalRoutes; r++)
            {
                int remainingOrders = sortedOrders.Count - index;
                int remainingRoutes = totalRoutes - r;

                // Cantidad que debe quedar fuera para cumplir mínimos en las siguientes rutas
                int expectedRemaining = remainingOrders - ((remainingRoutes - 1) * routeSchema.minOrdersPerRoute);
                if (expectedRemaining < 0)
                    expectedRemaining = 0;

                // Cantidad a tomar para esta ruta
                int take = Math.Min(target, expectedRemaining);

                if (take < routeSchema.minOrdersPerRoute)
                    take = routeSchema.minOrdersPerRoute;

                if (take > routeSchema.maxOrdersPerRoute)
                    take = routeSchema.maxOrdersPerRoute;

                // Última ruta: tomar todo lo que quede
                if (r == totalRoutes - 1)
                    take = remainingOrders;

                routes[r].AddRange(sortedOrders.Skip(index).Take(take));
                index += take;
            }

            // 4. Asignar preFol nuevo por ruta
            foreach (var route in routes)
            {
                if (route.Count == 0)
                    continue;

                string originalPreFol = route[0].PreFol;
                string fecEnt = route[0].FecEnt;

                string consecutivo = GetConsecutivo(fecEnt, mode);
                string newPreFol = $"{originalPreFol}-{consecutivo}";

                foreach (var order in route)
                    order.PreFol = newPreFol;
            }

            return routes;
        }
        private static ML.Result AssignRoutes(List<List<RouteLines>> listRoutes, string mode)
        {
            ML.Result result = new ML.Result();
            try
            {
                string queryIsolation = $@"SET ISOLATION TO DIRTY READ;";

                using (OdbcConnection connection = new OdbcConnection(DL.Connection.GetConnectionStringGen(mode)))
                {
                    connection.Open();
                    using (OdbcTransaction transaction = connection.BeginTransaction())
                    {
                        try
                        {
                            using (OdbcCommand cmd = new OdbcCommand(queryIsolation, connection, transaction))
                            {
                                int rowsAffected = cmd.ExecuteNonQuery();
                            }

                            foreach (List<RouteLines> route in listRoutes)
                            {
                                string preFol = route.First().PreFol;
                                string ptoAlm = route.First().PtoAlm;
                                string tipEnt = route.First().TipEnt;
                                string fecEnt = route.First().FecEnt;
                                string allOrd = string.Join(",",
                                            route.Select(x => $"'{x.OrdRel}'"));

                                string queryInsert = $@"INSERT INTO ora_hrouting(cod_emp,pto_alm,estatus,fec_gen,rut_typ,pre_fol)
                                            	        VALUES (1,{ptoAlm},0,'{fecEnt}','{tipEnt}','{preFol}');";

                                string queryUpdate = $@"UPDATE ora_crouting
                                                    SET estatus = 2
                                                    WHERE cod_emp = 1
                                                    AND pto_alm = {ptoAlm}
                                                    AND pre_fol = '{preFol.Split("-")[0]}'";
                                /*ora_crouting.estatus*
                                 * estatus 0 Preparado para asignación 
                                 * estatus 1 Asignado
                                 * estatus 2 PreAsignado
                                 */

                                string query = $@"UPDATE
                                                 ora_drouting
                                                SET pre_fol = '{preFol}',
                                                    estatus = 3
                                                WHERE cod_emp = 1
                                                AND pto_alm = 870
                                                AND estatus = 2
                                                AND tip_ent = '{tipEnt}'
                                                AND ord_rel IN ({allOrd})";
                                /* ora_drouting.estatus
                                 * estatus 0 default
                                 * estatus 1 Asignado
                                 * estatus 2 Apartado para asignación
                                 * estatus 3 PreAsignado
                                 */

                                using (OdbcCommand cmd = new OdbcCommand(query, connection, transaction))
                                {
                                    int rowsAffected = cmd.ExecuteNonQuery();
                                    if (rowsAffected < 1)
                                    {
                                        throw new Exception($@"No se actualizo ningun registro del detalle de ruta");
                                    }
                                }

                                using (OdbcCommand cmd = new OdbcCommand(queryUpdate, connection, transaction))
                                {
                                    int rowsAffected = cmd.ExecuteNonQuery();
                                    if (rowsAffected < 1)
                                    {
                                        throw new Exception($@"No se actualizo ningun registro del control de ruta");
                                    }
                                }

                                using (OdbcCommand cmd = new OdbcCommand(queryInsert, connection, transaction))
                                {
                                    int rowsAffected = cmd.ExecuteNonQuery();
                                    if (rowsAffected < 1)
                                    {
                                        throw new Exception($@"No inserto la cabecera de la ruta");
                                    }
                                }
                            }

                            result.Correct = true;
                            transaction.Commit();
                        }
                        catch (Exception ex)
                        {
                            transaction.Rollback();
                            throw;
                        }
                        
                    }
                }
            }
            catch (Exception ex)
            {
                result.Correct = false;
                result.Message = $@"Error al grabar la informacion {ex.Message}";
            }
            return result;
        }
        private static string GetConsecutivo(string fecEnt, string mode) 
        {
            string con = string.Empty;
            try
            {
                using(OdbcConnection connection = new OdbcConnection(DL.Connection.GetConnectionStringGen(mode)))
                {
                    connection.Open();

                    string queryTest = $@"SELECT COUNT(*)
                                        FROM ora_routing_fol
                                        WHERE cod_emp = 1
                                        AND fec_fol = '{fecEnt}'";

                    bool exist;

                    using(OdbcCommand cmd = new OdbcCommand(queryTest, connection))
                    {
                        using (OdbcDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                exist = reader.GetInt32(0) > 0 ? true : false;
                            }
                            else
                            {
                                throw new Exception($@"Error al leer folio");
                            }
                        }
                    }

                    if (exist)
                    {

                        string queryUpdate = $@"UPDATE ora_routing_fol
                                                    SET counter = counter+1
                                                    WHERE cod_emp = 1
                                                    AND fec_fol = '{fecEnt}'";

                        using (OdbcCommand cmd = new OdbcCommand(queryUpdate, connection))
                        {
                            int rowsAffected = cmd.ExecuteNonQuery();
                            if (rowsAffected < 1)
                            {
                                throw new Exception($@"Error al leer folio");
                            }
                        }

                        string queryGet = $@"SELECT counter
                                        FROM ora_routing_fol
                                        WHERE cod_emp = 1
                                        AND fec_fol = '{fecEnt}'";

                        using (OdbcCommand cmd = new OdbcCommand(queryGet, connection))
                        {
                            using (OdbcDataReader reader = cmd.ExecuteReader())
                            {
                                if (reader.Read())
                                {
                                    con = reader.GetInt32(0).ToString("D3");
                                }
                                else
                                {
                                    throw new Exception($@"Error al leer folio");
                                }
                            }
                        }


                    }
                    else
                    {
                        string queryInsert = $@"INSERT INTO ora_routing_fol (cod_emp,fec_fol,counter) VALUES (1,'{fecEnt}',1)";

                        using (OdbcCommand cmd = new OdbcCommand(queryInsert, connection))
                        {
                            int rowsAffected = cmd.ExecuteNonQuery();
                            if(rowsAffected < 1)
                            {
                                throw new Exception($@"Error al leer folio");
                            }
                            con = "001";
                        }
                    }
                }
            }
            catch (Exception ex)
            {

                throw;
            }
            return con;
        }
        /**/
        
        /*Descargar La pre-planeacion*/
        private static ML.Result GetPlaning(ML.DeliveryPlanner.Schema schema, string mode)
        {
            ML.Result result = new ML.Result();
            try
            {
                ML.DeliveryPlanner.RouteSchema routeSchema = new ML.DeliveryPlanner.RouteSchema();
                routeSchema.ptoAlm = schema.PtoAlm;
                routeSchema.tipEnt = schema.TipEnt;
                routeSchema.preFol = schema.PreFol;
                routeSchema.routeCount = int.Parse(schema.ScnCan);
                routeSchema.Orders = new List<ML.DeliveryPlanner.RouteLines>();

                string queryIsolation = $@"SET ISOLATION TO DIRTY READ;";

                using (OdbcConnection connection = new OdbcConnection(DL.Connection.GetConnectionStringGen(mode)))
                {
                    connection.Open();

                    using (OdbcCommand cmd = new OdbcCommand(queryIsolation, connection))
                    {
                        int execute = cmd.ExecuteNonQuery();
                    }

                    string queryGetScn = $@"SELECT A.pre_fol,A.pto_alm,A.num_scn,TRIM(A.ord_rel) AS ord_rel,
                                                A.fec_ent,A.tip_ent,A.div_ent,
                                                TRIM(Z.des_fam) AS des_fam,
                                                TRIM(C.pro_cli) AS edo_cli,
                                                TRIM(C.pob_cli) AS mun_cli,
                                                TRIM(D.sector) AS sector,
                                                C.cp_cli AS cod_pos,
                                                TRIM(C.col_cli) as col_cli,
                                                CASE WHEN (C.panel = 'N') THEN 'NO' ELSE 'SI' END AS panel,
                                                CASE WHEN (C.volado = 'N') THEN 'NO' ELSE 'SI' END AS volado,
                                                CASE WHEN (C.mas_gen = 'N') THEN 'NO' ELSE 'SI' END AS mas_gen,
                                                F.longitud,F.latitud
                                        FROM ora_drouting A, famil Z, edc_cab B,cli_direccion C, ora_sectores D,
                                                cli_coord F
                                        WHERE A.cod_emp = 1
                                        AND A.estatus = 0
                                        AND A.pto_alm = {routeSchema.ptoAlm}
                                        AND A.pre_fol LIKE '{routeSchema.preFol}-%'
                                        AND A.tip_ent = '{routeSchema.tipEnt}'
                                        AND Z.cod_fam2 = A.div_ent
                                        AND Z.cod_fam3 = ' '
                                        AND Z.cod_fam4 = ' '
                                        AND Z.cod_fam5 = ' '
                                        AND B.cod_emp = A.cod_emp
                                        AND B.pto_alm = A.pto_alm
                                        AND B.num_scn = A.num_scn
                                        AND C.cod_emp = B.cod_emp
                                        AND C.cod_dir = B.cod_dir
                                        AND D.cod_emp = A.cod_emp
                                        AND D.pto_alm = A.pto_alm
                                        AND D.cod_postal = C.cp_cli
                                        AND F.cod_emp = B.cod_emp
                                        AND F.cod_cli = B.cod_cli
                                        ORDER BY A.pre_fol";

                    using (OdbcCommand cmd = new OdbcCommand(queryGetScn, connection))
                    {
                        using (OdbcDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                ML.DeliveryPlanner.RouteLines order = new ML.DeliveryPlanner.RouteLines();

                                order.PreFol = reader.GetString(0);
                                order.PtoAlm = reader.GetString(1);
                                order.NumScn = reader.GetString(2);
                                order.OrdRel = reader.GetString(3);
                                order.FecEnt = reader.GetString(4);
                                order.TipEnt = reader.GetString(5);
                                order.DivEnt = reader.GetString(6);
                                order.DesFam = reader.GetString(7);
                                order.EdoCli = reader.GetString(8);
                                order.MunCli = reader.GetString(9);
                                order.Sector = reader.GetString(10);
                                order.CodPos = reader.GetString(11);
                                order.ColCli = reader.GetString(12);
                                order.Panel = reader.GetString(13);
                                order.Volado = reader.GetString(14);
                                order.MasGen = reader.GetString(15);
                                order.Longitud = reader.GetString(16);
                                order.Latitud = reader.GetString(17);

                                routeSchema.Orders.Add(order);
                            }
                        }
                    }
                }
                result.Correct = true;
                result.Object = routeSchema;
            }
            catch (Exception ex)
            {
                result.Correct = false;
                result.Message = $@"Error al obtener los datos de ruta {ex.Message}";
            }
            return result;
        }

        
        
        /*GetRoutesGenerated*/
        private static ML.Result GetOrders(ML.DeliveryPlanner.Schema schema, string mode)
        {
            ML.Result result = new ML.Result();
            try
            {
                ML.DeliveryPlanner.RouteSchema routeSchema = new ML.DeliveryPlanner.RouteSchema();
                routeSchema.ptoAlm = schema.PtoAlm;
                routeSchema.tipEnt = schema.TipEnt;
                routeSchema.preFol = schema.PreFol;
                routeSchema.routeCount = int.Parse(schema.ScnCan);
                routeSchema.Orders = new List<ML.DeliveryPlanner.RouteLines>();

                string queryIsolation = $@"SET ISOLATION TO DIRTY READ;";

                using (OdbcConnection connection = new OdbcConnection(DL.Connection.GetConnectionStringGen(mode)))
                {
                    connection.Open();

                    using (OdbcCommand cmd = new OdbcCommand(queryIsolation, connection))
                    {
                        int execute = cmd.ExecuteNonQuery();
                    }

                    string queryGetScn = $@"SELECT A.pre_fol,A.pto_alm,A.num_scn,TRIM(A.ord_rel) AS ord_rel,
                                                A.fec_ent,A.tip_ent,A.div_ent,
                                                TRIM(Z.des_fam) AS des_fam,
                                                TRIM(C.pro_cli) AS edo_cli,
                                                TRIM(C.pob_cli) AS mun_cli,
                                                TRIM(D.sector) AS sector,
                                                C.cp_cli AS cod_pos,
                                                TRIM(C.col_cli) as col_cli,
                                                CASE WHEN (C.panel = 'N') THEN 'NO' ELSE 'SI' END AS panel,
                                                CASE WHEN (C.volado = 'N') THEN 'NO' ELSE 'SI' END AS volado,
                                                CASE WHEN (C.mas_gen = 'N') THEN 'NO' ELSE 'SI' END AS mas_gen,
                                                F.longitud,F.latitud
                                        FROM ora_drouting A, famil Z, edc_cab B,cli_direccion C, ora_sectores D,
                                                cli_coord F
                                        WHERE A.cod_emp = 1
                                        AND A.estatus = 0
                                        AND A.pto_alm = {routeSchema.ptoAlm}
                                        AND A.pre_fol LIKE '{routeSchema.preFol}-%'
                                        AND A.tip_ent = '{routeSchema.tipEnt}'
                                        AND Z.cod_fam2 = A.div_ent
                                        AND Z.cod_fam3 = ' '
                                        AND Z.cod_fam4 = ' '
                                        AND Z.cod_fam5 = ' '
                                        AND B.cod_emp = A.cod_emp
                                        AND B.pto_alm = A.pto_alm
                                        AND B.num_scn = A.num_scn
                                        AND C.cod_emp = B.cod_emp
                                        AND C.cod_dir = B.cod_dir
                                        AND D.cod_emp = A.cod_emp
                                        AND D.pto_alm = A.pto_alm
                                        AND D.cod_postal = C.cp_cli
                                        AND F.cod_emp = B.cod_emp
                                        AND F.cod_cli = B.cod_cli
                                        ORDER BY A.pre_fol";

                    using (OdbcCommand cmd = new OdbcCommand(queryGetScn, connection))
                    {
                        using (OdbcDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                ML.DeliveryPlanner.RouteLines order = new ML.DeliveryPlanner.RouteLines();

                                order.PreFol = reader.GetString(0);
                                order.PtoAlm = reader.GetString(1);
                                order.NumScn = reader.GetString(2);
                                order.OrdRel = reader.GetString(3);
                                order.FecEnt = reader.GetString(4);
                                order.TipEnt = reader.GetString(5);
                                order.DivEnt = reader.GetString(6);
                                order.DesFam = reader.GetString(7);
                                order.EdoCli = reader.GetString(8);
                                order.MunCli = reader.GetString(9);
                                order.Sector = reader.GetString(10);
                                order.CodPos = reader.GetString(11);
                                order.ColCli = reader.GetString(12);
                                order.Panel = reader.GetString(13);
                                order.Volado = reader.GetString(14);
                                order.MasGen = reader.GetString(15);
                                order.Longitud = reader.GetString(16);
                                order.Latitud = reader.GetString(17);

                                routeSchema.Orders.Add(order);
                            }
                        }
                    }
                }
                result.Correct = true;
                result.Object = routeSchema;
            }
            catch (Exception ex)
            {
                result.Correct = false;
                result.Message = $@"Error al obtener los datos de ruta {ex.Message}";
            }
            return result;
        }

        
        /**/
        /**/
        /**/
        /**/
        /**/
    }
}
