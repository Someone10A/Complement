using System;
using System.Collections.Generic;
using System.Data.Odbc;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace BL.OutbondShipmentOperator
{
    public class OutbondShipment
    {
        /*GetShipmentsQualified*/
        public static ML.Result GetOutbondShipmentsQualified(string ptoAlm, string mode)
        {
            ML.Result result = new ML.Result();
            try
            {
                using (OdbcConnection connection = new OdbcConnection(DL.Connection.GetConnectionStringGen(mode)))
                {
                    connection.Open();

                    List<ML.OutbondShipmentOperator.OutboundShipment> outbondShipmentList = new List<ML.OutbondShipmentOperator.OutboundShipment>();
                    
                    string query = $@"SELECT UNIQUE 
                                                CASE WHEN B.car_sal IS NULL THEN 0 ELSE 1 END AS asignado,
                                                A.pto_alm,TRIM(A.car_sal) AS car_sal,A.fec_car,
                                                CASE
                                                WHEN (B.estatus IS NULL) THEN 'Ruta sin asigacion'
                                                WHEN (B.estatus = 0) THEN 'Ruta asignada'
                                                WHEN (B.estatus = 1) THEN 'Ruta Cerrada'
                                                WHEN (B.estatus = 2) THEN 'Ruta Abierta'
                                                WHEN (B.estatus = 3) THEN 'Ruta Finalizada'
                                                ELSE 'No Listado'
                                                END AS descripcion,
		                                        CASE
                                                WHEN(C.rfc_ope IS NULL) THEN 'S/D'
                                                ELSE TRIM(C.rfc_ope)
                                                END AS rfc_ope,
                                                CASE
                                                WHEN(C.rfc_ope IS NULL) THEN 'S/D'
                                                ELSE TRIM(C.nom_ope)
                                                END AS nom_ope
                                        FROM ora_ruta A
                                        LEFT JOIN ora_asignacion_operador B
                                                 ON B.cod_emp = 1
                                                AND B.pto_alm = A.pto_alm
                                                AND B.car_sal = A.car_sal
                                        LEFT JOIN ora_operadores C
                                                 ON C.cod_emp = B.cod_emp
                                                AND C.rfc_ope = B.rfc_ope
                                        WHERE A.estatus = 0
                                        AND A.car_sal NOT IN (SELECT car_sal FROM ora_ruta WHERE estatus = 1)
                                        AND A.ord_rel NOT IN (SELECT ord_rel FROM ora_rt)
                                        ORDER BY asignado ASC,A.fec_car DESC";

                    using(OdbcCommand cmd = new OdbcCommand(query, connection))
                    {
                        using (OdbcDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                ML.OutbondShipmentOperator.OutboundShipment outbondShipment = new ML.OutbondShipmentOperator.OutboundShipment();

                                outbondShipment.PtoAlm = reader.GetString(1);
                                outbondShipment.CarSal = reader.GetString(2);
                                outbondShipment.FecCar = reader.GetDateTime(3).ToString();
                                outbondShipment.Descripcion = reader.GetString(4).Trim();
                                outbondShipment.RfcOpe = reader.GetString(5);
                                outbondShipment.NomOpe = reader.GetString(6);

                                outbondShipmentList.Add(outbondShipment);
                            }
                        }
                    }

                    result.Correct = true;
                    result.Object = outbondShipmentList;
                }
            }
            catch (Exception ex)
            {
                result.Correct = false;
                result.Object = null;
                result.Message = $@"Error al obtener Cargas Aptas: {ex.Message}";
                result.Ex = ex;
            }
            return result;
        }
        /*GetOperadores*/
        public static ML.Result GetOperadoresActive(string mode)
        {
            ML.Result result = new ML.Result();
            try
            {
                using (OdbcConnection connection = new OdbcConnection(DL.Connection.GetConnectionStringGen(mode)))
                {
                    connection.Open();

                    string query = @"SELECT TRIM(rfc_ope),TRIM(nom_ope),active
                                    FROM ora_operadores
                                    WHERE active = 1
                                    AND rfc_ope NOT IN (SELECT rfc_ope
                                                    FROM ora_asignacion_operador
                                                    WHERE cod_emp = 1
                                                    AND estatus IN (0,2,3))
                                    ORDER BY active DESC, nom_ope ASC
                                    ";

                    List<ML.OutbondShipmentOperator.Operator> operatorList = new List<ML.OutbondShipmentOperator.Operator>();

                    using (OdbcCommand cmd = new OdbcCommand(query, connection))
                    {
                        using (OdbcDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                ML.OutbondShipmentOperator.Operator ope = new ML.OutbondShipmentOperator.Operator();

                                ope.RfcOpe = reader.GetString(0);
                                ope.NomOpe = reader.GetString(1);
                                ope.Active = reader.GetInt16(2).ToString();

                                operatorList.Add(ope);
                            }
                        }
                    }

                    result.Correct = true;
                    result.Object = operatorList;
                }
            }
            catch (Exception ex)
            {
                result.Correct = false;
                result.Message = $"Error al obtener operadores: {ex.Message}";
                result.Ex = ex;
            }
            return result;
        }
        /*InsertAssign*/
        public static ML.Result InsertAssign(ML.OutbondShipmentOperator.OutboundShipment outbondShipment, ML.OutbondShipmentOperator.Operator ope, string mode)
        {
            ML.Result result = new ML.Result();
            try
            {
                using (OdbcConnection connection = new OdbcConnection(DL.Connection.GetConnectionStringGen(mode)))
                {
                    connection.Open();

                    ML.Result resultValidateOpe = ValidateOpe(connection, ope.NomOpe);
                    if (!resultValidateOpe.Correct)
                    {
                        throw new Exception($@"{resultValidateOpe.Message}");
                    }
                    List<ML.OutbondShipmentOperator.PendingOrder> pendingOrders = (List<ML.OutbondShipmentOperator.PendingOrder>)resultValidateOpe.Object;

                    if(pendingOrders.Count() < 0)
                    {
                        result.Correct = false;
                        result.Message = resultValidateOpe.Message;
                        result.Object = pendingOrders;
                    }
                    else
                    {
                        ML.Result resultValidateShipment = ValidateShipment(connection, outbondShipment.CarSal);
                        if (!resultValidateShipment.Correct)
                        {
                            throw new Exception($@"{resultValidateShipment.Message}");
                        }

                        string query = $@"INSERT INTO ora_asignacion_operador (cod_emp, pto_alm, car_sal, rfc_ope, estatus)
                                          VALUES (1, ?, ?, ?, 0)";

                        using(OdbcCommand cmd = new OdbcCommand(query, connection))
                        {
                            cmd.Parameters.AddWithValue("pto_alm", outbondShipment.PtoAlm);
                            cmd.Parameters.AddWithValue("car_sal", outbondShipment.CarSal);
                            cmd.Parameters.AddWithValue("rfc_ope", ope.RfcOpe);

                            int rowsAffected = cmd.ExecuteNonQuery();
                            if (rowsAffected < 1)
                            {
                                throw new Exception($@"No se pudo insertar el registro de asignacion");
                            }
                        }

                        result.Correct = true;
                        result.Message = "Asignacion Realizada";
                        result.Object = pendingOrders;
                    }
                }
            }
            catch (Exception ex)
            {
                result.Correct = false;
                result.Object = null;
                result.Message = $@"Error al asignar la carga {ex.Message}";
                result.Ex = ex;
            }
            return result;
        }
        private static ML.Result ValidateOpe(OdbcConnection connection, string rfcOpe)
        {
            ML.Result result = new ML.Result();
            try
            {
                string query = $@"SELECT TRIM(B.car_sal) AS car_sal,TRIM(B.ord_rel) AS ord_rel,
                                        CASE
                                        WHEN (C.rt_stat IS NULL) THEN 'Sin Marcaje'
                                        WHEN (C.rt_stat = 1) THEN 'Entrega marcada'
                                        WHEN (C.rt_stat = 2) THEN 'Generando ASN'
                                        WHEN (C.rt_stat = 3) THEN 'Falta Regresar la mercancia'
                                        WHEN (C.rt_stat = 4) THEN 'Mercancia regresada'
                                        ELSE 'No Registrado'
                                        END AS estatus
                                FROM ora_asignacion_operador A
                                INNER JOIN ora_ruta B
                                            ON B.pto_alm = A.pto_alm
                                        AND B.car_sal = A.car_sal
                                        AND B.estatus <> 1
                                LEFT JOIN ora_rt C
                                            ON C.cod_emp = 1
                                        AND C.pto_alm = B.pto_alm
                                        AND C.ord_rel = B.ord_rel
                                WHERE A.cod_emp = 1
                                AND A.rfc_ope = ?";

                List<ML.OutbondShipmentOperator.PendingOrder> pendingOrders = new List<ML.OutbondShipmentOperator.PendingOrder>();

                using(OdbcCommand cmd = new OdbcCommand(query, connection))
                {
                    cmd.Parameters.AddWithValue("rfc_ope", rfcOpe);

                    using(OdbcDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            ML.OutbondShipmentOperator.PendingOrder pendingOrder = new ML.OutbondShipmentOperator.PendingOrder();

                            pendingOrder.CarSal = reader.GetString(0);
                            pendingOrder.OrdRel = reader.GetString(1);
                            pendingOrder.Estatus = reader.GetString(2);

                            pendingOrders.Add(pendingOrder);
                        }
                    }
                }

                result.Correct = true;
                result.Message = $@"El chofer tiene {pendingOrders.Count()} ordenes pendientes.";
                result.Object = pendingOrders;
            }
            catch (Exception ex)
            {
                result.Correct = false;
                result.Message = $@"Validacion de chofer {ex.Message}";
                result.Ex = ex;
            }
            return result;
        }
        private static ML.Result ValidateShipment(OdbcConnection connection, string carSal)
        {
            ML.Result result = new ML.Result();
            try
            {
                bool exist = false;
                string queryCheck = $@"SELECT COUNT(*)
                                        FROM ora_asignacion_operador
                                        WHERE cod_emp = 1
                                        AND car_sal = ?";

                using(OdbcCommand cmd = new OdbcCommand(queryCheck, connection))
                {
                    cmd.Parameters.AddWithValue("car_sal", carSal);
                    using(OdbcDataReader reader = cmd.ExecuteReader())
                    {
                        if(reader.Read())
                        {
                            exist = reader.GetInt32(0) > 0 ? true : false;
                        }
                    }
                }

                if (exist)
                {
                    throw new Exception($@"La ruta ya esta asignada a un chofer");
                }

                int eventos = 0;

                string query = $@"SELECT COUNT(*)
                                FROM ora_ruta A
                                LEFT JOIN ora_rt B
                                         ON B.pto_alm = A.pto_alm
                                        AND B.ord_rel = A.ord_rel
                                LEFT JOIN ora_ruta_eventos C
                                         ON C.car_sal = A.car_sal
                                        AND C.num_scn = A.num_scn
                                        AND C.ord_rel = A.ord_rel
                                WHERE A.pto_alm = 870
                                AND A.car_sal = ?
                                AND (B.ord_rel IS NOT NULL OR C.ord_rel IS NOT NULL)";

                using (OdbcCommand cmd = new OdbcCommand(query, connection))
                {
                    cmd.Parameters.AddWithValue("car_sal", carSal);
                    using (OdbcDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            eventos = reader.GetInt32(0);
                        }
                    }
                }

                if(eventos > 0)
                {
                    throw new Exception($@"La ruta, ya tiene {eventos} eventos marcados");
                }

                result.Correct = true;
            }
            catch (Exception ex)
            {
                result.Correct = false;
                result.Message = $@"Validacion de Ruta {ex.Message}";
                result.Ex = ex;
            }
            return result;
        }
        /*DeleteAssign*/
        public static ML.Result DeleteAssign(ML.OutbondShipmentOperator.OutboundShipment outbondShipment, string mode)
        {
            ML.Result result = new ML.Result();
            try
            {
                using (OdbcConnection connection = new OdbcConnection(DL.Connection.GetConnectionStringGen(mode)))
                {
                    connection.Open();

                    ML.Result resultValidateShipment = ValidateShipmentDelete(connection, outbondShipment.CarSal);
                    if (!resultValidateShipment.Correct)
                    {
                        throw new Exception($@"{resultValidateShipment.Message}");
                    }

                    string query = @"DELETE FROM 
                                            ora_asignacion_operador 
                                        WHERE car_sal = ?";

                    using (OdbcCommand cmd = new OdbcCommand(query, connection))
                    {
                        cmd.Parameters.Add(new OdbcParameter("@car_sal", outbondShipment.CarSal));

                        int rowsAffected = cmd.ExecuteNonQuery();

                        if (rowsAffected < 1)
                        {
                            throw new Exception($@"No se pudo borrar");
                        }
                    }
                    result.Correct = true;
                    result.Message = "Asignación eliminada correctamente";
                }
            }
            catch (Exception ex)
            {
                result.Correct = false;
                result.Message = $@"Error al eliminar la asignacion de la carga {ex.Message}";
                result.Ex = ex;
            }
            return result;
        }
        private static ML.Result ValidateShipmentDelete(OdbcConnection connection, string carSal)
        {
            ML.Result result = new ML.Result();
            try
            {
                int eventos = 0;

                string query = $@"SELECT COUNT(*)
                                FROM ora_ruta A
                                LEFT JOIN ora_rt B
                                         ON B.pto_alm = A.pto_alm
                                        AND B.ord_rel = A.ord_rel
                                LEFT JOIN ora_ruta_eventos C
                                         ON C.car_sal = A.car_sal
                                        AND C.num_scn = A.num_scn
                                        AND C.ord_rel = A.ord_rel
                                WHERE A.pto_alm = 870
                                AND A.car_sal = ?
                                AND (B.ord_rel IS NOT NULL OR C.ord_rel IS NOT NULL)";

                using (OdbcCommand cmd = new OdbcCommand(query, connection))
                {
                    cmd.Parameters.AddWithValue("car_sal", carSal);
                    using (OdbcDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            eventos = reader.GetInt32(0);
                        }
                    }
                }

                if (eventos > 0)
                {
                    throw new Exception($@"La ruta, ya tiene {eventos} eventos marcados");
                }

                result.Correct = true;
            }
            catch (Exception ex)
            {
                result.Correct = false;
                result.Message = $@"Validacion de Ruta {ex.Message}";
                result.Ex = ex;
            }
            return result;
        }
        /**/
    }
}
