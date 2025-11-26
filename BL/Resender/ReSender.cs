using DocumentFormat.OpenXml.Office.Word;
using DocumentFormat.OpenXml.Spreadsheet;
using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.Data.SqlClient;
using ML;
using System;
using System.Collections.Generic;
using System.Data.Odbc;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BL.Resender
{
    public class ReSender
    {
        public ML.Result GetOrder(string noOrder,string mode)
        {
            ML.Result result = new ML.Result();
            try
            {
                using (SqlConnection connection = new SqlConnection())
                {
                    connection.ConnectionString = DL.Connection.GetConnectionStringSig(mode);
                    connection.Open();

                    string query = $@"SELECT num_ped, fecha_gen,
		                                    CASE 
		                                    WHEN(estatus = '4') THEN 'Etiqueta Cancelada'
		                                    WHEN(estatus = '3')	THEN 'ASN enviado'
		                                    WHEN(estatus = '2')	THEN 'Apta para envio de correo'
		                                    WHEN(estatus = '5')	THEN 'Apta para envio de ASN'
		                                    ELSE 'Estatus desconocido'	END AS estatus,
		                                    fecha_env,ilpn,
		                                    LEFT(tienda_no_paq, CHARINDEX('-', tienda_no_paq) - 1) AS tienda,
		                                    LTRIM(RTRIM(tienda_no_paq)) as tienda_no_paq,sku ,unidades
                                    FROM sag_pre_recibo_ilpn
                                    WHERE cod_emp = 1 AND num_ped = '{noOrder}'
                                    ";

                    ML.ReSender.Order order = null;

                    using (SqlCommand cmd = new SqlCommand(query, connection))
                    {
                        using (var dr = cmd.ExecuteReader())
                        {
                            while (dr.Read())
                            {
                                if (order == null)
                                {
                                    order = new ML.ReSender.Order
                                    {
                                        Orden = dr["num_ped"].ToString(),
                                        Headers = new List<ML.ReSender.Header>()
                                    };
                                }

                                string creacion = dr["fecha_gen"].ToString();
                                string estatus = dr["estatus"].ToString();
                                string ultimoEnvio = dr["fecha_env"].ToString();

                                var header = order.Headers.FirstOrDefault(h =>
                                    h.Creacion == creacion &&
                                    h.Estatus == estatus &&
                                    h.UltimoEnvio == ultimoEnvio
                                );

                                if (header == null)
                                {
                                    header = new ML.ReSender.Header
                                    {
                                        Creacion = creacion,
                                        Estatus = estatus,
                                        UltimoEnvio = ultimoEnvio,
                                        Details = new List<ML.ReSender.Detail>()
                                    };

                                    order.Headers.Add(header);
                                }

                                header.Details.Add(new ML.ReSender.Detail
                                {
                                    Ilpn = dr["ilpn"].ToString(),
                                    Tienda = dr["tienda"].ToString(),
                                    Bulto = dr["tienda_no_paq"].ToString(),
                                    Sku = dr["sku"].ToString(),
                                    Cantidad = dr["unidades"].ToString()
                                });
                            }
                        }
                    }

                    result.Correct = true;
                    result.Object = order;
                }
            }
            catch (Exception ex)
            {
                result.Correct = false;
                result.Message = $@"Error al leer la orden: {ex.Message}";
            }
            return result;
        }
        //--------------------------------------------------------------------------
        public static string Init57(string dig)
        {
            string mode = "PRO";

            List<string> list = dig.Split(',').ToList();

            string response = string.Empty;

            foreach (string oc in list)
            {
                Result resultResend = Resend57(oc, mode);
                if (resultResend.Correct)
                {
                    response += (string)resultResend.Object;
                }
            }

            return response;
        }
        private static Result Resend57(string oc, string mode)
        {
            Result result = new Result();
            try
            {
                using (SqlConnection connection = new SqlConnection())
                {
                    connection.ConnectionString = DL.Connection.GetConnectionStringSig(mode);
                    connection.Open();

                    Result resultExist = Exist57(connection, oc);
                    if (!resultExist.Correct)
                    {
                        throw new Exception($@"{resultExist.Message}");
                    }
                    (bool exist, string fec) data = ((bool exist, string fec))resultExist.Object;

                    if (data.exist)
                    {
                        Result resultUpdte = Update57(connection, oc);
                        if (!resultUpdte.Correct)
                        {
                            throw new Exception($@"{resultUpdte.Message}");
                        }
                        result.Object = $"{resultUpdte.Message} Ultima hora {data.fec}\n";
                    }
                    else
                    {
                        result.Object = $"La oc {oc} no tiene un cartonizado en estatus de envio\n";
                    }

                    result.Correct = true;
                }
            }
            catch (Exception ex)
            {
                result.Correct = false;
                Console.WriteLine(ex.Message);
            }
            return result;
        }
        private static Result Exist57(SqlConnection connection, string oc)
        {
            Result result = new Result();
            try
            {
                string fec = string.Empty;
                bool exist = false;
                string query = $@"SELECT fecha_env
                                FROM sag_pre_recibo_ilpn
                                WHERE cod_emp = 1
                                AND estatus = 3
                                AND num_ped = '{oc}'
                                GROUP BY fecha_env";

                using (SqlCommand cmd = new SqlCommand(query, connection))
                {
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            fec = reader.GetDateTime(0).ToString();
                            exist = true;
                        }
                    }
                }

                result.Correct = true;
                result.Object = (exist, fec);
            }
            catch (Exception e)
            {
                result.Correct = false;
                result.Message = $@"Error al consultar oc {oc}";
            }
            return result;
        }
        private static Result Update57(SqlConnection connection, string oc)
        {
            Result result = new Result();
            try
            {
                string query = $@"UPDATE sag_pre_recibo_ilpn
                                SET estatus = 5
                                WHERE cod_emp = 1
                                AND estatus = 3
                                AND num_ped = '{oc}'";

                using (SqlCommand cmd = new SqlCommand(query, connection))
                {
                    int rowsAffected = cmd.ExecuteNonQuery();
                    if (rowsAffected < 1)
                    {
                        throw new Exception($@"No se actualizó ninguna fila ({rowsAffected}) rows Affected");
                    }
                    result.Correct = true;
                    result.Message = $@"{oc} Se actualizaron {rowsAffected} columnas.";
                }
            }
            catch (Exception e)
            {
                result.Correct = false;
                result.Message = $@"Error al actuaizar oc {oc} {e.Message}";
            }
            return result;
        }
        //--------------------------------------------------------------------------
        public static string Init13(string dig)
        {
            string mode = "PRO";

            List<string> list = dig.Split(',').ToList();

            string response = string.Empty;

            foreach (string oc in list)
            {
                Result resultResend = Resend13(oc, mode);
                if (resultResend.Correct)
                {
                    response += (string)resultResend.Object;
                }
            }

            return response;
        }
        private static Result Resend13(string oc, string mode)
        {
            Result result = new Result();
            try
            {
                using (SqlConnection connection = new SqlConnection())
                {
                    connection.ConnectionString = DL.Connection.GetConnectionStringSig(mode);
                    connection.Open();

                    Result resultExist = Exist(connection, oc);
                    if (!resultExist.Correct)
                    {
                        throw new Exception($@"{resultExist.Message}");
                    }
                    (bool exist, bool apta, DateTime fec) data = ((bool exist, bool apta, DateTime fec))resultExist.Object;

                    if (data.exist)
                    {
                        if (data.apta)
                        {
                            Result resultUpdte = Update13(connection, oc, data.fec);
                            if (!resultUpdte.Correct)
                            {
                                throw new Exception($@"{resultUpdte.Message}");
                            }
                            result.Object = $@"{resultUpdte.Message}";
                        }
                        else
                        {
                            result.Object = $"La oc {oc} tiene cartonización, pero no hay transferencias validas hasta el momento";
                        }
                    }
                    else
                    {
                        result.Object = $"La oc {oc} no se ha cartonizado aun";
                    }

                    result.Correct = true;
                }
            }
            catch (Exception ex)
            {
                result.Correct = false;
                Console.WriteLine(ex.Message);
            }
            return result;
        }
        private static Result Exist(SqlConnection connection, string oc)
        {
            Result result = new Result();
            try
            {
                bool apta = false;
                bool exist = false;
                DateTime fec = new DateTime();
                string query = $@"SELECT Estatus, Fecha_Hora
                                    FROM sag_pre_recibo_envio
                                    WHERE cod_emp = 1
                                    AND num_ped = '{oc}'
                                    GROUP BY Estatus, Fecha_Hora";

                using (SqlCommand cmd = new SqlCommand(query, connection))
                {
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            apta = reader.GetByte(0) == 0 ? false : true;
                            fec = reader.GetDateTime(1);

                            exist = true;

                            if (apta)
                            {
                                break;
                            }
                        }
                    }
                }

                result.Correct = true;
                result.Object = (exist, apta, fec);
            }
            catch (Exception e)
            {
                result.Correct = false;
                result.Message = $@"Error al consultar oc {oc}";
            }
            return result;
        }
        private static Result Update13(SqlConnection connection, string oc, DateTime fec)
        {
            Result result = new Result();
            try
            {

                string query = $@"UPDATE sag_pre_recibo_envio
	                                        SET Estatus = 0
                                        WHERE Estatus = 1
                                        AND num_ped = @NumPed
                                        AND Fecha_Hora = @FechaHora";


                using (SqlCommand cmd = new SqlCommand(query, connection))
                {
                    cmd.Parameters.AddWithValue("@NumPed", oc);
                    cmd.Parameters.AddWithValue("@FechaHora", fec);


                    int rowsAffected = cmd.ExecuteNonQuery();
                    if (rowsAffected < 1)
                    {
                        throw new Exception($@"No se actualizó ninguna fila ({rowsAffected}) rows Affected");
                    }
                    result.Correct = true;
                    result.Message = $@"{oc} se actualizaron {rowsAffected} columnas.";
                }
            }
            catch (Exception e)
            {
                result.Correct = false;
                result.Message = $@"Error al actuaizar oc {oc} {e.Message}";
            }
            return result;
        }
    }
}
