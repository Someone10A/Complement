using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Data.Odbc;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ML.CartaPorte;

namespace BL.CartaPorte
{
    public class CartaPorteWms
    {
        private static object GetSafeValue(System.Data.Common.DbDataReader reader, int index, string fieldName, string context = " ", string scn = " ")
        {
            return BL.CartaPorte.CartaPorte.GetSafeValue(reader, index, fieldName, context, scn);
        }

        // Método para generar id_uni desde car_sal
        private static string GenerarIdUniDesdeCarSal(string carSal)
        {
            if (string.IsNullOrEmpty(carSal))
            {
                return "SEAOIC";
            }

            // Reemplazar prefijos
            string processed = carSal;
            if (processed.StartsWith("VAL", StringComparison.OrdinalIgnoreCase))
            {
                processed = "870" + processed.Substring(3);
            }
            else if (processed.StartsWith("CIG", StringComparison.OrdinalIgnoreCase))
            {
                processed = "840" + processed.Substring(3);
            }

            // Extraer solo números
            string numbersOnly = Regex.Replace(processed, @"[^0-9]", "");

            return $"SEAOIC{numbersOnly}";
        }

        // Métodos principales del controlador
        public static async Task<ML.Result> GetScnByCono(string carSal, string mode)
        {
            ML.Result result = new ML.Result();
            try
            {
                var scns = await GetScnsByConoInternal(carSal, mode);

                if (scns == null || scns.Count == 0)
                {
                    result.Correct = false;
                    result.Message = "No se encontraron SCNs para la carga de salida";
                    return result;
                }

                var results = scns.Select(scn => new Dictionary<string, object>
                {
                    ["num_scn"] = scn
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
            return await BL.CartaPorte.CartaPorte.GetOperadores(mode);
        }

        public static async Task<ML.Result> GetUnidades(string mode)
        {
            return await BL.CartaPorte.CartaPorte.GetUnidades(mode);
        }

        private static async Task<List<string>> GetScnsByConoInternal(string carSal, string mode)
        {
            var scns = new List<string>();
            //System.Console.WriteLine($"[GET SCNs BY CAR_SAL WMS] Buscando SCNs para car_sal: {carSal}");

            using (var connection = new OdbcConnection(DL.Connection.GetConnectionStringGen(mode)))
            {
                await connection.OpenAsync();

                string query = @"
                    SELECT num_scn
                    FROM ora_ruta
                    WHERE car_sal = ?
                ";

                using (var command = new OdbcCommand(query, connection))
                {
                    command.Parameters.Add(new OdbcParameter("@car_sal", carSal));

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            var numScn = GetSafeValue(reader, 0, "num_scn", "GetScnsByConoInternal");
                            if (numScn != null)
                            {
                                scns.Add(numScn.ToString());
                            }
                        }
                    }
                }
            }

            //System.Console.WriteLine($"[GET SCNs BY CAR_SAL WMS] SCNs encontrados: {scns.Count} - {string.Join(", ", scns)}");
            return scns;
        }

        // Método para obtener car_sal desde folio (que ahora es car_sal)
        private static async Task<string?> GetCarSalFromFolio(string folio, string mode)
        {
            // En WMS, el folio es directamente el car_sal
            return folio;
        }

        // Método principal EnviarCarta
        public static async Task<ML.Result> EnviarCarta(ML.CartaPorte.EnviarCartaRequest request, string mode)
        {
            ML.Result result = new ML.Result();
            try
            {
                //System.Console.WriteLine($"[ENVIAR CARTA WMS] Iniciando proceso para folio: {request?.Folio}, FechaSalida: {request.FechaSalida}");

                if (request == null || string.IsNullOrEmpty(request.Folio))
                {
                    result.Correct = false;
                    result.Message = "Request inválido o folio faltante";
                    return result;
                }

                // Obtener car_sal (el folio es el car_sal en WMS)
                var carSal = request.Folio;
                //System.Console.WriteLine($"[ENVIAR CARTA WMS] Carga de salida: {carSal}");

                // Generar id_uni desde car_sal
                var idUni = GenerarIdUniDesdeCarSal(carSal);
                //System.Console.WriteLine($"[ENVIAR CARTA WMS] ID generado: {idUni}");
                var codEmp = 1;

                // Obtener SKUs ordenados por parada usando las nuevas tablas
                //System.Console.WriteLine($"[ENVIAR CARTA WMS] Obteniendo SKUs para car_sal: {carSal}");
                var skusOrderedByParada = await GetSkusByCarSalOrderedByParada(carSal, mode);
                if (skusOrderedByParada == null || skusOrderedByParada.Count == 0)
                {
                    //System.Console.WriteLine($"[ENVIAR CARTA WMS] No se encontraron SKUs para la carga de salida {carSal}");
                    result.Correct = false;
                    result.Message = $"No se encontraron SKUs para la carga de salida {carSal}";
                    return result;
                }

                var skusByParada = skusOrderedByParada.GroupBy(s => s["no_parada"]).OrderBy(g => g.Key).ToList();
                var totalEntregas = skusByParada.Count;
                //System.Console.WriteLine($"[ENVIAR CARTA WMS] Total de entregas encontradas: {totalEntregas}");

                // Verificar duplicados
                //System.Console.WriteLine($"[ENVIAR CARTA WMS] Verificando duplicados para ID: {idUni}");
                var duplicadosUbi = await BL.CartaPorte.CartaPorte.VerificarDuplicadosUbi(idUni, codEmp, totalEntregas, mode);
                if (duplicadosUbi.Count > 0)
                {
                    //System.Console.WriteLine($"[ENVIAR CARTA WMS] Duplicados encontrados en ubi_tim2: {string.Join(", ", duplicadosUbi)}");
                    result.Correct = false;
                    result.Message = $"ID ya generado: {idUni}";
                    return result;
                }

                var duplicadosInt = await BL.CartaPorte.CartaPorte.VerificarDuplicadosInt(idUni, codEmp, mode);
                if (duplicadosInt)
                {
                    //System.Console.WriteLine($"[ENVIAR CARTA WMS] Duplicados encontrados en int_tim24");
                    result.Correct = false;
                    result.Message = $"ID ya generado: {idUni}";
                    return result;
                }

                var duplicadosTrans = await BL.CartaPorte.CartaPorte.VerificarDuplicadosTrans(idUni, codEmp, mode);
                if (duplicadosTrans)
                {
                    //System.Console.WriteLine($"[ENVIAR CARTA WMS] Duplicados encontrados en trans_tim24");
                    result.Correct = false;
                    result.Message = $"ID ya generado: {idUni}";
                    return result;
                }

                var duplicadosOper = await BL.CartaPorte.CartaPorte.VerificarDuplicadosOper(idUni, codEmp, mode);
                if (duplicadosOper)
                {
                    //System.Console.WriteLine($"[ENVIAR CARTA WMS] Duplicados encontrados en oper_tim2");
                    result.Correct = false;
                    result.Message = $"ID ya generado: {idUni}";
                    return result;
                }

                var duplicadosDom = await BL.CartaPorte.CartaPorte.VerificarDuplicadosDom(idUni, codEmp, totalEntregas, mode);
                if (duplicadosDom.Count > 0)
                {
                    //System.Console.WriteLine($"[ENVIAR CARTA WMS] Duplicados encontrados en dom_tim2: {string.Join(", ", duplicadosDom)}");
                    result.Correct = false;
                    result.Message = $"ID ya generado: {idUni}";
                    return result;
                }
                //System.Console.WriteLine($"[ENVIAR CARTA WMS] No se encontraron duplicados, continuando...");

                // Obtener información del operador
                //System.Console.WriteLine($"[ENVIAR CARTA WMS] Obteniendo información del operador: {request.Operador}");
                var operadorInfo = await BL.CartaPorte.CartaPorte.GetOperadorInfo(request.Operador, mode);
                if (operadorInfo == null)
                {
                    //System.Console.WriteLine($"[ENVIAR CARTA WMS] No existe información del operador {request.Operador}");
                    result.Correct = false;
                    result.Message = $"No existe información del operador {request.Operador}";
                    return result;
                }
                //System.Console.WriteLine($"[ENVIAR CARTA WMS] Operador encontrado: RFC={operadorInfo["rfc_ope"]}, Lic={operadorInfo["lic_ope"]}");

                // Obtener información del transporte
                //System.Console.WriteLine($"[ENVIAR CARTA WMS] Obteniendo información del transporte: {request.Unidad}");
                var transporteInfo = await BL.CartaPorte.CartaPorte.GetTransporteInfo(request.Unidad, mode);
                if (transporteInfo == null)
                {
                    //System.Console.WriteLine($"[ENVIAR CARTA WMS] No existe información del transporte {request.Unidad}");
                    result.Correct = false;
                    result.Message = $"No existe información del transporte {request.Unidad}";
                    return result;
                }
                //System.Console.WriteLine($"[ENVIAR CARTA WMS] Transporte encontrado: Placa={transporteInfo["pla_vei"]}, Permiso={transporteInfo["per_sct"]}");

                // Obtener domicilios
                //System.Console.WriteLine($"[ENVIAR CARTA WMS] Obteniendo domicilios para car_sal: {carSal}");
                var domicilios = await GetDomiciliosPorCarSal(carSal, mode);
                if (domicilios == null || domicilios.Count == 0)
                {
                    //System.Console.WriteLine($"[ENVIAR CARTA WMS] No se encontraron domicilios para la carga de salida {carSal}");
                    result.Correct = false;
                    result.Message = $"No se encontraron domicilios para la carga de salida {carSal}";
                    return result;
                }
                //System.Console.WriteLine($"[ENVIAR CARTA WMS] Domicilios encontrados: {domicilios.Count}");

                // Validar domicilios
                //System.Console.WriteLine($"[ENVIAR CARTA WMS] Validando domicilios...");
                var validationResult = await ValidarDomicilios(domicilios, mode);
                if (!validationResult.Correct)
                {
                    //System.Console.WriteLine($"[ENVIAR CARTA WMS] Error en validación de domicilios: {validationResult.Message}");
                    return validationResult;
                }
                //System.Console.WriteLine($"[ENVIAR CARTA WMS] Domicilios validados correctamente");

                // Procesar entregas
                //System.Console.WriteLine($"[ENVIAR CARTA WMS] Procesando códigos fiscales por SKU...");
                var skusInfoList = new List<List<Dictionary<string, object>>>();
                var codigosFiscalesList = new List<List<Dictionary<string, object>>>();

                for (int i = 0; i < skusByParada.Count; i++)
                {
                    var paradaGroup = skusByParada[i];
                    var skus = paradaGroup.ToList();
                    var codigosFiscales = new List<Dictionary<string, object>>();

                    //System.Console.WriteLine($"[ENVIAR CARTA WMS] Procesando parada {paradaGroup.Key} con {skus.Count} SKUs");

                    foreach (var skuInfo in skus)
                    {
                        var sku = skuInfo["int_art"]?.ToString();
                        if (string.IsNullOrEmpty(sku))
                        {
                            //System.Console.WriteLine($"[ENVIAR CARTA WMS] SKU nulo o vacío en parada {paradaGroup.Key}");
                            result.Correct = false;
                            result.Message = $"SKU nulo o vacío en parada {paradaGroup.Key}";
                            return result;
                        }

                        var codigoFiscal = await GetCodigosFiscalesPorSku(sku, mode);
                        if (codigoFiscal == null && !sku.StartsWith("999"))
                        {
                            //System.Console.WriteLine($"[ENVIAR CARTA WMS] No se encontraron códigos fiscales para SKU {sku}");
                            result.Correct = false;
                            result.Message = $"No se encontraron códigos fiscales para SKU {sku}";
                            return result;
                        }

                        if (codigoFiscal != null)
                        {
                            codigosFiscales.Add(codigoFiscal);
                            //System.Console.WriteLine($"[ENVIAR CARTA WMS] SKU {sku}: Código fiscal={codigoFiscal["cve_codfis"]}, Peso={codigoFiscal["pes_art"]}");
                        }
                    }

                    skusInfoList.Add(skus);
                    codigosFiscalesList.Add(codigosFiscales);
                }

                // Procesar cada entrega
                //System.Console.WriteLine($"[ENVIAR CARTA WMS] Procesando {totalEntregas} entregas...");
                for (int i = 0; i < skusByParada.Count; i++)
                {
                    var paradaGroup = skusByParada[i];
                    var entregaNum = i + 1;
                    var codigosFiscales = codigosFiscalesList[i];
                    var skus = skusInfoList[i];
                    var numScns = skus.Select(s => s["num_scn"].ToString()).Distinct().ToList();

                    //System.Console.WriteLine($"[ENVIAR CARTA WMS] Procesando entrega {entregaNum} de {totalEntregas} - SCNs: {string.Join(", ", numScns)}");

                    var idOri = entregaNum == 1 ? "OR000870" : $"OR00000{entregaNum - 1}";
                    var idDes = $"DE00000{entregaNum}";

                    var domiciliosScn = domicilios.Where(d => numScns.Contains(d["num_scn"].ToString())).ToList();
                    //System.Console.WriteLine($"[ENVIAR CARTA WMS] Entrega {entregaNum}: {domiciliosScn.Count} domicilios encontrados");

                    DateTime fecSal = entregaNum == 1 ? request.FechaSalida : request.FechaSalida.AddHours(entregaNum - 1).AddMinutes(10 * (entregaNum - 1));
                    var fecLle = fecSal.AddHours(1);
                    var distancia = await BL.CartaPorte.CartaPorte.CalcularDistancia(entregaNum, domiciliosScn.Count > 0 ? domiciliosScn[0] : null);

                    //System.Console.WriteLine($"[ENVIAR CARTA WMS] Entrega {entregaNum}: Insertando origen-destino {idOri} -> {idDes}");
                    await BL.CartaPorte.CartaPorte.InsertarOrigenDestino(idOri, idDes, fecSal, fecLle, distancia, idUni, codEmp, entregaNum, mode);

                    //System.Console.WriteLine($"[ENVIAR CARTA WMS] Entrega {entregaNum}: Insertando {codigosFiscales.Count} mercancías");
                    foreach (var codigoFiscal in codigosFiscales)
                    {
                        await BL.CartaPorte.CartaPorte.InsertarMercancia(codigoFiscal, idOri, idDes, idUni, codEmp, mode);
                    }

                    //System.Console.WriteLine($"[ENVIAR CARTA WMS] Entrega {entregaNum}: Insertando domicilio origen");
                    await BL.CartaPorte.CartaPorte.InsertarDomicilioOrigen(idOri, idUni, codEmp, entregaNum, mode);

                    var domiciliosUnicos = AgruparDomiciliosUnicos(domiciliosScn);
                    //System.Console.WriteLine($"[ENVIAR CARTA WMS] Entrega {entregaNum}: Insertando {domiciliosUnicos.Count} domicilios únicos");
                    foreach (var domicilioUnico in domiciliosUnicos)
                    {
                        await InsertarDomicilio(idDes, domicilioUnico, idUni, codEmp, mode);
                    }
                }

                // Insertar transporte
                //System.Console.WriteLine($"[ENVIAR CARTA WMS] Calculando peso bruto total...");
                decimal pesoBrutoTotal = 0;
                foreach (var codigosFiscales in codigosFiscalesList)
                {
                    foreach (var codigoFiscal in codigosFiscales)
                    {
                        pesoBrutoTotal += (decimal)codigoFiscal["pes_art"];
                    }
                }
                //System.Console.WriteLine($"[ENVIAR CARTA WMS] Peso bruto total: {pesoBrutoTotal} kg");

                //System.Console.WriteLine($"[ENVIAR CARTA WMS] Insertando transporte y operador...");
                await BL.CartaPorte.CartaPorte.InsertarTransporte(transporteInfo, idUni, codEmp, pesoBrutoTotal, mode);
                await BL.CartaPorte.CartaPorte.InsertarOperador(operadorInfo, request.Operador, idUni, codEmp, mode);

                // Verificar si es operador específico
                var operadoresEspecificos = new[] { "Transportes MyM", "Paqueteria Castores" };
                bool esOperadorEspecifico = operadoresEspecificos.Contains(request.Operador);

                if (esOperadorEspecifico)
                {
                    //System.Console.WriteLine($"[ENVIAR CARTA WMS] Operador específico detectado: {request.Operador}, generando CSVs...");
                    await BL.CartaPorte.CartaPorte.GenerarCSVsDespuesInserts(request, idUni, codEmp, mode);
                    //System.Console.WriteLine($"[ENVIAR CARTA WMS] Proceso completado exitosamente para operador específico");
                    result.Correct = true;
                    result.Object = new { message = "Folio enviado a " + request.Operador, entregas = skusByParada.Count };
                    return result;
                }

                //System.Console.WriteLine($"[ENVIAR CARTA WMS] Proceso completado exitosamente. ID generado: {idUni}, Entregas: {skusByParada.Count}");
                result.Correct = true;
                result.Object = new { message = "ID generado: " + idUni, entregas = skusByParada.Count };
                return result;
            }
            catch (Exception ex)
            {
                result.Correct = false;
                result.Message = $"Error interno del servidor: {ex.Message}";
                result.Ex = ex;
                //System.Console.WriteLine($"[ENVIAR CARTA WMS] Error: {ex.Message}");
                //System.Console.WriteLine($"[ENVIAR CARTA WMS] Stack trace: {ex.StackTrace}");
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
                var numScn = domicilio["num_scn"]?.ToString();
                var codDir = domicilio["cod_dir"]?.ToString();
                var codPostal = domicilio["cod_postal"]?.ToString();
                var edoEntFed = domicilio["edo_entfed"]?.ToString();
                var delMunicipio = domicilio["del_municipio"]?.ToString();
                var colPoblacion = domicilio["col_poblacion"]?.ToString();
                var numExt = domicilio["num_ext"]?.ToString();

                //System.Console.WriteLine($"[VALIDAR DOMICILIOS WMS] Validando domicilio - SCN: {numScn}, cod_dir: {codDir}");

                var existeEstado = await BL.CartaPorte.CartaPorte.GetEstado(edoEntFed, mode);
                if (existeEstado != null)
                {
                    var codigosMunicipio = await BL.CartaPorte.CartaPorte.GetCodigosMunicipio(delMunicipio, edoEntFed, mode);
                    if (codigosMunicipio == null)
                    {
                        notFoundMunicipio.Add($"SCN: {numScn}, cod_dir: {codDir} - No existe municipio: '{delMunicipio}'");
                        //System.Console.WriteLine($"[VALIDAR DOMICILIOS WMS] Domicilio validado (con errores) - SCN: {numScn}, cod_dir: {codDir}. Buscando siguiente domicilio...");
                        continue;
                    }
                    domicilio["cve_mun"] = codigosMunicipio["cve_mun"];
                    domicilio["cve_est"] = codigosMunicipio["cve_est"];

                    var codigosLocalidad = await BL.CartaPorte.CartaPorte.GetLocalidad(edoEntFed, mode);
                    if (codigosLocalidad != null)
                    {
                        domicilio["cve_loca"] = codigosLocalidad["cve_loc"];
                        domicilio["cve_est"] = codigosLocalidad["cve_est"];
                    }
                }
                else
                {
                    notFoundEstado.Add($"SCN: {numScn}, cod_dir: {codDir} - No existe estado: '{edoEntFed}'");
                }

                if (!string.IsNullOrEmpty(colPoblacion))
                {
                    var codigoColonia = await BL.CartaPorte.CartaPorte.GetCodigoColonia(colPoblacion, codPostal, mode);
                    if (codigoColonia == null)
                    {
                        notFoundColonia.Add($"SCN: {numScn}, cod_dir: {codDir} - No existe colonia: '{colPoblacion}'");
                        //System.Console.WriteLine($"[VALIDAR DOMICILIOS WMS] Domicilio validado (con errores) - SCN: {numScn}, cod_dir: {codDir}. Buscando siguiente domicilio...");
                        continue;
                    }
                    domicilio["cve_col"] = codigoColonia["cve_col"];
                    domicilio["cod_postal_cat"] = codPostal;
                }

                // Validar número exterior (campo separado en cli_direccion)
                if (string.IsNullOrWhiteSpace(numExt))
                {
                    notFoundNumExt.Add($"SCN: {numScn}, cod_dir: {codDir} - Sin numero exterior");
                }
                else if (!numExt.Trim().Any(c => char.IsLetterOrDigit(c)))
                {
                    notFoundNumExt.Add($"SCN: {numScn}, cod_dir: {codDir} - Numero exterior no válido");
                }

                //System.Console.WriteLine($"[VALIDAR DOMICILIOS WMS] Domicilio validado - SCN: {numScn}, cod_dir: {codDir}. Buscando siguiente domicilio...");
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

        // Métodos auxiliares de consulta (específicos de WMS)

        private static async Task<List<Dictionary<string, object>>> GetSkusByCarSalOrderedByParada(string carSal, string mode)
        {
            var skus = new List<Dictionary<string, object>>();
            //System.Console.WriteLine($"[GET SKUs BY CAR_SAL WMS] Buscando SKUs tangibles para car_sal: {carSal}");

            using (var connection = new OdbcConnection(DL.Connection.GetConnectionStringGen(mode)))
            {
                await connection.OpenAsync();

                // Query usando ora_ruta, edc_cab, edc_det, arti
                // Filtrar artículos tangibles: cod_fam2 NOT IN (192,187)
                string query = @"
                    SELECT DISTINCT
                        R.num_scn,
                        D.int_art
                    FROM ora_ruta R
                    JOIN edc_cab C 
                        ON R.num_scn = C.num_scn
                    JOIN edc_det D 
                        ON C.cod_emp = D.cod_emp
                        AND C.cod_pto = D.cod_pto
                        AND C.num_edc = D.num_edc
                    JOIN arti A 
                        ON D.cod_emp = A.cod_emp
                        AND D.int_art = A.int_art
                    WHERE R.car_sal = ?
                        AND A.cod_fam2 NOT IN (192, 187)
                        AND C.cod_emp = 1
                    ORDER BY R.num_scn ASC
                ";

                using (var command = new OdbcCommand(query, connection))
                {
                    command.Parameters.Add(new OdbcParameter("@car_sal", carSal));

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        var scnToParada = new Dictionary<string, int>();
                        int paradaCounter = 1;

                        while (await reader.ReadAsync())
                        {
                            var numScn = GetSafeValue(reader, 0, "num_scn", "GetSkusByCarSalOrderedByParada")?.ToString();
                            var intArt = GetSafeValue(reader, 1, "int_art", "GetSkusByCarSalOrderedByParada")?.ToString();

                            // Agrupar por SCN (cada SCN es una parada)
                            if (string.IsNullOrEmpty(numScn))
                            {
                                continue;
                            }

                            int noParada;
                            if (!scnToParada.ContainsKey(numScn))
                            {
                                noParada = paradaCounter;
                                scnToParada[numScn] = paradaCounter;
                                paradaCounter++;
                            }
                            else
                            {
                                noParada = scnToParada[numScn];
                            }

                            skus.Add(new Dictionary<string, object>
                            {
                                ["no_parada"] = noParada,
                                ["num_scn"] = numScn,
                                ["int_art"] = intArt
                            });
                        }
                    }
                }
            }

            //System.Console.WriteLine($"[GET SKUs BY CAR_SAL WMS] SKUs encontrados: {skus.Count}, Agrupados en {skus.GroupBy(s => s["no_parada"]).Count()} paradas");
            return skus;
        }

        private static async Task<Dictionary<string, object>?> GetCodigosFiscalesPorSku(string sku, string mode)
        {
            if (sku.StartsWith("999"))
            {
                return null;
            }

            using (var connection = new OdbcConnection(DL.Connection.GetConnectionStringGen(mode)))
            {
                await connection.OpenAsync();

                // Query usando arti directamente desde gnx
                // arti.char_5 es el código fiscal que se insertará en int_tim24
                string query = @"
                    SELECT 
                        A.int_art,
                        A.des_art,
                        Y.pes_art,
                        A.char_5 as cve_codfis
                    FROM arti A
                    LEFT OUTER JOIN arti_vol Y
                        ON A.cod_emp = Y.cod_emp
                        AND A.int_art = Y.int_art
                    WHERE A.cod_emp = 1
                        AND A.int_art = ?
                ";

                using (var command = new OdbcCommand(query, connection))
                {
                    command.Parameters.Add(new OdbcParameter("@int_art", sku));

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            var intArt = GetSafeValue(reader, 0, "int_art", "GetCodigosFiscalesPorSku")?.ToString();
                            var desArt = GetSafeValue(reader, 1, "des_art", "GetCodigosFiscalesPorSku")?.ToString()?.Trim();
                            var pesArt = GetSafeValue(reader, 2, "pes_art", "GetCodigosFiscalesPorSku");
                            var cveCodfis = GetSafeValue(reader, 3, "cve_codfis", "GetCodigosFiscalesPorSku")?.ToString();

                            // Usar descripción del artículo
                            var desCodfis = string.IsNullOrEmpty(desArt) ? "Artículo" : desArt;

                            // Si no hay peso, usar 0
                            if (pesArt == null)
                            {
                                pesArt = 0.0m;
                            }

                            return new Dictionary<string, object>
                            {
                                ["sku"] = intArt,
                                ["des_codfis"] = desCodfis,
                                ["pes_art"] = pesArt,
                                ["cve_codfis"] = cveCodfis ?? ""
                            };
                        }
                    }
                }
            }

            return null;
        }

