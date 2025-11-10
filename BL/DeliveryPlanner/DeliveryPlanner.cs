using System;
using System.Collections.Generic;
using System.Data.Odbc;
using System.Linq;
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
                string query = $@"SELECT A.num_scn,A.pto_alm,TRIM(C.pro_cli) edo_cli,TRIM(C.pob_cli) mun_cli,D.sector,C.cp_cli,
                                    C.panel,C.volado,C.mas_gen
                            FROM ora_confirmacion A,edc_cab B, cli_direccion C, ora_sectores D
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

                                                                ";
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

                                readyInfo.IsReady = true;
                                readyInfo.NumScn = reader.GetString(0);
                                readyInfo.PtoAlm = reader.GetString(1);
                                readyInfo.ProCli = reader.GetString(2);
                                readyInfo.PobCli = reader.GetString(3);
                                readyInfo.Sector = reader.GetString(4);
                                readyInfo.CpCli = reader.GetString(5);
                                readyInfo.Panel = reader.GetString(6);
                                readyInfo.Volado = reader.GetString(7);
                                readyInfo.MasGen = reader.GetString(8);

                                readyInfoList.Add(readyInfo);
                            }
                        }
                    }
                }

                result.Correct = true;
                result.Object = readyInfoList;
            }
            catch (Exception ex)
            {
                result.Correct = false;
                result.Message = $@"Error al obtener Ordenes aptas";
            }
            return result;
        }
        
        public static ML.Result x()
        {
            ML.Result result = new ML.Result();
            try
            {
                result.Correct = true;
            }
            catch (Exception ex)
            {
                result.Correct = false;
                result.Message = $@"Error al obtener Ordenes";
            }
            return result;
        }


        /*OrderFreeze*/
        /*CreateRouting*/

        /*GetRoutesGenerated*/
        /**/
        /**/
        /**/
        /**/
        /**/
    }
}
