using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data.Odbc;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

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
                                            END AS ord_rel,A.div_ent,
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
                                planInfo.Division = reader.GetString(5);
                                planInfo.EdoCli = reader.GetString(6);
                                planInfo.MunCli = reader.GetString(7);
                                planInfo.Sector = reader.GetString(8);
                                planInfo.CpCli = reader.GetString(9);
                                planInfo.ColCli = reader.GetString(10);
                                planInfo.NomCli = reader.GetString(11);
                                planInfo.Panel = reader.GetString(12);
                                planInfo.Volado = reader.GetString(13);
                                planInfo.MasGen = reader.GetString(14);
                                planInfo.FecEnt = reader.GetDateTime(15).ToString("ddMMyyyy");

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
        /*GetRoutesGenerated*/
        /**/
        /**/
        /**/
        /**/
        /**/
    }
}
