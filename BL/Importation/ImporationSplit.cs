using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Odbc;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BL.Importation
{
    public class ImporationSplit
    {
        public static ML.Result GetOrders(string mode)
        {
            ML.Result result = new ML.Result();
            try
            {
                using(OdbcConnection connection = new OdbcConnection(DL.Connection.GetConnectionStringGen(mode)))
                {
                    connection.Open();

                    string query = $@"SELECT fol_gtm,oc_madre,oc_hija,total_piezas
                                        FROM ora_imp_corden
                                        WHERE estatus = 0
                                        ";

                    List<ML.Importation.ora_imp_corden> orders = new List<ML.Importation.ora_imp_corden>();

                    OdbcDataAdapter adapter = new OdbcDataAdapter(query, connection);

                    DataTable table = new DataTable();

                    adapter.Fill(table);

                    foreach(DataRow dataRow in table.Rows)
                    {
                        ML.Importation.ora_imp_corden orden = new ML.Importation.ora_imp_corden
                        {
                            fol_gtm = dataRow["fol_gtm"].ToString().Trim(),
                            oc_madre = dataRow["oc_madre"].ToString(),
                            oc_hija = dataRow["oc_hija"].ToString(),
                            total_piezas = dataRow["total_piezas"].ToString()
                        };

                        orders.Add(orden);
                    }

                    result.Correct = true;
                    result.Object = orders;
                }
            }
            catch (Exception ex)
            {
                result.Correct = false;
                result.Message = $@"Error al obtener las ordenes madre {ex.Message}";
                result.Ex = ex;
            }
            return result;
        }
        public static ML.Result GetDerivedOrders(string order, string mode)
        {
            ML.Result result = new ML.Result();
            try
            {
                using (OdbcConnection connection = new OdbcConnection(DL.Connection.GetConnectionStringGen(mode)))
                {
                    connection.Open();

                    string query = $@"SELECT A.id,A.ocorig,A.num_ped,SUM(B.cant_distribu)AS cant_distribu
                                        FROM dbmdw@gnx_mdw:imp_compras A,dblga@lga_prod:lgadorco B
                                        WHERE A.cod_emp = 1
                                        AND A.ocorig = {order}
                                        AND B.cod_empresa = A.cod_emp
                                        AND B.no_orden = A.num_ped
                                        GROUP BY 1,2,3";

                    List<ML.Importation.OrderDerived> orders = new List<ML.Importation.OrderDerived>();

                    OdbcDataAdapter adapter = new OdbcDataAdapter(query, connection);

                    DataTable table = new DataTable();

                    adapter.Fill(table);

                    foreach (DataRow dataRow in table.Rows)
                    {
                        ML.Importation.OrderDerived orden = new ML.Importation.OrderDerived
                        {
                            ocorig = dataRow["ocorig"].ToString(),
                            num_ped = dataRow["num_ped"].ToString(),
                            cant_distribu = dataRow["cant_distribu"].ToString()
                        };

                        orders.Add(orden);
                    }

                    result.Correct = true;
                    result.Object = orders;
                }
            }
            catch (Exception ex)
            {
                result.Correct = false;
                result.Message = $@"Error al obtener las ordenes hijas {ex.Message}";
                result.Ex = ex;
            }
            return result;
        }
        public static ML.Result InsertSplit(ML.Importation.ora_imp_divide divide, string mode)
        {
            ML.Result result = new ML.Result();
            try
            {
                using (OdbcConnection connection = new OdbcConnection(DL.Connection.GetConnectionStringGen(mode)))
                {
                    connection.Open();

                    if(divide.hijas.Count < 2)
                    {
                        throw new Exception($@"No se puede insertar menos de 2 ordenes hijas.");
                    }

                    divide.oc_hijas = string.Join(",", divide.hijas);

                    string query = $@"INSERT INTO ora_imp_divide (estatus,cod_emp,fol_gtm,oc_madre,oc_hijas)
                                        VALUES (0,1,?,?,?)";

                    using(OdbcCommand cmd = new OdbcCommand(query, connection))
                    {
                        cmd.Parameters.AddWithValue("fol_gtm", divide.fol_gtm);
                        cmd.Parameters.AddWithValue("oc_madre", divide.oc_madre);
                        cmd.Parameters.AddWithValue("oc_hijas", divide.oc_hijas);

                        int rowsAffected = cmd.ExecuteNonQuery();
                        if(rowsAffected < 1)
                        {
                            throw new Exception($@"No se inserto ningun registro");
                        }
                    }

                    result.Correct = true;
                    result.Message = $@"Relación insertada correctamente";
                }
            }
            catch (Exception ex)
            {
                result.Correct = false;
                result.Message = $@"Error al insertar division {ex.Message}";
                result.Ex = ex;
            }
            return result;
        }

        public static ML.Result GetRelation(string orderList, string mode)
        {
            ML.Result result = new ML.Result();
            try
            {
                using (OdbcConnection connection = new OdbcConnection(DL.Connection.GetConnectionStringGen(mode)))
                {
                    connection.Open();

                    string query = $@"SELECT A.id,A.ocorig,A.num_ped,SUM(B.cant_distribu) cant_distribu
                                        FROM dbmdw@gnx_mdw:imp_compras A,dblga@lga_prod:lgadorco B
                                        WHERE A.cod_emp = 1
                                        AND A.num_ped IN ({orderList})
                                        AND B.cod_empresa = A.cod_emp
                                        AND B.no_orden = A.num_ped
                                        GROUP BY 1,2,3
                                        ORDER BY 1
                                        ";

                    List<ML.Importation.OrderDerived> orders = new List<ML.Importation.OrderDerived>();

                    OdbcDataAdapter adapter = new OdbcDataAdapter(query, connection);

                    DataTable table = new DataTable();

                    adapter.Fill(table);

                    foreach (DataRow dataRow in table.Rows)
                    {
                        ML.Importation.OrderDerived orden = new ML.Importation.OrderDerived
                        {
                            ocorig = dataRow["ocorig"].ToString(),
                            num_ped = dataRow["num_ped"].ToString(),
                            cant_distribu = dataRow["cant_distribu"].ToString()
                        };

                        orders.Add(orden);
                    }

                    result.Correct = true;
                    result.Object = orders;
                }
            }
            catch (Exception ex)
            {
                result.Correct = false;
                result.Message = $@"Error al obtener las ordenes relacionadas {ex.Message}";
                result.Ex = ex;
            }
            return result;
        }
    }
}
