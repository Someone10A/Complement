using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Odbc;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BL.TrackingManager
{
    public class TrackingManager
    {
        public static ML.Result GetTrackingPerDay(string date, string cod_pto, string mode)
        {
            ML.Result result = new ML.Result();
            try
            {
                using(OdbcConnection connection = new OdbcConnection(DL.Connection.GetConnectionStringGen(mode)))
                {
                    connection.Open();

                    string query = $@"SELECT car_sal,pto_alm,
	                                    CASE       
		                                    WHEN MIN(estatus) = 1 AND MAX(estatus) = 1 THEN 2
                                            WHEN MIN(estatus) = 0 AND MAX(estatus) = 0 THEN 0
                                            ELSE 1
                                        END AS estado_entrega
                                    FROM ora_ruta
                                    WHERE DATE(fec_car) =  '{date}'
                                    AND pto_alm = {cod_pto}
                                    GROUP BY 1,2
                                    ORDER BY 3 ASC";

                    List<ML.TrackingManager.OutboundShipment> outboundShipmentList = new List<ML.TrackingManager.OutboundShipment>();

                    using (OdbcCommand cmd = new OdbcCommand(query, connection))
                    {
                        using(OdbcDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                ML.TrackingManager.OutboundShipment outboundShipment = new ML.TrackingManager.OutboundShipment();

                                outboundShipment.car_sal = reader.GetString(0);
                                outboundShipment.pto_alm = reader.GetInt32(1).ToString();
                                outboundShipment.estatus = reader.GetInt32(2).ToString();

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
                result.Message = $@"Se obtuvo un error al consultar los viajes.";
            }
            return result;
        }
        public static ML.Result GetOrdersPerOutboundShipment(ML.TrackingManager.OutboundShipment outboundShipment, string mode)
        {
            ML.Result result = new ML.Result();
            try
            {
                using(OdbcConnection connection = new OdbcConnection(DL.Connection.GetConnectionStringGen(mode)))
                {
                    connection.Open();

                    string query = $@"SELECT
	                                        A.fec_car,
	                                        A.fec_act,
	                                        TRIM(A.car_sal) AS car_sal,
	                                        TRIM(A.ord_rel) AS ord_rel,
	                                        B.num_scn,
	                                        UPPER(TRIM(F.nom_cli)||' '||TRIM(F.ape1_cli)||' '||TRIM(F.ape2_cli)) ||':'|| TRIM(TO_CHAR(F.cod_cli)||REPLACE(F.nom_cli, ' ', '')) AS cliente,
	                                        A.estatus,
                                        CASE
	                                        WHEN A.estatus = 0 THEN '-Pendiente'
	                                        WHEN A.estatus = 1 THEN '-Cerrado'
	                                        WHEN A.estatus = 2 THEN '-Creando evento'
	                                        ELSE 'Estado No listado.'
                                        END AS estatus_ord_rel,
	                                        B.estado||C.estado AS estado,
                                        CASE
	                                        WHEN B.estado = 'P' THEN '-Retenido-Transito'				
	                                        WHEN B.estado = 'T' THEN '-Transito'
	                                        WHEN B.estado = 'I' THEN '-Impreso'
	                                        WHEN B.estado = 'X' THEN '-Cancelado'
	                                        WHEN B.estado = 'E' THEN '-Entregado'
	                                        WHEN B.estado = 'G' THEN '-Generado'
	                                        ELSE '-Estado desconocido'
                                        END AS estado_gnx,
	                                    CASE
	                                        WHEN D.rt_stat IS NULL THEN 'X'
	                                        ELSE TO_CHAR(D.rt_stat)
                                        END AS rt_stat,
                                        CASE
	                                        WHEN D.rt_stat IS NULL THEN 'Esperando interfaz de evento'
	                                        WHEN D.rt_stat = 1 THEN 'Interfaz ENTREGA para Genesix generada'
	                                        WHEN D.rt_stat = 2 THEN 'Generando ASN para WMS'
	                                        WHEN D.rt_stat = 3 THEN 'ASN Generado'
	                                        WHEN D.rt_stat = 4 THEN 'Interfaz Retención para Genesix generada'
	                                        WHEN D.rt_stat = 5 THEN 'Evento marcado por base-Generando transmision'
	                                        ELSE 'Estado No listado.'
                                        END AS estado_rt,
	                                    CASE
	                                        WHEN E.des_mot IS NULL THEN 'X'
	                                        ELSE TO_CHAR(E.cod_mot)
	                                    END AS cod_mot,
                                        CASE WHEN (E.des_mot IS NULL)
                                        THEN
	                                        '-Evento no registrado aun'
                                        ELSE
	                                        '-'||TRIM(E.des_mot)
                                        END AS motivo
                                        FROM ora_ruta A
                                        JOIN edc_cab B ON B.cod_emp = 1 AND B.num_scn = A.num_scn
                                        JOIN ordedc_cab C ON C.cod_emp = B.cod_emp AND C.cod_pto = B.cod_pto AND C.num_edc = B.num_edc
                                        LEFT JOIN ora_rt D ON D.cod_emp = B.cod_emp AND D.ord_rel = A.ord_rel
                                        LEFT JOIN ora_motivos_rt E ON E.cod_mot = D.cod_mot
                                        INNER JOIN clientes F ON F.cod_emp = B.cod_emp AND F.cod_cli = B.cod_cli
                                        WHERE A.pto_alm = {outboundShipment.pto_alm}
                                        AND A.car_sal = '{outboundShipment.car_sal}'
                                        ORDER BY A.estatus DESC";

                    List<ML.TrackingManager.TrackingManager> trackingManagertList = new List<ML.TrackingManager.TrackingManager>();

                    OdbcDataAdapter adapter = new OdbcDataAdapter(query, connection);

                    DataTable table = new DataTable();

                    adapter.Fill(table);


                    foreach (DataRow dataRow in table.Rows)
                    {
                        ML.TrackingManager.TrackingManager trackingManager = new ML.TrackingManager.TrackingManager
                        {
                            fec_car = dataRow["fec_car"].ToString(),
                            fec_act = dataRow["fec_act"].ToString(),
                            car_sal = dataRow["car_sal"].ToString(),
                            ord_rel = dataRow["ord_rel"].ToString(),
                            num_scn = dataRow["num_scn"].ToString(),
                            cliente = dataRow["cliente"].ToString(),
                            estatus = int.Parse(dataRow["estatus"].ToString()),
                            estatus_ord_rel = dataRow["estatus_ord_rel"].ToString(),
                            estado = dataRow["estado"].ToString(),
                            estado_gnx = dataRow["estado_gnx"].ToString(),
                            rt_stat = dataRow["rt_stat"].ToString(),
                            estado_rt = dataRow["estado_rt"].ToString(),
                            cod_mot = dataRow["cod_mot"].ToString(),
                            motivo = dataRow["motivo"].ToString(),
                        };

                        trackingManagertList.Add(trackingManager);
                    }

                    result.Correct = true;
                    result.Object = trackingManagertList;
                }
            }
            catch (Exception ex)
            {
                result.Correct = false;
                result.Message = $@"Se obtuvo un error al consultar los viajes.";
            }
            return result;
        }
        public static ML.Result GetDetail(string ord_rel, string mode)
        {
            ML.Result result = new ML.Result();
            try
            {
                using (OdbcConnection connection = new OdbcConnection(DL.Connection.GetConnectionStringGen(mode)))
                {
                    connection.Open();
                    string query = $@"SELECT TRIM(olpn)
                                    FROM ora_ruta_olpn
                                    WHERE ord_rel = '{ord_rel}'";

                    List<string> olpnList = new List<string>();

                    using(OdbcCommand cmd = new OdbcCommand(query, connection))
                    {
                        using(OdbcDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read()) 
                            { 
                                string olpn = reader.GetString(0);

                                olpnList.Add(olpn);
                            }
                        }
                    }
                    result.Correct = true;
                    result.Object = olpnList;
                }
            }
            catch (Exception ex) 
            {
                result.Correct = false;
                result.Message = $@"Se obtuvo un error al consultar los viajes.";
            }
            return result;  
        }
        public static ML.Result GetReturnedOrders(string pto_alm, string mode)
        {
            ML.Result result = new ML.Result();
            try
            {
                using (OdbcConnection connection = new OdbcConnection(DL.Connection.GetConnectionStringGen(mode)))
                {
                    connection.Open();

                    string query = $@"SELECT
	                                        A.fec_car,
	                                        A.fec_act,
	                                        TRIM(A.car_sal) AS car_sal,
	                                        TRIM(A.ord_rel) AS ord_rel,
	                                        B.num_scn,
	                                        UPPER(TRIM(F.nom_cli)||' '||TRIM(F.ape1_cli)||' '||TRIM(F.ape2_cli)) ||':'|| TRIM(TO_CHAR(F.cod_cli)||REPLACE(F.nom_cli, ' ', '')) AS cliente,
	                                        A.estatus,
                                        CASE
	                                        WHEN A.estatus = 0 THEN '-Pendiente'
	                                        WHEN A.estatus = 1 THEN '-Cerrado'
	                                        WHEN A.estatus = 2 THEN '-Creando evento'
	                                        ELSE 'Estado No listado.'
                                        END AS estatus_ord_rel,
	                                        B.estado||C.estado AS estado,
                                        CASE
	                                        WHEN B.estado = 'P' THEN '-Retenido-Transito'				
	                                        WHEN B.estado = 'T' THEN '-Transito'
	                                        WHEN B.estado = 'I' THEN '-Impreso'
	                                        WHEN B.estado = 'X' THEN '-Cancelado'
	                                        WHEN B.estado = 'E' THEN '-Entregado'
	                                        WHEN B.estado = 'G' THEN '-Generado'
	                                        ELSE '-Estado desconocido'
                                        END AS estado_gnx,
	                                    CASE
	                                        WHEN D.rt_stat IS NULL THEN 'X'
	                                        ELSE TO_CHAR(D.rt_stat)
                                        END AS rt_stat,
                                        CASE
	                                        WHEN D.rt_stat IS NULL THEN 'Esperando interfaz de evento'
	                                        WHEN D.rt_stat = 1 THEN 'Interfaz ENTREGA para Genesix generada'
	                                        WHEN D.rt_stat = 2 THEN 'Generando ASN para WMS'
	                                        WHEN D.rt_stat = 3 THEN 'ASN Generado'
	                                        WHEN D.rt_stat = 4 THEN 'Interfaz Retención para Genesix generada'
	                                        WHEN D.rt_stat = 5 THEN 'Evento marcado por base-Generando transmision'
	                                        ELSE 'Estado No listado.'
                                        END AS estado_rt,
	                                    CASE
	                                        WHEN E.des_mot IS NULL THEN 'X'
	                                        ELSE TO_CHAR(E.cod_mot)
	                                    END AS cod_mot,
                                        CASE WHEN (E.des_mot IS NULL)
                                        THEN
	                                        '-Evento no registrado aun'
                                        ELSE
	                                        '-'||TRIM(E.des_mot)
                                        END AS motivo
                                        FROM ora_ruta A
                                        JOIN edc_cab B ON B.cod_emp = 1 AND B.num_scn = A.num_scn
                                        JOIN ordedc_cab C ON C.cod_emp = B.cod_emp AND C.cod_pto = B.cod_pto AND C.num_edc = B.num_edc
                                        LEFT JOIN ora_rt D ON D.cod_emp = B.cod_emp AND D.ord_rel = A.ord_rel
                                        LEFT JOIN ora_motivos_rt E ON E.cod_mot = D.cod_mot
                                        INNER JOIN clientes F ON F.cod_emp = B.cod_emp AND F.cod_cli = B.cod_cli
                                        WHERE A.pto_alm = {pto_alm}
                                        AND D.rt_stat = 3
                                        ORDER BY A.estatus DESC";

                    List<ML.TrackingManager.TrackingManager> trackingManagertList = new List<ML.TrackingManager.TrackingManager>();

                    OdbcDataAdapter adapter = new OdbcDataAdapter(query, connection);

                    DataTable table = new DataTable();

                    adapter.Fill(table);


                    foreach (DataRow dataRow in table.Rows)
                    {
                        ML.TrackingManager.TrackingManager trackingManager = new ML.TrackingManager.TrackingManager
                        {
                            fec_car = dataRow["fec_car"].ToString(),
                            fec_act = dataRow["fec_act"].ToString(),
                            car_sal = dataRow["car_sal"].ToString(),
                            ord_rel = dataRow["ord_rel"].ToString(),
                            num_scn = dataRow["num_scn"].ToString(),
                            cliente = dataRow["cliente"].ToString(),
                            estatus = int.Parse(dataRow["estatus"].ToString()),
                            estatus_ord_rel = dataRow["estatus_ord_rel"].ToString(),
                            estado = dataRow["estado"].ToString(),
                            estado_gnx = dataRow["estado_gnx"].ToString(),
                            rt_stat = dataRow["rt_stat"].ToString(),
                            estado_rt = dataRow["estado_rt"].ToString(),
                            cod_mot = dataRow["cod_mot"].ToString(),
                            motivo = dataRow["motivo"].ToString(),
                        };

                        trackingManagertList.Add(trackingManager);
                    }

                    result.Correct = true;
                    result.Object = trackingManagertList;
                }
            }
            catch (Exception ex)
            {
                result.Correct = false;
                result.Message = $@"Se obtuvo un error al consultar los viajes.";
            }
            return result;
        }
    }
}
