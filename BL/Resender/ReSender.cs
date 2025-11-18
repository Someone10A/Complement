using DocumentFormat.OpenXml.Office.Word;
using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.Data.SqlClient;
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


    }
}
