using System;
using System.Threading.Tasks;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using MimeKit.Text;

namespace AppListaTelefonica.Services;

public class EmailService
{
    private readonly DatabaseService _db;
    
    public EmailService()
    {
        _db = new DatabaseService();
    }
    
    private async Task<(string Servidor, int Porta, string Utilizador, string Password)?> ObterConfiguracaoSmtpAsync()
    {
        var config = await _db.ObterConfiguracoesApp();
        
        if (config == null)
        {
            System.Diagnostics.Debug.WriteLine("EmailService: Configurações SMTP não encontradas na BD!");
            return null;
        }
        
        System.Diagnostics.Debug.WriteLine($"EmailService: Config SMTP carregada - Servidor: {config.ServidorSmtp}:{config.PortaSmtp}, User: {config.UtilizadorSmtp}");
        return (config.ServidorSmtp, config.PortaSmtp, config.UtilizadorSmtp, config.PalavraPasseSmtp);
    }
    
    private async Task<bool> EnviarEmailAsync(string destinatario, string assunto, string corpoHtml)
    {
        try
        {
            var configSmtp = await ObterConfiguracaoSmtpAsync();
            
            if (configSmtp == null)
            {
                System.Diagnostics.Debug.WriteLine("EmailService: Config SMTP nula. Email NÃO enviado.");
                return false;
            }
            
            var (servidor, porta, utilizador, password) = configSmtp.Value;
            
            var mensagem = new MimeMessage();
            mensagem.From.Add(new MailboxAddress("Lista Telefónica", utilizador));
            mensagem.To.Add(new MailboxAddress("", destinatario));
            mensagem.Subject = assunto;
            
            mensagem.Body = new TextPart(TextFormat.Html)
            {
                Text = corpoHtml
            };
            
            using var cliente = new SmtpClient();
            
            // Em produção, REMOVER esta linha para validar certificados SSL
            cliente.ServerCertificateValidationCallback = (s, c, h, e) => true;
            
            System.Diagnostics.Debug.WriteLine($"EmailService: A conectar a {servidor}:{porta}...");
            await cliente.ConnectAsync(servidor, porta, SecureSocketOptions.StartTls);
            
            System.Diagnostics.Debug.WriteLine($"EmailService: A autenticar como {utilizador}...");
            await cliente.AuthenticateAsync(utilizador, password);
            
            System.Diagnostics.Debug.WriteLine($"EmailService: A enviar para {destinatario}...");
            await cliente.SendAsync(mensagem);
            
            await cliente.DisconnectAsync(true);
            
            System.Diagnostics.Debug.WriteLine("EmailService: Email enviado com SUCESSO!");
            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"EmailService: ERRO - {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"EmailService: StackTrace - {ex.StackTrace}");
            return false;
        }
    }
    
