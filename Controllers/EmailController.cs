using Microsoft.AspNetCore.Mvc;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace RelatorioAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class EmailController : ControllerBase
    {
        private readonly IConfiguration _config;

        public EmailController(IConfiguration config)
        {
            _config = config;
        }

        [HttpPost]
        public async Task<IActionResult> SendEmail([FromForm] EmailRequest request)
        {
            if (request.File == null || request.File.Length == 0)
                return BadRequest("Arquivo não enviado.");

            try
            {
                // Criar a mensagem
                var message = new MimeMessage();
                message.From.Add(new MailboxAddress("Sistema JB", _config["EmailSettings:Email"]));
                message.To.Add(MailboxAddress.Parse(request.Email));
                message.Subject = $"Relatório de Produção - {request.Supervisor}";

                var builder = new BodyBuilder
                {
                    TextBody = $"Segue em anexo o relatório de produção do supervisor {request.Supervisor}."
                };

                using (var ms = new MemoryStream())
                {
                    await request.File.CopyToAsync(ms);
                    ms.Position = 0;
                    builder.Attachments.Add(request.File.FileName, ms.ToArray());
                }

                message.Body = builder.ToMessageBody();

                // Enviar e-mail
                using var client = new SmtpClient();

                // Somente para desenvolvimento: ignorar erros de certificado autoassinado
                client.ServerCertificateValidationCallback = (s, c, h, e) => true;

                // Conectar usando STARTTLS
                await client.ConnectAsync(_config["EmailSettings:SmtpHost"],
                                          int.Parse(_config["EmailSettings:SmtpPort"]),
                                          SecureSocketOptions.StartTls);

                await client.AuthenticateAsync(_config["EmailSettings:Email"],
                                               _config["EmailSettings:Password"]);

                await client.SendAsync(message);
                await client.DisconnectAsync(true);

                return Ok("E-mail enviado com sucesso!");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Erro ao enviar e-mail: {ex.Message}");
            }
        }
    }

    public class EmailRequest
    {
        public string Supervisor { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public IFormFile? File { get; set; }
    }
}