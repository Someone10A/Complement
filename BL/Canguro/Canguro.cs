using System;
using System.Collections.Generic;
using System.Data.Odbc;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BL.Canguro
{
    public class Canguro
    {
        public static ML.Result GetWorkList(string pto_alm, string mode)
        {
            ML.Result result = new ML.Result();
            try
            {
                using(OdbcConnection connection = new OdbcConnection(DL.Connection.GetConnectionStringGen(mode)))
                {
                    connection.Open();

                    string query = $@"SELECT 
                                        A.cd_id,
                                        A.tda_venta,
                                        A.sales_check, 
                                        A.no_transf,
                                        CASE WHEN (K.intento IS NULL) THEN '1' ELSE TO_CHAR(K.intento) END AS intento,
                                        CASE WHEN (K.fec_act IS NULL)
                                                THEN 'NA'
                                                ELSE TO_CHAR(K.fec_act)
                                        END AS fecha,
                                        CASE WHEN K.estatus IS NULL THEN 'X' ELSE TO_CHAR(K.estatus) END AS idEst,
                                        CASE
                                        WHEN K.estatus IS NULL THEN 'Sin estatus'
                                        WHEN K.estatus = 0 THEN 'Mantenimiento'
                                        WHEN K.estatus = 1 THEN 'Listo para enviar Orden Cabecera'
                                        WHEN K.estatus = 2 THEN 'Listo para enviar ASN'
                                        WHEN K.estatus = 3 THEN 'Listo para marcar evento'
                                        WHEN K.estatus = 4 THEN 'Finalizado Entregado'
                                        ELSE 'Estatus desconocido'
                                    END AS desc_estatus
                                FROM
                                                dblga@lga_prod:lgahventa2 A,
                                                dblga@lga_prod:lgadventa2 B,
                                                dblga@lga_prod:lgaetiqeta C,
                                                OUTER ora_canguro K
                                WHERE A.cod_empresa = 1
                                AND A.tip_entrega = 4
                                AND A.cd_id = {pto_alm}
                                AND A.f_venta > '01012026'
                                AND B.cod_empresa = A.cod_empresa
                                AND B.cd_id = A.cd_id
                                AND B.sales_check = A.sales_check
                                AND C.cod_empresa = B.cod_empresa
                                AND C.cd_id = B.cd_id
                                AND C.no_etiqueta = B.no_etiqueta
                                AND C.st_etiqueta NOT IN (7,9,10)
                                AND K.cod_empresa = A.cod_empresa
                                AND K.tda_venta  = A.tda_venta
                                AND K.sales_check = A.sales_check
                                AND K.no_transf = A.no_transf
                                AND K.cd_id = A.cd_id
                                ORDER BY 2";

                    List<ML.Canguro.CanguroInfo> canguroInfoList = new List<ML.Canguro.CanguroInfo>();

                    using(OdbcCommand cmd = new OdbcCommand(query, connection))
                    {
                        using(OdbcDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                ML.Canguro.CanguroInfo canguroInfo = new ML.Canguro.CanguroInfo();

                                canguroInfo.Almacen = reader.GetString(0);
                                canguroInfo.Tienda = reader.GetString(1);
                                canguroInfo.Scn = reader.GetString(2);
                                canguroInfo.Transfer = reader.GetString(3);
                                canguroInfo.Intento = reader.GetString(4);
                                canguroInfo.UltFecha = reader.GetString(5);
                                canguroInfo.IdEstatus = reader.GetString(6).Trim();
                                canguroInfo.Estatus = reader.GetString(7).Trim();

                                canguroInfoList.Add(canguroInfo);
                            }
                        }
                    }

                    result.Correct = true;
                    result.Message = $@"{canguroInfoList.Count} Resultados extraidos";
                    result.Object = canguroInfoList;
                }
            }
            catch (Exception ex)
            {
                result.Correct = false;
                result.Ex = ex;
                result.Message = $@"Error al obtener lista de trabajo: {ex.Message}";
            }
            return result;
        }

        public static ML.Result GetCanguroInfo(ML.Canguro.CanguroInfo canguro, string mode)
        {
            ML.Result result = new ML.Result();
            try
            {
                using(OdbcConnection connection = new OdbcConnection(DL.Connection.GetConnectionStringGen(mode)))
                {
                    connection.Open();

                    string query = $@"SELECT FIRST 1
	                                B.facility AS facility,
	                                A.sales_check||'_'||C.intento AS OrderRelease,
	                                A.sales_check AS SCN,
	                                A.no_transf AS transfer,
	                                A.f_entrega AS FechaEntrega,
	                                D.no_cliente AS CodigoCliente,
	                                TRIM(D.nombres)||' '||TRIM(D.ap_materno)||' '||TRIM(D.ap_paterno) AS nombreCliente,
                                    CASE WHEN (D.tel_casa  IS NULL OR D.tel_casa = '') THEN '00000000' ELSE D.tel_casa  END AS telefono0,
                                    CASE WHEN (D.tel_casa1 IS NULL OR D.tel_casa1 = '') THEN '00000000' ELSE D.tel_casa1 END AS telefono1,
                                    CASE WHEN (D.tel_casa2 IS NULL OR D.tel_casa2 = '') THEN '00000000' ELSE D.tel_casa2 END AS telefono2,
	                                TRIM(D.ciudad) AS Estado,
	                                TRIM(D.edo_entfed) AS codigoEstado,
	                                TRIM(D.del_municipio) AS municipio,
	                                TRIM(D.col_poblacion) AS Colonia,
	                                TRIM(D.cod_postal) AS CodigoPostal,
	                                TRIM(D.calle) AS Calle,
                                    CASE WHEN(TRIM(D.calle1[29,38]) = '' OR D.calle1 IS NULL) THEN 'NA' ELSE TRIM(D.calle1[29,38]) END AS NumExt,
                                    CASE WHEN(TRIM(D.calle1[39,48]) = '' OR D.calle1 IS NULL) THEN 'NA' ELSE TRIM(D.calle1[39,48]) END AS NumInt,
	                                CASE WHEN (D.referencia1 IS NULL OR TRIM(D.referencia1) = '') THEN 'Sin Observaciones' ELSE TRIM(D.referencia1) END AS Observaciones,
		                            CASE WHEN (D.referencia IS NULL OR TRIM(D.referencia) = '') THEN 'Sin Referencia' ELSE TRIM(D.referencia) END AS Referencias,
	                                CASE WHEN (A.panel <> 'S') THEN 'S' ELSE 'N' END AS panel,
	                                CASE WHEN (A.mas_gente <> 'S') THEN 'S' ELSE 'N' END AS masgente,
	                                CASE WHEN (A.volar <> 'S') THEN 'S' ELSE 'N' END AS volar,
	                                C.longitud AS longitud,
	                                C.latitud AS latitud
                                FROM dblga@lga_prod:lgahventa2 A,
	                                ora_fac_go B,
	                                ora_canguro C,
	                                dblga@lga_prod:lgacliente D
                                WHERE A.cod_empresa = 1
                                AND A.tip_entrega = 4
                                AND A.cd_id = {canguro.Almacen}
                                AND A.tda_venta = {canguro.Tienda}
                                AND A.sales_check = '{canguro.Scn}'
                                AND A.no_transf = '{canguro.Transfer}'
                                AND B.cen_pto = A.cd_id
                                AND C.cod_empresa = A.cod_empresa
                                AND C.cd_id = A.cd_id
                                AND C.tda_venta = A.tda_venta
                                AND C.sales_check = A.sales_check
                                AND C.no_transf = A.no_transf
                                AND D.no_cliente = A.no_cliente
                                ";

                    ML.Canguro.Header header = new ML.Canguro.Header();

                    using(OdbcCommand cmd = new OdbcCommand(query, connection))
                    {
                        using(OdbcDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                header.Facility = reader.GetString(0);
                                header.OrderRelease = reader.GetString(1);
                                header.SCN = reader.GetString(2);
                                header.Transfer = reader.GetString(3);
                                header.FechaEntrega = reader.GetString(4);
                                header.CodigoCliente = reader.GetString(5);
                                header.NombreCliente = reader.GetString(6);
                                header.Telefono0 = reader.GetString(7).Trim();
                                header.Telefono1 = reader.GetString(8).Trim();
                                header.Telefono2 = reader.GetString(9).Trim();
                                header.Estado = reader.GetString(10);
                                header.CodigoEstado = reader.GetString(11);
                                header.Municipio = reader.GetString(12);
                                header.Colonia = reader.GetString(13).Replace("(","").Replace(")","");
                                header.CodigoPostal = reader.GetString(14);
                                header.Calle = reader.GetString(15);
                                header.NumExt = reader.GetString(16);
                                header.NumInt = reader.GetString(17);
                                header.Observaciones = reader.GetString(18);
                                header.Referencias = reader.GetString(19);
                                header.Panel = reader.GetString(20);
                                header.MasGente = reader.GetString(21);
                                header.Volado = reader.GetString(22);
                                header.Longitud = reader.GetString(23);
                                header.Latitud = reader.GetString(24);
                            }
                            else
                            {
                                throw new Exception($@"No se pudo leer datos del canguro");
                            }
                        }
                    }

                    result.Correct = true;
                    result.Message = $@"Datos obtenidos";
                    result.Object = header;
                }
            }
            catch (Exception ex)
            {
                result.Correct = false;
                result.Ex = ex;
                result.Message = $@"Error al obtener lista de trabajo: {ex.Message}";
            }
            return result;
        }

        public static ML.Result AcceptCanguro( ML.Canguro.CanguroInfo canguro, string user, string mode)
        {
            ML.Result result = new ML.Result();
            try
            {
                using (OdbcConnection connection = new OdbcConnection(DL.Connection.GetConnectionStringGen(mode)))
                {
                    connection.Open();

                    string query = $@"INSERT INTO ora_canguro 
                                    VALUES(1,{canguro.Tienda},'{canguro.Scn}','{canguro.Transfer}',{canguro.Almacen},{user},1,CURRENT,0,0,0)";

                    using (OdbcCommand cmd = new OdbcCommand(query, connection))
                    {
                        int rowsAffected = cmd.ExecuteNonQuery();
                        if(rowsAffected < 1)
                        {
                            throw new Exception($@"No se ejecuto");
                        }
                    }
                }
                    
                result.Correct = true;
            }
            catch (Exception ex)
            {
                result.Correct = false;
                result.Ex = ex;
                result.Message = $@"Error al obtener lista de trabajo: {ex.Message}";
            }
            return result;
        }

        public static ML.Result Maintenance(ML.Canguro.Maintenance maintenance, string user, string mode)
        {
            ML.Result result = new ML.Result();
            try
            {
                using (OdbcConnection connection = new OdbcConnection(DL.Connection.GetConnectionStringGen(mode)))
                {
                    connection.Open();

                    UpdateLgaEnt(connection, maintenance,user);
                    UpdateLgaCliente(connection, maintenance,user);
                    UpdateLgahventa2(connection, maintenance,user);
                    UpdateCanguro(connection, maintenance,user);

                }

                result.Correct = true;
                result.Message = $@"Se actualizo exitosamente el SCN para enviar a WMS";
            }
            catch (Exception ex)
            {
                result.Correct = false;
                result.Ex = ex;
                result.Message = $@"Error al dar mantenimiento al canguro: {ex.Message}";
            }
            return result;
        }

        private static void UpdateLgaEnt(OdbcConnection connection, ML.Canguro.Maintenance maintenance, string user)
        {
            char[] linea = new string(' ', 50).ToCharArray();

            string num_int = maintenance.Header.NumInt.PadRight(10);
            string num_ext = maintenance.Header.NumExt.PadRight(10);

            Array.Copy(num_int.ToCharArray(), 0, linea, 29, 10);
            Array.Copy(num_ext.ToCharArray(), 0, linea, 39, 10);

            string nums = new string(linea);
            string fecEnt = (DateTime.ParseExact(maintenance.Header.FechaEntrega, "yyyy-MM-dd", CultureInfo.InvariantCulture)).ToString("ddMMyyyy");

            string updateQuery = $@"UPDATE dblga@lga_prod:lgaent 
                                   SET f_entrega = '{fecEnt}',
                                       direc_cte = '{maintenance.Header.Calle}',
                                       direc_cte1 = '{nums}',
                                       col_poblacion = '{maintenance.Header.Colonia}',
                                       del_municipio = '{maintenance.Header.Municipio}',
                                       cod_postal = '{maintenance.Header.CodigoPostal}',
                                       referencia1 = '{maintenance.Header.Observaciones}',
                                       observ = '{maintenance.Header.Observaciones}',
                                       referencia = '{maintenance.Header.Referencias}',
                                       f_act = CURRENT,
                                       usuario = '{user}'
                                   WHERE cod_empresa = 1 
                                   AND cd_id = {maintenance.Canguro.Almacen}
                                   AND no_transf = '{maintenance.Canguro.Transfer}'";

            using (OdbcCommand cmd = new OdbcCommand(updateQuery, connection))
            {
                int rowsAffected = cmd.ExecuteNonQuery();

                //if (rowsAffected < 1)
                //{
                //    throw new Exception($"UpdateLgaEnt - No rows affected");
                //}
            }
        }
        private static void UpdateLgaCliente(OdbcConnection connection, ML.Canguro.Maintenance maintenance, string user)
        {
            char[] linea = new string(' ', 50).ToCharArray();

            string num_int = maintenance.Header.NumInt.PadRight(10);
            string num_ext = maintenance.Header.NumExt.PadRight(10);

            Array.Copy(num_int.ToCharArray(), 0, linea, 29, 10);
            Array.Copy(num_ext.ToCharArray(), 0, linea, 39, 10);

            string nums = new string(linea);

            string updateQuery = $@"UPDATE dblga@lga_prod:lgacliente 
                                   SET calle = '{maintenance.Header.Calle}',
                                       calle1 = '{nums}',
                                       col_poblacion = '{maintenance.Header.Colonia}',
                                       del_municipio = '{maintenance.Header.Municipio}',
                                       ciudad = '{maintenance.Header.Estado}',
                                       edo_entfed = '{maintenance.Header.CodigoEstado}',
                                       cod_postal = '{maintenance.Header.CodigoPostal}',
                                       referencia1  = '{maintenance.Header.Observaciones}',  
                                       referencia = '{maintenance.Header.Referencias}'
                                   WHERE no_cliente = {maintenance.Header.CodigoCliente}";

            using (OdbcCommand cmd = new OdbcCommand(updateQuery, connection))
            {
                int rowsAffected = cmd.ExecuteNonQuery();

                //if (rowsAffected < 1)
                //{
                //    throw new Exception($"UpdateLgaEnt - No rows affected");
                //}
            }
        }
        private static void UpdateLgahventa2(OdbcConnection connection, ML.Canguro.Maintenance maintenance, string user)
        {
            string fecEnt = (DateTime.ParseExact(maintenance.Header.FechaEntrega, "yyyy-MM-dd", CultureInfo.InvariantCulture)).ToString("ddMMyyyy");

            string updateQuery = $@"UPDATE dblga@lga_prod:lgahventa2
                                   SET f_entrega = '{fecEnt}',
                                       volar = '{maintenance.Header.Volado}',
                                       panel = '{maintenance.Header.Panel}',
                                       mas_gente = '{maintenance.Header.MasGente}'
                                   WHERE cod_empresa = 1 
                                   AND cd_id = {maintenance.Canguro.Almacen}
                                   AND no_transf = {maintenance.Canguro.Transfer}";

            using (OdbcCommand cmd = new OdbcCommand(updateQuery, connection))
            {
                int rowsAffected = cmd.ExecuteNonQuery();

                //if (rowsAffected < 1)
                //{
                //    throw new Exception($"[Warning] UpdateLgahventa - No rows affected");
                //}
            }
        }
        private static void UpdateCanguro(OdbcConnection connection, ML.Canguro.Maintenance maintenance, string user)
        {
            string updateQuery = $@"UPDATE ora_canguro 
                                       SET longitud = {maintenance.Header.Longitud}, 
                                           latitud = {maintenance.Header.Latitud},
                                            fec_act = CURRENT,
                                            estatus = 1,
                                            usu_id = {user}
                                       WHERE cod_empresa = 1 
                                       AND tda_venta = {maintenance.Canguro.Tienda}
                                       AND cd_id = {maintenance.Canguro.Almacen}
                                       AND sales_check = '{maintenance.Canguro.Scn}'
                                       AND no_transf = {maintenance.Canguro.Transfer}
                                        ";

            using (OdbcCommand cmd = new OdbcCommand(updateQuery, connection))
            {
                int rowsAffected = cmd.ExecuteNonQuery();

                if (rowsAffected < 1)
                {
                    throw new Exception("No rows affected in ora_canguro update");
                }
            }
        }

        public static ML.Result Delivered(ML.Canguro.Delivered delivered, string user, string mode)
        {
            ML.Result result = new ML.Result();
            try
            {
                if (delivered.IsDelivered)
                {
                    ML.Result resultIsDelivered = IsDelivered(delivered.CanguroInfo, user, mode);
                    if (!resultIsDelivered.Correct)
                    {
                        throw new Exception($@"{resultIsDelivered.Message}");
                    }
                    result.Message = resultIsDelivered.Message;
                }
                else
                {
                    ML.Result resultIsNotDelivered = IsNotDelivered(delivered.CanguroInfo, user, mode);
                    if (!resultIsNotDelivered.Correct)
                    {
                        throw new Exception($@"{resultIsNotDelivered.Message}");
                    }
                    result.Message = resultIsNotDelivered.Message;
                }

                result.Correct = true;
            }
            catch (Exception ex)
            {
                result.Correct = false;
                result.Ex = ex;
                result.Message = $@"Error al marcar el proceso {ex.Message}";
            }
            return result;
        }
        private static ML.Result IsDelivered(ML.Canguro.CanguroInfo canguroInfo, string user, string mode)
        {
            ML.Result result = new ML.Result();
            try
            {
                using(OdbcConnection connection = new OdbcConnection(DL.Connection.GetConnectionStringGen(mode)))
                {
                    connection.Open();

                    string queryLga = $@"UPDATE
                                             dblga@lga_prod:lgaetiqeta B
                                        SET
                                            st_etiqueta = 10,
                                            usuario_cancela = 79,
                                            f_cancelacion = CURRENT
                                        WHERE EXISTS (SELECT 1
                                                FROM dblga@lga_prod:lgadventa2 A
                                                WHERE A.cod_empresa = 1
                                                AND A.tip_entrega = 4
                                                AND A.cd_id = {canguroInfo.Almacen}
                                                AND A.sales_check = '{canguroInfo.Scn}'
                                                AND B.cod_empresa = A.cod_empresa
                                                AND B.cd_id = A.cd_id
                                                AND B.no_etiqueta = A.no_etiqueta
                                                AND B.tip_etiqueta = 1)
                                        ";

                    using (OdbcCommand cmd = new OdbcCommand(queryLga, connection))
                    {
                        int rowsAffected = cmd.ExecuteNonQuery();
                        if (rowsAffected < 1)
                        {
                            throw new Exception($@"Ningun registro sin alterar logistica");
                        }
                    }

                    string query = $@"UPDATE ora_canguro 
                                        SET estatus = 4, 
                                        usu_id = {user}
                                       WHERE cod_empresa = 1,
                                        AND cd_id = {canguroInfo.Almacen}
                                        AND tda_venta = {canguroInfo.Tienda}
                                        AND sales_check = '{canguroInfo.Scn}'
                                        AND no_transf = {canguroInfo.Transfer}
                                        ";

                    using (OdbcCommand cmd = new OdbcCommand(query, connection))
                    {
                        int rowsAffected = cmd.ExecuteNonQuery();
                        if (rowsAffected < 1)
                        {
                            throw new Exception($@"Ningun registro sin alterar");
                        }
                    }
                }
                result.Correct = true;
                result.Message = $@"Se marcó como entregado el SCN canguro {canguroInfo.Scn} exitosamente";
            }
            catch (Exception ex)
            {
                result.Correct = false;
                result.Ex = ex;
                result.Message = $@"Error al marcar como entregado {canguroInfo.Scn}: {ex.Message}";
            }
            return result;
        }
        private static ML.Result IsNotDelivered(ML.Canguro.CanguroInfo canguroInfo, string user, string mode)
        {
            ML.Result result = new ML.Result();
            try
            {
                using (OdbcConnection connection = new OdbcConnection(DL.Connection.GetConnectionStringGen(mode)))
                {
                    connection.Open();

                    string query = $@"UPDATE ora_canguro 
                                        SET estatus = 2, 
                                        intento = intento + 1 
                                        usu_id = {user}
                                       WHERE cod_empresa = 1,
                                        AND cd_id = {canguroInfo.Almacen}
                                        AND tda_venta = {canguroInfo.Tienda}
                                        AND sales_check = '{canguroInfo.Scn}'
                                        AND no_transf = {canguroInfo.Transfer}
                                        ";

                    using (OdbcCommand cmd = new OdbcCommand(query, connection))
                    {
                        int rowsAffected = cmd.ExecuteNonQuery();
                        if (rowsAffected < 1 )
                        {
                            throw new Exception($@"Ningun registro sin alterar");
                        }
                    }
                }
                result.Correct = true;
                result.Message = $@"Se marcó como no entregado el SCN canguro {canguroInfo.Scn}, se prepara para nuevo ciclo";
            }
            catch (Exception ex)
            {
                result.Correct = false;
                result.Ex = ex;
                result.Message = $@"Error al marcar como no entregado {canguroInfo.Scn}: {ex.Message}";
            }
            return result;
        }
    }
}