        private static async Task<List<Dictionary<string, object>>> GetDomiciliosPorCarSal(string carSal, string mode)
        {
            var domicilios = new List<Dictionary<string, object>>();
            //System.Console.WriteLine($"[GET DOMICILIOS BY CAR_SAL WMS] Buscando domicilios para car_sal: {carSal}");

            using (var connection = new OdbcConnection(DL.Connection.GetConnectionStringGen(mode)))
            {
                await connection.OpenAsync();

                // Query usando ora_ruta, edc_cab, cli_direccion para obtener domicilios
                // edc_cab.cod_dir se usa para buscar en cli_direccion
                // num_ext y num_int son campos separados en cli_direccion
                string query = @"
                    SELECT DISTINCT
                        R.num_scn,
                        C.cod_cli,
                        C.cod_dir,
                        D.dir_cli as direc_cte,
                        D.num_ext,
                        D.num_int,
                        D.ent_calles as referencia,
                        D.ent_calles2 as referencia1,
                        D.col_cli as col_poblacion,
                        D.pob_cli as del_municipio,
                        D.pob_cli as ciudad,
                        D.pro_cli as edo_entfed,
                        D.cp_cli as cod_postal
                    FROM ora_ruta R
                    JOIN edc_cab C 
                        ON R.num_scn = C.num_scn
                    JOIN cli_direccion D
                        ON C.cod_dir = D.cod_dir
                    WHERE R.car_sal = ?
                        AND C.cod_emp = 1
                ";

                using (var command = new OdbcCommand(query, connection))
                {
                    command.Parameters.Add(new OdbcParameter("@car_sal", carSal));

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            var numScn = GetSafeValue(reader, 0, "num_scn", "GetDomiciliosPorCarSal")?.ToString();

                            domicilios.Add(new Dictionary<string, object>
                            {
                                ["num_scn"] = numScn,
                                ["cod_cli"] = GetSafeValue(reader, 1, "cod_cli", "GetDomiciliosPorCarSal", numScn)?.ToString(),
                                ["cod_dir"] = GetSafeValue(reader, 2, "cod_dir", "GetDomiciliosPorCarSal", numScn)?.ToString(),
                                ["direc_cte"] = GetSafeValue(reader, 3, "direc_cte", "GetDomiciliosPorCarSal", numScn)?.ToString(),
                                ["num_ext"] = GetSafeValue(reader, 4, "num_ext", "GetDomiciliosPorCarSal", numScn)?.ToString(),
                                ["num_int"] = GetSafeValue(reader, 5, "num_int", "GetDomiciliosPorCarSal", numScn)?.ToString(),
                                ["referencia"] = GetSafeValue(reader, 6, "referencia", "GetDomiciliosPorCarSal", numScn)?.ToString(),
                                ["referencia1"] = GetSafeValue(reader, 7, "referencia1", "GetDomiciliosPorCarSal", numScn)?.ToString(),
                                ["col_poblacion"] = GetSafeValue(reader, 8, "col_poblacion", "GetDomiciliosPorCarSal", numScn)?.ToString()?.Trim(),
                                ["del_municipio"] = GetSafeValue(reader, 9, "del_municipio", "GetDomiciliosPorCarSal", numScn)?.ToString()?.Trim(),
                                ["ciudad"] = GetSafeValue(reader, 10, "ciudad", "GetDomiciliosPorCarSal", numScn)?.ToString(),
                                ["edo_entfed"] = GetSafeValue(reader, 11, "edo_entfed", "GetDomiciliosPorCarSal", numScn)?.ToString(),
                                ["cod_postal"] = GetSafeValue(reader, 12, "cod_postal", "GetDomiciliosPorCarSal", numScn)?.ToString()
                            });
                        }
                    }
                }
            }

