using System;
using System.Collections.Generic;
using System.Data.Odbc;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BL.CartaPorte
{
    public class CartaPorte
    {
        private static object GetSafeValue(System.Data.Common.DbDataReader reader, int index, string fieldName, string context = " ", string scn = " ")
        {
            if (reader.IsDBNull(index))
            {
                var scnInfo = !string.IsNullOrEmpty(scn) ? $" (SCN: {scn})" : " ";
                System.Console.WriteLine($"[{context}] NULL value: {fieldName} field{scnInfo}");
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
                System.Console.WriteLine($"[{context}] Error converting {fieldName} field: {ex.Message}");
                return null;
            }
        }

        // Métodos principales del controlador
        public static async Task<ML.Result> GetScnByCono(string precon, string mode)
        {
            ML.Result result = new ML.Result();
            try
            {
                var scns = await GetScnsByConoInternal(precon, mode);
                var resultadoEmbarque = await ValidarEstadoEmbarque(precon, mode);

                if (resultadoEmbarque != 3)
                {
                    if (resultadoEmbarque == -1)
                    {
                        result.Correct = false;
                        result.Message = "No se encontro folio de conocimiento";
                        return result;
                    }
                    result.Correct = false;
                    result.Message = "Folio y conocimiento de embarque no cerrados";
                    return result;
                }

                var results = scns.Select(scn => new Dictionary<string, object>
                {
                    ["sales_check"] = scn
                }).ToList();

                result.Correct = true;
                result.Object = results;
            }
            catch (Exception ex)
            {
                result.Correct = false;
                result.Message = $"Error: {ex.Message}";
                result.Ex = ex;
            }
            return result;
        }

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
                string query = @"
                    SELECT * FROM tra_pro
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

        // Métodos privados auxiliares
        private static async Task<List<string>> GetScnsByConoInternal(string precon, string mode)
        {
            var scns = new List<string>();

            using (var connection = new OdbcConnection(DL.Connection.GetConnectionStringLga(mode)))
            {
                await connection.OpenAsync();

                string query = @"
                    SELECT DISTINCT lgahventa.sales_check
                    FROM lgahventa
                    JOIN lgadventa ON lgahventa.cod_empresa = lgadventa.cod_empresa
                        AND lgahventa.cd_id = lgadventa.cd_id
                        AND lgahventa.sales_check = lgadventa.sales_check
                    JOIN lgaetiqeta ON lgahventa.cod_empresa = lgaetiqeta.cod_empresa
                        AND lgahventa.cd_id = lgaetiqeta.cd_id
                        AND lgadventa.no_etiqueta = lgaetiqeta.no_etiqueta
                    WHERE lgahventa.cod_empresa = 1
                      AND lgahventa.cd_id = 870
                      AND lgahventa.tip_entrega = 1
                      AND lgaetiqeta.no_conoc = ?
                ";

                using (var command = new OdbcCommand(query, connection))
                {
                    command.Parameters.Add(new OdbcParameter("@no_precon", precon));

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            var salesCheck = GetSafeValue(reader, 0, "sales_check", "GetScnsByFolio");
                            scns.Add(salesCheck?.ToString());
                        }
                    }
                }
            }

            return scns;
        }

        private static async Task<int> ValidarEstadoEmbarque(string folio, string mode)
        {
            using (var connection = new OdbcConnection(DL.Connection.GetConnectionStringLga(mode)))
            {
                await connection.OpenAsync();

                string query = @"
                    SELECT L.st_embarque
                      FROM lgahembrqe L, lgadembrqe U
                     WHERE L.cod_empresa = U.cod_empresa
                       AND L.cd_id = U.cd_id
                       AND L.folio_embarque = U.folio_embarque
                       AND L.cod_empresa = 1
                       AND L.cd_id = 870
                       AND U.no_conoc = ?
                ";

                using (var command = new OdbcCommand(query, connection))
                {
                    command.Parameters.Add(new OdbcParameter("@no_conoc", folio));

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            var estado = GetSafeValue(reader, 0, "st_embarque", "ValidarEstadoEmbarque");

                            if (estado == null)
                            {
                                return -1;
                            }

                            var estadoInt = Convert.ToInt32(estado);
                            return estadoInt;
                        }
                    }
                }
            }

            return -1;
        }

        // Método principal EnviarCarta
        public static async Task<ML.Result> EnviarCarta(ML.CartaPorte.EnviarCartaRequest request, string mode)
        {
            ML.Result result = new ML.Result();
            try
            {
                System.Console.WriteLine($"\n[ENVIAR CARTA] Iniciando proceso para folio: {request?.Folio}, FechaSalida: {request.FechaSalida}");

                if (request == null || string.IsNullOrEmpty(request.Folio))
                {
                    result.Correct = false;
                    result.Message = "Request inválido o folio faltante";
                    return result;
                }

                var idUni = $"SEAOIC{request.Folio}";
                var codEmp = 1;

                // Verificar duplicados
                var skusOrderedByParada = await GetSkusByFolioOrderedByParada(request.Folio, mode);
                if (skusOrderedByParada == null || skusOrderedByParada.Count == 0)
                {
                    result.Correct = false;
                    result.Message = $"No se encontraron SKUs para el folio {request.Folio}";
                    return result;
                }

                var skusByParada = skusOrderedByParada.GroupBy(s => s["no_parada"]).OrderBy(g => g.Key).ToList();
                var totalEntregas = skusByParada.Count;

                // Verificar duplicados
                var duplicadosUbi = await VerificarDuplicadosUbi(idUni, codEmp, totalEntregas, mode);
                if (duplicadosUbi.Count > 0)
                {
                    result.Correct = false;
                    result.Message = $"ID ya generado: {idUni}";
                    return result;
                }

                var duplicadosInt = await VerificarDuplicadosInt(idUni, codEmp, mode);
                if (duplicadosInt)
                {
                    result.Correct = false;
                    result.Message = $"ID ya generado: {idUni}";
                    return result;
                }

                var duplicadosTrans = await VerificarDuplicadosTrans(idUni, codEmp, mode);
                if (duplicadosTrans)
                {
                    result.Correct = false;
                    result.Message = $"ID ya generado: {idUni}";
                    return result;
                }

                var duplicadosOper = await VerificarDuplicadosOper(idUni, codEmp, mode);
                if (duplicadosOper)
                {
                    result.Correct = false;
                    result.Message = $"ID ya generado: {idUni}";
                    return result;
                }

                var duplicadosDom = await VerificarDuplicadosDom(idUni, codEmp, totalEntregas, mode);
                if (duplicadosDom.Count > 0)
                {
                    result.Correct = false;
                    result.Message = $"ID ya generado: {idUni}";
                    return result;
                }

                // Obtener información del operador
                var operadorInfo = await GetOperadorInfo(request.Operador, mode);
                if (operadorInfo == null)
                {
                    result.Correct = false;
                    result.Message = $"No existe información del operador {request.Operador}";
                    return result;
                }

                // Obtener información del transporte
                var transporteInfo = await GetTransporteInfo(request.Unidad, mode);
                if (transporteInfo == null)
                {
                    result.Correct = false;
                    result.Message = $"No existe información del transporte {request.Unidad}";
                    return result;
                }

                // Obtener domicilios
                var domicilios = await GetDomiciliosPorFolio(request.Folio, mode);
                if (domicilios == null || domicilios.Count == 0)
                {
                    result.Correct = false;
                    result.Message = $"No se encontraron domicilios para el folio {request.Folio}";
                    return result;
                }

                // Validar domicilios
                var validationResult = await ValidarDomicilios(domicilios, mode);
                if (!validationResult.Correct)
                {
                    return validationResult;
                }

                // Procesar entregas
                var skusInfoList = new List<List<Dictionary<string, object>>>();
                var codigosFiscalesList = new List<List<Dictionary<string, object>>>();

                for (int i = 0; i < skusByParada.Count; i++)
                {
                    var paradaGroup = skusByParada[i];
                    var skus = paradaGroup.ToList();
                    var codigosFiscales = new List<Dictionary<string, object>>();

                    foreach (var skuInfo in skus)
                    {
                        var sku = skuInfo["sku"]?.ToString();
                        if (string.IsNullOrEmpty(sku))
                        {
                            result.Correct = false;
                            result.Message = $"SKU nulo o vacío en parada {paradaGroup.Key}";
                            return result;
                        }

                        var codigoFiscal = await GetCodigosFiscalesPorSku(sku, mode);
                        if (codigoFiscal == null && !sku.StartsWith("999"))
                        {
                            result.Correct = false;
                            result.Message = $"No se encontraron códigos fiscales para SKU {sku}";
                            return result;
                        }

                        if (codigoFiscal != null)
                        {
                            codigosFiscales.Add(codigoFiscal);
                        }
                    }

                    skusInfoList.Add(skus);
                    codigosFiscalesList.Add(codigosFiscales);
                }

                // Procesar cada entrega
                for (int i = 0; i < skusByParada.Count; i++)
                {
                    var paradaGroup = skusByParada[i];
                    var entregaNum = i + 1;
                    var codigosFiscales = codigosFiscalesList[i];
                    var skus = skusInfoList[i];
                    var salesChecks = skus.Select(s => s["sales_check"].ToString()).Distinct().ToList();

                    var idOri = entregaNum == 1 ? "OR000870" : $"OR00000{entregaNum - 1}";
                    var idDes = $"DE00000{entregaNum}";

                    var domiciliosScn = domicilios.Where(d => salesChecks.Contains(d["sales_check"].ToString())).ToList();

                    DateTime fecSal = entregaNum == 1 ? request.FechaSalida : request.FechaSalida.AddHours(entregaNum - 1).AddMinutes(10 * (entregaNum - 1));
                    var fecLle = fecSal.AddHours(1);
                    var distancia = await CalcularDistancia(entregaNum, domiciliosScn[0]);

                    await InsertarOrigenDestino(idOri, idDes, fecSal, fecLle, distancia, idUni, codEmp, entregaNum, mode);

                    foreach (var codigoFiscal in codigosFiscales)
                    {
                        await InsertarMercancia(codigoFiscal, idOri, idDes, idUni, codEmp, mode);
                    }

                    await InsertarDomicilioOrigen(idOri, idUni, codEmp, entregaNum, mode);

                    var domiciliosUnicos = AgruparDomiciliosUnicos(domiciliosScn);
                    foreach (var domicilioUnico in domiciliosUnicos)
                    {
                        await InsertarDomicilio(idDes, domicilioUnico, idUni, codEmp, mode);
                    }
                }

                // Insertar transporte
                decimal pesoBrutoTotal = 0;
                foreach (var codigosFiscales in codigosFiscalesList)
                {
                    foreach (var codigoFiscal in codigosFiscales)
                    {
                        pesoBrutoTotal += (decimal)codigoFiscal["pes_art"];
                    }
                }

                await InsertarTransporte(transporteInfo, idUni, codEmp, pesoBrutoTotal, mode);
                await InsertarOperador(operadorInfo, request.Operador, idUni, codEmp, mode);

                // Verificar si es operador específico
                var operadoresEspecificos = new[] { "Transportes MyM", "Paqueteria Castores" };
                bool esOperadorEspecifico = operadoresEspecificos.Contains(request.Operador);

                if (esOperadorEspecifico)
                {
                    await GenerarCSVsDespuesInserts(request, idUni, codEmp, mode);
                    result.Correct = true;
                    result.Object = new { message = "Folio enviado a " + request.Operador, entregas = skusByParada.Count };
                    return result;
                }

                result.Correct = true;
                result.Object = new { message = "ID generado: " + idUni, entregas = skusByParada.Count };
                return result;
            }
            catch (Exception ex)
            {
                result.Correct = false;
                result.Message = $"Error interno del servidor: {ex.Message}";
                result.Ex = ex;
                System.Console.WriteLine($"[ENVIAR CARTA] Error: {ex.Message}");
                System.Console.WriteLine($"[ENVIAR CARTA] Stack trace: {ex.StackTrace}");
                return result;
            }
        }

        // Métodos auxiliares privados
        private static async Task<ML.Result> ValidarDomicilios(List<Dictionary<string, object>> domicilios, string mode)
        {
            ML.Result result = new ML.Result();
            var notFoundEstado = new List<string>();
            var notFoundMunicipio = new List<string>();
            var notFoundColonia = new List<string>();
            var notFoundNumExt = new List<string>();

            foreach (var domicilio in domicilios)
            {
                var salesCheck = domicilio["sales_check"]?.ToString();
                var codPostal = domicilio["cod_postal"]?.ToString();
                var edoEntFed = domicilio["edo_entfed"]?.ToString();
                var delMunicipio = domicilio["del_municipio"]?.ToString();
                var colPoblacion = domicilio["col_poblacion"]?.ToString();
                var numIntExt = domicilio["direc_cte1"]?.ToString();

                var existeEstado = await GetEstado(edoEntFed, mode);
                if (existeEstado != null)
                {
                    var codigosMunicipio = await GetCodigosMunicipio(delMunicipio, edoEntFed, mode);
                    if (codigosMunicipio == null)
                    {
                        notFoundMunicipio.Add($"{salesCheck} - No existe municipio: '{delMunicipio}'");
                        continue;
                    }
                    domicilio["cve_mun"] = codigosMunicipio["cve_mun"];
                    domicilio["cve_est"] = codigosMunicipio["cve_est"];

                    var codigosLocalidad = await GetLocalidad(edoEntFed, mode);
                    if (codigosLocalidad != null)
                    {
                        domicilio["cve_loca"] = codigosLocalidad["cve_loc"];
                        domicilio["cve_est"] = codigosLocalidad["cve_est"];
                    }
                }
                else
                {
                    notFoundEstado.Add($"{salesCheck} - No existe estado: '{edoEntFed}'");
                }

                if (!string.IsNullOrEmpty(colPoblacion))
                {
                    var codigoColonia = await GetCodigoColonia(colPoblacion, codPostal, mode);
                    if (codigoColonia == null)
                    {
                        notFoundColonia.Add($"{salesCheck} - No existe colonia: '{colPoblacion}'");
                        continue;
                    }
                    domicilio["cve_col"] = codigoColonia["cve_col"];
                    domicilio["cod_postal_cat"] = codPostal;
                }

                if (string.IsNullOrWhiteSpace(numIntExt))
                {
                    notFoundNumExt.Add($"{salesCheck} - Sin numero exterior");
                }
                else if (numIntExt.Length <= 40 ||
                    !numIntExt.Substring(30, Math.Min(10, numIntExt.Length - 30)).Any(c => char.IsLetterOrDigit(c)) ||
                    !numIntExt.Substring(40, Math.Min(10, numIntExt.Length - 40)).Any(c => char.IsLetterOrDigit(c)))
                {
                    notFoundNumExt.Add($"{salesCheck} - Numero exterior no válido");
                }
            }

            var allErrors = new List<string>();
            allErrors.AddRange(notFoundEstado);
            allErrors.AddRange(notFoundMunicipio);
            allErrors.AddRange(notFoundColonia);
            allErrors.AddRange(notFoundNumExt);

            if (allErrors.Any())
            {
                result.Correct = false;
                result.Message = string.Join("\n", allErrors);
                return result;
            }

            result.Correct = true;
            return result;
        }

        // Métodos auxiliares de consulta
        private static async Task<Dictionary<string, object>?> GetOperadorInfo(string nombreOperador, string mode)
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

        private static async Task<Dictionary<string, object>?> GetTransporteInfo(string unidad, string mode)
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

        private static async Task<List<Dictionary<string, object>>> GetSkusByFolioOrderedByParada(string folio, string mode)
        {
            var skus = new List<Dictionary<string, object>>();

            using (var connection = new OdbcConnection(DL.Connection.GetConnectionStringLga(mode)))
            {
                await connection.OpenAsync();

                string query = @"
                    SELECT O.no_parada, D.sales_check, D.sku, E.no_transf
                      FROM lgahventa P, lgaent E, lgadventa D, lgaetiqeta U, lgaparada O
                     WHERE P.cod_empresa = E.cod_empresa
                       AND P.cd_id       = E.cd_id
                       AND P.no_transf   = E.no_transf
                       AND P.cod_empresa = D.cod_empresa
                       AND P.cd_id       = D.cd_id
                       AND P.sales_check = D.sales_check
                       AND P.cod_empresa = U.cod_empresa
                       AND P.cd_id       = U.cd_id
                       AND D.no_etiqueta = U.no_etiqueta
                       AND P.cod_empresa = O.cod_empresa
                       AND P.cd_id       = O.cd_id
                       AND P.sales_check = O.sales_check
                       AND P.no_transf   = E.no_transf
                       AND U.st_etiqueta = 7
                       AND P.cod_empresa = 1
                       AND P.cd_id       = 870
                       AND U.no_conoc = ?
                     ORDER BY O.no_parada ASC
                ";

                using (var command = new OdbcCommand(query, connection))
                {
                    command.Parameters.Add(new OdbcParameter("@folio", folio));

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            var noParada = GetSafeValue(reader, 0, "no_parada", "GetSkusByFolioOrderedByParada");
                            var salesCheck = GetSafeValue(reader, 1, "sales_check", "GetSkusByFolioOrderedByParada");
                            var sku = GetSafeValue(reader, 2, "sku", "GetSkusByFolioOrderedByParada");
                            var noTransf = GetSafeValue(reader, 3, "no_transf", "GetSkusByFolioOrderedByParada");

                            skus.Add(new Dictionary<string, object>
                            {
                                ["no_parada"] = noParada,
                                ["sales_check"] = salesCheck,
                                ["sku"] = sku,
                                ["no_transf"] = noTransf
                            });
                        }
                    }
                }
            }

            return skus;
        }

        private static async Task<Dictionary<string, object>?> GetCodigosFiscalesPorSku(string sku, string mode)
        {
            if (sku.StartsWith("999"))
            {
                return null;
            }

            using (var connection = new OdbcConnection(DL.Connection.GetConnectionStringLga(mode)))
            {
                await connection.OpenAsync();

                string query = @"
                    SELECT L.sku,
                        K.des_codfis,
                        Y.pes_art,
                        K.cve_codfis
                    FROM lgacat_sku L
                    JOIN gen@gnx_prod_tcp:arti U 
                        ON L.cod_empresa = U.cod_emp
                    AND L.cod_interno = U.int_art
                    JOIN dbmdw@gnx_mdw:cat_codfis K 
                        ON U.char_5 = K.cve_codfis
                    LEFT OUTER JOIN gen@gnx_prod_tcp:arti_vol Y
                        ON L.cod_empresa = Y.cod_emp
                    AND L.cod_interno = Y.int_art
                    WHERE L.cod_empresa = 1
                    AND L.sku = ?
                ";

                using (var command = new OdbcCommand(query, connection))
                {
                    command.Parameters.Add(new OdbcParameter("@sku", sku));

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            var skuValue = GetSafeValue(reader, 0, "sku", "GetCodigosFiscalesPorSku")?.ToString();
                            var desCodfis = GetSafeValue(reader, 1, "des_codfis", "GetCodigosFiscalesPorSku")?.ToString()?.Trim();
                            var pesArt = GetSafeValue(reader, 2, "pes_art", "GetCodigosFiscalesPorSku");
                            var cveCodfis = GetSafeValue(reader, 3, "cve_codfis", "GetCodigosFiscalesPorSku")?.ToString();

                            return new Dictionary<string, object>
                            {
                                ["sku"] = skuValue,
                                ["des_codfis"] = desCodfis,
                                ["pes_art"] = pesArt,
                                ["cve_codfis"] = cveCodfis
                            };
                        }
                    }
                }
            }

            return null;
        }

        private static async Task<List<Dictionary<string, object>>> GetDomiciliosPorFolio(string folio, string mode)
        {
            var domicilios = new List<Dictionary<string, object>>();

            using (var connection = new OdbcConnection(DL.Connection.GetConnectionStringLga(mode)))
            {
                await connection.OpenAsync();

                string query = @"
                    SELECT D.sales_check, D.sku, E.direc_cte, E.direc_cte1, E.referencia,
                           E.referencia1, E.col_poblacion, E.del_municipio, E.ciudad, E.edo_entfed,
                           E.cod_postal
                      FROM lgahventa P, lgaent E, lgadventa D, lgaetiqeta U, lgaparada O
                     WHERE P.cod_empresa = E.cod_empresa
                       AND P.cd_id       = E.cd_id
                       AND P.no_transf   = E.no_transf
                       AND P.cod_empresa = D.cod_empresa
                       AND P.cd_id       = D.cd_id
                       AND P.sales_check = D.sales_check
                       AND P.cod_empresa = U.cod_empresa
                       AND P.cd_id       = U.cd_id
                       AND D.no_etiqueta = U.no_etiqueta
                       AND P.cod_empresa = O.cod_empresa
                       AND P.cd_id       = O.cd_id
                       AND P.sales_check = O.sales_check
                       AND P.no_transf   = E.no_transf
                       AND U.st_etiqueta = 7
                       AND P.cod_empresa = 1
                       AND P.cd_id       = 870
                       AND U.no_conoc = ?
                ";

                using (var command = new OdbcCommand(query, connection))
                {
                    command.Parameters.Add(new OdbcParameter("@folio", folio));

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            var salesCheck = GetSafeValue(reader, 0, "sales_check", "GetDomiciliosPorFolio")?.ToString();

                            domicilios.Add(new Dictionary<string, object>
                            {
                                ["sales_check"] = salesCheck,
                                ["sku"] = GetSafeValue(reader, 1, "sku", "GetDomiciliosPorFolio", salesCheck)?.ToString(),
                                ["direc_cte"] = GetSafeValue(reader, 2, "direc_cte", "GetDomiciliosPorFolio", salesCheck)?.ToString(),
                                ["direc_cte1"] = GetSafeValue(reader, 3, "direc_cte1", "GetDomiciliosPorFolio", salesCheck)?.ToString(),
                                ["referencia"] = GetSafeValue(reader, 4, "referencia", "GetDomiciliosPorFolio", salesCheck)?.ToString(),
                                ["referencia1"] = GetSafeValue(reader, 5, "referencia1", "GetDomiciliosPorFolio", salesCheck)?.ToString(),
                                ["col_poblacion"] = GetSafeValue(reader, 6, "col_poblacion", "GetDomiciliosPorFolio", salesCheck)?.ToString()?.Trim(),
                                ["del_municipio"] = GetSafeValue(reader, 7, "del_municipio", "GetDomiciliosPorFolio", salesCheck)?.ToString()?.Trim(),
                                ["ciudad"] = GetSafeValue(reader, 8, "ciudad", "GetDomiciliosPorFolio", salesCheck)?.ToString(),
                                ["edo_entfed"] = GetSafeValue(reader, 9, "edo_entfed", "GetDomiciliosPorFolio", salesCheck)?.ToString(),
                                ["cod_postal"] = GetSafeValue(reader, 10, "cod_postal", "GetDomiciliosPorFolio", salesCheck)?.ToString()
                            });
                        }
                    }
                }
            }

            return domicilios;
        }

        private static async Task<Dictionary<string, object>?> GetEstado(string claveEstado, string mode)
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

        private static async Task<Dictionary<string, object>?> GetCodigoColonia(string nomCol, string codPos, string mode)
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

        private static async Task<Dictionary<string, object>?> GetCodigosMunicipio(string descMunicipio, string claveEstado, string mode)
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

        private static async Task<Dictionary<string, object>?> GetLocalidad(string claveEstado, string mode)
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

        private static Task<double> CalcularDistancia(int entregaNum, Dictionary<string, object> domicilioInfo)
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
        private static async Task InsertarOrigenDestino(string idOri, string idDes, DateTime fecSal, DateTime fecLle,
            double distancia, string idUni, int codEmp, int entregaNum, string mode)
        {
            System.Console.WriteLine($"[INSERTAR ORIGEN-DESTINO] Entrega {entregaNum} - Origen: {idOri}, Destino: {idDes}, Distancia: {distancia} km");

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

        private static async Task InsertarMercancia(Dictionary<string, object> codigoFiscal, string idOri, string idDes,
            string idUni, int codEmp, string mode)
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

        private static async Task InsertarTransporte(Dictionary<string, object> transporteInfo, string idUni, int codEmp, decimal pesoBruto, string mode)
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

        private static async Task InsertarOperador(Dictionary<string, object> operadorInfo, string nombreOperador,
            string idUni, int codEmp, string mode)
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

        private static (string numInt, string numExt) ParseDirecCte1(string direcCte1)
        {
            if (string.IsNullOrEmpty(direcCte1))
            {
                return (" ", " ");
            }

            string numbersPart;
            if (direcCte1.Length > 30)
            {
                numbersPart = direcCte1.Substring(30).Trim();
            }
            else
            {
                numbersPart = direcCte1.Trim();
            }

            var parts = numbersPart.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length >= 2)
            {
                return (parts[0], parts[1]);
            }
            else if (parts.Length == 1)
            {
                return (" ", parts[0]);
            }

            return (" ", " ");
        }

        private static async Task InsertarDomicilioOrigen(string idOri, string idUni, int codEmp, int entregaNum, string mode)
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

        private static async Task InsertarDomicilio(string idDes, Dictionary<string, object> domicilioInfo,
            string idUni, int codEmp, string mode)
        {
            var direcCte = domicilioInfo["direc_cte"]?.ToString();
            var direcCte1 = domicilioInfo["direc_cte1"]?.ToString();
            var referencia = domicilioInfo["referencia"]?.ToString();
            var cveCol = domicilioInfo["cve_col"]?.ToString();
            var cveMun = domicilioInfo["cve_mun"]?.ToString();
            var cveEst = domicilioInfo["cve_est"]?.ToString();
            var cveLoca = domicilioInfo["cve_loca"]?.ToString();
            var codPostalCat = domicilioInfo["cod_postal_cat"]?.ToString();

            var (numInt, numExt) = ParseDirecCte1(direcCte1);

            using (var connection = new OdbcConnection(DL.Connection.GetConnectionStringGen(mode)))
            {
                await connection.OpenAsync();

                string query = @"
                    INSERT INTO dom_tim2 (des_ori, calle, num_ext, num_int, col, loca, ref, muni, est, pais, cod_pos, id_uni, cod_emp)
                    VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)
                ";

                using (var command = new OdbcCommand(query, connection))
                {
                    command.Parameters.Add(new OdbcParameter("@des_ori", idDes.PadRight(8).Substring(0, 8)));
                    command.Parameters.Add(new OdbcParameter("@calle", (direcCte ?? " ").PadRight(150).Substring(0, 150)));
                    command.Parameters.Add(new OdbcParameter("@num_ext", numExt.PadRight(50).Substring(0, 50)));
                    command.Parameters.Add(new OdbcParameter("@num_int", numInt.PadRight(50).Substring(0, 50)));
                    command.Parameters.Add(new OdbcParameter("@col", (cveCol ?? " ").PadRight(5).Substring(0, 5)));
                    command.Parameters.Add(new OdbcParameter("@loca", (cveLoca ?? " ").PadRight(2).Substring(0, 2)));
                    command.Parameters.Add(new OdbcParameter("@ref", (referencia ?? " ").PadRight(150).Substring(0, 150)));
                    command.Parameters.Add(new OdbcParameter("@muni", (cveMun ?? " ").PadRight(3).Substring(0, 3)));
                    command.Parameters.Add(new OdbcParameter("@est", (cveEst ?? " ").PadRight(3).Substring(0, 3)));
                    command.Parameters.Add(new OdbcParameter("@pais", "MEX".PadRight(3).Substring(0, 3)));
                    command.Parameters.Add(new OdbcParameter("@cod_pos", (codPostalCat ?? " ").PadRight(5).Substring(0, 5)));
                    command.Parameters.Add(new OdbcParameter("@id_uni", idUni.PadRight(25).Substring(0, 25)));
                    command.Parameters.Add(new OdbcParameter("@cod_emp", (decimal)codEmp));

                    await command.ExecuteNonQueryAsync();
                }
            }
        }

        // Métodos de verificación de duplicados
        private static async Task<List<string>> VerificarDuplicadosUbi(string idUni, int codEmp, int totalEntregas, string mode)
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

        private static async Task<bool> VerificarDuplicadosInt(string idUni, int codEmp, string mode)
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

        private static async Task<bool> VerificarDuplicadosTrans(string idUni, int codEmp, string mode)
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

        private static async Task<bool> VerificarDuplicadosOper(string idUni, int codEmp, string mode)
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

        private static async Task<List<string>> VerificarDuplicadosDom(string idUni, int codEmp, int totalEntregas, string mode)
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

        private static List<Dictionary<string, object>> AgruparDomiciliosUnicos(List<Dictionary<string, object>> domicilios)
        {
            var domiciliosUnicos = new List<Dictionary<string, object>>();
            var domiciliosVistos = new HashSet<string>();

            foreach (var domicilio in domicilios)
            {
                var direcCte = domicilio["direc_cte"]?.ToString() ?? " ";
                var direcCte1 = domicilio["direc_cte1"]?.ToString() ?? " ";
                var referencia = domicilio["referencia"]?.ToString() ?? " ";
                var referencia1 = domicilio["referencia1"]?.ToString() ?? " ";
                var colPoblacion = domicilio["col_poblacion"]?.ToString() ?? " ";
                var delMunicipio = domicilio["del_municipio"]?.ToString() ?? " ";
                var localidad = domicilio["cve_loca"]?.ToString() ?? " ";
                var ciudad = domicilio["ciudad"]?.ToString() ?? " ";
                var edoEntfed = domicilio["edo_entfed"]?.ToString() ?? " ";
                var codPostal = domicilio["cod_postal"]?.ToString() ?? " ";

                var claveDomicilio = $"{direcCte}|{direcCte1}|{referencia}|{referencia1}|{colPoblacion}|{delMunicipio}|{localidad}|{ciudad}|{edoEntfed}|{codPostal}";

                if (!domiciliosVistos.Contains(claveDomicilio))
                {
                    domiciliosVistos.Add(claveDomicilio);
                    domiciliosUnicos.Add(domicilio);
                }
            }

            return domiciliosUnicos;
        }

        // Métodos para generar CSVs y enviar emails
        private static async Task GenerarCSVsDespuesInserts(ML.CartaPorte.EnviarCartaRequest request, string idUni, int codEmp, string mode)
        {
            try
            {
                System.Console.WriteLine($"[GENERAR CSVs] Iniciando generación de CSVs para operador: {request.Operador}");

                var csvIntTim24 = await GenerarCSVIntTim24(idUni, codEmp, mode);
                var csvDomTim2 = await GenerarCSVDomTim2(idUni, codEmp, mode);

                await EnviarEmailConCSVs(csvIntTim24, csvDomTim2, request.Folio, request.Operador, mode);
                await EliminarDatosTablas(idUni, codEmp, mode);

                System.Console.WriteLine($"[GENERAR CSVs] CSVs generados, enviados y datos eliminados exitosamente");
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"[GENERAR CSVs] Error: {ex.Message}");
                throw;
            }
        }

        private static async Task<string> GenerarCSVIntTim24(string idUni, int codEmp, string mode)
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

        private static async Task<string> GenerarCSVDomTim2(string idUni, int codEmp, string mode)
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

        private static async Task EliminarDatosTablas(string idUni, int codEmp, string mode)
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
                        System.Console.WriteLine($"[ELIMINAR DATOS] Error al eliminar de tabla {tabla}: {ex.Message}");
                    }
                }
            }
        }

        private static async Task EnviarEmailConCSVs(string csvIntTim24, string csvDomTim2, string folio, string operador, string mode)
        {
            // Nota: Este método requiere configuración de email desde appsettings.json
            // Por ahora se deja como placeholder - se puede implementar después con IConfiguration
            System.Console.WriteLine($"[ENVIAR EMAIL] Preparando envío de email con CSVs para folio: {folio}");
            // TODO: Implementar envío de email con configuración
        }

        private static string GenerarCSV(List<Dictionary<string, object>> data)
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
    }
}
