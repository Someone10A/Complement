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

                ML.Result resultValidateUser = ValidateUser(login.usu_id, mode);
                if (!resultValidateUser.Correct)
                {
                    throw new Exception(resultValidateUser.Message, resultValidateUser.Ex);
                }

                ML.Result resultValidatePassword = ValidatePassword(login.usu_id, login.usu_pass, mode);
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
        private static ML.Result ValidateUser(string userid, string mode)
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
        private static ML.Result ValidatePassword(string userid, string password, string mode)
        {
            ML.Result result = new ML.Result();
            try
            {
                string query = $@"SELECT TRIM(B.usu_pass), TRIM(B.cv_area), TRIM(B.usu_nombre), A.sub_rol
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
        public static List<string> GetBaseUsers(string mode)
        {
            ML.Result result = new ML.Result();
            List<string> baseUsers = new List<string>();
            try
            {
                string query = $@"SELECT A.usu_id
                                FROM ora_lga_usu A, dblga@lga_prod:lgausuario B
                                WHERE B.usu_id = A.usu_id
                                AND cv_area = 'CON'
                                AND sub_rol = 'BAS''";
                
                using (OdbcConnection connection = new OdbcConnection(DL.Connection.GetConnectionStringGen(mode)))
                {
                    connection.Open();

                    using (OdbcCommand cmd = new OdbcCommand(query, connection))
                    {
                        using (OdbcDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read()) 
                            {
                                string us = reader.GetString(0);

                                baseUsers.Add(us);
                            }
                        }
                    }
                }

                result.Correct = true;
                result.Object = baseUsers;
            }
            catch (Exception ex)
            {
                result.Correct = false;
                result.Message = $@"Error al obtener base users {ex.Message}";
                result.Ex = ex;
            }
            return baseUsers;
        } 
        public static List<string> GetInternetUsers(string mode)
        {
            ML.Result result = new ML.Result();
            List<string> users = new List<string>();
            try
            {
                string query = $@"SELECT A.usu_id
                                FROM ora_lga_usu A, dblga@lga_prod:lgausuario B
                                WHERE B.usu_id = A.usu_id
                                AND cv_area = 'CON'
                                AND sub_rol = 'INT''";
                
                using (OdbcConnection connection = new OdbcConnection(DL.Connection.GetConnectionStringGen(mode)))
                {
                    connection.Open();

                    using (OdbcCommand cmd = new OdbcCommand(query, connection))
                    {
                        using (OdbcDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read()) 
                            {
                                string us = reader.GetString(0);

                                users.Add(us);
                            }
                        }
                    }
                }

                result.Correct = true;
                result.Object = users;
            }
            catch (Exception ex)
            {
                result.Correct = false;
                result.Message = $@"Error al obtener internet users {ex.Message}";
                result.Ex = ex;
            }
            return users;
        } 
        public static List<string> GetSupervisorUsers(string mode)
        {
            ML.Result result = new ML.Result();
            List<string> users = new List<string>();
            try
            {
                string query = $@"SELECT A.usu_id
                                FROM ora_lga_usu A, dblga@lga_prod:lgausuario B
                                WHERE B.usu_id = A.usu_id
                                AND cv_area = 'CON'
                                AND sub_rol = 'SUP''";
                
                using (OdbcConnection connection = new OdbcConnection(DL.Connection.GetConnectionStringGen(mode)))
                {
                    connection.Open();

                    using (OdbcCommand cmd = new OdbcCommand(query, connection))
                    {
                        using (OdbcDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read()) 
                            {
                                string us = reader.GetString(0);

                                users.Add(us);
                            }
                        }
                    }
                }

                result.Correct = true;
                result.Object = users;
            }
            catch (Exception ex)
            {
                result.Correct = false;
                result.Message = $@"Error al obtener supervisor users {ex.Message}";
                result.Ex = ex;
            }
            return users;
        } 
    }
}
