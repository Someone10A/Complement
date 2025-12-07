using System;
using System.Collections.Generic;
using System.Data.Odbc;
using System.Linq;

namespace BL.RouteOperator
{
    public class AsignarCarSal
    {
        public static ML.Result GetCargasSalidaVirgenes(string mode)
        {
            ML.Result result = new ML.Result();
            try
            {
                using (OdbcConnection connection = new OdbcConnection(DL.Connection.GetConnectionStringGen(mode)))
                {
                    connection.Open();

                    // Obtener todas las car_sal que tienen registros en ora_rt para excluirlas
                    string queryConOraRt = @"SELECT DISTINCT A.car_sal
                                            FROM ora_ruta A
                                            INNER JOIN ora_rt B ON B.ord_rel = A.ord_rel
                                            WHERE A.pto_alm = 870
                                            AND A.car_sal IS NOT NULL
                                            AND TRIM(A.car_sal) <> ''";

                    var cargasConOraRt = new HashSet<string>();
                    
                    using (OdbcCommand cmd = new OdbcCommand(queryConOraRt, connection))
                    {
                        using (OdbcDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                string carSal = reader.GetString(0)?.Trim();
                                if (!string.IsNullOrEmpty(carSal))
                                {
                                    cargasConOraRt.Add(carSal);
                                }
                            }
                        }
                    }

                    // Obtener todas las car_sal únicas que NO están en la lista anterior
                    string queryTodas = @"SELECT DISTINCT car_sal
                                         FROM ora_ruta
                                         WHERE pto_alm = 870
                                         AND car_sal IS NOT NULL
                                         AND TRIM(car_sal) <> ''
                                         ORDER BY car_sal";

                    var todasCargasSalida = new List<string>();
                    
                    using (OdbcCommand cmd = new OdbcCommand(queryTodas, connection))
                    {
                        using (OdbcDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                string carSal = reader.GetString(0)?.Trim();
                                if (!string.IsNullOrEmpty(carSal) && !cargasConOraRt.Contains(carSal))
                                {
                                    todasCargasSalida.Add(carSal);
                                }
                            }
                        }
                    }

                    // Obtener estatus de ora_asignacion_operador
                    string queryAsignaciones = @"SELECT cod_emp, pto_alm, car_sal, rfc_ope, estatus
                                                FROM ora_asignacion_operador
                                                WHERE estatus IN (0, 1, 2, 3)";

                    var asignacionesDict = new Dictionary<string, ML.RouteOperator.AsignacionOperador>();
                    
                    using (OdbcCommand cmd = new OdbcCommand(queryAsignaciones, connection))
                    {
                        using (OdbcDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                string carSalAsignada = reader.IsDBNull(2) ? null : reader.GetString(2)?.Trim();
                                if (!string.IsNullOrEmpty(carSalAsignada))
                                {
                                    asignacionesDict[carSalAsignada] = new ML.RouteOperator.AsignacionOperador
                                    {
                                        cod_emp = reader.GetDecimal(0),
                                        pto_alm = reader.GetString(1)?.Trim() ?? "",
                                        car_sal = carSalAsignada,
                                        rfc_ope = reader.IsDBNull(3) ? null : reader.GetString(3)?.Trim(),
                                        estatus = reader.GetInt16(4)
                                    };
                                }
                            }
                        }
                    }

                    // Verificar para cada car_sal si tiene registros con estatus 1 o 2 (ya no es virgen)
                    var cargasConInfo = new List<ML.RouteOperator.CargaSalidaConAsignacion>();
                    int totalCount = 0;

