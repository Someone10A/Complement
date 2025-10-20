using ML.LastMileDelivery;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.Odbc;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace BL.LastMileDelivery
{
    public class LastMileDelivery
    {
        public static ML.Result GetShipmentByShipment(string date, string cod_pto, string mode)
        {
            ML.Result result = new ML.Result();
            try
            {
                string queryOpen = $@"SET ISOLATION TO DIRTY READ;";
                string queryBuildWmsData = $@"SELECT car_sal,DATE(fec_car) AS fec_car, num_scn,ord_rel
                                        FROM ora_ruta
                                        WHERE DATE(fec_car) = '{date}'
                                        INTO TEMP dataembwms
                                        ";
                string queryBuidLgaData = GetQueryLGA(date);
                string queryUnion = $@"SELECT num_scn
                                            FROM dataembwms
                                            UNION ALL
                                            SELECT num_scn
                                            FROM dataemblga
                                            INTO TEMP dataallscn";
                string queryUnique = $@"SELECT DISTINCT num_scn
                                            FROM dataallscn
                                            INTO TEMP datascn";
                string queryGet = $@"SELECT A.num_scn,
	                                    B.ord_rel AS ord_rel_wms,
	                                    B.fec_car AS fec_car_wms,
	                                    B.car_sal AS car_sal_wms,
	                                    CASE WHEN (E.intentos IS NULL)
	                                    THEN
	                                        D.cod_pto||D.num_edc||'000'
	                                    ELSE
	                                        D.cod_pto||D.num_edc||LPAD(TO_CHAR(E.intentos),3,'0')
	                                    END AS ord_rel_lga,
	                                    C.fec_car AS fec_car_lga,
	                                    C.folio_embarque AS car_sal_lga
                                    FROM datascn A
                                    LEFT JOIN dataembwms B ON B.num_scn = A.num_scn
                                    LEFT JOIN dataemblga C ON C.num_scn = A.num_scn
                                    JOIN edc_cab D ON D.num_scn = A.num_scn
                                    LEFT JOIN ora_rt_envio E ON E.cod_pto = D.cod_pto
			                                    AND E.num_edc = D.num_edc
			                                    AND E.num_scn = D.num_scn
                                    Order by 4, 7";
                string queryDropWmsData = $@"DROP TABLE dataembwms;";
                string queryDropLgaData = $@"DROP TABLE dataemblga;";
                string queryDropUnion = $@"DROP TABLE dataallscn;";
                string queryDropUnique = $@"DROP TABLE datascn;";

                DataTable table = new DataTable();

                using (OdbcConnection connection = new OdbcConnection(DL.Connection.GetConnectionStringGen(mode)))
                {
                    connection.Open();

                    using (OdbcCommand cmd = new OdbcCommand(queryOpen, connection))
                    {
                        cmd.ExecuteNonQuery();
                    }

                    using (OdbcCommand cmd = new OdbcCommand(queryBuildWmsData, connection))
                    {
                        cmd.ExecuteNonQuery();
                    }
                    using (OdbcCommand cmd = new OdbcCommand(queryBuidLgaData, connection))
                    {
                        cmd.ExecuteNonQuery();
                    }
                    using (OdbcCommand cmd = new OdbcCommand(queryUnion, connection))
                    {
                        cmd.ExecuteNonQuery();
                    }
                    using (OdbcCommand cmd = new OdbcCommand(queryUnique, connection))
                    {
                        cmd.ExecuteNonQuery();
                    }

                    using (OdbcDataAdapter adapter = new OdbcDataAdapter(queryGet, connection))
                    {
                        adapter.Fill(table);
                    }

                    using (OdbcCommand cmd = new OdbcCommand(queryDropWmsData, connection))
                    {
                        cmd.ExecuteNonQuery();
                    }
                    using (OdbcCommand cmd = new OdbcCommand(queryDropLgaData, connection))
                    {
                        cmd.ExecuteNonQuery();
                    }
                    using (OdbcCommand cmd = new OdbcCommand(queryDropUnion, connection))
                    {
                        cmd.ExecuteNonQuery();
                    }
                    using (OdbcCommand cmd = new OdbcCommand(queryDropUnique, connection))
                    {
                        cmd.ExecuteNonQuery();
                    }
                }

                List<ML.LastMileDelivery.ShipmentInfo> shipments = new List<ML.LastMileDelivery.ShipmentInfo>();

                foreach (DataRow row in table.Rows)
                {
                    var shipment = new ShipmentInfo
                    {
                        num_scn = row["num_scn"]?.ToString(),
                        ord_rel_wms = row.IsNull("ord_rel_wms") ? null : row["ord_rel_wms"].ToString(),
                        fec_car_wms = row.IsNull("fec_car_wms") ? null : row["fec_car_wms"].ToString(),
                        car_sal_wms = row.IsNull("car_sal_wms") ? null : row["car_sal_wms"].ToString(),
                        ord_rel_lga = row.IsNull("ord_rel_lga") ? null : row["ord_rel_lga"].ToString(),
                        fec_car_lga = row.IsNull("fec_car_lga") ? null : row["fec_car_lga"].ToString(),
                        car_sal_lga = row.IsNull("car_sal_lga") ? null : row["car_sal_lga"].ToString()
                    };

                    shipments.Add(shipment);
                }

                result.Correct = true;
                result.Object = shipments;
            }
            catch (Exception ex)
            {
                result.Correct = false;
                result.Message = ex.Message;
            }
            return result;
        }
        private static string GetQueryLGA(string date)
        {
            string today = DateTime.Now.ToString("ddMMyyyy");
            if (date == today)
            {
                return $@"SELECT DISTINCT A.folio_embarque, DATE(f_h_fin_embar) AS fec_car, 
		                            E.sales_check[1,16] AS num_scn
                            FROM dblga@lga_prod:lgahembrqe A, dblga@lga_prod:lgadembrqe B,
		                            dblga@lga_prod:lgaetiqeta C,
                                    dblga@lga_prod:lgadventa D, dblga@lga_prod:lgahventa E
                            WHERE A.cod_empresa = 1
                            AND A.cd_id = 870
                            AND A.st_embarque = 3
                            AND DATE(A.f_h_ini_embar) > '03102025'
                            AND A.f_h_fin_embar IS NOT NULL
                            AND DATE(A.f_h_fin_embar)  = TODAY
                            AND B.cod_empresa = A.cod_empresa
                            AND B.folio_embarque = A.folio_embarque
                            AND B.cd_id = A.cd_id
                            AND B.st_det_emb = 3
                            AND C.cod_empresa = B.cod_empresa
                            AND C.no_conoc = B.no_conoc
                            AND C.cd_id = B.cd_id
                            AND D.cod_empresa = C.cod_empresa
                            AND D.cd_id = C.cd_id
                            AND D.no_etiqueta = C.no_etiqueta
                            AND E.cod_empresa = D.cod_empresa
                            AND E.cd_id = D.cd_id
                            AND E.sales_check = D.sales_check
                            AND E.tip_entrega = 1
                            INTO TEMP dataemblga";
            }
            else 
            {
                return $@"SELECT folio_embarque, fec_car, num_scn
                            FROM ora_lga_emb
                            WHERE fec_car = '{date}'
                            INTO TEMP dataemblga";
            }
        }
        /*--------------------*/
        public static ML.Result GetShipmentByQuery(ML.LastMileDelivery.QueryInfo queryInfo, string mode)
        {
            ML.Result result = new ML.Result();
            try
            {
                string queryOpen = $@"SET ISOLATION TO DIRTY READ;";
                string queryBuildLeft = GetLeftTable(queryInfo);
                string queryBuildRight = GetRigthTable(queryInfo);
                string queryUnion = $@"SELECT num_scn
                                        FROM dataembleft
                                        UNION ALL
                                        SELECT num_scn
                                        FROM dataembright
                                        WHERE num_scn IN (SELECT num_scn FROM dataembleft)
                                        INTO TEMP dataallscn";
                string queryUnique = $@"SELECT DISTINCT num_scn
                                            FROM dataallscn
                                            INTO TEMP datascn";
                string queryGet = GetQueryGet(queryInfo);
                string queryDropWmsData = $@"DROP TABLE dataembleft;";
                string queryDropLgaData = $@"DROP TABLE dataembright;";
                string queryDropUnion = $@"DROP TABLE dataallscn;";
                string queryDropUnique = $@"DROP TABLE datascn;";

                DataTable table = new DataTable();

                using (OdbcConnection connection = new OdbcConnection(DL.Connection.GetConnectionStringGen(mode)))
                {
                    connection.Open();

                    using (OdbcCommand cmd = new OdbcCommand(queryOpen, connection))
                    {
                        cmd.ExecuteNonQuery();
                    }

                    using (OdbcCommand cmd = new OdbcCommand(queryBuildLeft, connection))
                    {
                        cmd.ExecuteNonQuery();
                    }
                    using (OdbcCommand cmd = new OdbcCommand(queryBuildRight, connection))
                    {
                        cmd.ExecuteNonQuery();
                    }
                    using (OdbcCommand cmd = new OdbcCommand(queryUnion, connection))
                    {
                        cmd.ExecuteNonQuery();
                    }
                    using (OdbcCommand cmd = new OdbcCommand(queryUnique, connection))
                    {
                        cmd.ExecuteNonQuery();
                    }

                    using (OdbcDataAdapter adapter = new OdbcDataAdapter(queryGet, connection))
                    {
                        adapter.Fill(table);
                    }

                    using (OdbcCommand cmd = new OdbcCommand(queryDropWmsData, connection))
                    {
                        cmd.ExecuteNonQuery();
                    }
                    using (OdbcCommand cmd = new OdbcCommand(queryDropLgaData, connection))
                    {
                        cmd.ExecuteNonQuery();
                    }
                    using (OdbcCommand cmd = new OdbcCommand(queryDropUnion, connection))
                    {
                        cmd.ExecuteNonQuery();
                    }
                    using (OdbcCommand cmd = new OdbcCommand(queryDropUnique, connection))
                    {
                        cmd.ExecuteNonQuery();
                    }
                }

                List<ML.LastMileDelivery.ShipmentInfo> shipments = new List<ML.LastMileDelivery.ShipmentInfo>();

                foreach (DataRow row in table.Rows)
                {
                    var shipment = new ShipmentInfo
                    {
                        num_scn = row["num_scn"]?.ToString(),
                        ord_rel_wms = row.IsNull("ord_rel_wms") ? null : row["ord_rel_wms"].ToString(),
                        fec_car_wms = row.IsNull("fec_car_wms") ? null : row["fec_car_wms"].ToString(),
                        car_sal_wms = row.IsNull("car_sal_wms") ? null : row["car_sal_wms"].ToString(),
                        ord_rel_lga = row.IsNull("ord_rel_lga") ? null : row["ord_rel_lga"].ToString(),
                        fec_car_lga = row.IsNull("fec_car_lga") ? null : row["fec_car_lga"].ToString(),
                        car_sal_lga = row.IsNull("car_sal_lga") ? null : row["car_sal_lga"].ToString()
                    };

                    shipments.Add(shipment);
                }

                result.Correct = true;
                result.Object = shipments;
            }
            catch (Exception ex)
            {
                result.Correct = false;
                result.Message = ex.Message;
            }
            return result;
        }
        private static string GetLeftTable(ML.LastMileDelivery.QueryInfo queryInfo)
        {
            if (queryInfo.pivotTable == "WMS")
            {
                return $@"SELECT car_sal,DATE(fec_car) AS fec_car, num_scn,ord_rel
                            FROM ora_ruta
                            WHERE pto_alm = {queryInfo.cod_pto}
                            AND DATE(fec_car) = '{queryInfo.fec_pri}'
                            INTO TEMP dataembleft";
            }
            else
            {
                return $@"SELECT folio_embarque, fec_car, num_scn
                            FROM ora_lga_emb
                            WHERE pto_alm = {queryInfo.cod_pto}
                            AND fec_car = '{queryInfo.fec_pri}'
                            INTO TEMP dataembleft";
            }
        }
        private static string GetRigthTable(ML.LastMileDelivery.QueryInfo queryInfo)
        {
            if (queryInfo.pivotTable == "WMS")
            {
                return $@"SELECT folio_embarque, fec_car, num_scn
                            FROM ora_lga_emb
                            WHERE pto_alm = {queryInfo.cod_pto}
                            AND fec_car >= '{queryInfo.fec_sec_min}'
                            AND fec_car <= '{queryInfo.fec_sec_max}'
                            INTO TEMP dataembright";
            }
            else
            {
                return $@"SELECT car_sal,DATE(fec_car) AS fec_car, num_scn,ord_rel
                            FROM ora_ruta
                            WHERE pto_alm = {queryInfo.cod_pto}
                            AND DATE(fec_car) >= '{queryInfo.fec_sec_min}'
                            AND DATE(fec_car) <= '{queryInfo.fec_sec_max}'
                            INTO TEMP dataembright";
            }
        }
        private static string GetQueryGet(ML.LastMileDelivery.QueryInfo queryInfo)
        {
            if (queryInfo.pivotTable == "WMS")
            {
                return $@"SELECT A.num_scn,
	                            B.ord_rel AS ord_rel_wms,
	                            B.fec_car AS fec_car_wms,
	                            B.car_sal AS car_sal_wms,
	                            CASE WHEN (E.intentos IS NULL)
	                            THEN
	                                D.cod_pto||D.num_edc||'000'
	                            ELSE
	                                D.cod_pto||D.num_edc||LPAD(TO_CHAR(E.intentos),3,'0')
	                            END AS ord_rel_lga,
	                            C.fec_car AS fec_car_lga,
	                            C.folio_embarque AS car_sal_lga
                            FROM datascn A
                            LEFT JOIN dataembleft B ON B.num_scn = A.num_scn
                            LEFT JOIN dataembright C ON C.num_scn = A.num_scn
                            JOIN edc_cab D ON D.num_scn = A.num_scn
                            LEFT JOIN ora_rt_envio E ON E.cod_pto = D.cod_pto
			                            AND E.num_edc = D.num_edc
			                            AND E.num_scn = D.num_scn
                            Order by 4, 7";
            }
            else
            {
                return $@"SELECT A.num_scn,
	                            B.ord_rel AS ord_rel_wms,
	                            B.fec_car AS fec_car_wms,
	                            B.car_sal AS car_sal_wms,
	                            CASE WHEN (E.intentos IS NULL)
	                            THEN
		                            D.cod_pto||D.num_edc||'000'
	                            ELSE
		                            D.cod_pto||D.num_edc||LPAD(TO_CHAR(E.intentos),3,'0')
	                            END AS ord_rel_lga,
	                            C.fec_car AS fec_car_lga,
	                            C.folio_embarque AS car_sal_lga
                            FROM datascn A
                            LEFT JOIN dataembright B ON B.num_scn = A.num_scn
                            LEFT JOIN dataembleft C ON C.num_scn = A.num_scn
                            JOIN edc_cab D ON D.num_scn = A.num_scn
                            LEFT JOIN ora_rt_envio E ON E.cod_pto = D.cod_pto
			                            AND E.num_edc = D.num_edc
			                            AND E.num_scn = D.num_scn
                            AND B.ord_rel IS NOT NULL
                            Order by 4, 7";
            }
        }
    }
}
