using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Odbc;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BL.Importation
{
    public class ImportationMatch
    {
        public static ML.Result GetMatchOrders(ML.Importation.ImportationMatch importationMatch,string mode)
        {
            ML.Result result = new ML.Result();
            try
            {
                using (OdbcConnection connection = new OdbcConnection(DL.Connection.GetConnectionStringGen(mode)))
                {
                    connection.Open();

                    ML.Result resultGetIdInt = GetIdInt(connection, importationMatch);
                    if (!resultGetIdInt.Correct)
                    {
                        throw resultGetIdInt.Ex;
                    }
                    string idInt = (string)resultGetIdInt.Object;

                    ML.Result resultCreateTemps = CreateTemps(connection, importationMatch, idInt);
                    if (!resultCreateTemps.Correct)
                    {
                        throw resultCreateTemps.Ex;
                    }

                    ML.Result resultGetDiffs = GetDiffs(connection, importationMatch);
                    if (!resultGetDiffs.Correct)
                    {
                        throw resultGetDiffs.Ex;
                    }
                    List<ML.Importation.Match> matchList = (List<ML.Importation.Match>)resultGetDiffs.Object;

                    ML.Result resultDropTemps = DropTemps(connection);
                    if (!resultDropTemps.Correct)
                    {
                        throw resultDropTemps.Ex;
                    }

                    result.Correct = true;
                    result.Object = matchList;
                }
            }
            catch (Exception ex)
            {
                result.Correct = false;
                result.Message = $@"Error proceso Match GTM LGA {ex.Message}";
                result.Ex = ex;
            }
            return result;
        }
        private static ML.Result GetIdInt(OdbcConnection connection, ML.Importation.ImportationMatch importationMatch)
        {
            ML.Result result = new ML.Result();
            try
            {
                string query = $@"SELECT TRIM(id_int)
                                    FROM ora_imp_corden
                                    WHERE cod_emp = 1
                                    AND estatus = 0
                                    AND fol_gtm = '{importationMatch.FolGtm}'
                                    AND oc_madre = {importationMatch.OcMadre}
                                    ;";

                string idInt = string.Empty;

                using(OdbcCommand cmd = new OdbcCommand(query, connection))
                {
                    using(OdbcDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read()) 
                        {
                            idInt = reader.GetString(0);
                        }
                        else
                        {
                            throw new Exception($@"No existe la orden con folio {importationMatch.FolGtm} y OcMadre {importationMatch.OcMadre} o aun no llega");
                        }
                    }
                }

                result.Correct = true;
                result.Object = idInt;
            }
            catch (Exception ex)
            {
                result.Correct = false;
                result.Message = $@"Error al obtener orden de GTM {ex.Message}";
                result.Ex = ex;
            }
            return result;
        }
        private static ML.Result CreateTemps(OdbcConnection connection, ML.Importation.ImportationMatch importationMatch, string idInt)
        {
            ML.Result result = new ML.Result();
            try
            {
                string queryGTM = $@"SELECT sku,piezas
                                        FROM ora_imp_dorden
                                        WHERE cod_emp = 1
                                        AND id_int IN ('{idInt}')
                                        INTO TEMP zchecagtmgtm
                                ;";

                using(OdbcCommand cmd = new OdbcCommand(queryGTM, connection))
                {
                    cmd.ExecuteNonQuery();
                }
                string queryLGA = GetQueryLGA(importationMatch.Key).Replace("ORDEN_COMPRA", importationMatch.OcHija);

                using (OdbcCommand cmd = new OdbcCommand(queryLGA, connection))
                {
                    cmd.ExecuteNonQuery();
                }

                result.Correct = true;
            }
            catch (Exception ex)
            {
                result.Correct = false;
                result.Message = $@"Error al crear tablas temporales {ex.Message}";
                result.Ex = ex;
            }
            return result;
        }
        private static string GetQueryLGA(string key)
        {
            if(key == "sku")
            {
                return $@"SELECT sku,SUM(cant_distribu) as piezas
                            FROM dblga@lga_prod:lgadorco
                            WHERE cod_empresa = 1
                            AND pto_emisor = 999
                            AND no_orden = ORDEN_COMPRA
                            GROUP BY 1
                            INTO TEMP zchecagtmlga";
            }
            else
            {
                return $@"SELECT TRIM(cod_interno) AS sku,SUM(cant_distribu) as piezas
                            FROM dblga@lga_prod:lgadorco
                            WHERE cod_empresa = 1
                            AND pto_emisor = 999
                            AND no_orden = ORDEN_COMPRA
                            GROUP BY 1
                            INTO TEMP zchecagtmlga";
            }
        }
        private static ML.Result GetDiffs(OdbcConnection connection, ML.Importation.ImportationMatch importationMatch)
        {
            ML.Result result = new ML.Result();
            try
            {
                string query = GetQueryMatch(importationMatch.Pivote);

                List<ML.Importation.Match> matchList = new List<ML.Importation.Match>();

                using (OdbcCommand cmd = new OdbcCommand(query, connection))
                {
                    using (OdbcDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        { 
                            ML.Importation.Match match = new ML.Importation.Match();

                            match.Sku = reader.GetString(0);

                            if (importationMatch.Pivote == "GTM")
                            {
                                match.PiezasGtm = reader.GetInt32(1);
                                match.PiezasLGA = reader.GetInt32(2);
                                match.Diff = match.PiezasGtm - match.PiezasLGA;
                            }
                            else
                            {
                                match.PiezasLGA = reader.GetInt32(1);
                                match.PiezasGtm = reader.GetInt32(2);
                                match.Diff = match.PiezasLGA - match.PiezasGtm;
                            }

                            matchList.Add(match);   
                        }
                    }
                }
                matchList.Sort((a, b) =>
                {
                    if (a.Diff == 0 && b.Diff == 0)
                        return 0;
                    if (a.Diff == 0)
                        return 1;
                    if (b.Diff == 0)
                        return -1;
                    return b.Diff.CompareTo(a.Diff);
                });


                result.Correct = true;
                result.Object = matchList;
            }
            catch (Exception ex)
            {
                result.Correct = false;
                result.Message = $@"Error al matchear ordenes {ex.Message}";
                result.Ex = ex;
            }
            return result;
        }
        private static string GetQueryMatch(string pivote)
        {
            if (pivote == "GTM")
            {
                return $@"SELECT A.sku AS sku,
                                CASE WHEN (A.piezas IS NULL) THEN 0 ELSE A.piezas END AS piezasGTM,
                                CASE WHEN (B.piezas IS NULL) THEN 0 ELSE B.piezas END AS piezasLGA
                                FROM zchecagtmgtm A, OUTER zchecagtmlga B
                        WHERE B.sku = A.sku";
            }
            else
            {
                return $@"SELECT A.sku AS sku,
                                CASE WHEN (A.piezas IS NULL) THEN 0 ELSE A.piezas END AS piezasLGA,
                                CASE WHEN (B.piezas IS NULL) THEN 0 ELSE B.piezas END AS piezasGTM
                                FROM zchecagtmlga A, OUTER zchecagtmgtm B
                        WHERE B.sku = A.sku";
            }
        }
        private static ML.Result DropTemps(OdbcConnection connection)
        {
            ML.Result result = new ML.Result();
            try
            {
                string queryGTM = $@"DROP TABLE zchecagtmgtm";

                using (OdbcCommand cmd = new OdbcCommand(queryGTM, connection))
                {
                    cmd.ExecuteNonQuery();
                }
                string queryLGA = $@"DROP TABLE zchecagtmlga";

                using (OdbcCommand cmd = new OdbcCommand(queryLGA, connection))
                {
                    cmd.ExecuteNonQuery();
                }

                result.Correct = true;
            }
            catch (Exception ex)
            {
                result.Correct = false;
                result.Message = $@"Error al dropear tablas temporales {ex.Message}";
                result.Ex = ex;
            }
            return result;
        }
    }
}
