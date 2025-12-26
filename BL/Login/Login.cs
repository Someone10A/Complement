using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.Odbc;

namespace BL.Login
{
    public class Login
    {
        public static ML.Result Log(ML.Login.Login login, string mode)
        {
            ML.Result result = new ML.Result();
            try
            {
                if(login.usu_id == null || login.usu_pass == null)
                {
                    throw new Exception($@"Usuario o contraseña vacios");
                }

                ML.Result resultValidateUser = ValidateAccount(login.usu_id, mode);
                if (!resultValidateUser.Correct)
                {
                    throw new Exception(resultValidateUser.Message, resultValidateUser.Ex);
                }

                // Obtener la bandera del tipo de usuario (LGA o OPE)
                bool isLGA = resultValidateUser.Object is bool flag ? flag : true; // Por defecto LGA si no se especifica

                ML.Result resultValidatePassword = ValidatePassword(login.usu_id, login.usu_pass, mode, isLGA);
                if (!resultValidatePassword.Correct)
                {
                    throw new Exception(resultValidatePassword.Message, resultValidatePassword.Ex);
                }
                ML.Login.Login loged = (ML.Login.Login)resultValidatePassword.Object;

                result.Correct = true;
                result.Object = loged;
            }
            catch (Exception ex)
            {
                result.Correct = false;
                result.Message = ex.Message;
                result.Ex = ex;
            }
            return result;
        }