    public async Task<bool> EnviarCodigoVerificacaoEmailAsync(string email, string codigo)
    {
        string assunto = "Código de Verificação - Lista Telefónica";
        
        string corpoHtml = $@"
            <html>
            <head>
                <style>
                    body {{ font-family: Arial, sans-serif; background-color: #f4f4f4; margin: 0; padding: 0; }}
                    .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
                    .header {{ background-color: #2c3e50; color: white; padding: 20px; text-align: center; }}
                    .content {{ background-color: white; padding: 30px; border-radius: 5px; margin-top: 20px; }}
                    .codigo {{ font-size: 32px; font-weight: bold; text-align: center; color: #2c3e50; 
                               padding: 20px; background-color: #ecf0f1; border-radius: 5px; letter-spacing: 5px; }}
                    .footer {{ text-align: center; margin-top: 20px; color: #7f8c8d; font-size: 12px; }}
                    .aviso {{ color: #e74c3c; font-size: 12px; margin-top: 20px; }}
                </style>
            </head>
            <body>
                <div class='container'>
                    <div class='header'>
                        <h1>Verificação de Email</h1>
                    </div>
                    <div class='content'>
                        <p>Olá!</p>
                        <p>Obrigado por se registar na <strong>Lista Telefónica</strong>.</p>
                        <p>Para verificar o seu endereço de email, utilize o seguinte código:</p>
                        
                        <div class='codigo'>{codigo}</div>
                        
                        <p>Este código é válido por <strong>15 minutos</strong>.</p>
                        
                        <div class='aviso'>
                            <p>⚠️ Se não solicitou este registo, por favor ignore este email.</p>
                            <p>Nunca partilhe este código com ninguém.</p>
                        </div>
                    </div>
                    <div class='footer'>
                        <p>© {DateTime.Now.Year} Lista Telefónica. Todos os direitos reservados.</p>
                        <p>Este é um email automático. Por favor, não responda.</p>
                    </div>
                </div>
            </body>
            </html>";
        
        return await EnviarEmailAsync(email, assunto, corpoHtml);
    }
    
    public async Task<bool> EnviarCodigoRecuperacaoPasswordAsync(string email, string codigo)
    {
        string assunto = "Recuperação de Password - Lista Telefónica";
        
        string corpoHtml = $@"
            <html>
            <head>
                <style>
                    body {{ font-family: Arial, sans-serif; background-color: #f4f4f4; margin: 0; padding: 0; }}
                    .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
                    .header {{ background-color: #e67e22; color: white; padding: 20px; text-align: center; }}
                    .content {{ background-color: white; padding: 30px; border-radius: 5px; margin-top: 20px; }}
                    .codigo {{ font-size: 32px; font-weight: bold; text-align: center; color: #e67e22; 
                               padding: 20px; background-color: #fef5e7; border-radius: 5px; letter-spacing: 5px; }}
                    .footer {{ text-align: center; margin-top: 20px; color: #7f8c8d; font-size: 12px; }}
                    .aviso {{ color: #e74c3c; font-size: 12px; margin-top: 20px; }}
                </style>
            </head>
            <body>
                <div class='container'>
                    <div class='header'>
                        <h1>Recuperação de Password</h1>
                    </div>
                    <div class='content'>
                        <p>Olá!</p>
                        <p>Recebemos um pedido de recuperação de password para a sua conta na <strong>Lista Telefónica</strong>.</p>
                        <p>Utilize o seguinte código para redefinir a sua password:</p>
                        
                        <div class='codigo'>{codigo}</div>
                        
                        <p>Este código é válido por <strong>15 minutos</strong>.</p>
                        
                        <div class='aviso'>
                            <p>⚠️ Se não solicitou a recuperação de password, por favor ignore este email.</p>
                            <p>Nunca partilhe este código com ninguém.</p>
                        </div>
                    </div>
                    <div class='footer'>
                        <p>© {DateTime.Now.Year} Lista Telefónica. Todos os direitos reservados.</p>
                        <p>Este é um email automático. Por favor, não responda.</p>
                    </div>
                </div>
            </body>
            </html>";
        
        return await EnviarEmailAsync(email, assunto, corpoHtml);
    }
    
    public async Task<bool> EnviarNotificacaoPasswordAlteradaAsync(string email)
    {
        string assunto = "Password Alterada - Lista Telefónica";
        
        string corpoHtml = $@"
            <html>
            <head>
                <style>
                    body {{ font-family: Arial, sans-serif; background-color: #f4f4f4; margin: 0; padding: 0; }}
                    .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
                    .header {{ background-color: #27ae60; color: white; padding: 20px; text-align: center; }}
                    .content {{ background-color: white; padding: 30px; border-radius: 5px; margin-top: 20px; }}
                    .footer {{ text-align: center; margin-top: 20px; color: #7f8c8d; font-size: 12px; }}
                    .aviso {{ color: #e74c3c; font-size: 12px; margin-top: 20px; }}
                </style>
            </head>
            <body>
                <div class='container'>
                    <div class='header'>
                        <h1>Password Alterada</h1>
                    </div>
                    <div class='content'>
                        <p>Olá!</p>
                        <p>A password da sua conta na <strong>Lista Telefónica</strong> foi alterada com sucesso.</p>
                        <p><strong>Data e hora:</strong> {DateTime.Now:dd/MM/yyyy HH:mm}</p>
                        
                        <div class='aviso'>
                            <p>⚠️ Se não foi você que alterou a password, contacte imediatamente o administrador do sistema.</p>
                        </div>
                    </div>
                    <div class='footer'>
                        <p>© {DateTime.Now.Year} Lista Telefónica. Todos os direitos reservados.</p>
                        <p>Este é um email automático. Por favor, não responda.</p>
                    </div>
                </div>
            </body>
            </html>";
        
        return await EnviarEmailAsync(email, assunto, corpoHtml);
    }
    
    public async Task<bool> EnviarNotificacaoBoasVindasAsync(string email, bool isAdmin)
    {
        string assunto = "Bem-vindo à Lista Telefónica";
        string cargo = isAdmin ? "Administrador" : "Utilizador";
        
        string corpoHtml = $@"
            <html>
            <head>
                <style>
                    body {{ font-family: Arial, sans-serif; background-color: #f4f4f4; margin: 0; padding: 0; }}
                    .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
                    .header {{ background-color: #3498db; color: white; padding: 20px; text-align: center; }}
                    .content {{ background-color: white; padding: 30px; border-radius: 5px; margin-top: 20px; }}
                    .badge {{ display: inline-block; background-color: #2ecc71; color: white; padding: 5px 15px; 
                              border-radius: 20px; font-weight: bold; }}
                    .footer {{ text-align: center; margin-top: 20px; color: #7f8c8d; font-size: 12px; }}
                </style>
            </head>
            <body>
                <div class='container'>
                    <div class='header'>
                        <h1>Bem-vindo!</h1>
                    </div>
                    <div class='content'>
                        <p>Olá!</p>
                        <p>A sua conta na <strong>Lista Telefónica</strong> foi criada com sucesso.</p>
                        <p>Tipo de conta: <span class='badge'>{cargo}</span></p>
                        <p>Para começar a usar a aplicação, verifique o seu email com o código que lhe enviamos separadamente.</p>
                        <p>Obrigado por se juntar a nós!</p>
                    </div>
                    <div class='footer'>
                        <p>© {DateTime.Now.Year} Lista Telefónica. Todos os direitos reservados.</p>
                    </div>
                </div>
            </body>
            </html>";
        
        return await EnviarEmailAsync(email, assunto, corpoHtml);
    }
}