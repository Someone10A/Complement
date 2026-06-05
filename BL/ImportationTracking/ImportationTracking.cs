    using DocumentFormat.OpenXml.Wordprocessing;
using ML.ImportationTracking;
using System;
using System.Collections.Generic;
using System.Data.Odbc;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace BL.ImportationTracking
{
    public class ImportationTracking
    {
        //Public Visualiza ordenes aptas a imprimir
        public static ML.Result GetOrdersToPrint(string mode)
        {
            ML.Result result = new ML.Result();
            try
            {
                using (OdbcConnection connection = new OdbcConnection(DL.Connection.GetConnectionStringGen(mode)))
                {
                    connection.Open();

                    string query = $@"SELECT X.no_orden,A.cd_id,TRIM(C.razon_social) AS razon_social,
		                                        CASE WHEN (D.no_pedimento IS NULL) THEN 'No registrado' ELSE TRIM(D.no_pedimento) END AS pedimento,
		                                        COUNT(DISTINCT etiqueta)
                                        FROM hermes_imp_control X,hermes_imp_detail Y,
	                                        dblga@lga_prod:lgaindorco A,dblga@lga_prod:lgahorco B,dblga@lga_prod:lgaprovee C, OUTER dblga@lga_prod:lgafolio_pzas D
                                        WHERE X.estatus IN (0)
                                        AND Y.no_orden = X.no_orden
                                        AND A.cod_empresa = 1
                                        AND A.no_orden = X.no_orden
                                        AND B.cod_empresa = A.cod_empresa
                                        AND B.pto_emisor = 999
                                        AND B.no_orden = A.no_orden
                                        AND C.cod_empresa  = B.cod_empresa
                                        AND C.cv_proveedor = B.cv_proveedor
                                        AND D.cod_empresa = A.cod_empresa
                                        AND D.no_folio = A.no_orden
                                        AND D.usuario = 500
                                        GROUP BY 1,2,3,4";

                    List<ML.ImportationTracking.OrdenCompra> ordenList = new List<ML.ImportationTracking.OrdenCompra>();

                    using (OdbcCommand cmd = new OdbcCommand(query, connection))
                    {
                        using (OdbcDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                ML.ImportationTracking.OrdenCompra orden = new ML.ImportationTracking.OrdenCompra();

                                orden.NoOrden = reader.GetString(0);
                                orden.AlmacenDestino = reader.GetString(1);
                                orden.RazonSocial = reader.GetString(2);
                                orden.Pedimento = reader.GetString(3);
                                orden.CantidadBultos = reader.GetInt32(4).ToString();

                                ordenList.Add(orden);
                            }
                        }
                    }

                    result.Correct = true;
                    result.Object = ordenList;
                }
            }
            catch (Exception ex)
            {
                result.Correct = false;
                result.Message = $@"Error al obtener la informacion de ordenes {ex.Message}";
            }
            return result;
        }


        //Public Recibe OC y manda si es viable o no es Viable y si existe ya o no
        public static ML.Result EvaluateOC(string noOrden, string mode)
        {
            ML.Result result = new ML.Result();
            try
            {
                using (OdbcConnection connection = new OdbcConnection(DL.Connection.GetConnectionStringGen(mode)))
                {
                    connection.Open();

                    string query = $@"SELECT A.no_orden,
		                                    CASE WHEN (D.no_pedimento IS NULL) THEN 'No registrado' ELSE TRIM(D.no_pedimento) END AS pedimento,
		                                    A.cd_id,
		                                    B.division,
		                                    B.cv_proveedor,
		                                    TRIM(C.razon_social) AS razon_social,
		                                    A.ind_pvta
                                    FROM dblga@lga_prod:lgaindorco A,dblga@lga_prod:lgahorco B,dblga@lga_prod:lgaprovee C, OUTER dblga@lga_prod:lgafolio_pzas D
                                    WHERE A.cod_empresa = 1
                                    AND A.no_orden = {noOrden}
                                    AND A.no_orden NOT IN (SELECT no_orden FROM hermes_imp_control)
                                    AND B.cod_empresa = A.cod_empresa
                                    AND B.pto_emisor = 999
                                    AND B.no_orden = A.no_orden
                                    AND C.cod_empresa  = B.cod_empresa
                                    AND C.cv_proveedor = B.cv_proveedor
                                    AND D.cod_empresa = A.cod_empresa
                                    AND D.no_folio = A.no_orden
                                    AND D.usuario = 500";

                    ML.ImportationTracking.OrdenCompra orden = new ML.ImportationTracking.OrdenCompra();

                    using (OdbcCommand cmd = new OdbcCommand(query, connection))
                    {
                        using(OdbcDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                orden.Find = true;//Bandera para no leer object en Front
                                orden.NoOrden = reader.GetString(0);
                                orden.Pedimento = reader.GetString(1);
                                orden.AlmacenDestino = reader.GetString(2);
                                orden.Division = reader.GetString(3);
                                orden.IdProveedor = reader.GetString(4);
                                orden.RazonSocial = reader.GetString(5);
                                orden.PasoE = reader.GetString(6) == "X" ? true : false; //Bandera importante si es false, no puede mandar a llamar Generate()
                                //orden.CantidadBultos = reader.GetString(7);

                                result.Message = $@"{noOrden} encontrada";
                            }
                            else
                            {
                                orden.Find = false;
                                result.Message = $@"No se encontro la OC {noOrden} apta, sin pasos o ya impresa";
                            }
                        }
                    }

                    result.Correct = true;
                    result.Object = orden;
                }
            }
            catch (Exception ex)
            {
                result.Correct = false;
                result.Message = $@"Error en Evaluar si la OC es correcta {ex.Message}";
            }
            return result;
        }


        //Public Recibe OC y Bultos y etiquetadora
        public static ML.Result Generate(ML.ImportationTracking.OrdenCompra ordenCompra, string mode)
        {
            ML.Result resultGenerate = new ML.Result();
            try
            {
                int bultos = int.Parse(ordenCompra.CantidadBultos); // Ajusta según tu modelo

                using (OdbcConnection connection = new OdbcConnection(DL.Connection.GetConnectionStringGen(mode)))
                {
                    connection.Open();
                    using (OdbcTransaction transaction = connection.BeginTransaction())
                    {
                        try
                        {
                            // GetConsecutivos
                            ML.Result resultGetConsecutivos = GetConsecutivos(connection, transaction, mode);
                            if (!resultGetConsecutivos.Correct)
                            {
                                throw new Exception(resultGetConsecutivos.Message);
                            }
                            ML.ImportationTracking.Consecutivos consecutivos = (ML.ImportationTracking.Consecutivos)resultGetConsecutivos.Object;

                            // GenerateSecuenc
                            ML.Result resultGenerateSecuenc = GenerateSecuenc(consecutivos, bultos, mode);
                            if (!resultGenerateSecuenc.Correct)
                            {
                                throw new Exception(resultGenerateSecuenc.Message);
                            }
                            ML.ImportationTracking.Consecutivos consecutivosActualizados = (ML.ImportationTracking.Consecutivos)resultGenerateSecuenc.Objects[0];
                            List<ML.ImportationTracking.Etiqueta> etiquetas = (List<ML.ImportationTracking.Etiqueta>)resultGenerateSecuenc.Objects[1];

                            // InsertCab
                            ML.Result resultInsertCab = InsertCab(connection, transaction, ordenCompra, mode);
                            if (!resultInsertCab.Correct)
                            {
                                throw new Exception(resultInsertCab.Message);
                            }

                            // InsertDet
                            ML.Result resultInsertDet = InsertDet(connection, transaction, ordenCompra, etiquetas, mode);
                            if (!resultInsertDet.Correct)
                            {
                                throw new Exception(resultInsertDet.Message);
                            }

                            // UpdateConsecutivos
                            ML.Result resultUpdateConsecutivos = UpdateConsecutivos(connection, transaction, consecutivosActualizados, mode);
                            if (!resultUpdateConsecutivos.Correct)
                            {
                                throw new Exception(resultUpdateConsecutivos.Message);
                            }

                            
                            transaction.Commit();

                            resultGenerate.Correct = true;
                            resultGenerate.Message = "Etiquetas generadas correctamente";
                        }
                        catch (Exception)
                        {
                            transaction.Rollback();
                            throw;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                resultGenerate.Correct = false;
                resultGenerate.Message = $@"Error al generar las etiquetas {ex.Message}";
            }
            return resultGenerate;
        }
        //Private Recupera folio y nomenglatura
        private static ML.Result GetConsecutivos(OdbcConnection connection,OdbcTransaction transaction, string mode)
        {
            ML.Result result = new ML.Result();
            try
            {
                string query = $@"SELECT TRIM(tipo),contador
                                    FROM hermes_imp_conta";

                ML.ImportationTracking.Consecutivos consecutivos = new ML.ImportationTracking.Consecutivos();

                using (OdbcCommand cmd = new OdbcCommand(query, connection, transaction))
                {
                    using(OdbcDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            consecutivos.Prefijo = reader.GetString(0);
                            consecutivos.Consecutivo = reader.GetInt64(1);

                            result.Correct = true;
                            result.Object = consecutivos;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                result.Correct = false;
                result.Message = $@"Error al recuperar folio {ex.Message}";
            }
            return result;
        }
        //Private genera consec y bultos
        private static ML.Result GenerateSecuenc(ML.ImportationTracking.Consecutivos consecutivos, int bultos, string mode)
        {
            ML.Result result = new ML.Result();
            try
            {
                List<ML.ImportationTracking.Etiqueta> etiquetas = new List<ML.ImportationTracking.Etiqueta>();
                int seq = 0;
                for(int i = 0; i < bultos; i++)
                {
                    ML.ImportationTracking.Etiqueta eti = new ML.ImportationTracking.Etiqueta();
                    consecutivos.Consecutivo++;
                    seq++;

                    eti.NoEtiqueta = $@"{consecutivos.Prefijo}{consecutivos.Consecutivo.ToString("D10")}";
                    eti.Bulto = $@"{seq.ToString("D3")}/{bultos.ToString("D3")}";

                    etiquetas.Add(eti);
                }

                
                result.Objects = new List<object>();
                result.Objects.Add(consecutivos);
                result.Objects.Add(etiquetas);
                result.Correct = true;
            }
            catch (Exception ex)
            {
                result.Correct = false;
                result.Message = $@"Error al armar folios  {ex.Message}";
            }
            return result;
        }
        //Private Inserta registros
        private static ML.Result InsertCab(OdbcConnection connection, OdbcTransaction transaction, ML.ImportationTracking.OrdenCompra ordenCompra, string mode)
        {
            ML.Result result = new ML.Result();
            try
            {
                string query = $@"INSERT INTO hermes_imp_control(no_orden,file,estatus)
                                VALUES ({ordenCompra.NoOrden},'',0)";

                using (OdbcCommand cmd = new OdbcCommand(query, connection, transaction))
                {
                    int rowsAffectd = cmd.ExecuteNonQuery();
                    if (rowsAffectd < 1)
                    {
                        throw new Exception($@"No se inserto la cabecera");
                    }
                }

                result.Correct = true;
            }
            catch (Exception ex)
            {
                result.Correct = false;
                result.Message = $@"Error al insertar la cabecera {ex.Message}";
            }
            return result;
        }
        private static ML.Result InsertDet(OdbcConnection connection, OdbcTransaction transaction, ML.ImportationTracking.OrdenCompra ordenCompra, List<ML.ImportationTracking.Etiqueta> etiquetas, string mode)
        {
            ML.Result result = new ML.Result();
            try
            {
                foreach(ML.ImportationTracking.Etiqueta etiqueta in etiquetas)
                {
                    string query = $@"INSERT INTO hermes_imp_detail(no_orden,etiqueta,bulto,estatus)
                                    VALUES ({ordenCompra.NoOrden},'{etiqueta.NoEtiqueta}','{etiqueta.Bulto}',0)";

                    using (OdbcCommand cmd = new OdbcCommand(query, connection, transaction))
                    {
                        int rowsAffectd = cmd.ExecuteNonQuery();
                        if (rowsAffectd < 1)
                        {
                            throw new Exception($@"No se inserto algun detalle");
                        }
                    }
                }

                result.Correct = true;
            }
            catch (Exception ex)
            {
                result.Correct = false;
                result.Message = $@"Error al insertar la cabecera {ex.Message}";
            }
            return result;
        }
        //Private actualiza folio
        private static ML.Result UpdateConsecutivos(OdbcConnection connection, OdbcTransaction transaction, ML.ImportationTracking.Consecutivos consecutivos, string mode)
        {
            ML.Result result = new ML.Result();
            try
            {
                string query = $@"UPDATE hermes_imp_conta
                                    SET contador = {consecutivos.Consecutivo}
                                    WHERE tipo = '{consecutivos.Prefijo}'";

                using (OdbcCommand cmd = new OdbcCommand(query, connection, transaction))
                {
                    int rowsAffectd = cmd.ExecuteNonQuery();
                    if (rowsAffectd < 1)
                    {
                        throw new Exception($@"No se actualizo el foliador");
                    }
                }

                result.Correct = true;
            }
            catch (Exception ex)
            {
                result.Correct = false;
                result.Message = $@"Error al recuperar folio {ex.Message}";
            }
            return result;
        }


        public static ML.Result PrintOrder(ML.ImportationTracking.PtrInfo ptrInfo, bool reprint, string mode)
        {
            ML.Result resultPrintOrder = new ML.Result();
            try
            {
                ML.Result resultGetDetails = GetDetails(ptrInfo.NoOrden, reprint, mode);
                if (!resultGetDetails.Correct)
                {
                    throw new Exception(resultGetDetails.Message);
                }
                ML.ImportationTracking.OrdenCompra orden = (ML.ImportationTracking.OrdenCompra)resultGetDetails.Objects[0];
                List<ML.ImportationTracking.Etiqueta> etiquetas = (List<ML.ImportationTracking.Etiqueta>)resultGetDetails.Objects[1];

                ML.Result resultBuildDetails = BuildDetails(orden, etiquetas);
                if (!resultBuildDetails.Correct)
                {
                    throw new Exception(resultBuildDetails.Message);
                }
                List<string[]> linesDetail = (List<string[]>)resultBuildDetails.Object;
                orden = null;
                etiquetas = null;

                ML.Result resultCreateLPZ = CreateLPZ(linesDetail, ptrInfo.NoOrden, mode);
                if (!resultCreateLPZ.Correct)
                {
                    throw new Exception(resultCreateLPZ.Message);
                }
                string filePath = (string)resultCreateLPZ.Object;
                string fileName = Path.GetFileName(filePath);
                linesDetail = null;

                ML.Result resultSendDocument = SendDocument(filePath, mode);
                if (!resultSendDocument.Correct)
                {
                    throw new Exception(resultSendDocument.Message);
                }
                
                ML.Result resultUpdate = Update(ptrInfo.NoOrden, fileName, mode);
                if (!resultUpdate.Correct)
                {
                    throw new Exception(resultUpdate.Message);
                }

                ML.Result resultExecutePrint = ExecutePrint(ptrInfo.Ptr, fileName, mode);
                if (!resultExecutePrint.Correct)
                {
                    throw new Exception(resultExecutePrint.Message);
                }
                    

                resultPrintOrder.Correct = true;
                resultPrintOrder.Message = $"Orden {ptrInfo.NoOrden} impresa correctamente";
            }
            catch (Exception ex)
            {
                resultPrintOrder.Correct = false;
                resultPrintOrder.Message = $@"Error en Imprimiendo orden {ptrInfo.NoOrden} {ex.Message}";
            }
            return resultPrintOrder;
        }
        //Private lee datos para imprimir
        private static ML.Result GetDetails(string noOrden, bool reprint, string mode)
        {
            ML.Result result = new ML.Result();
            try
            {
                using (OdbcConnection connection = new OdbcConnection(DL.Connection.GetConnectionStringGen(mode)))
                {
                    connection.Open();
                    string stat = reprint ? "1" : "0";

                    string query = $@"SELECT 
	                                    CASE WHEN (D.no_pedimento IS NULL) THEN 'No registrado' ELSE TRIM(D.no_pedimento) END AS pedimento,
	                                    X.no_orden,
	                                    B.division,
	                                    B.cv_proveedor,
	                                    TRIM(C.razon_social) AS razon_social,
	                                    TRIM(Y.etiqueta) AS etiqueta,
	                                    TRIM(Y.bulto) AS bulto
                                    FROM hermes_imp_control X,hermes_imp_detail Y,
	                                    dblga@lga_prod:lgaindorco A,dblga@lga_prod:lgahorco B,dblga@lga_prod:lgaprovee C, OUTER dblga@lga_prod:lgafolio_pzas D
                                    WHERE X.no_orden = {noOrden}
                                    AND X.estatus IN ({stat})
                                    AND Y.no_orden = X.no_orden
                                    AND A.cod_empresa = 1
                                    AND A.no_orden = X.no_orden
                                    AND B.cod_empresa = A.cod_empresa
                                    AND B.pto_emisor = 999
                                    AND B.no_orden = A.no_orden
                                    AND C.cod_empresa  = B.cod_empresa
                                    AND C.cv_proveedor = B.cv_proveedor
                                    AND D.cod_empresa = A.cod_empresa
                                    AND D.no_folio = A.no_orden
                                    AND D.usuario = 500";

                    
                    ML.ImportationTracking.OrdenCompra orden = new ML.ImportationTracking.OrdenCompra();
                    List<ML.ImportationTracking.Etiqueta> etiquetas = new List<ML.ImportationTracking.Etiqueta>();

                    using (OdbcCommand cmd = new OdbcCommand(query, connection))
                    {
                        using (OdbcDataReader reader = cmd.ExecuteReader())
                        {
                            bool header = false;
                            while (reader.Read())
                            {
                                if (!header)
                                {
                                    orden.Pedimento = reader.GetString(0);
                                    orden.NoOrden = reader.GetString(1);
                                    orden.Division = reader.GetString(2);
                                    orden.IdProveedor = reader.GetString(3);
                                    orden.RazonSocial = reader.GetString(4);

                                    header = true;
                                }
                                ML.ImportationTracking.Etiqueta eti = new ML.ImportationTracking.Etiqueta();

                                eti.NoEtiqueta = reader.GetString(5).Trim();
                                eti.Bulto = reader.GetString(6).Trim();

                                etiquetas.Add(eti);
                            }
                        }
                    }

                    result.Correct = true;
                    result.Objects = new List<object>();
                    result.Objects.Add(orden);
                    result.Objects.Add(etiquetas);
                }
            }
            catch (Exception ex)
            {
                result.Correct = false;
                result.Message = $@"Error al obtener detalles de etiqueta {ex.Message}";
            }
            return result;
        }
        //Traducir
        private static ML.Result BuildDetails(ML.ImportationTracking.OrdenCompra orden, List<ML.ImportationTracking.Etiqueta> etiquetas)
        {
            ML.Result result = new ML.Result();
            try
            {
                List<string[]> linesDetail = new List<string[]>();

                foreach (ML.ImportationTracking.Etiqueta etiqueta in etiquetas)
                {
                    string lin01 = $"^XA";
                    string lin02 = $"^ILFORMATOZEBRA8^FS";
                    string lin03 = $"^PQ0001,0,1,Y";
                    string lin04 = $"^FO50,20^BC,100,N,N^FD{etiqueta.NoEtiqueta}^FS";
                    string lin05 = $"^FO50,140^A0N,25,30^FD{etiqueta.NoEtiqueta}^FS";
                    string lin06 = $"^FO280,170^A0N,25,27^FD{orden.Pedimento}^FS";
                    string lin07 = $"^FO280,200^A0N,25,27^FD{orden.NoOrden}^FS";
                    string lin08 = $"^FO170,230^A0N,25,27^FD{orden.Division}^FS";
                    string lin09 = $"^FO440,230^A0N,25,27^FD{orden.IdProveedor}^FS";
                    string lin10 = $"^FO280,260^A0N,25,27^FD{orden.RazonSocial}^FS";
                    string lin11 = $"^FO280,290^A0N,25,27^FD{etiqueta.Bulto}^FS";
                    string lin12 = $"^XZ";

                    string[] zebraDetail = { lin01, lin02, lin03, lin04, lin05, lin06, lin07, lin08, lin09, lin10, lin11, lin12 };

                    linesDetail.Add(zebraDetail);
                }

                result.Correct = true;
                result.Object = linesDetail;
            }
            catch (Exception ex)
            {
                result.Correct = false;
                result.Message = "Error al armar las lineas " + ex.Message;
            }
            return result;
        }
        //Private genera archivo
        private static ML.Result CreateLPZ(List<string[]> linesDetail, string noOrden, string mode)
        {
            ML.Result result = new ML.Result();
            string pathSalida = DL.Directory.GetOutputPathLPZ(mode);
            try
            {
                string dateTime = DateTime.Now.ToString("yyyyMMddHHmm");
                string fileName = $@"Eti_{noOrden}_{dateTime}.lpz";
                string path = System.IO.Path.Combine(
                                            pathSalida,
                                            fileName
                                        );

                using (StreamWriter writer = new StreamWriter(path))
                {
                    writer.WriteLine("^XA");
                    writer.WriteLine("^MD02");
                    writer.WriteLine("^PR7");
                    writer.WriteLine("^FO50,170^A0N,25,27^FDPedimento:^FS");
                    writer.WriteLine("^FO50,200^A0N,25,27^FDOrden de Compra:^FS");
                    writer.WriteLine("^FO50,230^A0N,25,27^FDDivision:^FS");
                    writer.WriteLine("^FO280,230^A0N,25,27^FDId Proveedor:^FS");
                    writer.WriteLine("^FO50,260^A0N,25,27^FDRazon Social:^FS");
                    writer.WriteLine("^FO50,290^A0N,25,27^FDBulto:^FS");
                    writer.WriteLine("^ISFORMATOZEBRA8,N^FS");
                    writer.WriteLine("^XZ");

                    foreach (string[] detail in linesDetail)
                    {
                        foreach (string line in detail)
                        {
                            writer.WriteLine(line);
                        }
                    }
                }

                result.Correct = true;
                result.Object = path;
            }
            catch (Exception ex)
            {
                result.Correct = false;
                result.Message = $"Error al armar el lpz path {pathSalida} " + ex.Message;
                result.Ex = ex;
            }
            return result;
        }
        //Private envia a sistema archivo
        private static ML.Result SendDocument(string filePath, string mode)
        {
            ML.Result result = new ML.Result();
            string fileName = Path.GetFileName(filePath);
            try
            {

                string ftpUrl = $"ftp://{DL.Connection.GetLegacyIp(mode)}/{DL.Connection.GetInterfacePath(mode).TrimEnd('/')}/{fileName}";

                string fileContentText = File.ReadAllText(filePath);
                fileContentText = fileContentText.Replace("\r", "");
                File.WriteAllText(filePath, fileContentText);

                byte[] fileContents = File.ReadAllBytes(filePath);

                FtpWebRequest request = (FtpWebRequest)WebRequest.Create(ftpUrl);
                request.Method = WebRequestMethods.Ftp.UploadFile;
                request.Credentials = new NetworkCredential(DL.Connection.GetLegacyUser(mode), DL.Connection.GetLegacyPwd(mode));
                request.ContentLength = fileContents.Length;

                using (Stream requestStream = request.GetRequestStream())
                {
                    requestStream.Write(fileContents, 0, fileContents.Length);
                }

                using (FtpWebResponse response = (FtpWebResponse)request.GetResponse())
                {
                    result.Correct = true;
                    result.Object = ftpUrl;
                    File.Delete(filePath);
                }

            }
            catch (Exception ex)
            {
                result.Correct = false;
                result.Message = $@" al enviar el archivo {fileName}: {ex.Message}";
            }

            return result;
        }
        //Private actualiza datos
        private static ML.Result Update(string noOrden, string file, string mode)
        {
            ML.Result result = new ML.Result();
            try
            {
                using (OdbcConnection connection = new OdbcConnection(DL.Connection.GetConnectionStringGen(mode)))
                {
                    connection.Open();

                    string query = $@"UPDATE hermes_imp_control
                                SET estatus = 1,
                                file = '{file}'
                                WHERE no_orden = {noOrden}";

                    
                    using (OdbcCommand cmd = new OdbcCommand(query, connection))
                    {
                        int rowsAffected = cmd.ExecuteNonQuery();
                        if (rowsAffected < 1)
                        {
                            throw new Exception($@"No se realizó la insercion archivo {file}");
                        }
                    }

                    result.Correct = true;
                }
            }
            catch (Exception ex)
            {
                result.Correct = false;
                result.Message = $@"Error al insertar data de seguimiento {ex.Message}";
            }
            return result;
        }
        //Private ejectuta lp -d
        private static ML.Result ExecutePrint(string ptr, string file, string mode)
        {
            ML.Result result = new ML.Result();
            try
            {
                string cmd = $@"lp -d {ptr} {DL.Connection.GetInterfacePath(mode)}{file}";

                string host = DL.Connection.GetLegacyIp(mode);
                string username = DL.Connection.GetLegacyUser(mode);
                string password = DL.Connection.GetLegacyPwd(mode);
                int port = 22;

                using (var client = new Renci.SshNet.SshClient(host, port, username, password))
                {
                    client.Connect();

                    if (client.IsConnected)
                    {
                        // Ejecutar comando
                        var command = client.RunCommand(cmd);

                        // Verificar si hubo error
                        if (!string.IsNullOrEmpty(command.Error))
                        {
                            throw new Exception($"Error en comando: {command.Error}");
                        }

                        result.Correct = true;
                        result.Message = "Impresión ejecutada correctamente";

                        client.Disconnect();
                    }
                    else
                    {
                        throw new Exception("No se pudo conectar al servidor Linux");
                    }
                }
            }
            catch (Exception ex)
            {
                result.Correct = false;
                result.Message = $@"Error al ejecutar impresión {ex.Message}";
            }
            return result;
        }

        public static ML.Result RePrintOrder(ML.ImportationTracking.PtrInfo ptrInfo, string mode)
        {
            ML.Result result = new ML.Result();
            try
            {
                using (OdbcConnection connection = new OdbcConnection(DL.Connection.GetConnectionStringGen(mode)))
                {
                    connection.Open();

                    //PASO 1
                    string queryExist = $@"SELECT COUNT(*)
                                    FROM hermes_imp_control
                                    WHERE estatus = 1
                                    AND no_orden = {ptrInfo.NoOrden}";

                    bool exist = false;

                    using (OdbcCommand cmd = new OdbcCommand(queryExist, connection))
                    {
                        using (OdbcDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                exist = reader.GetInt32(0) > 0 ? true : false; 
                            }
                        }
                    }

                    if (!exist)
                    {
                        throw new Exception($@"La orden {ptrInfo.NoOrden} no está apta para reimpresion");
                    }

                    string query = $@"SELECT A.no_orden,
		                                    CASE WHEN (D.no_pedimento IS NULL) THEN 'No registrado' ELSE TRIM(D.no_pedimento) END AS pedimento,
		                                    A.cd_id,
		                                    B.division,
		                                    B.cv_proveedor,
		                                    TRIM(C.razon_social) AS razon_social,
		                                    A.ind_pvta
                                    FROM dblga@lga_prod:lgaindorco A,dblga@lga_prod:lgahorco B,dblga@lga_prod:lgaprovee C, OUTER dblga@lga_prod:lgafolio_pzas D
                                    WHERE A.cod_empresa = 1
                                    AND A.no_orden = {ptrInfo.NoOrden}
                                    AND B.cod_empresa = A.cod_empresa
                                    AND B.pto_emisor = 999
                                    AND B.no_orden = A.no_orden
                                    AND C.cod_empresa  = B.cod_empresa
                                    AND C.cv_proveedor = B.cv_proveedor
                                    AND D.cod_empresa = A.cod_empresa
                                    AND D.no_folio = A.no_orden
                                    AND D.usuario = 500";

                    ML.ImportationTracking.OrdenCompra orden = new ML.ImportationTracking.OrdenCompra();

                    using (OdbcCommand cmd = new OdbcCommand(query, connection))
                    {
                        using (OdbcDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                orden.NoOrden = reader.GetString(0);
                                orden.Pedimento = reader.GetString(1);
                                orden.AlmacenDestino = reader.GetString(2);
                                orden.Division = reader.GetString(3);
                                orden.IdProveedor = reader.GetString(4);
                                orden.RazonSocial = reader.GetString(5);
                                orden.PasoE = reader.GetString(6) == "X" ? true : false; //Bandera importante si es false, no puede mandar a llamar Generate()
                                
                                result.Message = $@"{ptrInfo.NoOrden} encontrada";
                            }
                            else
                            {
                                orden.Find = false;
                                result.Message = $@"No se encontro la OC {ptrInfo.NoOrden} apta, sin pasos o ya impresa";
                            }
                        }
                    }

                    if (!orden.PasoE)
                    {
                        throw new Exception($@"La orden {ptrInfo.NoOrden} no está apta para reimpresion por paso E");
                    }

                    ML.Result resultPrintOrder = PrintOrder(ptrInfo, true, mode);
                    if (!resultPrintOrder.Correct)
                    {
                        throw new Exception($@"{resultPrintOrder.Message}");
                    }

                    result.Correct = true;
                    result.Message = $@"Re impresion correcta";
                }
            }
            catch (Exception ex)
            {
                result.Correct = false;
                result.Message = $@"Error en reimprimir {ex.Message}";
            }
            return result;
        }

        public static ML.Result GetPrinters(string ptoAlm, string mode)
        {
            ML.Result result = new ML.Result();
            try
            {
                using (OdbcConnection connection = new OdbcConnection(DL.Connection.GetConnectionStringGen(mode)))
                {
                    connection.Open();

                    string queryExist = $@"SELECT imp_nombre
                                            FROM dblga@lga_prod:lgaimpresora
                                            WHERE imp_status = 1
                                            AND cv_almacen = {ptoAlm}
                                            ";

                    var printers = new List<object>();

                    using (OdbcCommand cmd = new OdbcCommand(queryExist, connection))
                    {
                        using (OdbcDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                string ptr = reader.GetString(0);

                                printers.Add(new
                                {
                                    value = ptr,
                                    text = ptr,
                                    description = ptr
                                });
                            }
                        }
                    }

                    result.Correct = true;
                    result.Object = printers;
                }
            }
            catch (Exception ex)
            {
                result.Correct = false;
                result.Message = $@"Error en jalar etiquetas {ex.Message}";
            }
            return result;
        }
    }
}
