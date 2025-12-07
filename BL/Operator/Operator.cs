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

        /*
         *Metodo:       AcceptRoute
         *Vista:        Asignado.cshtml
         *Descripcion:  Aceptar ruta del chofer
         *Entrada:      "Cabecera de ruta de chofer"  y mode
         *Salida:       ML.Result y Mensaje en result.Message
         *Proceso:      Actualizar ora_asignacion_operador a un estatus de Aceptada
         */

        /*
         *Metodo:       GetDetailRoute
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

        /*
         *Metodo:       GetHistorical
         *Vista:        Historico.cshtml
         *Descripcion:  Obtener el historico de rutas la asignadas al chofer 
         *Entrada:      string de rfcOpe y mode
         *Salida:       ML.Result y dentro del object una Lista de clase "Cabecera de ruta de chofer" 
         *Proceso:      Replicar proceso de ListOperadores y mostrar su estatus actual de la ruta
         *              Dentro de lo posible mostrar el % de entregas, si no validar hacerlo en la vista el calculo
         */
    }
}