        private static ML.Result ValidateAccount(string userid, string mode)
        {
            ML.Result result = new ML.Result();
            try
            {
                bool isNumeric = !string.IsNullOrEmpty(userid) && userid.All(char.IsDigit);
                bool isLGA = isNumeric;

                if (isNumeric)
                {
                    result = ValidateUserLGA(userid, mode);
                }
                else
                {
                    result = ValidateUserOPE(userid, mode);
                }

                if (result.Correct)
                {
                    result.Object = isLGA;
                }
            }
            catch (Exception ex)
            {
                result.Correct = false;
                result.Message = $@"{ex.Message}";
                result.Ex = ex;
            }
            return result;
        }
        private static ML.Result ValidateUserLGA(string userid, string mode)
        {
            ML.Result result = new ML.Result();
            try
            {
                string query = $@"SELECT B.usu_id
                                FROM ora_lga_usu A, dblga@lga_prod:lgausuario B
                                WHERE A.usu_id = ?
                                AND B.usu_id = A.usu_id
                                AND B.usu_status = 1";

                using (OdbcConnection connection = new OdbcConnection(DL.Connection.GetConnectionStringGen(mode)))
                {
                    connection.Open();

                    using (OdbcCommand cmd = new OdbcCommand(query, connection))
                    {
                        cmd.Parameters.AddWithValue("A.usu_id", userid);

                        using (OdbcDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                result.Correct = true;
                            }
                            else
                            {
                                throw new Exception($@"No se encontró el usuario dado de alta");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                result.Correct = false;
                result.Message = $@"{ex.Message}";
                result.Ex = ex;
            }
            return result;
        }
        private static ML.Result ValidateUserOPE(string userid, string mode)
        {
            ML.Result result = new ML.Result();
            try
            {
                string query = $@"SELECT rfc_ope
                                FROM ora_operadores
                                WHERE rfc_ope = ?
                                AND active = '1'";

                using (OdbcConnection connection = new OdbcConnection(DL.Connection.GetConnectionStringGen(mode)))
                {
                    connection.Open();

                    using (OdbcCommand cmd = new OdbcCommand(query, connection))
                    {
                        cmd.Parameters.AddWithValue("rfc_ope", userid);

                        using (OdbcDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                result.Correct = true;
                            }
                            else
                            {
                                throw new Exception($@"No se encontró el operador dado de alta o está inactivo");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                result.Correct = false;
                result.Message = $@"{ex.Message}";
                result.Ex = ex;
            }
            return result;
        }

        private static ML.Result ValidatePassword(string userid, string password, string mode, bool isLGA)
        {
            ML.Result result = new ML.Result();
            try
            {
                if (isLGA)
                {
                    result = ValidatePasswordLGA(userid, password, mode);
                }
                else
                {
                    result = ValidatePasswordOPE(userid, password, mode);
                }
            }
            catch (Exception ex)
            {
                result.Correct = false;
                result.Message = $@"Error al iniciar sesion {ex.Message}";
                result.Ex = ex;
            }
            return result;
        }
        private static ML.Result ValidatePasswordLGA(string userid, string password, string mode)
        {
            ML.Result result = new ML.Result();
            try
            {
                string query = $@"SELECT TRIM(B.usu_pass), TRIM(B.cv_area), TRIM(B.usu_nombre), A.sub_rol, B.cv_almacen
                                FROM ora_lga_usu A, dblga@lga_prod:lgausuario B
                                WHERE A.usu_id = ?
                                AND B.usu_id = A.usu_id";

                using (OdbcConnection connection = new OdbcConnection(DL.Connection.GetConnectionStringGen(mode)))
                {
                    connection.Open();

                    using (OdbcCommand cmd = new OdbcCommand(query, connection))
                    {
                        cmd.Parameters.AddWithValue("A.usu_id", userid);

                        using (OdbcDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                bool match = reader.GetString(0) == password ? true : false;
                                if (match)  
                                {
                                    ML.Login.Login logged = new ML.Login.Login();

                                    logged.usu_id = userid;
                                    logged.cv_area = reader.GetString(1);
                                    logged.usu_nombre  = reader.GetString(2);
                                    logged.sub_rol = reader.GetString(3);
                                    logged.nombre = logged.usu_nombre.Split(' ')[0];
                                    logged.pto_alm = reader.GetString(4);

                                    result.Correct = true;
                                    result.Object = logged;
                                }
                                else
                                {
                                    result.Message = $@"Usuario o contraseña incorrecta";
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                result.Correct = false;
                result.Message = $@"Error al iniciar sesion {ex.Message}";
                result.Ex = ex;
            }
            return result;
        }
        private static ML.Result ValidatePasswordOPE(string userid, string password, string mode)
        {
            ML.Result result = new ML.Result();
            try
            {
                string query = $@"SELECT TRIM(password), TRIM(nom_ope)
                                FROM ora_operadores
                                WHERE rfc_ope = ?
                                AND active = '1'";

                using (OdbcConnection connection = new OdbcConnection(DL.Connection.GetConnectionStringGen(mode)))
                {
                    connection.Open();

                    using (OdbcCommand cmd = new OdbcCommand(query, connection))
                    {
                        cmd.Parameters.AddWithValue("rfc_ope", userid);

                        using (OdbcDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                string dbPassword = reader.IsDBNull(0) ? "" : reader.GetString(0);
                                bool match = dbPassword == password;
                                
                                if (match)  
                                {
                                    ML.Login.Login logged = new ML.Login.Login();

                                    logged.usu_id = userid;
                                    logged.cv_area = "OPE"; // Área para operadores
                                    logged.usu_nombre = reader.IsDBNull(1) ? "" : reader.GetString(1);
                                    logged.sub_rol = "OPE"; // Rol para operadores
                                    logged.nombre = logged.usu_nombre.Split(' ')[0];
                                    logged.pto_alm = "870"; // Punto de almacén por defecto para operadores

                                    result.Correct = true;
                                    result.Object = logged;
                                }
                                else
                                {
                                    result.Message = $@"Usuario o contraseña incorrecta";
                                }
                            }
                            else
                            {
                                result.Message = $@"Usuario o contraseña incorrecta";
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                result.Correct = false;
                result.Message = $@"Error al iniciar sesion {ex.Message}";
                result.Ex = ex;
            }
            return result;
        } 
    }
}
