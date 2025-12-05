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

                    string query = @"UPDATE ora_operadores
                                    SET active = ?
                                    WHERE cod_emp = ?
                                    AND rfc_ope = ?";

                    using (OdbcCommand cmd = new OdbcCommand(query, connection))
                    {
                        cmd.Parameters.Add(new OdbcParameter("@active", active));
                        cmd.Parameters.Add(new OdbcParameter("@cod_emp", codEmp));
                        cmd.Parameters.Add(new OdbcParameter("@rfc_ope", rfcOpe.Trim()));

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
                                    ORDER BY rfc_ope, 
                                             CASE WHEN estatus = 0 THEN 1 
                                                  WHEN estatus = 2 THEN 2 
                                                  WHEN estatus = 3 THEN 3 
                                                  WHEN estatus = 1 THEN 4 
                                                  ELSE 5 END ASC";

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

                                    if (asignacion.estatus != 1)
                                    {
                                        if (!asignaciones.ContainsKey(rfcOpe))
                                        {
                                            asignaciones[rfcOpe] = asignacion;
                                        }
                                        else
                                        {
                                            // Si ya existe, mantener la que tenga menor estatus (mayor prioridad: 0 < 2 < 3)
                                            var asignacionExistente = asignaciones[rfcOpe];
                                            if (asignacion.estatus < asignacionExistente.estatus)
                                            {
                                                asignaciones[rfcOpe] = asignacion;
                                            }
                                        }
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



        public static ML.Result DeleteAsignacionOperador(string carSal, string mode)
        {
            ML.Result result = new ML.Result();
            try
            {
                
                using (OdbcConnection connection = new OdbcConnection(DL.Connection.GetConnectionStringGen(mode)))
                {
                    connection.Open();

                    string deleteQuery = @"DELETE FROM ora_asignacion_operador WHERE car_sal = ?";

                    using (OdbcCommand cmd = new OdbcCommand(deleteQuery, connection))
                    {
                        string carSalTrimmed = (carSal ?? "").Trim();
                        if (carSalTrimmed.Length > 60) carSalTrimmed = carSalTrimmed.Substring(0, 60);
                        carSalTrimmed = carSalTrimmed.PadRight(60);

                        cmd.Parameters.Add(new OdbcParameter("@car_sal", carSalTrimmed));

                        int rowsAffected = cmd.ExecuteNonQuery();

                        if (rowsAffected > 0)
                        {
                            result.Correct = true;
                            result.Message = "Asignación eliminada correctamente";
                        }
                    }
                }
            }
            catch (Exception ex)
            {
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