            //System.Console.WriteLine($"[GET DOMICILIOS BY CAR_SAL WMS] Domicilios encontrados: {domicilios.Count}");
            return domicilios;
        }

        // Métodos de inserción (específicos de WMS)

        private static async Task InsertarDomicilio(string idDes, Dictionary<string, object> domicilioInfo, string idUni, int codEmp, string mode)
        {
            var direcCte = domicilioInfo["direc_cte"]?.ToString();
            var numExt = domicilioInfo["num_ext"]?.ToString();
            var numInt = domicilioInfo["num_int"]?.ToString();
            var referencia = domicilioInfo["referencia"]?.ToString();
            var cveCol = domicilioInfo["cve_col"]?.ToString();
            var cveMun = domicilioInfo["cve_mun"]?.ToString();
            var cveEst = domicilioInfo["cve_est"]?.ToString();
            var cveLoca = domicilioInfo["cve_loca"]?.ToString();
            var codPostalCat = domicilioInfo["cod_postal_cat"]?.ToString();
            var numScn = domicilioInfo["num_scn"]?.ToString();
            //System.Console.WriteLine($"[INSERTAR DOMICILIO WMS] ID={idUni}, Destino={idDes}, SCN={numScn}, Calle={direcCte}, NumExt={numExt}, CP={codPostalCat}");

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
                    command.Parameters.Add(new OdbcParameter("@num_ext", (numExt ?? " ").PadRight(50).Substring(0, 50)));
                    command.Parameters.Add(new OdbcParameter("@num_int", (numInt ?? " ").PadRight(50).Substring(0, 50)));
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


        private static List<Dictionary<string, object>> AgruparDomiciliosUnicos(List<Dictionary<string, object>> domicilios)
        {
            var domiciliosUnicos = new List<Dictionary<string, object>>();
            var domiciliosVistos = new HashSet<string>();

            foreach (var domicilio in domicilios)
            {
                var direcCte = domicilio["direc_cte"]?.ToString() ?? " ";
                var numExt = domicilio["num_ext"]?.ToString() ?? " ";
                var numInt = domicilio["num_int"]?.ToString() ?? " ";
                var referencia = domicilio["referencia"]?.ToString() ?? " ";
                var referencia1 = domicilio["referencia1"]?.ToString() ?? " ";
                var colPoblacion = domicilio["col_poblacion"]?.ToString() ?? " ";
                var delMunicipio = domicilio["del_municipio"]?.ToString() ?? " ";
                var localidad = domicilio["cve_loca"]?.ToString() ?? " ";
                var ciudad = domicilio["ciudad"]?.ToString() ?? " ";
                var edoEntfed = domicilio["edo_entfed"]?.ToString() ?? " ";
                var codPostal = domicilio["cod_postal"]?.ToString() ?? " ";

                var claveDomicilio = $"{direcCte}|{numExt}|{numInt}|{referencia}|{referencia1}|{colPoblacion}|{delMunicipio}|{localidad}|{ciudad}|{edoEntfed}|{codPostal}";

                if (!domiciliosVistos.Contains(claveDomicilio))
                {
                    domiciliosVistos.Add(claveDomicilio);
                    domiciliosUnicos.Add(domicilio);
                }
            }

            return domiciliosUnicos;
        }

    }
}
