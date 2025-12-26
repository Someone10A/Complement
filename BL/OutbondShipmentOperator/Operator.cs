using System;
using System.Collections.Generic;
using System.Data.Odbc;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BL.OutbondShipmentOperator
{
    public class Operator
    {
        /*Get Operadores*/
        public static ML.Result GetOperadores(string mode)
        {
            ML.Result result = new ML.Result();
            try
            {
                using (OdbcConnection connection = new OdbcConnection(DL.Connection.GetConnectionStringGen(mode)))
                {
                    connection.Open();

                    string query = @"SELECT TRIM(rfc_ope),TRIM(nom_ope),active
                                        FROM ora_operadores
                                        ORDER BY active DESC, nom_ope ASC";

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
        /*UpdateStatus*/
        public static ML.Result UpdateStatus(string rfcOpe, string active, string mode)
        {
            ML.Result result = new ML.Result();
            try
            {
                using (OdbcConnection connection = new OdbcConnection(DL.Connection.GetConnectionStringGen(mode)))
                {
                    connection.Open();

                    string query = $@"UPDATE ora_operadores
                                    SET active = ?
                                    WHERE cod_emp = 1
                                    AND rfc_ope = ?";

                    using (OdbcCommand cmd = new OdbcCommand(query, connection))
                    {
                        cmd.Parameters.Add(new OdbcParameter("@active", active));
                        cmd.Parameters.Add(new OdbcParameter("@rfc_ope", rfcOpe.Trim()));

                        int rowsAffected = cmd.ExecuteNonQuery();

                        if (rowsAffected < 1)
                        {
                            throw new Exception($@"No se encontró el registro para actualizar, sin registros afectados");
                        }
                    }

                    result.Correct = true;
                    result.Message = "Estado actualizado correctamente";
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
        /*AddOperador*/
        public static ML.Result AddOperador(ML.OutbondShipmentOperator.Operator ope, string mode)
        {
            ML.Result result = new ML.Result();
            try
            {
                using (OdbcConnection connection = new OdbcConnection(DL.Connection.GetConnectionStringGen(mode)))
                {
                    connection.Open();
                    string checkQuery = @"SELECT COUNT(*) FROM ora_operadores
                                            WHERE cod_emp = 1
                                            AND rfc_ope = ?";

                    using (OdbcCommand checkCmd = new OdbcCommand(checkQuery, connection))
                    {
                        checkCmd.Parameters.Add(new OdbcParameter("@rfc_ope", ope.RfcOpe));

                        int count = Convert.ToInt32(checkCmd.ExecuteScalar());

                        if (count > 0)
                        {
                            throw new Exception($@"Ya existe un operador con este RFC");
                        }
                    }

                    string insertQuery = @"INSERT INTO ora_operadores (cod_emp, rfc_ope, nom_ope, active, password, fec_pass)
                                          VALUES (1, ?, ?, 1, ?, CURRENT)";

                    using (OdbcCommand cmd = new OdbcCommand(insertQuery, connection))
                    {
                        cmd.Parameters.Add(new OdbcParameter("@rfc_ope", ope.RfcOpe));
                        cmd.Parameters.Add(new OdbcParameter("@nom_ope", ope.NomOpe));
                        cmd.Parameters.Add(new OdbcParameter("@password", ope.Password));

                        int rowsAffected = cmd.ExecuteNonQuery();

                        if (rowsAffected < 1)
                        {
                            throw new Exception($@"No se pudo agregar el operador");
                        }
                    }
                    result.Correct = true;
                    result.Message = "Operador agregado correctamente";
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
        /*ResetPassWord*/
        public static ML.Result ResetPassword(string rfcOpe, string newPassword, string mode)
        {
            ML.Result result = new ML.Result();
            try
            {
                using (OdbcConnection connection = new OdbcConnection(DL.Connection.GetConnectionStringGen(mode)))
                {
                    connection.Open();

                    string query = $@"UPDATE ora_operadores
                                        SET password = ?,
                                            fec_pass = CURRENT
                                        WHERE cod_emp = 1
                                        AND rfc_ope = ?";

                    using (OdbcCommand cmd = new OdbcCommand(query, connection))
                    {
                        cmd.Parameters.Add(new OdbcParameter("@password", newPassword));
                        cmd.Parameters.Add(new OdbcParameter("@rfc_ope", rfcOpe));

                        int rowsAffected = cmd.ExecuteNonQuery();

                        if (rowsAffected < 1)
                        {
                            throw new Exception($@"No hay registros actualizados");
                        }
                    }

                    result.Correct = true;
                    result.Message = "Contraseña actualizada correctamente";
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
