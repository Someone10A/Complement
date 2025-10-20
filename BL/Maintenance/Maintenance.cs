using System;
using System.Collections.Generic;
using System.Data.Odbc;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

        public static ML.Result UpdateScnInfo(ML.Maintenance.ConfirmedInfoByScn confirmedInfo, string mode)
        {
            ML.Result result = new ML.Result();
            try
            {
                using (OdbcConnection connection = new OdbcConnection(DL.Connection.GetConnectionStringGen(mode)))
                {
                    connection.Open();

                    /*
                        Si es Rdd 
	                        actualizar ora_integra_envio

                        Actualizar lgaent

                        Insertar O Actualizar cli_dir_coord

                        Actualizar cli_direccion

                        Si es posfecha 
	                        actualizar edc_cab

                        Si Confirmado
	                        Insertar o actualizar Tabla para universo de ruteo
	                        actualizar ora_mantenimiento
	
                        */

                    result.Correct = true;
                    result.Object = "";
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
    }
}