                    foreach (string carSal in todasCargasSalida)
                    {
                        string checkEstatus = @"SELECT COUNT(*)
                                               FROM ora_ruta
                                               WHERE pto_alm = 870
                                               AND car_sal = ?
                                               AND estatus IN (1, 2)";

                        int countEstatus = 0;
                        using (OdbcCommand cmd = new OdbcCommand(checkEstatus, connection))
                        {
                            cmd.Parameters.Add(new OdbcParameter("@car_sal", carSal));
                            object countResult = cmd.ExecuteScalar();
                            countEstatus = countResult != null ? Convert.ToInt32(countResult) : 0;
                        }

                        if (countEstatus == 0)
                        {
                            totalCount++;
                            var cargaInfo = new ML.RouteOperator.CargaSalidaConAsignacion
                            {
                                CarSal = carSal,
                                TieneAsignacion = asignacionesDict.ContainsKey(carSal),
                                Asignacion = asignacionesDict.ContainsKey(carSal) ? asignacionesDict[carSal] : null
                            };
                            cargasConInfo.Add(cargaInfo);
                        }
                    }

                    result.Correct = true;
                    result.Object = cargasConInfo;
                }
            }
            catch (Exception ex)
            {
                result.Correct = false;
                result.Message = $"Error al obtener cargas de salida vírgenes: {ex.Message}";
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

                    string carSal = asignacion.car_sal ?? "";
                    string rfcOpe = asignacion.rfc_ope ?? "";

                    // Verifica si el operador tiene ruta asignada (estatus 0)
                    bool tieneRutaActiva = TieneAsignacionConEstatus(rfcOpe, 0, connection);
                    
                    if (tieneRutaActiva)
                    {
                        result.Correct = false;
                        result.Message = "El operador ya tiene una ruta activa";
                        return result;
                    }
                    
                    // Revisa en ora_ruta que los scn tengan estatus = 1 de todas sus rutas cerradas en ora_asignacion_operador
                    var rutasResult = GetRutasConEstatusNoCerrado(rfcOpe, mode);
                    var rutasConEstatusNoCerrado = rutasResult.Correct ? (List<ML.RouteOperator.RutaConScn>)rutasResult.Object : new List<ML.RouteOperator.RutaConScn>();
                    
                    if (rutasConEstatusNoCerrado != null && rutasConEstatusNoCerrado.Count > 0)
                    {
                        result.Correct = false;
                        var rutasInfo = string.Join(", ", rutasConEstatusNoCerrado.Select(r => r.car_sal));
                        result.Message = $"El operador tiene rutas activas que aún no están cerradas: {rutasInfo}. No es posible asignar otra ruta.";
                        return result;
                    }

                    string insertQuery = @"INSERT INTO ora_asignacion_operador (cod_emp, pto_alm, car_sal, rfc_ope, estatus)
                                          VALUES (?, ?, ?, ?, ?)";

                    using (OdbcCommand cmd = new OdbcCommand(insertQuery, connection))
                    {
                        string ptoAlm = asignacion.pto_alm.Trim();
                        if (ptoAlm.Length > 15) ptoAlm = ptoAlm.Substring(0, 15);


                        cmd.Parameters.Add(new OdbcParameter("@cod_emp", asignacion.cod_emp));
                        cmd.Parameters.Add(new OdbcParameter("@pto_alm", ptoAlm));
                        cmd.Parameters.Add(new OdbcParameter("@car_sal", carSal));
                        cmd.Parameters.Add(new OdbcParameter("@rfc_ope", rfcOpe));
                        cmd.Parameters.Add(new OdbcParameter("@estatus", asignacion.estatus));

                        int rowsAffected = cmd.ExecuteNonQuery();

                        if (rowsAffected > 0)
                        {
                            result.Correct = true;
                            result.Message = "Ruta asignada correctamente";
                        }
                        else
                        {
                            result.Correct = false;
                            result.Message = "No se pudo asignar la ruta";
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                
                string errorMessage = ex.Message.ToUpper();
                if (errorMessage.Contains("UNIQUE") || errorMessage.Contains("DUPLICATE") || errorMessage.Contains("23000"))
                {
                    string carSal = (asignacion.car_sal ?? "").Trim();
                    if (carSal.Length > 60) carSal = carSal.Substring(0, 60);

                    // Obtiene operador asignado a car_sal
                    string nombreOperador = GetOperadorAsignadoPorCarSal(carSal, mode);
                    
                    if (!string.IsNullOrEmpty(nombreOperador))
                    {
                        result.Correct = false;
                        result.Message = $"La carga de salida está asignada al operador: {nombreOperador}";
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

        private static bool TieneAsignacionConEstatus(string rfcOpe, int estatus, OdbcConnection connection)
        {
            try
            {
                string query = @"SELECT COUNT(*)
                                FROM ora_asignacion_operador
                                WHERE rfc_ope = ?
                                AND estatus = ?";

                using (OdbcCommand cmd = new OdbcCommand(query, connection))
                {
                    cmd.Parameters.Add(new OdbcParameter("@rfc_ope", rfcOpe.Trim()));
                    cmd.Parameters.Add(new OdbcParameter("@estatus", estatus));

                    object result = cmd.ExecuteScalar();
                    int count = result != null ? Convert.ToInt32(result) : 0;
                    return count > 0;
                }
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public static ML.Result GetRutasConEstatusNoCerrado(string rfcOpe, string mode)
        {
            ML.Result result = new ML.Result();
            try
            {
                using (OdbcConnection connection = new OdbcConnection(DL.Connection.GetConnectionStringGen(mode)))
                {
                    connection.Open();

                    string query = @"SELECT A.rfc_ope, B.estatus, B.car_sal, B.num_scn
                                    FROM ora_asignacion_operador A
                                    INNER JOIN ora_ruta B
                                            ON B.pto_alm = A.pto_alm
                                            AND B.car_sal = A.car_sal
                                    WHERE A.rfc_ope = ?
                                    AND B.estatus <> 1";

                    using (OdbcCommand cmd = new OdbcCommand(query, connection))
                    {
                        cmd.Parameters.Add(new OdbcParameter("@rfc_ope", rfcOpe.Trim()));

                        using (OdbcDataReader reader = cmd.ExecuteReader())
                        {
                            var rutasDict = new Dictionary<string, ML.RouteOperator.RutaConScn>();

                            while (reader.Read())
                            {
                                string rfcOpeResult = reader.IsDBNull(0) ? null : reader.GetString(0)?.Trim();
                                short estatus = reader.GetInt16(1);
                                string carSal = reader.IsDBNull(2) ? null : reader.GetString(2)?.Trim();
                                string numScn = reader.IsDBNull(3) ? null : reader.GetString(3)?.Trim();

                                if (!string.IsNullOrEmpty(carSal))
                                {
                                    if (!rutasDict.ContainsKey(carSal))
                                    {
                                        rutasDict[carSal] = new ML.RouteOperator.RutaConScn
                                        {
                                            car_sal = carSal,
                                            estatus = estatus,
                                            scns = new List<string>()
                                        };
                                    }

                                    if (!string.IsNullOrEmpty(numScn) && !rutasDict[carSal].scns.Contains(numScn))
                                    {
                                        rutasDict[carSal].scns.Add(numScn);
                                    }
                                }
                            }

                            result.Correct = true;
                            result.Object = rutasDict.Values.ToList();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                result.Correct = false;
                result.Message = $"Error al obtener las rutas con estatus no cerrado: {ex.Message}";
                result.Ex = ex;
            }
            return result;
        }

        private static string GetOperadorAsignadoPorCarSal(string carSal, string mode)
        {
            try
            {
                using (OdbcConnection connection = new OdbcConnection(DL.Connection.GetConnectionStringGen(mode)))
                {
                    connection.Open();

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
            }
            return "";
        }

        public static ML.Result GetAsignacionPorCarSal(string carSal, string mode)
        {
            ML.Result result = new ML.Result();
            try
            {
                using (OdbcConnection connection = new OdbcConnection(DL.Connection.GetConnectionStringGen(mode)))
                {
                    connection.Open();

                    string query = @"SELECT cod_emp, pto_alm, car_sal, rfc_ope, estatus
                                    FROM ora_asignacion_operador
                                    WHERE car_sal = ?";

                    using (OdbcCommand cmd = new OdbcCommand(query, connection))
                    {
                        cmd.Parameters.Add(new OdbcParameter("@car_sal", carSal.Trim()));
                        
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
                                result.Correct = false;
                                result.Message = "No se encontró asignación para la carga de salida especificada";
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

    }
}

