using System;
using System.Collections.Generic;
using System.Data.Odbc;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BL.Operator
{
    public class Operator
    {
        /*
         *Metodo:       GetAssignedRoute
         *Vista:        Asignado.cshtml
         *Descripcion:  Obtener Exclusivamente 1 ruta la asignada al chofer en la vista de ListOperadores
         *Entrada:      string de rfcOpe y mode
         *Salida:       ML.Result y dentro del object una Lista de clase "Cabecera de ruta de chofer" 
         *Proceso:      Replicar proceso de ListOperadores y mostrar su estatus actual de la ruta
         *              Validar ya que aqui en el deber ser solo deberia de obtener 1 registro en el deber ser
         */
        public static ML.Result GetAssignedRoute(string ope,string mode)
        {
            ML.Result result = new ML.Result();
            try
            {
                using(OdbcConnection connection = new OdbcConnection(DL.Connection.GetConnectionStringGen(mode)))
                {
                    connection.Open();

                    string query = $@"SELECT FIRST 1 A.estatus,
                                            CASE
                                            WHEN (A.estatus = 0) THEN 'Ruta asignada'
                                            WHEN (A.estatus = 1) THEN 'Ruta Cerrada'
                                            WHEN (A.estatus = 2) THEN 'Ruta Abierta'
                                            WHEN (A.estatus = 3) THEN 'Ruta Finalizada'
                                            ELSE 'No Listado'
                                            END AS descripcion,
                                            A.pto_alm, 
                                            TRIM(A.car_sal) AS car_sal,
                                            B.fec_car,
                                            TRIM(C.nom_ope) AS nom_ope,
                                            TRIM(A.rfc_ope) AS rfc_ope
                                    FROM ora_asignacion_operador A
                                    INNER JOIN ora_ruta B 
                                         ON B.car_sal = A.car_sal
                                    LEFT JOIN ora_operadores C
                                         ON C.rfc_ope = A.rfc_ope
                                    WHERE A.cod_emp = 1
                                    AND A.rfc_ope = '{ope}'
                                    AND A.estatus IN (0,2,3)";

                    List<ML.Operator.RouteHeader> routes = new List<ML.Operator.RouteHeader>();
                    ML.Operator.RouteHeader route = new ML.Operator.RouteHeader();

                    using (OdbcCommand cmd = new OdbcCommand(query, connection))
                    {
                        using (OdbcDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                route.Estatus = reader.GetString(0);
                                route.Descripcion = reader.GetString(1);
                                route.PtoAlm = reader.GetString(2);
                                route.CarSal = reader.GetString(3);
                                route.FecCar = reader.GetDateTime(4).ToString();
                                route.NomOpe = reader.GetString(5);
                                route.RfcOpe = reader.GetString(6);
                                route.Comentario = reader.GetString(5);
                            }
                        }
                    }
                    routes.Add(route);
                    result.Correct = true;
                    result.Object = routes;
                }
            }
            catch (Exception ex)
            {
                result.Correct = false;
                result.Message = $@"Error al obtener la ruta {ex.Message}";
            }
            return result;
        }

        /*
         *Metodo:       AcceptRoute
         *Vista:        Asignado.cshtml
         *Descripcion:  Aceptar ruta del chofer
         *Entrada:      "Cabecera de ruta de chofer"  y mode
         *Salida:       ML.Result y Mensaje en result.Message
         *Proceso:      Actualizar ora_asignacion_operador a un estatus de Aceptada
         */
        public static ML.Result AcceptRoute(ML.Operator.RouteHeader route, string mode)
        {
            ML.Result result = new ML.Result();
            try
            {
                using (OdbcConnection connection = new OdbcConnection(DL.Connection.GetConnectionStringGen(mode)))
                {
                    connection.Open();

                    string query = $@"UPDATE
                                             ora_asignacion_operador
                                        SET estatus = 2
                                        WHERE cod_emp = 1
                                        AND pto_alm = {route.PtoAlm}
                                        AND car_sal = '{route.CarSal}'
                                        AND rfc_ope = '{route.RfcOpe}'
                                        AND estatus = 0
                                        ";

                    using (OdbcCommand cmd = new OdbcCommand(query, connection))
                    {
                        int rowsAffected = cmd.ExecuteNonQuery();

                        if (rowsAffected < 1)
                        {
                            throw new Exception($@"No se pudo aceptar la ruta");
                        }
                    }
                    result.Correct = true;
                    result.Message = $@"Ruta aceptada";
                }
            }
            catch (Exception ex)
            {
                result.Correct = false;
                result.Message = $@"Error al aceptar la ruta";
            }
            return result;
        }
        public static ML.Result FinishRoute(ML.Operator.RouteHeader route, string mode)
        {
            ML.Result result = new ML.Result();
            try
            {
                using (OdbcConnection connection = new OdbcConnection(DL.Connection.GetConnectionStringGen(mode)))
                {
                    connection.Open();

                    ML.Result resultValidateShipment = ValidateShipment(connection, route);
                    if (!resultValidateShipment.Correct)
                    {
                        throw new Exception($@"{resultValidateShipment.Message}");
                    }


                    string query = $@"UPDATE
                                             ora_asignacion_operador
                                        SET estatus = 3
                                        WHERE cod_emp = 1
                                        AND pto_alm = {route.PtoAlm}
                                        AND car_sal = '{route.CarSal}'
                                        AND rfc_ope = '{route.RfcOpe}'
                                        AND estatus = 2
                                        ";

                    using (OdbcCommand cmd = new OdbcCommand(query, connection))
                    {
                        int rowsAffected = cmd.ExecuteNonQuery();

                        if (rowsAffected < 1)
                        {
                            throw new Exception($@"No se pudo marcar la finalizacion de la ruta");
                        }
                    }
                    result.Correct = true;
                    result.Message = $@"Ruta finalizada";
                }
            }
            catch (Exception ex)
            {
                result.Correct = false;
                result.Message = $@"Error al finalizar la ruta {ex.Message}";
            }
            return result;
        }
        private static ML.Result ValidateShipment(OdbcConnection connection, ML.Operator.RouteHeader route)
        {
            ML.Result result = new ML.Result();
            try
            {
                string query = $@"SELECT COUNT(*)
                                    FROM ora_asignacion_operador A
                                    INNER JOIN ora_ruta B
	                                     ON B.pto_alm = A.pto_alm
	                                    AND B.car_sal = A.car_sal
                                    LEFT JOIN ora_ruta_eventos C
	                                     ON C.pto_alm = B.pto_alm
	                                    AND C.ord_rel = B.ord_rel
	                                    AND C.num_scn = B.num_scn
                                    WHERE A.cod_emp = 1
                                    AND A.pto_alm = {route.PtoAlm.Trim()}
                                    AND A.estatus = {route.Estatus}
                                    AND A.car_sal = '{route.CarSal}'
                                    AND A.rfc_ope = '{route.RfcOpe}'
                                    AND C.cod_mot IS NULL";

                int pend = 0;

                using(OdbcCommand command = new OdbcCommand(query, connection))
                {
                    using(OdbcDataReader reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            pend = reader.GetInt32(0);
                        }
                        else
                        {
                            throw new Exception($@"No se pudo leer los registros de la ruta del chofer");
                        }
                    }
                }

                if (pend > 0)
                {
                    throw new Exception($@"Aun se tienen {pend} registros pendientes de marcar evento");
                }

                result.Correct = true;
            }
            catch (Exception ex)
            {
                result.Correct = false;
                result.Message = $@"Validacion de Ruta no exitosa {ex.Message}";
                result.Ex = ex;
            }
            return result;
        }
        /*
         *Metodo:       GetOrdersPerRoute
         *Vista:        Ruta.cshtml
         *Descripcion:  Traer Registros por ruta
         *Entrada:      "Cabecera de ruta de chofer"  y mode
         *Salida:       ML.Result y Detalle de Ruta
         *Proceso:      Obtener basandose en el query de BL.BaseControl.BaseControl.GetOrdersPerRoute 
         *              Pero con modificaciones, para tomar en cuenta que la llave es 
         *              ora_asignacion_operador & ora_ruta & ora_ruta_eventos o ora_rt
         *              Depende si la ruta esta abierta o cerrada va a apuntar a ora_ruta_eventos o a ora_rt 
         *              ora_ruta_eventos cuando aun esta abierta, o finalizada
         *              ora_rt solo cuando ya esta cerrada
         *              Se pueden usar metodos privados para mayor control 
         */
        public static ML.Result GetOrdersPerRoute(ML.Operator.RouteHeader route, string mode)
        {
            ML.Result result = new ML.Result();
            try
            {
                using (OdbcConnection connection = new OdbcConnection(DL.Connection.GetConnectionStringGen(mode)))
                {
                    connection.Open();


                    string queryAssigned = $@"SELECT CASE
                                        WHEN(C.fec_act IS NULL) THEN B.fec_car
                                        ELSE C.fec_act 
		                                END AS fec_act,
                                        TRIM(A.car_sal) AS car_sal,
                                        TRIM(B.ord_rel) AS ord_rel,
                                        B.num_scn,
                                        E.cod_pto,
                                        F.cod_cli,
                                        UPPER(TRIM(F.nom_cli)||' '||TRIM(F.ape1_cli)||' '||TRIM(F.ape2_cli)) AS cliente,
                                        CASE
                                        WHEN (C.cod_mot IS NULL) THEN 'Sin marcaje'
                                        ELSE 'Evento Asignado' 
		                                END as estatus_ruta,
                                        CASE
                                        WHEN E.estado = 'P' THEN E.estado||'-Retenido-Transito'
                                        WHEN E.estado = 'T' THEN E.estado||'-Transito'
                                        WHEN E.estado = 'I' THEN E.estado||'-Impreso'
                                        WHEN E.estado = 'X' THEN E.estado||'-Cancelado'
                                        WHEN E.estado = 'C' THEN E.estado||'-Cancelado'
                                        WHEN E.estado = 'E' THEN E.estado||'-Entregado'
                                        WHEN E.estado = 'D' THEN E.estado||'-Devuelto'
                                        WHEN E.estado = 'G' THEN E.estado||'-Generado'
                                        ELSE E.estado||'-Estado desconocido'
                                        END AS estatus_gnx,
                                        CASE 
                                        WHEN (A.estatus = 0) THEN 'Ruta aun no aceptada'
                                        WHEN (A.estatus = 3) THEN 'Ruta finalizada'
                                        ELSE 'Ruta Trabajandose' 
                                        END AS estatus_rt,
                                        CASE
                                        WHEN (C.cod_mot IS NULL) THEN 'No marcado aun'
                                        ELSE TRIM(D.des_mot) 
		                                END AS des_mot,
                                        CASE
                                        WHEN(G.scn_nvo IS NULL) THEN 'NO'
                                        ELSE 'Es un RDD de cambio' 
		                                END AS rdd_info,
                                        TRIM(H.pro_cli)||' '||TRIM(H.pob_cli)||' '||H.cp_cli||' '||
                                        TRIM(H.col_cli)||' '||TRIM(H.dir_cli) AS dirreccion
                                FROM ora_asignacion_operador A
                                INNER JOIN ora_ruta B
                                         ON B.pto_alm = A.pto_alm
                                        AND B.car_sal = A.car_sal
                                LEFT JOIN ora_ruta_eventos C
                                         ON C.pto_alm = B.pto_alm
                                        AND C.car_sal = B.car_sal
                                        AND C.ord_rel = B.ord_rel
                                        AND C.num_scn = B.num_scn
                                LEFT  JOIN ora_motivos_rt D
                                        ON D.cod_mot = C.cod_mot
                                INNER JOIN edc_cab E
                                         ON E.cod_emp = 1
                                        AND E.num_scn = B.num_scn
                                LEFT JOIN clientes F
                                         ON F.cod_emp = E.cod_emp
                                        AND F.cod_cli = E.cod_cli
                                LEFT JOIN rdd_cab G
                                         ON G.cod_emp = E.cod_emp
                                        AND G.scn_nvo = E.num_scn
                                LEFT JOIN cli_direccion H
                                         ON H.cod_emp = E.cod_emp
                                        AND H.cod_dir = E.cod_dir
                                WHERE A.pto_alm  = {route.PtoAlm}
                                AND A.car_sal = '{route.CarSal}'
                                AND A.rfc_ope = '{route.RfcOpe}'";
                    string queryClosed = $@"SELECT 
	                                            CASE
	                                            WHEN(C.f_procesa IS NULL) THEN B.fec_act
	                                            ELSE C.f_procesa 
	                                            END AS fec_act,
	                                            TRIM(A.car_sal) AS car_sal,
	                                            TRIM(B.ord_rel) AS ord_rel,
	                                            B.num_scn,
	                                            E.cod_pto,
	                                            F.cod_cli,
	                                            UPPER(TRIM(F.nom_cli)||' '||TRIM(F.ape1_cli)||' '||TRIM(F.ape2_cli)) AS cliente,
	                                            CASE
	                                            WHEN (B.estatus = 0) THEN '0-Abierto'
	                                            WHEN (B.estatus = 1) THEN '1-Cerrado'
	                                            WHEN (B.estatus = 2) THEN '2-Procesando'
	                                            ELSE B.estatus||'Desconocido'
	                                            END AS estatus_ruta,
	                                            CASE
	                                            WHEN E.estado = 'P' THEN E.estado||'-Retenido-Transito'
	                                            WHEN E.estado = 'T' THEN E.estado||'-Transito'
	                                            WHEN E.estado = 'I' THEN E.estado||'-Impreso'
	                                            WHEN E.estado = 'X' THEN E.estado||'-Cancelado'
	                                            WHEN E.estado = 'C' THEN E.estado||'-Cancelado'
	                                            WHEN E.estado = 'E' THEN E.estado||'-Entregado'
	                                            WHEN E.estado = 'D' THEN E.estado||'-Devuelto'
	                                            WHEN E.estado = 'G' THEN E.estado||'-Generado'
	                                            ELSE E.estado||'-Estado desconocido'
	                                            END AS estatus_gnx,
	                                            CASE
                                                WHEN C.rt_stat IS NULL THEN 'X-Esperando interfaz de evento'
                                                WHEN C.rt_stat = 1 THEN '1-Interfaz ENTREGA para Genesix generada'
                                                WHEN C.rt_stat = 2 THEN '2-Generando ASN para WMS'
                                                WHEN C.rt_stat = 3 THEN '3-ASN Generado'
                                                WHEN C.rt_stat = 4 THEN '4-Interfaz Retención para Genesix generada'
                                                WHEN C.rt_stat = 5 THEN '5-Evento marcado por base-Generando transmision'
                                                ELSE C.rt_stat||'-Estado No listado.'
	                                            END AS estatus_rt,
	                                            CASE WHEN (D.des_mot IS NULL)
	                                            THEN
		                                            'X-Evento no registrado aun'
	                                            ELSE
		                                            D.cod_mot||'-'||TRIM(D.des_mot)
	                                            END AS des_mot,
	                                            CASE
	                                            WHEN(G.scn_nvo IS NULL) THEN 'NO'
	                                            ELSE 'Es un RDD de cambio' 
	                                            END AS rdd_info,
                                                'X' AS direccion
                                            FROM ora_asignacion_operador A
                                            INNER JOIN ora_ruta B
	                                             ON B.pto_alm = A.pto_alm
	                                            AND B.car_sal = A.car_sal
                                            LEFT JOIN ora_rt C
	                                             ON C.cod_emp = 1
	                                            AND C.pto_alm = B.pto_alm
	                                            AND C.ord_rel = B.ord_rel
                                            LEFT  JOIN ora_motivos_rt D
	                                            ON D.cod_mot = C.cod_mot
                                            INNER JOIN edc_cab E
	                                             ON E.cod_emp = 1
	                                            AND E.num_scn = B.num_scn
                                            LEFT JOIN clientes F
	                                             ON F.cod_emp = E.cod_emp
	                                            AND F.cod_cli = E.cod_cli
                                            LEFT JOIN rdd_cab G
	                                             ON G.cod_emp = E.cod_emp
	                                            AND G.scn_nvo = E.num_scn
                                            WHERE A.pto_alm  = {route.PtoAlm}
                                            AND A.car_sal = '{route.CarSal}'
                                            AND A.rfc_ope = '{route.RfcOpe}'
                                            ";

                    string query = route.Estatus == "1" ? queryClosed : queryAssigned;

                    List <ML.Operator.RouteDetail> routeDetailList = new List<ML.Operator.RouteDetail>();

                    using (OdbcCommand cmd = new OdbcCommand(query, connection))
                    {
                        using (OdbcDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                ML.Operator.RouteDetail routeDetail = new ML.Operator.RouteDetail();

                                routeDetail.FecAct = reader.GetDateTime(0).ToString();
                                routeDetail.CarSal = reader.GetString(1);
                                routeDetail.OrdRel = reader.GetString(2);
                                routeDetail.NumScn = reader.GetString(3);
                                routeDetail.CodPto = reader.GetString(4);
                                routeDetail.CodCli = reader.GetString(5);
                                routeDetail.Cliente = reader.GetString(6);
                                routeDetail.EstatusRuta = reader.GetString(7);
                                routeDetail.EstatusGnx = reader.GetString(8).Trim();
                                routeDetail.EstatusRT = reader.GetString(9).Trim();
                                routeDetail.Motivo = reader.GetString(10).Trim();
                                routeDetail.RddInfo = reader.GetString(11).Trim();
                                routeDetail.Dirreccion = reader.GetString(12).Trim();

                                routeDetailList.Add(routeDetail);
                            }
                        }
                    }

                    result.Correct = true;
                    result.Object = routeDetailList;
                }
            }
            catch (Exception ex)
            {
                result.Correct = false;
                result.Message = $@"Se obtuvo un error al consultar el detalle de la ruta.";
            }
            return result;
        }
        //Hacer que el usuario no pueda ver el motivo 15 Cliente cancela a menos que el estatus del SCN sea X o C
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

                    List<ML.Operator.Reason> reasonList = new List<ML.Operator.Reason>();

                    using (OdbcCommand cmd = new OdbcCommand(query, connection))
                    {
                        using (OdbcDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                ML.Operator.Reason reason = new ML.Operator.Reason();

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
        /*
         *Metodo:       AssignEvent
         *Vista:        Ruta.cshtml
         *Descripcion:  Asignar evento de entrega en ora_ruta_eventos
         *Entrada:      Clase compatible y mode
         *Salida:       ML.Result y Mensaje en result.Message
         *Proceso:      Hacer insert o Update a la tabla ora_ruta_eventos
         *              Por si acaso, siempre validar estatus de ruta para proseguir con la accion
         *              usu_mot usuario que marco el motivo 
         *              fec_act ultima fecha de actualizacion   
         */
        public static ML.Result AssignEvent(ML.Operator.RouteHeader route, ML.Operator.RouteDetail routeDetail,string mode)
        {
            ML.Result result = new ML.Result();
            try
            {
                using(OdbcConnection connection = new OdbcConnection(DL.Connection.GetConnectionStringGen(mode)))
                {
                    connection.Open();
                    string queryTest = $@"SELECT COUNT(*)
                                        FROM ora_ruta_eventos 
                                        WHERE pto_alm = {route.PtoAlm}
                                        AND car_sal = '{routeDetail.CarSal}'
                                        AND ord_rel = '{routeDetail.OrdRel}'
                                        AND num_scn = num_scn";
                    bool exist = false;
                    using (OdbcCommand cmd = new OdbcCommand(queryTest, connection))
                    {
                        using(OdbcDataReader reader = cmd.ExecuteReader())
                        {
                            if(reader.Read())
                            {
                                exist = reader.GetInt32(0) > 0 ? true : false;
                            }
                        }
                    }

                    string query = !exist ?
                                    $@"INSERT INTO ora_ruta_eventos(pto_alm,car_sal,ord_rel,num_scn,fec_act,cod_mot,usu_mot)
	                                    VALUES ({route.PtoAlm},'{route.CarSal}','{routeDetail.OrdRel}','{routeDetail.NumScn}',CURRENT,{routeDetail.Motivo},'{route.RfcOpe}')" :
                                    $@"UPDATE 
	                                        ora_ruta_eventos 
                                        SET fec_act = CURRENT,
	                                        cod_mot = {routeDetail.Motivo},
	                                        usu_mot = '{route.RfcOpe}'
                                        WHERE pto_alm = {route.PtoAlm}
                                        AND car_sal = '{route.CarSal}'
                                        AND ord_rel = '{routeDetail.OrdRel}'
                                        AND num_scn = '{routeDetail.NumScn}'";

                    string verb = !exist ? "Insertar" : "Actualizar";

                    using (OdbcCommand cmd = new OdbcCommand(query, connection))
                    {
                        int rowsAffected = cmd.ExecuteNonQuery();
                        if(rowsAffected < 1)
                        {
                            throw new Exception($@"No se pudo {verb} el evento de la orden {routeDetail.OrdRel}");
                        }
                    }
                    result.Correct = true;
                    result.Message = $@"{verb} evento exitoso";
                }
            }
            catch (Exception ex)
            {
                result.Correct = false;
                result.Message = $@"{ex.Message}, por favor vuelva a intentar";
            }
            return result;
        }
        /*
         *Metodo:       GetHistorical
         *Vista:        Historico.cshtml
         *Descripcion:  Obtener el historico de rutas la asignadas al chofer 
         *Entrada:      string de rfcOpe y mode
         *Salida:       ML.Result y dentro del object una Lista de clase "Cabecera de ruta de chofer" 
         *Proceso:      Replicar proceso de ListOperadores y mostrar su estatus actual de la ruta
         *              Dentro de lo posible mostrar el % de entregas, si no validar hacerlo en la vista el calculo
         */
        public static ML.Result GetHistorical(string ope, string mode)
        {
            ML.Result result = new ML.Result();
            try
            {
                using (OdbcConnection connection = new OdbcConnection(DL.Connection.GetConnectionStringGen(mode)))
                {
                    connection.Open();

                    string query = $@"SELECT A.estatus,
                                            CASE
                                            WHEN (A.estatus = 0) THEN 'Ruta asignada'
                                            WHEN (A.estatus = 1) THEN 'Ruta Cerrada'
                                            WHEN (A.estatus = 2) THEN 'Ruta Abierta'
                                            WHEN (A.estatus = 3) THEN 'Ruta Finalizada'
                                            ELSE 'No Listado'
                                            END AS descripcion,
                                            A.pto_alm, 
                                            TRIM(A.car_sal) AS car_sal,
                                            B.fec_car,
                                            TRIM(C.nom_ope) AS nom_ope,
                                            TRIM(A.rfc_ope) AS rfc_ope
                                    FROM ora_asignacion_operador A
                                    INNER JOIN ora_ruta B 
                                            ON B.car_sal = A.car_sal
                                    LEFT JOIN ora_operadores C
                                            ON C.rfc_ope = A.rfc_ope
                                    WHERE A.cod_emp = 1
                                    AND A.rfc_ope = '{ope}'
                                    AND A.estatus IN (1)
                                    GROUP BY 1,2,3,4,5,6,7";

                    List<ML.Operator.RouteHeader> routes = new List<ML.Operator.RouteHeader>();

                    using (OdbcCommand cmd = new OdbcCommand(query, connection))
                    {
                        using (OdbcDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                ML.Operator.RouteHeader route = new ML.Operator.RouteHeader();

                                route.Estatus = reader.GetString(0);
                                route.Descripcion = reader.GetString(1);
                                route.PtoAlm = reader.GetString(2);
                                route.CarSal = reader.GetString(3);
                                route.FecCar = reader.GetDateTime(4).ToString();
                                route.NomOpe = reader.GetString(5);
                                route.RfcOpe = reader.GetString(6);

                                routes.Add(route);
                            }
                        }
                    }

                    result.Correct = true;
                    result.Object = routes;
                }
            }
            catch (Exception ex)
            {
                result.Correct = false;
                result.Message = $@"Error al obtener el historico de rutas {ex.Message}";
            }
            return result;
        }
    }
}
