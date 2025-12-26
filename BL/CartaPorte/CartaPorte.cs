using System;
using System.Collections.Generic;
using System.Data.Odbc;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using ML;
using ML.CartaPorte;

namespace BL.CartaPorte
{
    public static class CartaPorte
    {
        // Método auxiliar para obtener valores seguros del reader
        public static object GetSafeValue(System.Data.Common.DbDataReader reader, int index, string fieldName, string context = " ", string scn = " ")
        {
            if (reader.IsDBNull(index))
            {
                var scnInfo = !string.IsNullOrEmpty(scn) ? $" (SCN: {scn})" : " ";
                //System.Console.WriteLine($"[{context}] NULL value: {fieldName} field{scnInfo}");
                return null;
            }

            try
            {
                var value = reader.GetValue(index);

                if (value is decimal)
                {
                    return (decimal)value;
                }
                else if (value is int || value is short || value is long)
                {
                    return Convert.ToInt32(value);
                }
                else if (value is double || value is float)
                {
                    return Convert.ToDouble(value);
                }
                else if (value is DateTime)
                {
                    return (DateTime)value;
                }
                else
                {
                    return value.ToString();
                }
            }
            catch (Exception ex)
            {
                //System.Console.WriteLine($"[{context}] Error converting {fieldName} field: {ex.Message}");
                return null;
            }
        }

        // Métodos públicos principales
        public static async Task<ML.Result> GetOperadores(string mode)
        {
            ML.Result result = new ML.Result();
            try
            {
                string query = @"
                    SELECT * FROM oper_tda
                    WHERE cod_emp = 1
                    AND cod_pto = 870
                    ORDER BY nom_ope ASC
                ";

                using (var connection = new OdbcConnection(DL.Connection.GetConnectionStringGen(mode)))
                {
                    await connection.OpenAsync();

                    using (var command = new OdbcCommand(query, connection))
                    {
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            var results = new List<Dictionary<string, object>>();

                            while (await reader.ReadAsync())
                            {
                                var row = new Dictionary<string, object>();

                                for (int i = 0; i < reader.FieldCount; i++)
                                {
                                    row[reader.GetName(i)] = reader.GetValue(i);
                                }

                                results.Add(row);
                            }

                            if (results.Count == 0)
                            {
                                result.Correct = false;
                                result.Message = "Records not found.";
                                return result;
                            }

                            result.Correct = true;
                            result.Object = results;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                result.Correct = false;
                result.Message = $"Internal Server Error: {ex.Message}";
                result.Ex = ex;
            }
            return result;
        }

        public static async Task<ML.Result> GetUnidades(string mode)
        {
            ML.Result result = new ML.Result();
            try
            {
                string query = @"SELECT * FROM tra_pro
                                    WHERE cod_emp = 1
                                    AND cod_pto = 870
                                    ORDER BY num_eco ASC
                                ";

                using (var connection = new OdbcConnection(DL.Connection.GetConnectionStringGen(mode)))
                {
                    await connection.OpenAsync();

                    using (var command = new OdbcCommand(query, connection))
                    {
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            var results = new List<Dictionary<string, object>>();

                            while (await reader.ReadAsync())
                            {
                                var row = new Dictionary<string, object>();

                                for (int i = 0; i < reader.FieldCount; i++)
                                {
                                    row[reader.GetName(i)] = reader.GetValue(i);
                                }

                                results.Add(row);
                            }

                            if (results.Count == 0)
                            {
                                result.Correct = false;
                                result.Message = "Records not found.";
                                return result;
                            }

                            result.Correct = true;
                            result.Object = results;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                result.Correct = false;
                result.Message = $"Internal Server Error: {ex.Message}";
                result.Ex = ex;
            }
            return result;
        }

        // Métodos auxiliares de consulta
        public static async Task<Dictionary<string, object>?> GetOperadorInfo(string nombreOperador, string mode)
        {
            using (var connection = new OdbcConnection(DL.Connection.GetConnectionStringGen(mode)))
            {
                await connection.OpenAsync();

                string query = @"
                    SELECT rfc_ope, lic_ope 
                    FROM oper_tda
                    WHERE nom_ope = ? 
                    AND cod_emp = 1 
                    AND cod_pto = 870
                ";

                using (var command = new OdbcCommand(query, connection))
                {
                    command.Parameters.Add(new OdbcParameter("@nom_ope", nombreOperador));

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            var rfcOpe = GetSafeValue(reader, 0, "rfc_ope", "GetOperadorPorFolio")?.ToString()?.Trim();
                            var licOpe = GetSafeValue(reader, 1, "lic_ope", "GetOperadorPorFolio")?.ToString();

                            return new Dictionary<string, object>
                            {
                                ["rfc_ope"] = rfcOpe,
                                ["lic_ope"] = licOpe
                            };
                        }
                    }
                }
            }

            return null;
        }

        public static async Task<Dictionary<string, object>?> GetTransporteInfo(string unidad, string mode)
        {
            using (var connection = new OdbcConnection(DL.Connection.GetConnectionStringGen(mode)))
            {
                await connection.OpenAsync();

                string query = @"
                    SELECT per_sct, con_vei, pla_vei, mod_vei, num_per 
                    FROM tra_pro 
                    WHERE num_eco = ? 
                    AND cod_emp = 1 
                    AND cod_pto = 870
                ";

                using (var command = new OdbcCommand(query, connection))
                {
                    command.Parameters.Add(new OdbcParameter("@num_eco", unidad));

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            var perSct = GetSafeValue(reader, 0, "per_sct", "GetVehiculoPorFolio")?.ToString();
                            var conVei = GetSafeValue(reader, 1, "con_vei", "GetVehiculoPorFolio")?.ToString();
                            var plaVei = GetSafeValue(reader, 2, "pla_vei", "GetVehiculoPorFolio")?.ToString()?.Trim();
                            var modVei = GetSafeValue(reader, 3, "mod_vei", "GetVehiculoPorFolio")?.ToString();
                            var numPer = GetSafeValue(reader, 4, "num_per", "GetVehiculoPorFolio")?.ToString();

                            return new Dictionary<string, object>
                            {
                                ["per_sct"] = perSct,
                                ["con_vei"] = conVei,
                                ["pla_vei"] = plaVei,
                                ["mod_vei"] = modVei,
                                ["num_per"] = numPer
                            };
                        }
                    }
                }
            }

            return null;
        }

