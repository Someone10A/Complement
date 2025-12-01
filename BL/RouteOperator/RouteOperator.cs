using System;
using System.Collections.Generic;
using System.Data.Odbc;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BL.RouteOperator
{
    public class RouteOperator
    {
        public static ML.Result GetOperadores(string mode)
        {
            ML.Result result = new ML.Result();
            try
            {
                using (OdbcConnection connection = new OdbcConnection(DL.Connection.GetConnectionStringGen(mode)))
                {
                    connection.Open();

                    string query = @"SELECT cod_emp, rfc_ope, nom_ope, active, password
                                    FROM ora_operadores
                                    ORDER BY active DESC, nom_ope ASC";

                    using (OdbcCommand cmd = new OdbcCommand(query, connection))
                    {
                        using (OdbcDataReader reader = cmd.ExecuteReader())
                        {
                            var operadores = new List<ML.RouteOperator.Operador>();

                            while (reader.Read())
                            {
                                var operador = new ML.RouteOperator.Operador
                                {
                                    cod_emp = reader.GetDecimal(0),
                                    rfc_ope = reader.IsDBNull(1) ? null : reader.GetString(1)?.Trim(),
                                    nom_ope = reader.GetString(2)?.Trim() ?? "",
                                    active = reader.GetString(3)?.Trim() ?? "0",
                                    password = reader.GetString(4)?.Trim() ?? ""
                                };

                                operadores.Add(operador);
                            }

                            result.Correct = true;
                            result.Object = operadores;
                        }
                    }
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

        public static ML.Result UpdateActiveStatus(decimal codEmp, string rfcOpe, string active, string mode)
        {
            ML.Result result = new ML.Result();
            try
            {
                using (OdbcConnection connection = new OdbcConnection(DL.Connection.GetConnectionStringGen(mode)))
                {
                    connection.Open();

                    string query;
                    if (string.IsNullOrEmpty(rfcOpe))
                    {
                        query = @"UPDATE ora_operadores
                                SET active = ?
                                WHERE cod_emp = ?
                                AND (rfc_ope IS NULL OR rfc_ope = '')";
                    }
                    else
                    {
                        query = @"UPDATE ora_operadores
                                SET active = ?
                                WHERE cod_emp = ?
                                AND rfc_ope = ?";
                    }

                    using (OdbcCommand cmd = new OdbcCommand(query, connection))
                    {
                        cmd.Parameters.Add(new OdbcParameter("@active", active));
                        cmd.Parameters.Add(new OdbcParameter("@cod_emp", codEmp));
                        if (!string.IsNullOrEmpty(rfcOpe))
                        {
                            cmd.Parameters.Add(new OdbcParameter("@rfc_ope", rfcOpe.Trim()));
                        }

                        int rowsAffected = cmd.ExecuteNonQuery();

                        if (rowsAffected > 0)
                        {
                            result.Correct = true;
                            result.Message = "Estado actualizado correctamente";
                        }
                        else
                        {
                            result.Correct = false;
                            result.Message = "No se encontró el registro para actualizar";
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                result.Correct = false;
                result.Message = $"Error al actualizar el estado: {ex.Message}";
                result.Ex = ex;
            }
            return result;
        }

        public static ML.Result AddOperador(ML.RouteOperator.Operador operador, string mode)
        {
            ML.Result result = new ML.Result();
            try
            {
                using (OdbcConnection connection = new OdbcConnection(DL.Connection.GetConnectionStringGen(mode)))
                {
                    connection.Open();

                    // Verificar si ya existe un operador con el mismo RFC
                    if (!string.IsNullOrEmpty(operador.rfc_ope))
                    {
                        string checkQuery = @"SELECT COUNT(*) FROM ora_operadores
                                            WHERE cod_emp = ?
                                            AND rfc_ope = ?";

                        using (OdbcCommand checkCmd = new OdbcCommand(checkQuery, connection))
                        {
                            checkCmd.Parameters.Add(new OdbcParameter("@cod_emp", operador.cod_emp));
                            checkCmd.Parameters.Add(new OdbcParameter("@rfc_ope", operador.rfc_ope.Trim()));

                            int count = Convert.ToInt32(checkCmd.ExecuteScalar());

                            if (count > 0)
                            {
                                result.Correct = false;
                                result.Message = "Ya existe un operador con este RFC";
                                return result;
                            }
                        }
                    }

                    string insertQuery = @"INSERT INTO ora_operadores (cod_emp, rfc_ope, nom_ope, active, password, fec_pass)
                                          VALUES (?, ?, ?, ?, ?, CURRENT)";

                    using (OdbcCommand cmd = new OdbcCommand(insertQuery, connection))
                    {
                        string rfcOpe = (operador.rfc_ope ?? "").Trim();
                        if (rfcOpe.Length > 15) rfcOpe = rfcOpe.Substring(0, 15);
                        rfcOpe = rfcOpe.PadRight(15);

                        string nomOpe = operador.nom_ope.Trim();
                        if (nomOpe.Length > 60) nomOpe = nomOpe.Substring(0, 60);
                        nomOpe = nomOpe.PadRight(60);

                        string password = operador.password.Trim();
                        if (password.Length > 16) password = password.Substring(0, 16);
                        password = password.PadRight(16);

                        cmd.Parameters.Add(new OdbcParameter("@cod_emp", operador.cod_emp));
                        cmd.Parameters.Add(new OdbcParameter("@rfc_ope", rfcOpe));
                        cmd.Parameters.Add(new OdbcParameter("@nom_ope", nomOpe));
                        cmd.Parameters.Add(new OdbcParameter("@active", operador.active ?? "0"));
                        cmd.Parameters.Add(new OdbcParameter("@password", password));

                        int rowsAffected = cmd.ExecuteNonQuery();

                        if (rowsAffected > 0)
                        {
                            result.Correct = true;
                            result.Message = "Operador agregado correctamente";
                        }
                        else
                        {
                            result.Correct = false;
                            result.Message = "No se pudo agregar el operador";
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                result.Correct = false;
                result.Message = $"Error al agregar el operador: {ex.Message}";
                result.Ex = ex;
            }
            return result;
        }

        public static ML.Result ValidateRuta(string carSal, string mode)
        {
            ML.Result result = new ML.Result();
            try
            {
                //Console.WriteLine($"[ValidateRuta] Iniciando validación de ruta. carSal: '{carSal}', mode: '{mode}'");
                
                using (OdbcConnection connection = new OdbcConnection(DL.Connection.GetConnectionStringGen(mode)))
                {
                    //Console.WriteLine($"[ValidateRuta] Abriendo conexión a base de datos...");
                    connection.Open();
                    //Console.WriteLine($"[ValidateRuta] Conexión abierta exitosamente");

                    int count = ValidateRutaCount(carSal, connection);
                    //Console.WriteLine($"[ValidateRuta] Count obtenido: {count}");

                    result.Correct = true;
                    result.Object = count;
                    result.Message = count == 0 ? "Ruta apta para asignar" : "Ruta no es apta";
                    //Console.WriteLine($"[ValidateRuta] Validación completada. Correct: {result.Correct}, Message: {result.Message}");
                }
            }
            catch (Exception ex)
            {
                //Console.WriteLine($"[ValidateRuta] ERROR: {ex.Message}");
                //Console.WriteLine($"[ValidateRuta] StackTrace: {ex.StackTrace}");
                result.Correct = false;
                result.Message = $"Error al validar la ruta: {ex.Message}";
                result.Ex = ex;
            }
            return result;
        }

        public static ML.Result GetAllAsignacionesOperadores(string mode)
        {
            ML.Result result = new ML.Result();
            try
            {
                using (OdbcConnection connection = new OdbcConnection(DL.Connection.GetConnectionStringGen(mode)))
                {
                    connection.Open();

                    string query = @"SELECT cod_emp, pto_alm, car_sal, rfc_ope, estatus
                                    FROM ora_asignacion_operador
                                    WHERE rfc_ope IS NOT NULL
                                    ORDER BY rfc_ope, estatus ASC";

                    using (OdbcCommand cmd = new OdbcCommand(query, connection))
                    {
                        using (OdbcDataReader reader = cmd.ExecuteReader())
                        {
                            var asignaciones = new Dictionary<string, ML.RouteOperator.AsignacionOperador>();

                            while (reader.Read())
                            {
                                string rfcOpe = reader.IsDBNull(3) ? null : reader.GetString(3)?.Trim();
                                if (!string.IsNullOrEmpty(rfcOpe))
                                {
                                    var asignacion = new ML.RouteOperator.AsignacionOperador
                                    {
                                        cod_emp = reader.GetDecimal(0),
                                        pto_alm = reader.GetString(1)?.Trim() ?? "",
                                        car_sal = reader.IsDBNull(2) ? null : reader.GetString(2)?.Trim(),
                                        rfc_ope = rfcOpe,
                                        estatus = reader.GetInt16(4)
                                    };

                                    if (!asignaciones.ContainsKey(rfcOpe))
                                    {
                                        asignaciones[rfcOpe] = asignacion;
                                    }
                                }
                            }

                            result.Correct = true;
                            result.Object = asignaciones;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                result.Correct = false;
                result.Message = $"Error al obtener las asignaciones: {ex.Message}";
                result.Ex = ex;
            }
            return result;
        }

        public static ML.Result GetAsignacionOperador(string rfcOpe, string mode)
        {
            ML.Result result = new ML.Result();
            try
            {
                using (OdbcConnection connection = new OdbcConnection(DL.Connection.GetConnectionStringGen(mode)))
                {
                    connection.Open();

                    string query = @"SELECT cod_emp, pto_alm, car_sal, rfc_ope, estatus
                                    FROM ora_asignacion_operador
                                    WHERE rfc_ope = ?
                                    ORDER BY estatus ASC";

                    using (OdbcCommand cmd = new OdbcCommand(query, connection))
                    {
                        cmd.Parameters.Add(new OdbcParameter("@rfc_ope", rfcOpe.Trim()));

                        using (OdbcDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                var asignacion = new ML.RouteOperator.AsignacionOperador
                                {
                                    cod_emp = reader.GetDecimal(0),
                                    pto_alm = reader.GetString(1)?.Trim() ?? "",
                                    car_sal = reader.IsDBNull(2) ? null : reader.GetString(2)?.Trim(),
                                    rfc_ope = reader.IsDBNull(3) ? null : reader.GetString(3)?.Trim(),
                                    estatus = reader.GetInt16(4)
                                };

                                result.Correct = true;
                                result.Object = asignacion;
                            }
                            else
                            {
                                result.Correct = true;
                                result.Object = null;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                result.Correct = false;
                result.Message = $"Error al obtener la asignación: {ex.Message}";
                result.Ex = ex;
            }
            return result;
        }

        public static ML.Result InsertAsignacionOperador(ML.RouteOperator.AsignacionOperador asignacion, string mode)
        {
            ML.Result result = new ML.Result();
            try
            {
                using (OdbcConnection connection = new OdbcConnection(DL.Connection.GetConnectionStringGen(mode)))
                {
                    connection.Open();

                    //Correccion Siento que estas validaciones estan de mas, ya que deben de venir los datos de la vista
                            //los datos de la vista deben venir del Back
                    string carSal = (asignacion.car_sal ?? "").Trim();
                    if (carSal.Length > 60) carSal = carSal.Substring(0, 60);

                    string rfcOpe = (asignacion.rfc_ope ?? "").Trim();
                    if (rfcOpe.Length > 15) rfcOpe = rfcOpe.Substring(0, 15);

                    // Variable para almacenar información sobre asignación previa eliminada
                    string carSalEliminada = null;

                    // Paso 0: Verificar si el operador ya tiene una asignación existente
                    var asignacionExistente = GetAsignacionOperadorByRfc(rfcOpe, connection);
                    
                    if (asignacionExistente != null)
                    {
                        //Console.WriteLine($"[InsertAsignacionOperador] Paso 0: Se encontró asignación existente. car_sal: '{asignacionExistente.car_sal}', estatus: {asignacionExistente.estatus}");
                        
                        // Si estatus != 1, mantener el registro y no permitir asignar otra ruta
                        if (asignacionExistente.estatus != 1)
                        {
                            result.Correct = false;
                            result.Message = "El operador tiene una ruta asignada que aún no se ha cerrado. No es posible asignar otra ruta.";
                            //Console.WriteLine($"[InsertAsignacionOperador] Paso 0 fallido: {result.Message}");
                            return result;
                        }
                        
                        // Si estatus = 1, verificar en ora_ruta
                        //Console.WriteLine($"[InsertAsignacionOperador] Paso 0: estatus = 1, verificando ora_ruta...");
                        string carSalExistente = (asignacionExistente.car_sal ?? "").Trim();
                        int estatusRuta = GetEstatusRuta(carSalExistente, connection);
                        //Console.WriteLine($"[InsertAsignacionOperador] Paso 0: estatus en ora_ruta para car_sal '{carSalExistente}': {estatusRuta}");
                        
                        if (estatusRuta == 1)
                        {
                            // Borrar el registro de ora_asignacion_operador
                            //Console.WriteLine($"[InsertAsignacionOperador] Paso 0: estatus_ruta = 1, borrando asignación existente...");
                            DeleteAsignacionOperadorByRfc(rfcOpe, connection);
                            carSalEliminada = carSalExistente; // Guardar para mensaje posterior
                            //Console.WriteLine($"[InsertAsignacionOperador] Paso 0: Asignación existente borrada. Continuando con nueva asignación...");
                        }
                        else
                        {
                            // Mantener el registro y no permitir asignar otra ruta
                            result.Correct = false;
                            result.Message = "El operador tiene una ruta asignada que aún no está finalizada en ora_ruta. No es posible asignar otra ruta.";
                            //Console.WriteLine($"[InsertAsignacionOperador] Paso 0 fallido: {result.Message}");
                            return result;
                        }
                    }
                    //Console.WriteLine($"[InsertAsignacionOperador] Paso 0 exitoso: No hay asignación existente o fue eliminada. Continuando...");

                    // Paso 1: Verificar que car_sal exista en ora_ruta
                    //Console.WriteLine($"[InsertAsignacionOperador] Paso 1: Validando que car_sal existe en ora_ruta: '{carSal}'");
                    bool existeEnRuta = ExisteCarSalEnRuta(carSal, mode);
                    
                    if (!existeEnRuta)
                    {
                        result.Correct = false;
                        result.Message = $"La carga de salida '{carSal}' no existe";
                        //Console.WriteLine($"[InsertAsignacionOperador] Paso 1 fallido: {result.Message}. NO se ejecutará la query de validación.");
                        return result;
                    }
                    //Console.WriteLine($"[InsertAsignacionOperador] Paso 1 exitoso: car_sal existe en ora_ruta. Continuando con validación...");

                    // Paso 2: Ejecutar query de validación (solo si existe en ora_ruta)
                    //Console.WriteLine($"[InsertAsignacionOperador] Paso 2: Ejecutando validación de ruta apta...");
                    int validationCount = ValidateRutaCount(carSal, connection);
                    //Console.WriteLine($"[InsertAsignacionOperador] Paso 2: Resultado de validación: count={validationCount}");

                    if (validationCount != 0)
                    {
                        result.Correct = false;
                        result.Message = "Ruta no es apta para asignar";
                        //Console.WriteLine($"[InsertAsignacionOperador] Paso 2 fallido: {result.Message}. NO se ejecutará la inserción.");
                        return result;
                    }
                    //Console.WriteLine($"[InsertAsignacionOperador] Paso 2 exitoso: ruta apta (count=0). Continuando con inserción...");

                    string insertQuery = @"INSERT INTO ora_asignacion_operador (cod_emp, pto_alm, car_sal, rfc_ope, estatus)
                                          VALUES (?, ?, ?, ?, ?)";

                    //Console.WriteLine($"[InsertAsignacionOperador] Query de inserción preparada: {insertQuery}");

                    using (OdbcCommand cmd = new OdbcCommand(insertQuery, connection))
                    {
                        string ptoAlm = asignacion.pto_alm.Trim();
                        if (ptoAlm.Length > 15) ptoAlm = ptoAlm.Substring(0, 15);

                        //Console.WriteLine($"[InsertAsignacionOperador] Valores procesados - pto_alm: '{ptoAlm}', car_sal: '{carSal}', rfc_ope: '{rfcOpe}'");

                        cmd.Parameters.Add(new OdbcParameter("@cod_emp", asignacion.cod_emp));
                        cmd.Parameters.Add(new OdbcParameter("@pto_alm", ptoAlm));
                        cmd.Parameters.Add(new OdbcParameter("@car_sal", carSal));
                        cmd.Parameters.Add(new OdbcParameter("@rfc_ope", rfcOpe));
                        cmd.Parameters.Add(new OdbcParameter("@estatus", asignacion.estatus));

                        //Console.WriteLine($"[InsertAsignacionOperador] Parámetros agregados. Ejecutando ExecuteNonQuery...");
                        int rowsAffected = cmd.ExecuteNonQuery();
                        //Console.WriteLine($"[InsertAsignacionOperador] ExecuteNonQuery completado. RowsAffected: {rowsAffected}");

                        if (rowsAffected > 0)
                        {
                            result.Correct = true;
                            // Si se eliminó una asignación previa, informar al usuario
                            if (!string.IsNullOrEmpty(carSalEliminada))
                            {
                                result.Message = $"Se eliminó la asignación previa de la ruta '{carSalEliminada}' (finalizada) y se asignó la nueva ruta correctamente.";
                            }
                            else
                            {
                                result.Message = "Ruta asignada correctamente";
                            }
                            //Console.WriteLine($"[InsertAsignacionOperador] Inserción exitosa. Message: {result.Message}");
                        }
                        else
                        {
                            result.Correct = false;
                            result.Message = "No se pudo asignar la ruta";
                            //Console.WriteLine($"[InsertAsignacionOperador] Inserción fallida. Message: {result.Message}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                //Console.WriteLine($"[InsertAsignacionOperador] ERROR: {ex.Message}");
                //Console.WriteLine($"[InsertAsignacionOperador] StackTrace: {ex.StackTrace}");
                
                string errorMessage = ex.Message.ToUpper();
                if (errorMessage.Contains("UNIQUE") || errorMessage.Contains("DUPLICATE") || errorMessage.Contains("23000"))
                {
                    // Obtener el operador asignado a carga de salida
                    string carSal = (asignacion.car_sal ?? "").Trim();
                    if (carSal.Length > 60) carSal = carSal.Substring(0, 60);
                    
                    string nombreOperador = GetOperadorAsignadoPorCarSal(carSal, mode);
                    
                    if (!string.IsNullOrEmpty(nombreOperador))
                    {
                        result.Correct = false;
                        result.Message = $"La carga de salida está asignada al operador: {nombreOperador}";
                    }
                    else
                    {
                        result.Correct = false;
                        result.Message = "La carga de salida ya está asignada a otro operador";
                    }
                }
                else
                {
                    result.Correct = false;
                    result.Message = $"Error al asignar la ruta: {ex.Message}";
                }
                result.Ex = ex;
            }
            return result;
        }

        private static ML.RouteOperator.AsignacionOperador GetAsignacionOperadorByRfc(string rfcOpe, OdbcConnection connection)
        {
            try
            {
                /*Correccion aqui en realidad, deberia buscar un estatus IN(...) buscando la ruta con estatus "vivos" si no hay registros si puede asignar nueva ruta*/
                string query = @"SELECT cod_emp, pto_alm, car_sal, rfc_ope, estatus
                                FROM ora_asignacion_operador
                                WHERE rfc_ope = ?
                                ORDER BY estatus ASC";

                using (OdbcCommand cmd = new OdbcCommand(query, connection))
                {
                    cmd.Parameters.Add(new OdbcParameter("@rfc_ope", rfcOpe.Trim()));

                    using (OdbcDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            var asignacion = new ML.RouteOperator.AsignacionOperador
                            {
                                cod_emp = reader.GetDecimal(0),
                                pto_alm = reader.GetString(1)?.Trim() ?? "",
                                car_sal = reader.IsDBNull(2) ? null : reader.GetString(2)?.Trim(),
                                rfc_ope = reader.IsDBNull(3) ? null : reader.GetString(3)?.Trim(),
                                estatus = reader.GetInt16(4)
                            };
                            return asignacion;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                //Console.WriteLine($"[GetAsignacionOperadorByRfc] ERROR: {ex.Message}");
            }
            return null;
        }

        private static int GetEstatusRuta(string carSal, OdbcConnection connection)
        {
            try
            {
                string query = @"SELECT estatus
                                FROM ora_ruta
                                WHERE car_sal = ?
                                AND pto_alm = 870";

                using (OdbcCommand cmd = new OdbcCommand(query, connection))
                {
                    cmd.Parameters.Add(new OdbcParameter("@car_sal", carSal.Trim()));
                    object result = cmd.ExecuteScalar();
                    if (result != null && !Convert.IsDBNull(result))
                    {
                        return Convert.ToInt32(result);
                    }
                }
            }
            catch (Exception ex)
            {
                //Console.WriteLine($"[GetEstatusRuta] ERROR: {ex.Message}");
            }
            return -1; // Retornar -1 si no se encuentra o hay error
        }

        private static void DeleteAsignacionOperadorByRfc(string rfcOpe, OdbcConnection connection)
        {
            try
            {
                string deleteQuery = @"DELETE FROM ora_asignacion_operador WHERE rfc_ope = ?";

                using (OdbcCommand cmd = new OdbcCommand(deleteQuery, connection))
                {
                    string rfcOpeTrimmed = (rfcOpe ?? "").Trim();
                    if (rfcOpeTrimmed.Length > 15) rfcOpeTrimmed = rfcOpeTrimmed.Substring(0, 15);

                    cmd.Parameters.Add(new OdbcParameter("@rfc_ope", rfcOpeTrimmed));
                    cmd.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                //Console.WriteLine($"[DeleteAsignacionOperadorByRfc] ERROR: {ex.Message}");
                throw; // Re-lanzar la excepción para que sea manejada en el método llamador
            }
        }

        private static string GetOperadorAsignadoPorCarSal(string carSal, string mode)
        {
            try
            {
                using (OdbcConnection connection = new OdbcConnection(DL.Connection.GetConnectionStringGen(mode)))
                {
                    connection.Open();

                    // Primero obtener el RFC del operador asignado
                    string queryAsignacion = @"SELECT rfc_ope
                                             FROM ora_asignacion_operador
                                             WHERE car_sal = ?";

                    string rfcOpe = null;
                    using (OdbcCommand cmd = new OdbcCommand(queryAsignacion, connection))
                    {
                        cmd.Parameters.Add(new OdbcParameter("@car_sal", carSal.Trim()));
                        object result = cmd.ExecuteScalar();
                        if (result != null && !Convert.IsDBNull(result))
                        {
                            rfcOpe = result.ToString()?.Trim();
                        }
                    }

                    // Si encontramos el RFC, obtener el nombre del operador
                    if (!string.IsNullOrEmpty(rfcOpe))
                    {
                        string queryOperador = @"SELECT nom_ope
                                                FROM ora_operadores
                                                WHERE rfc_ope = ?";

                        using (OdbcCommand cmd = new OdbcCommand(queryOperador, connection))
                        {
                            cmd.Parameters.Add(new OdbcParameter("@rfc_ope", rfcOpe));
                            object result = cmd.ExecuteScalar();
                            if (result != null && !Convert.IsDBNull(result))
                            {
                                return result.ToString()?.Trim() ?? "";
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                //Console.WriteLine($"[GetOperadorAsignadoPorCarSal] ERROR: {ex.Message}");
            }
            return "";
        }

        private static bool ExisteCarSalEnRuta(string carSal, string mode)
        {
            try
            {
                //Console.WriteLine($"[ExisteCarSalEnRuta] Validando existencia de car_sal en ora_ruta: '{carSal}'");
                
                using (OdbcConnection connection = new OdbcConnection(DL.Connection.GetConnectionStringGen(mode)))
                {
                    connection.Open();

                    string query = @"SELECT COUNT(*)
                                   FROM ora_ruta
                                   WHERE car_sal = ?";

                    using (OdbcCommand cmd = new OdbcCommand(query, connection))
                    {
                        cmd.Parameters.Add(new OdbcParameter("@car_sal", carSal.Trim()));
                        object result = cmd.ExecuteScalar();
                        int count = result != null ? Convert.ToInt32(result) : 0;
                        
                        bool existe = count > 0;
                        //Console.WriteLine($"[ExisteCarSalEnRuta] Resultado: existe={existe}, count={count}");
                        return existe;
                    }
                }
            }
            catch (Exception ex)
            {
                //Console.WriteLine($"[ExisteCarSalEnRuta] ERROR: {ex.Message}");
                return false;
            }
        }

        private static int ValidateRutaCount(string carSal, OdbcConnection connection)
        {
            string query = @"SELECT COUNT(DISTINCT B.ord_rel)
                           FROM ora_ruta A,OUTER ora_rt B
                           WHERE A.pto_alm = 870
                           AND A.car_sal = ?
                           AND A.estatus NOT IN (1,2)
                           AND B.ord_rel = A.ord_rel";

            using (OdbcCommand cmd = new OdbcCommand(query, connection))
            {
                cmd.Parameters.Add(new OdbcParameter("@car_sal", carSal.Trim()));
                object countObj = cmd.ExecuteScalar();
                int count = countObj != null ? Convert.ToInt32(countObj) : 0;
                return count;
            }
        }

        public static ML.Result DeleteAsignacionOperador(string carSal, string mode)
        {
            ML.Result result = new ML.Result();
            try
            {
                //Console.WriteLine($"[DeleteAsignacionOperador] Iniciando eliminación. car_sal: '{carSal}', mode: '{mode}'");
                
                using (OdbcConnection connection = new OdbcConnection(DL.Connection.GetConnectionStringGen(mode)))
                {
                    //Console.WriteLine($"[DeleteAsignacionOperador] Abriendo conexión a base de datos...");
                    connection.Open();
                    //Console.WriteLine($"[DeleteAsignacionOperador] Conexión abierta exitosamente");

                    string deleteQuery = @"DELETE FROM ora_asignacion_operador WHERE car_sal = ?";
                    //Console.WriteLine($"[DeleteAsignacionOperador] Query preparada: {deleteQuery}");

                    using (OdbcCommand cmd = new OdbcCommand(deleteQuery, connection))
                    {
                        string carSalTrimmed = (carSal ?? "").Trim();
                        if (carSalTrimmed.Length > 60) carSalTrimmed = carSalTrimmed.Substring(0, 60);
                        carSalTrimmed = carSalTrimmed.PadRight(60);

                        cmd.Parameters.Add(new OdbcParameter("@car_sal", carSalTrimmed));
                        //Console.WriteLine($"[DeleteAsignacionOperador] Parámetro car_sal: '{carSalTrimmed}'. Ejecutando ExecuteNonQuery...");

                        int rowsAffected = cmd.ExecuteNonQuery();
                        //Console.WriteLine($"[DeleteAsignacionOperador] ExecuteNonQuery completado. RowsAffected: {rowsAffected}");

                        if (rowsAffected > 0)
                        {
                            result.Correct = true;
                            result.Message = "Asignación eliminada correctamente";
                            //Console.WriteLine($"[DeleteAsignacionOperador] Eliminación exitosa. Message: {result.Message}");
                        }
                        else
                        {
                            result.Correct = false;
                            result.Message = "No se encontró la asignación para eliminar";
                            //Console.WriteLine($"[DeleteAsignacionOperador] Eliminación fallida. Message: {result.Message}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                //Console.WriteLine($"[DeleteAsignacionOperador] ERROR: {ex.Message}");
                //Console.WriteLine($"[DeleteAsignacionOperador] StackTrace: {ex.StackTrace}");
                result.Correct = false;
                result.Message = $"Error al eliminar la asignación: {ex.Message}";
                result.Ex = ex;
            }
            return result;
        }

        public static ML.Result ResetPassword(decimal codEmp, string rfcOpe, string newPassword, string mode)
        {
            ML.Result result = new ML.Result();
            try
            {
                using (OdbcConnection connection = new OdbcConnection(DL.Connection.GetConnectionStringGen(mode)))
                {
                    connection.Open();

                    string query;
                    if (string.IsNullOrEmpty(rfcOpe))
                    {
                        query = @"UPDATE ora_operadores
                                SET password = ?,
                                    fec_pass = CURRENT
                                WHERE cod_emp = ?
                                AND (rfc_ope IS NULL OR rfc_ope = '')";
                    }
                    else
                    {
                        query = @"UPDATE ora_operadores
                                SET password = ?,
                                    fec_pass = CURRENT
                                WHERE cod_emp = ?
                                AND rfc_ope = ?";
                    }

                    using (OdbcCommand cmd = new OdbcCommand(query, connection))
                    {
                        string password = newPassword.Trim();
                        if (password.Length > 16) password = password.Substring(0, 16);
                        password = password.PadRight(16);

                        cmd.Parameters.Add(new OdbcParameter("@password", password));
                        cmd.Parameters.Add(new OdbcParameter("@cod_emp", codEmp));
                        if (!string.IsNullOrEmpty(rfcOpe))
                        {
                            cmd.Parameters.Add(new OdbcParameter("@rfc_ope", rfcOpe.Trim()));
                        }

                        int rowsAffected = cmd.ExecuteNonQuery();

                        if (rowsAffected > 0)
                        {
                            result.Correct = true;
                            result.Message = "Contraseña actualizada correctamente";
                        }
                        else
                        {
                            result.Correct = false;
                            result.Message = "No se encontró el registro para actualizar";
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                result.Correct = false;
                result.Message = $"Error al actualizar la contraseña: {ex.Message}";
                result.Ex = ex;
            }
            return result;
        }
    }
}

