using ML.CartaPorte;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;

namespace BL.CartaPorte
{
    public static class EmailHelper
    {
        public static async Task EnviarEmailConCSVs(string csvIntTim24, string csvDomTim2, string folio, string operador, EmailConfiguration emailConfig)
        {
            try
            {
                //System.Console.WriteLine($"[ENVIAR EMAIL] Preparando envío de email con CSVs para folio: {folio}");

                // Configuración del email desde parámetro
                var smtpServer = emailConfig.SmtpServer;
                var emailFrom = emailConfig.FromEmail;

                // Determinar destinatario principal según el operador desde configuración
                string emailTo;
                if (emailConfig.Operators != null && emailConfig.Operators.ContainsKey(operador))
                {
                    emailTo = emailConfig.Operators[operador];
                }
                else
                {
                    emailTo = emailConfig.DefaultEmail ?? "benitezem@globalhitss.com"; // Fallback
                    //System.Console.WriteLine($"[ENVIAR EMAIL] Operador '{operador}' no encontrado en configuración, usando email por defecto: {emailTo}");
                }

                // Emails de copia desde configuración
                var emailsCopia = emailConfig.CopyEmails ?? new List<string>();

                using (var smtpClient = new SmtpClient(smtpServer))
                {
                    smtpClient.UseDefaultCredentials = true;

                    var message = new MailMessage();
                    message.From = new MailAddress(emailFrom);
                    message.Subject = $"Carta Porte Proveedor - Folio {folio} - Operador {operador}";

                    // Agregar destinatario principal
                    message.To.Add(emailTo);

                    // Agregar destinatarios de copia
                    foreach (var emailCopia in emailsCopia)
                    {
                        message.CC.Add(emailCopia);
                    }

                    // Crear el cuerpo del mensaje en HTML
                    var cuerpoMensaje = $@"
                        <html>
                        <head>
                            <title>Carta Porte Proveedor</title>
                        </head>
                        <body style='font-family: Arial, sans-serif; margin: 0; padding: 0; background-color: #f0f0f0;'>
                            <div style='max-width: 600px; margin: 0 auto; background-color: #ffffff; border-radius: 8px; overflow: hidden;'>
                                <div style='background-color: #1e3a8a; height: 20px;'></div>
                                <div style='padding: 20px;'>
                                    <h3 style='color: #333333;'>Carta Porte Proveedor - Folio {folio}</h3>
                                    <p style='color: #666666;'>Se adjuntan los archivos para la entrega del folio <strong>{folio}</strong> y operador <strong>{operador}</strong>.</p>
                                    <ul style='color: #666666;'>
                                        <li>Datos de mercancía (CSV adjunto)</li>
                                        <li>Datos de domicilios (CSV adjunto)</li>
                                    </ul>
                                    <p style='color: #666666;'>Fecha de envío: {DateTime.Now:yyyy-MM-dd HH:mm:ss}</p>
                                    <br>
                                    <p style='color: #666666;'>Atentamente,</p>
                                    <p style='color: #666666;'>Sears</p>
                                </div>
                            </div>
                        </body>
                        </html>";

                    message.IsBodyHtml = true;
                    message.Body = cuerpoMensaje;

                    // Adjuntar CSV int_tim24
                    var intTim24Bytes = System.Text.Encoding.UTF8.GetBytes(csvIntTim24);
                    var intTim24Attachment = new Attachment(new MemoryStream(intTim24Bytes), $"Mercancias_{folio}_{DateTime.Now:yyyyMMdd_HHmmss}.csv", "text/csv");
                    message.Attachments.Add(intTim24Attachment);

                    // Adjuntar CSV dom_tim2
                    var domTim2Bytes = System.Text.Encoding.UTF8.GetBytes(csvDomTim2);
                    var domTim2Attachment = new Attachment(new MemoryStream(domTim2Bytes), $"Domicilios_{folio}_{DateTime.Now:yyyyMMdd_HHmmss}.csv", "text/csv");
                    message.Attachments.Add(domTim2Attachment);

                    //System.Console.WriteLine($"[ENVIAR EMAIL] Intentando enviar email desde {emailFrom} a {emailTo}");
                    await Task.Run(() => smtpClient.Send(message));

                    var todosDestinatarios = new List<string> { emailTo };
                    todosDestinatarios.AddRange(emailsCopia);
                    //System.Console.WriteLine($"[ENVIAR EMAIL] Email enviado exitosamente a: {string.Join(", ", todosDestinatarios)}");
                }
            }
            catch (Exception ex)
            {
                //System.Console.WriteLine($"[ENVIAR EMAIL] Error al enviar email: {ex.Message}");
                //System.Console.WriteLine($"[ENVIAR EMAIL] Stack trace: {ex.StackTrace}");
                throw;
            }
        }
    }
}