        public static async Task<Dictionary<string, object>?> GetEstado(string claveEstado, string mode)
        {
            using (var connection = new OdbcConnection(DL.Connection.GetConnectionStringGen(mode)))
            {
                await connection.OpenAsync();

                string query = @"
                    SELECT id
                    FROM cat_mun
                    WHERE cve_est = ?
                    LIMIT 1
                ";

                using (var command = new OdbcCommand(query, connection))
                {
                    command.Parameters.Add(new OdbcParameter("@cve_est", claveEstado));

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            var idEstado = GetSafeValue(reader, 0, "id", "GetEstado")?.ToString();

                            return new Dictionary<string, object>
                            {
                                ["id"] = idEstado
                            };
                        }
                    }
                }
            }

            return null;
        }

        public static async Task<Dictionary<string, object>?> GetCodigoColonia(string nomCol, string codPos, string mode)
        {
            using (var connection = new OdbcConnection(DL.Connection.GetConnectionStringGen(mode)))
            {
                await connection.OpenAsync();

                string query = @"
                    SELECT cve_col
                    FROM cat_cp
                    WHERE nom_col LIKE CONCAT(?, '%')
                    AND cod_postal = ?
                ";

                using (var command = new OdbcCommand(query, connection))
                {
                    command.Parameters.Add(new OdbcParameter("@nom_col", nomCol));
                    command.Parameters.Add(new OdbcParameter("@codPos", codPos));

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            var cveCol = GetSafeValue(reader, 0, "cve_col", "GetCodigoPostalPorColonia")?.ToString();
                            return new Dictionary<string, object>
                            {
                                ["cve_col"] = cveCol
                            };
                        }
                    }
                }
            }

            return null;
        }

        public static async Task<Dictionary<string, object>?> GetCodigosMunicipio(string descMunicipio, string claveEstado, string mode)
        {
            using (var connection = new OdbcConnection(DL.Connection.GetConnectionStringGen(mode)))
            {
                await connection.OpenAsync();

                string query = @"
                    SELECT cve_mun, cve_est 
                    FROM cat_mun
                    WHERE desc = ?
                    AND cve_est = ?
                ";

                using (var command = new OdbcCommand(query, connection))
                {
                    command.Parameters.Add(new OdbcParameter("@desc", descMunicipio));
                    command.Parameters.Add(new OdbcParameter("@cve_est", claveEstado));

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            var cveMun = GetSafeValue(reader, 0, "cve_mun", "GetClaveMunicipio")?.ToString();
                            var cveEst = GetSafeValue(reader, 1, "cve_est", "GetClaveMunicipio")?.ToString();

                            return new Dictionary<string, object>
                            {
                                ["cve_mun"] = cveMun,
                                ["cve_est"] = cveEst
                            };
                        }
                    }
                }
            }

            return null;
        }

        public static async Task<Dictionary<string, object>?> GetLocalidad(string claveEstado, string mode)
        {
            using (var connection = new OdbcConnection(DL.Connection.GetConnectionStringGen(mode)))
            {
                await connection.OpenAsync();

                string query = @"
                    SELECT cve_loc, cve_est
                    FROM cat_loc
                    WHERE cve_est = ?
                    LIMIT 1
                ";

                using (var command = new OdbcCommand(query, connection))
                {
                    command.Parameters.Add(new OdbcParameter("@cve_est", claveEstado));

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            var cveLoc = GetSafeValue(reader, 0, "cve_loc", "GetClaveLocalidad")?.ToString();
                            var cveEst = GetSafeValue(reader, 1, "cve_est", "GetClaveLocalidad")?.ToString();

                            return new Dictionary<string, object>
                            {
                                ["cve_loc"] = cveLoc,
                                ["cve_est"] = cveEst
                            };
                        }
                    }
                }
            }

            return null;
        }

        public static Task<double> CalcularDistancia(int entregaNum, Dictionary<string, object>? domicilioInfo)
        {
            if (entregaNum == 1)
            {
                return Task.FromResult(2.10);
            }
            else
            {
                return Task.FromResult(3.10);
            }
        }

        // Métodos de inserción
        public static async Task InsertarOrigenDestino(string idOri, string idDes, DateTime fecSal, DateTime fecLle,
            double distancia, string idUni, int codEmp, int entregaNum, string mode)
        {
            using (var connection = new OdbcConnection(DL.Connection.GetConnectionStringGen(mode)))
            {
                await connection.OpenAsync();

                string rfcRem = entregaNum == 1 ? "SOM101125UEA" : "XAXX010101000";
                string rfcDes = "XAXX010101000";

                string query = @"
                    INSERT INTO ubi_tim2 (rfc_rem, rfc_des, id_ori, id_des, fec_sal, fec_lle, dis_rec, id_uni, cod_emp)
                    VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?)
                ";

                using (var command = new OdbcCommand(query, connection))
                {
                    command.Parameters.Add(new OdbcParameter("@rfc_rem", rfcRem.PadRight(13).Substring(0, 13)));
                    command.Parameters.Add(new OdbcParameter("@rfc_des", rfcDes.PadRight(13).Substring(0, 13)));
                    command.Parameters.Add(new OdbcParameter("@id_ori", idOri.PadRight(8).Substring(0, 8)));
                    command.Parameters.Add(new OdbcParameter("@id_des", idDes.PadRight(8).Substring(0, 8)));
                    command.Parameters.Add(new OdbcParameter("@fec_sal", fecSal.ToString("yyyy-MM-dd HH:mm:ss").PadRight(19).Substring(0, 19)));
                    command.Parameters.Add(new OdbcParameter("@fec_lle", fecLle.ToString("yyyy-MM-dd HH:mm:ss").PadRight(19).Substring(0, 19)));
                    command.Parameters.Add(new OdbcParameter("@dis_rec", (decimal)distancia));
                    command.Parameters.Add(new OdbcParameter("@id_uni", idUni.PadRight(25).Substring(0, 25)));
                    command.Parameters.Add(new OdbcParameter("@cod_emp", (decimal)codEmp));

                    await command.ExecuteNonQueryAsync();
                }
            }
        }

        public static async Task InsertarMercancia(Dictionary<string, object> codigoFiscal, string idOri, string idDes, string idUni, int codEmp, string mode)
        {
            string idOriMercancia = "OR000870";
            string idDesMercancia = idDes;

            using (var connection = new OdbcConnection(DL.Connection.GetConnectionStringGen(mode)))
            {
                await connection.OpenAsync();

                string query = @"
                    INSERT INTO int_tim24 (bie_tra, des_tra, id_ori, id_des, num_pza, cla_uni, pes_pza, 
                        pedim, fra_aran, mat_peli, cve_peli, emba, des_emba, tip_docum, doc_aduan, 
                        rf_clmpo, cofepris, ingr_activo, quimico, deno_gene, deno_disti, fabrica, 
                        f_caduc, lote, farmac, esp_transp, reg_sanita, permi_imp, vucem, cas, 
                        rs_emp_imp, san_plag_cofe, d_fabric, d_formu, d_maquila, uso_autor, id_uni, cod_emp)
                    VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)
                ";

                using (var command = new OdbcCommand(query, connection))
                {
                    command.Parameters.Add(new OdbcParameter("@bie_tra", codigoFiscal["cve_codfis"].ToString().PadRight(15).Substring(0, 15)));
                    command.Parameters.Add(new OdbcParameter("@des_tra", codigoFiscal["des_codfis"].ToString().PadRight(150).Substring(0, 150)));
                    command.Parameters.Add(new OdbcParameter("@id_ori", idOriMercancia.PadRight(8).Substring(0, 8)));
                    command.Parameters.Add(new OdbcParameter("@id_des", idDesMercancia.PadRight(8).Substring(0, 8)));
                    command.Parameters.Add(new OdbcParameter("@num_pza", (decimal)1.000000));
                    command.Parameters.Add(new OdbcParameter("@cla_uni", "XKI".PadRight(3).Substring(0, 3)));
                    command.Parameters.Add(new OdbcParameter("@pes_pza", (decimal)codigoFiscal["pes_art"]));
                    command.Parameters.Add(new OdbcParameter("@pedim", " ".PadRight(21).Substring(0, 21)));
                    command.Parameters.Add(new OdbcParameter("@fra_aran", " ".PadRight(10).Substring(0, 10)));
                    command.Parameters.Add(new OdbcParameter("@mat_peli", " ".PadRight(2).Substring(0, 2)));
                    command.Parameters.Add(new OdbcParameter("@cve_peli", " ".PadRight(4).Substring(0, 4)));
                    command.Parameters.Add(new OdbcParameter("@emba", " ".PadRight(4).Substring(0, 4)));
                    command.Parameters.Add(new OdbcParameter("@des_emba", " ".PadRight(150).Substring(0, 150)));
                    command.Parameters.Add(new OdbcParameter("@tip_docum", " ".PadRight(2).Substring(0, 2)));
                    command.Parameters.Add(new OdbcParameter("@doc_aduan", " ".PadRight(150).Substring(0, 150)));
                    command.Parameters.Add(new OdbcParameter("@rf_clmpo", " ".PadRight(13).Substring(0, 13)));
                    command.Parameters.Add(new OdbcParameter("@cofepris", " ".PadRight(2).Substring(0, 2)));
                    command.Parameters.Add(new OdbcParameter("@ingr_activo", " ".PadRight(1000).Substring(0, 1000)));
                    command.Parameters.Add(new OdbcParameter("@quimico", " ".PadRight(150).Substring(0, 150)));
                    command.Parameters.Add(new OdbcParameter("@deno_gene", " ".PadRight(50).Substring(0, 50)));
                    command.Parameters.Add(new OdbcParameter("@deno_disti", " ".PadRight(50).Substring(0, 50)));
                    command.Parameters.Add(new OdbcParameter("@fabrica", " ".PadRight(240).Substring(0, 240)));
                    command.Parameters.Add(new OdbcParameter("@f_caduc", " ".PadRight(10).Substring(0, 10)));
                    command.Parameters.Add(new OdbcParameter("@lote", " ".PadRight(10).Substring(0, 10)));
                    command.Parameters.Add(new OdbcParameter("@farmac", " ".PadRight(2).Substring(0, 2)));
                    command.Parameters.Add(new OdbcParameter("@esp_transp", " ".PadRight(2).Substring(0, 2)));
                    command.Parameters.Add(new OdbcParameter("@reg_sanita", " ".PadRight(15).Substring(0, 15)));
                    command.Parameters.Add(new OdbcParameter("@permi_imp", " ".PadRight(6).Substring(0, 6)));
                    command.Parameters.Add(new OdbcParameter("@vucem", " ".PadRight(25).Substring(0, 25)));
                    command.Parameters.Add(new OdbcParameter("@cas", " ".PadRight(15).Substring(0, 15)));
                    command.Parameters.Add(new OdbcParameter("@rs_emp_imp", " ".PadRight(80).Substring(0, 80)));
                    command.Parameters.Add(new OdbcParameter("@san_plag_cofe", " ".PadRight(60).Substring(0, 60)));
                    command.Parameters.Add(new OdbcParameter("@d_fabric", " ".PadRight(600).Substring(0, 600)));
                    command.Parameters.Add(new OdbcParameter("@d_formu", " ".PadRight(600).Substring(0, 600)));
                    command.Parameters.Add(new OdbcParameter("@d_maquila", " ".PadRight(600).Substring(0, 600)));
                    command.Parameters.Add(new OdbcParameter("@uso_autor", " ".PadRight(1000).Substring(0, 1000)));
                    command.Parameters.Add(new OdbcParameter("@id_uni", idUni.PadRight(25).Substring(0, 25)));
                    command.Parameters.Add(new OdbcParameter("@cod_emp", (decimal)codEmp));

                    await command.ExecuteNonQueryAsync();
                }
            }
        }

        public static async Task InsertarTransporte(Dictionary<string, object> transporteInfo, string idUni, int codEmp, decimal pesoBruto, string mode)
        {
            using (var connection = new OdbcConnection(DL.Connection.GetConnectionStringGen(mode)))
            {
                await connection.OpenAsync();

                string query = @"
                    INSERT INTO trans_tim24 (per_sct, num_per, nom_ase, num_seg, con_veh, pla_veh, mod_veh,
                        tip_rem1, pla_rem1, tip_rem2, pla_rem2, aseg_carga, num_carga, aseg_med, num_med,
                        peso_bruto, prima_seg, id_uni, cod_emp)
                    VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)
                ";

                using (var command = new OdbcCommand(query, connection))
                {
                    command.Parameters.Add(new OdbcParameter("@per_sct", transporteInfo["per_sct"].ToString().PadRight(40).Substring(0, 40)));
                    command.Parameters.Add(new OdbcParameter("@num_per", transporteInfo["num_per"].ToString().PadRight(20).Substring(0, 20)));
                    command.Parameters.Add(new OdbcParameter("@nom_ase", "SEGUROS INBURSA S.A. GRUPO FIN".PadRight(30).Substring(0, 30)));
                    command.Parameters.Add(new OdbcParameter("@num_seg", "2610020000000".PadRight(20).Substring(0, 20)));
                    command.Parameters.Add(new OdbcParameter("@con_veh", transporteInfo["con_vei"].ToString().PadRight(20).Substring(0, 20)));
                    command.Parameters.Add(new OdbcParameter("@pla_veh", transporteInfo["pla_vei"].ToString().PadRight(10).Substring(0, 10)));
                    command.Parameters.Add(new OdbcParameter("@mod_veh", transporteInfo["mod_vei"].ToString().PadRight(4).Substring(0, 4)));
                    command.Parameters.Add(new OdbcParameter("@tip_rem1", " ".PadRight(6).Substring(0, 6)));
                    command.Parameters.Add(new OdbcParameter("@pla_rem1", " ".PadRight(10).Substring(0, 10)));
                    command.Parameters.Add(new OdbcParameter("@tip_rem2", " ".PadRight(6).Substring(0, 6)));
                    command.Parameters.Add(new OdbcParameter("@pla_rem2", " ".PadRight(10).Substring(0, 10)));
                    command.Parameters.Add(new OdbcParameter("@aseg_carga", "SEGUROS INBURSA S.A. GRUPO FINANCIERO INBURSA".PadRight(50).Substring(0, 50)));
                    command.Parameters.Add(new OdbcParameter("@num_carga", "2610020000000".PadRight(30).Substring(0, 30)));
                    command.Parameters.Add(new OdbcParameter("@aseg_med", " ".PadRight(50).Substring(0, 50)));
                    command.Parameters.Add(new OdbcParameter("@num_med", " ".PadRight(30).Substring(0, 30)));
                    command.Parameters.Add(new OdbcParameter("@peso_bruto", pesoBruto));
                    command.Parameters.Add(new OdbcParameter("@prima_seg", " "));
                    command.Parameters.Add(new OdbcParameter("@id_uni", idUni.PadRight(25).Substring(0, 25)));
                    command.Parameters.Add(new OdbcParameter("@cod_emp", (decimal)codEmp));

                    await command.ExecuteNonQueryAsync();
                }
            }
        }

        public static async Task InsertarOperador(Dictionary<string, object> operadorInfo, string nombreOperador, string idUni, int codEmp, string mode)
        {
            using (var connection = new OdbcConnection(DL.Connection.GetConnectionStringGen(mode)))
            {
                await connection.OpenAsync();

                string query = @"
                    INSERT INTO oper_tim2 (rfc_ope, num_lic, nom_ope, tip_fig, part_trans, id_uni, cod_emp)
                    VALUES (?, ?, ?, ?, ?, ?, ?)
                ";

                using (var command = new OdbcCommand(query, connection))
                {
                    command.Parameters.Add(new OdbcParameter("@rfc_ope", operadorInfo["rfc_ope"].ToString().PadRight(13).Substring(0, 13)));
                    command.Parameters.Add(new OdbcParameter("@num_lic", operadorInfo["lic_ope"].ToString().PadRight(20).Substring(0, 20)));
                    command.Parameters.Add(new OdbcParameter("@nom_ope", nombreOperador.PadRight(30).Substring(0, 30)));
                    command.Parameters.Add(new OdbcParameter("@tip_fig", "01".PadRight(2).Substring(0, 2)));
                    command.Parameters.Add(new OdbcParameter("@part_trans", "PT02".PadRight(4).Substring(0, 4)));
                    command.Parameters.Add(new OdbcParameter("@id_uni", idUni.PadRight(25).Substring(0, 25)));
                    command.Parameters.Add(new OdbcParameter("@cod_emp", (decimal)codEmp));

                    await command.ExecuteNonQueryAsync();
                }
            }
        }

        public static async Task InsertarDomicilioOrigen(string idOri, string idUni, int codEmp, int entregaNum, string mode)
        {
            using (var connection = new OdbcConnection(DL.Connection.GetConnectionStringGen(mode)))
            {
                await connection.OpenAsync();

                if (entregaNum == 1)
                {
                    string query = @"
                        INSERT INTO dom_tim2 (des_ori, calle, num_ext, num_int, col, loca, ref, muni, est, pais, cod_pos, id_uni, cod_emp)
                        VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)
                    ";

                    using (var command = new OdbcCommand(query, connection))
                    {
                        command.Parameters.Add(new OdbcParameter("@des_ori", idOri.PadRight(8).Substring(0, 8)));
                        command.Parameters.Add(new OdbcParameter("@calle", "NORTE 45".PadRight(150).Substring(0, 150)));
                        command.Parameters.Add(new OdbcParameter("@num_ext", "1014".PadRight(50).Substring(0, 50)));
                        command.Parameters.Add(new OdbcParameter("@num_int", "0".PadRight(50).Substring(0, 50)));
                        command.Parameters.Add(new OdbcParameter("@col", "0402".PadRight(5).Substring(0, 5)));
                        command.Parameters.Add(new OdbcParameter("@loca", "02".PadRight(2).Substring(0, 2)));
                        command.Parameters.Add(new OdbcParameter("@ref", " ".PadRight(150).Substring(0, 150)));
                        command.Parameters.Add(new OdbcParameter("@muni", "002".PadRight(3).Substring(0, 3)));
                        command.Parameters.Add(new OdbcParameter("@est", "CMX".PadRight(3).Substring(0, 3)));
                        command.Parameters.Add(new OdbcParameter("@pais", "MEX".PadRight(3).Substring(0, 3)));
                        command.Parameters.Add(new OdbcParameter("@cod_pos", "02300".PadRight(5).Substring(0, 5)));
                        command.Parameters.Add(new OdbcParameter("@id_uni", idUni.PadRight(25).Substring(0, 25)));
                        command.Parameters.Add(new OdbcParameter("@cod_emp", (decimal)codEmp));

                        await command.ExecuteNonQueryAsync();
                    }
                }
                else
                {
                    string idDestinoAnterior = $"DE00000{entregaNum - 1}";

                    string selectQuery = @"
                        SELECT calle, num_ext, num_int, col, loca, ref, muni, est, pais, cod_pos
                        FROM dom_tim2 
                        WHERE des_ori = ? AND id_uni = ? AND cod_emp = ?
                    ";

                    using (var selectCommand = new OdbcCommand(selectQuery, connection))
                    {
                        selectCommand.Parameters.Add(new OdbcParameter("@des_ori_anterior", idDestinoAnterior.PadRight(8).Substring(0, 8)));
                        selectCommand.Parameters.Add(new OdbcParameter("@id_uni", idUni.PadRight(25).Substring(0, 25)));
                        selectCommand.Parameters.Add(new OdbcParameter("@cod_emp", (decimal)codEmp));

                        using (var reader = await selectCommand.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                string insertQuery = @"
                                    INSERT INTO dom_tim2 (des_ori, calle, num_ext, num_int, col, loca, ref, muni, est, pais, cod_pos, id_uni, cod_emp)
                                    VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)
                                ";

                                using (var insertCommand = new OdbcCommand(insertQuery, connection))
                                {
                                    var calle = GetSafeValue(reader, 0, "calle", "InsertDomicilios")?.ToString();
                                    var numExt = GetSafeValue(reader, 1, "num_ext", "InsertDomicilios")?.ToString();
                                    var numInt = GetSafeValue(reader, 2, "num_int", "InsertDomicilios")?.ToString();
                                    var col = GetSafeValue(reader, 3, "col", "InsertDomicilios")?.ToString();
                                    var loca = GetSafeValue(reader, 4, "loca", "InsertDomicilios")?.ToString();
                                    var refe = GetSafeValue(reader, 5, "ref", "InsertDomicilios")?.ToString();
                                    var muni = GetSafeValue(reader, 6, "muni", "InsertDomicilios")?.ToString();
                                    var est = GetSafeValue(reader, 7, "est", "InsertDomicilios")?.ToString();
                                    var pais = GetSafeValue(reader, 8, "pais", "InsertDomicilios")?.ToString();
                                    var codPos = GetSafeValue(reader, 9, "cod_pos", "InsertDomicilios")?.ToString();

                                    insertCommand.Parameters.Add(new OdbcParameter("@des_ori", idOri.PadRight(8).Substring(0, 8)));
                                    insertCommand.Parameters.Add(new OdbcParameter("@calle", (calle ?? " ").PadRight(150).Substring(0, 150)));
                                    insertCommand.Parameters.Add(new OdbcParameter("@num_ext", (numExt ?? " ").PadRight(50).Substring(0, 50)));
                                    insertCommand.Parameters.Add(new OdbcParameter("@num_int", (numInt ?? " ").PadRight(50).Substring(0, 50)));
                                    insertCommand.Parameters.Add(new OdbcParameter("@col", (col ?? " ").PadRight(5).Substring(0, 5)));
                                    insertCommand.Parameters.Add(new OdbcParameter("@loca", (loca ?? " ").PadRight(2).Substring(0, 2)));
                                    insertCommand.Parameters.Add(new OdbcParameter("@ref", (refe ?? " ").PadRight(150).Substring(0, 150)));
                                    insertCommand.Parameters.Add(new OdbcParameter("@muni", (muni ?? " ").PadRight(3).Substring(0, 3)));
                                    insertCommand.Parameters.Add(new OdbcParameter("@est", (est ?? " ").PadRight(3).Substring(0, 3)));
                                    insertCommand.Parameters.Add(new OdbcParameter("@pais", (pais ?? " ").PadRight(3).Substring(0, 3)));
                                    insertCommand.Parameters.Add(new OdbcParameter("@cod_pos", (codPos ?? " ").PadRight(5).Substring(0, 5)));
                                    insertCommand.Parameters.Add(new OdbcParameter("@id_uni", idUni.PadRight(25).Substring(0, 25)));
                                    insertCommand.Parameters.Add(new OdbcParameter("@cod_emp", (decimal)codEmp));

                                    await insertCommand.ExecuteNonQueryAsync();
                                }
                            }
                        }
                    }
                }
            }
        }

        // Métodos de verificación de duplicados
        public static async Task<List<string>> VerificarDuplicadosUbi(string idUni, int codEmp, int totalEntregas, string mode)
        {
            var duplicados = new List<string>();

            using (var connection = new OdbcConnection(DL.Connection.GetConnectionStringGen(mode)))
            {
                await connection.OpenAsync();

                for (int i = 0; i < totalEntregas; i++)
                {
                    var entregaNum = i + 1;
                    var idOri = entregaNum == 1 ? "OR000870" : $"OR00000{entregaNum - 1}";
                    var idDes = $"DE00000{entregaNum}";

                    string checkQuery = @"
                        SELECT COUNT(*) FROM ubi_tim2 
                        WHERE cod_emp = ? AND id_uni = ? AND id_ori = ? AND id_des = ?
                    ";

                    using (var checkCommand = new OdbcCommand(checkQuery, connection))
                    {
                        checkCommand.Parameters.Add(new OdbcParameter("@cod_emp", (decimal)codEmp));
                        checkCommand.Parameters.Add(new OdbcParameter("@id_uni", idUni.PadRight(25).Substring(0, 25)));
                        checkCommand.Parameters.Add(new OdbcParameter("@id_ori", idOri.PadRight(8).Substring(0, 8)));
                        checkCommand.Parameters.Add(new OdbcParameter("@id_des", idDes.PadRight(8).Substring(0, 8)));

                        var count = Convert.ToInt32(await checkCommand.ExecuteScalarAsync());

                        if (count > 0)
                        {
                            duplicados.Add($"Entrega {entregaNum}: {idOri}-{idDes}");
                        }
                    }
                }
            }

            return duplicados;
        }

        public static async Task<bool> VerificarDuplicadosInt(string idUni, int codEmp, string mode)
        {
            using (var connection = new OdbcConnection(DL.Connection.GetConnectionStringGen(mode)))
            {
                await connection.OpenAsync();

                string checkQuery = @"
                    SELECT COUNT(*) FROM int_tim24 
                    WHERE cod_emp = ? AND id_uni = ?
                ";

                using (var checkCommand = new OdbcCommand(checkQuery, connection))
                {
                    checkCommand.Parameters.Add(new OdbcParameter("@cod_emp", (decimal)codEmp));
                    checkCommand.Parameters.Add(new OdbcParameter("@id_uni", idUni.PadRight(25).Substring(0, 25)));

                    var count = Convert.ToInt32(await checkCommand.ExecuteScalarAsync());
                    return count > 0;
                }
            }
        }

        public static async Task<bool> VerificarDuplicadosTrans(string idUni, int codEmp, string mode)
        {
            using (var connection = new OdbcConnection(DL.Connection.GetConnectionStringGen(mode)))
            {
                await connection.OpenAsync();

                string checkQuery = @"
                    SELECT COUNT(*) FROM trans_tim24 
                    WHERE cod_emp = ? AND id_uni = ?
                ";

                using (var checkCommand = new OdbcCommand(checkQuery, connection))
                {
                    checkCommand.Parameters.Add(new OdbcParameter("@cod_emp", (decimal)codEmp));
                    checkCommand.Parameters.Add(new OdbcParameter("@id_uni", idUni.PadRight(25).Substring(0, 25)));

                    var count = Convert.ToInt32(await checkCommand.ExecuteScalarAsync());
                    return count > 0;
                }
            }
        }

        public static async Task<bool> VerificarDuplicadosOper(string idUni, int codEmp, string mode)
        {
            using (var connection = new OdbcConnection(DL.Connection.GetConnectionStringGen(mode)))
            {
                await connection.OpenAsync();

                string checkQuery = @"
                    SELECT COUNT(*) FROM oper_tim2 
                    WHERE cod_emp = ? AND id_uni = ?
                ";

                using (var checkCommand = new OdbcCommand(checkQuery, connection))
                {
                    checkCommand.Parameters.Add(new OdbcParameter("@cod_emp", (decimal)codEmp));
                    checkCommand.Parameters.Add(new OdbcParameter("@id_uni", idUni.PadRight(25).Substring(0, 25)));

                    var count = Convert.ToInt32(await checkCommand.ExecuteScalarAsync());
                    return count > 0;
                }
            }
        }

        public static async Task<List<string>> VerificarDuplicadosDom(string idUni, int codEmp, int totalEntregas, string mode)
        {
            var duplicados = new List<string>();

            using (var connection = new OdbcConnection(DL.Connection.GetConnectionStringGen(mode)))
            {
                await connection.OpenAsync();

                for (int i = 0; i < totalEntregas; i++)
                {
                    var entregaNum = i + 1;
                    var idDes = $"DE00000{entregaNum}";

                    string checkQuery = @"
                        SELECT COUNT(*) FROM dom_tim2 
                        WHERE cod_emp = ? AND id_uni = ? AND des_ori = ?
                    ";

                    using (var checkCommand = new OdbcCommand(checkQuery, connection))
                    {
                        checkCommand.Parameters.Add(new OdbcParameter("@cod_emp", (decimal)codEmp));
                        checkCommand.Parameters.Add(new OdbcParameter("@id_uni", idUni.PadRight(25).Substring(0, 25)));
                        checkCommand.Parameters.Add(new OdbcParameter("@des_ori", idDes.PadRight(8).Substring(0, 8)));

                        var count = Convert.ToInt32(await checkCommand.ExecuteScalarAsync());

                        if (count > 0)
                        {
                            duplicados.Add($"Entrega {entregaNum}: {idDes}");
                        }
                    }
                }
            }

            return duplicados;
        }

        // Métodos para generar CSVs y enviar emails
        public static async Task GenerarCSVsDespuesInserts(ML.CartaPorte.EnviarCartaRequest request, string idUni, int codEmp, string mode)
        {
            try
            {
                var csvIntTim24 = await GenerarCSVIntTim24(idUni, codEmp, mode);
                var csvDomTim2 = await GenerarCSVDomTim2(idUni, codEmp, mode);

                await EnviarEmailConCSVs(csvIntTim24, csvDomTim2, request.Folio, request.Operador, mode);
                await EliminarDatosTablas(idUni, codEmp, mode);
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public static async Task<string> GenerarCSVIntTim24(string idUni, int codEmp, string mode)
        {
            var csvData = new List<Dictionary<string, object>>();

            using (var connection = new OdbcConnection(DL.Connection.GetConnectionStringGen(mode)))
            {
                await connection.OpenAsync();

                string query = @"
                    SELECT bie_tra, des_tra, id_ori, id_des, num_pza, cla_uni, pes_pza, 
                           id_uni, cod_emp
                    FROM int_tim24 
                    WHERE id_uni = ? AND cod_emp = ?
                    ORDER BY id_des
                ";

                using (var command = new OdbcCommand(query, connection))
                {
                    command.Parameters.Add(new OdbcParameter("@id_uni", idUni));
                    command.Parameters.Add(new OdbcParameter("@cod_emp", codEmp));

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            var row = new Dictionary<string, object>();
                            for (int i = 0; i < reader.FieldCount; i++)
                            {
                                var fieldName = reader.GetName(i);
                                var value = reader.IsDBNull(i) ? null : reader.GetValue(i);
                                row[fieldName] = value;
                            }
                            csvData.Add(row);
                        }
                    }
                }
            }

            return GenerarCSV(csvData);
        }

        public static async Task<string> GenerarCSVDomTim2(string idUni, int codEmp, string mode)
        {
            var csvData = new List<Dictionary<string, object>>();

            using (var connection = new OdbcConnection(DL.Connection.GetConnectionStringGen(mode)))
            {
                await connection.OpenAsync();

                string query = @"
                    SELECT des_ori, calle, num_ext, num_int, col, loca, ref, muni, est, 
                           pais, cod_pos, id_uni, cod_emp
                    FROM dom_tim2 
                    WHERE id_uni = ? AND cod_emp = ?
                    ORDER BY des_ori
                ";

                using (var command = new OdbcCommand(query, connection))
                {
                    command.Parameters.Add(new OdbcParameter("@id_uni", idUni));
                    command.Parameters.Add(new OdbcParameter("@cod_emp", codEmp));

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            var row = new Dictionary<string, object>();
                            for (int i = 0; i < reader.FieldCount; i++)
                            {
                                var fieldName = reader.GetName(i);
                                var value = reader.IsDBNull(i) ? null : reader.GetValue(i);
                                row[fieldName] = value;
                            }
                            csvData.Add(row);
                        }
                    }
                }
            }

            return GenerarCSV(csvData);
        }

        public static async Task EliminarDatosTablas(string idUni, int codEmp, string mode)
        {
            using (var connection = new OdbcConnection(DL.Connection.GetConnectionStringGen(mode)))
            {
                await connection.OpenAsync();

                var tablas = new[]
                {
                    "int_tim24",
                    "dom_tim2",
                    "ubi_tim2",
                    "trans_tim24",
                    "oper_tim2"
                };

                foreach (var tabla in tablas)
                {
                    try
                    {
                        string deleteQuery = $"DELETE FROM {tabla} WHERE id_uni = ? AND cod_emp = ?";

                        using (var command = new OdbcCommand(deleteQuery, connection))
                        {
                            command.Parameters.Add(new OdbcParameter("@id_uni", idUni));
                            command.Parameters.Add(new OdbcParameter("@cod_emp", codEmp));

                            await command.ExecuteNonQueryAsync();
                        }
                    }
                    catch (Exception ex)
                    {
                        // Log error if needed
                    }
                }
            }
        }

        public static async Task EnviarEmailConCSVs(string csvIntTim24, string csvDomTim2, string folio, string operador, string mode)
        {
            var emailConfig = ObtenerEmailConfiguration();
            await EmailHelper.EnviarEmailConCSVs(csvIntTim24, csvDomTim2, folio, operador, emailConfig);
        }

        public static EmailConfiguration ObtenerEmailConfiguration()
        {
            var currentDir = Directory.GetCurrentDirectory();
            var appsettingsPath = Path.Combine(currentDir, "appsettings.json");

            // Si no está en el directorio actual, buscar en el directorio padre (PL)
            if (!File.Exists(appsettingsPath))
            {
                var parentDir = Directory.GetParent(currentDir)?.FullName;
                if (parentDir != null)
                {
                    appsettingsPath = Path.Combine(parentDir, "PL", "appsettings.json");
                }
            }

            var builder = new ConfigurationBuilder()
                .SetBasePath(Path.GetDirectoryName(appsettingsPath) ?? currentDir)
                .AddJsonFile(Path.GetFileName(appsettingsPath), optional: false, reloadOnChange: true);

            var configuration = builder.Build();
            var emailConfig = new EmailConfiguration();

            configuration.GetSection("EmailConfiguration").Bind(emailConfig);

            return emailConfig;
        }

        public static string GenerarCSV(List<Dictionary<string, object>> data)
        {
            if (data.Count == 0)
            {
                return "";
            }

            var csv = new System.Text.StringBuilder();

            var headers = data[0].Keys.ToList();
            csv.AppendLine(string.Join(",", headers.Select(h => $"\"{h}\"")));

            foreach (var row in data)
            {
                var values = headers.Select(h =>
                {
                    var value = row.ContainsKey(h) ? row[h]?.ToString()?.Replace("\"", "\"\"") ?? "" : "";
                    return $"\"{value}\"";
                });
                csv.AppendLine(string.Join(",", values));
            }

            return csv.ToString();
        }

        public static async Task InsertarOraRuteoCartaPorte(string idUni, string carSal, int estatus, string rfcOpe, string numEco, int? usuCon, DateTime fecCon, int codEmp, string mode)
        {
            using (var connection = new OdbcConnection(DL.Connection.GetConnectionStringGen("DEV")))
            {
                await connection.OpenAsync();

                string query = @"
                    INSERT INTO ora_ruteo_carta_porte (cod_emp, id_uni, car_sal, estatus, rfc_ope, num_eco, usu_con, fec_con)
                    VALUES (?, ?, ?, ?, ?, ?, ?, ?)
                ";

                using (var command = new OdbcCommand(query, connection))
                {
                    command.Parameters.Add(new OdbcParameter("@cod_emp", (decimal)codEmp));
                    command.Parameters.Add(new OdbcParameter("@id_uni", idUni.PadRight(30).Substring(0, 30)));
                    command.Parameters.Add(new OdbcParameter("@car_sal", carSal.PadRight(50).Substring(0, 50)));
                    command.Parameters.Add(new OdbcParameter("@estatus", (short)estatus));
                    command.Parameters.Add(new OdbcParameter("@rfc_ope", (rfcOpe ?? " ").PadRight(15).Substring(0, 15)));
                    command.Parameters.Add(new OdbcParameter("@num_eco", (numEco ?? " ").PadRight(20).Substring(0, 20)));
                    command.Parameters.Add(new OdbcParameter("@usu_con", usuCon.HasValue ? (decimal)usuCon.Value : DBNull.Value));
                    command.Parameters.Add(new OdbcParameter("@fec_con", fecCon.ToString("yyyy-MM-dd HH:mm")));

                    await command.ExecuteNonQueryAsync();
                }
            }
        }

    }
}
