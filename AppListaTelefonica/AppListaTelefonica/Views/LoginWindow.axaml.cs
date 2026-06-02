using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;
using AppListaTelefonica.Services;
using AppListaTelefonica.Models;

namespace AppListaTelefonica.Views;

public partial class LoginWindow : Window
{
    private readonly AuthService _auth;
    private readonly ConfiguracaoService _configService;
    private DateTime? _ultimoLogin;

    public LoginWindow()
    {
        InitializeComponent();
        _auth = new AuthService();
        _configService = new ConfiguracaoService();

        // Associar eventos
        var loginBtn = this.FindControl<Button>("LoginButton");
        if (loginBtn != null) loginBtn.Click += OnLoginClick;

        var registerBtn = this.FindControl<Button>("RegisterButton");
        if (registerBtn != null) registerBtn.Click += OnRegisterClick;

        var forgotBtn = this.FindControl<Button>("ForgotPasswordButton");
        if (forgotBtn != null) forgotBtn.Click += OnForgotPasswordClick;

        var regPasswordBox = this.FindControl<PasswordBox>("RegPasswordBox");
        if (regPasswordBox != null) regPasswordBox.TextChanged += OnPasswordStrengthChanged;

        // Tentar sessão automática
        _ = TentarSessaoAutomatica();
    }

    private async Task TentarSessaoAutomatica()
    {
        var emailGuardado = ObterEmailGuardado();
        var ultimoLoginGuardado = ObterUltimoLoginGuardado();

        if (string.IsNullOrEmpty(emailGuardado) || !ultimoLoginGuardado.HasValue)
            return;

        // Obter duração da sessão da base de dados
        int duracaoSessaoDias = await _configService.ObterDuracaoSessaoDias();

        // Verificar se a sessão ainda é válida
        if ((DateTime.UtcNow - ultimoLoginGuardado.Value).TotalDays <= duracaoSessaoDias)
        {
            // Preencher email automaticamente
            var emailBox = this.FindControl<TextBox>("LoginEmailBox");
            if (emailBox != null) emailBox.Text = emailGuardado;

            // Opcional: fazer login automático (podes descomentar se quiseres)
            // var passBox = this.FindControl<PasswordBox>("LoginPasswordBox");
            // if (passBox != null && !string.IsNullOrEmpty(passBox.Password))
            // {
            //     await FazerLoginAutomatico(emailGuardado, passBox.Password);
            // }
        }
    }

    private string ObterEmailGuardado()
    {
        // Usar uma configuração simples em memória ou ficheiro
        // Como não queres Preferences, podes guardar num ficheiro simples
        var filePath = System.IO.Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "SGLT", "last_email.txt");

        if (System.IO.File.Exists(filePath))
            return System.IO.File.ReadAllText(filePath);

        return "";
    }

    private void GuardarEmail(string email)
    {
        var dirPath = System.IO.Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "SGLT");

        if (!System.IO.Directory.Exists(dirPath))
            System.IO.Directory.CreateDirectory(dirPath);

        var filePath = System.IO.Path.Combine(dirPath, "last_email.txt");
        System.IO.File.WriteAllText(filePath, email);
    }

    private DateTime? ObterUltimoLoginGuardado()
    {
        var filePath = System.IO.Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "SGLT", "last_login.txt");

        if (System.IO.File.Exists(filePath))
        {
            if (DateTime.TryParse(System.IO.File.ReadAllText(filePath), out DateTime lastLogin))
                return lastLogin;
        }
        return null;
    }

    private void GuardarUltimoLogin(DateTime data)
    {
        var dirPath = System.IO.Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "SGLT");

        if (!System.IO.Directory.Exists(dirPath))
            System.IO.Directory.CreateDirectory(dirPath);

        var filePath = System.IO.Path.Combine(dirPath, "last_login.txt");
        System.IO.File.WriteAllText(filePath, data.ToString("o"));
    }

    private void OnPasswordStrengthChanged(object? sender, TextChangedEventArgs e)
    {
        var passwordBox = this.FindControl<PasswordBox>("RegPasswordBox");
        var strengthBar = this.FindControl<ProgressBar>("StrengthBar");
        var strengthText = this.FindControl<TextBlock>("StrengthText");

        if (strengthBar == null || strengthText == null) return;

        string password = passwordBox?.Password ?? "";
        int strength = CalcularForcaPalavraPasse(password);
        strengthBar.Value = strength;

        if (strength < 40)
        {
            strengthText.Text = "Fraca - Use 12+ caracteres, maiúsculas e números";
            strengthText.Foreground = new SolidColorBrush(Color.Parse("#EF4444"));
        }
        else if (strength < 70)
        {
            strengthText.Text = "Média - Adicione caracteres especiais";
            strengthText.Foreground = new SolidColorBrush(Color.Parse("#F59E0B"));
        }
        else
        {
            strengthText.Text = "Forte - Palavra-passe segura";
            strengthText.Foreground = new SolidColorBrush(Color.Parse("#10B981"));
        }
    }

    private int CalcularForcaPalavraPasse(string password)
    {
        if (string.IsNullOrEmpty(password)) return 0;

        int score = 0;
        if (password.Length >= 12) score += 30;
        if (System.Text.RegularExpressions.Regex.IsMatch(password, "[A-Z]")) score += 25;
        if (System.Text.RegularExpressions.Regex.IsMatch(password, "[a-z]")) score += 10;
        if (System.Text.RegularExpressions.Regex.IsMatch(password, "[0-9]")) score += 15;
        if (System.Text.RegularExpressions.Regex.IsMatch(password, "[^a-zA-Z0-9]")) score += 20;

        return Math.Min(100, score);
    }

    private async void OnLoginClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        var emailBox = this.FindControl<TextBox>("LoginEmailBox");
        var passBox = this.FindControl<PasswordBox>("LoginPasswordBox");
        var errorBorder = this.FindControl<Border>("LoginErrorBorder");
        var errorText = this.FindControl<TextBlock>("LoginErrorText");
        var loginBtn = this.FindControl<Button>("LoginButton");

        string email = emailBox?.Text ?? "";
        string password = passBox?.Password ?? "";

        if (string.IsNullOrWhiteSpace(email))
        {
            ShowError(errorBorder, errorText, "Insira o email");
            return;
        }

        if (string.IsNullOrWhiteSpace(password))
        {
            ShowError(errorBorder, errorText, "Insira a palavra-passe");
            return;
        }

        if (loginBtn != null) loginBtn.IsEnabled = false;

        var resultado = await _auth.Login(email, password);

        if (resultado.Sucesso)
        {
            // Guardar email e último login para sessão persistente
            GuardarEmail(email);
            GuardarUltimoLogin(DateTime.UtcNow);

            var mainWindow = new MainWindow();
            mainWindow.Show();
            Close();
        }
        else
        {
            ShowError(errorBorder, errorText, resultado.Mensagem);
            if (loginBtn != null) loginBtn.IsEnabled = true;
        }
    }

    private async void OnRegisterClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        var emailBox = this.FindControl<TextBox>("RegEmailBox");
        var passBox = this.FindControl<PasswordBox>("RegPasswordBox");
        var confirmBox = this.FindControl<PasswordBox>("RegConfirmPasswordBox");
        var unidadeBox = this.FindControl<TextBox>("RegUnidadeBox");
        var errorBorder = this.FindControl<Border>("RegErrorBorder");
        var errorText = this.FindControl<TextBlock>("RegErrorText");
        var successBorder = this.FindControl<Border>("RegSuccessBorder");
        var registerBtn = this.FindControl<Button>("RegisterButton");

        if (errorBorder != null) errorBorder.IsVisible = false;
        if (successBorder != null) successBorder.IsVisible = false;

        string email = emailBox?.Text ?? "";
        string password = passBox?.Password ?? "";
        string confirm = confirmBox?.Password ?? "";
        string unidade = unidadeBox?.Text ?? "";

        if (string.IsNullOrWhiteSpace(email))
        {
            ShowError(errorBorder, errorText, "Insira o email");
            return;
        }

        if (!email.Contains("@") || !email.Contains("."))
        {
            ShowError(errorBorder, errorText, "Email inválido");
            return;
        }

        if (string.IsNullOrWhiteSpace(password))
        {
            ShowError(errorBorder, errorText, "Insira a palavra-passe");
            return;
        }

        if (password.Length < 12)
        {
            ShowError(errorBorder, errorText, "A palavra-passe deve ter pelo menos 12 caracteres");
            return;
        }

        if (password != confirm)
        {
            ShowError(errorBorder, errorText, "As palavras-passe não coincidem");
            return;
        }

        if (CalcularForcaPalavraPasse(password) < 50)
        {
            ShowError(errorBorder, errorText, "Palavra-passe muito fraca. Use maiúsculas, números e caracteres especiais");
            return;
        }

        if (registerBtn != null) registerBtn.IsEnabled = false;

        var resultado = await _auth.Registar(email, password, string.IsNullOrWhiteSpace(unidade) ? null : unidade);

        if (resultado.Sucesso)
        {
            if (successBorder != null)
            {
                var successText = this.FindControl<TextBlock>("RegSuccessText");
                if (successText != null) successText.Text = resultado.Mensagem;
                successBorder.IsVisible = true;
            }

            // Limpar campos
            if (emailBox != null) emailBox.Text = "";
            if (passBox != null) passBox.Text = "";
            if (confirmBox != null) confirmBox.Text = "";
            if (unidadeBox != null) unidadeBox.Text = "";

            var strengthBar = this.FindControl<ProgressBar>("StrengthBar");
            if (strengthBar != null) strengthBar.Value = 0;

            // Abrir janela de verificação
            await Task.Delay(2000);
            var verificationWindow = new VerificacaoEmailWindow(email, resultado.UtilizadorId);
            verificationWindow.Show();
            Close();
        }
        else
        {
            ShowError(errorBorder, errorText, resultado.Mensagem);
            if (registerBtn != null) registerBtn.IsEnabled = true;
        }
    }

    private async void OnForgotPasswordClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        var emailBox = this.FindControl<TextBox>("LoginEmailBox");
        var errorBorder = this.FindControl<Border>("LoginErrorBorder");
        var errorText = this.FindControl<TextBlock>("LoginErrorText");

        string email = emailBox?.Text ?? "";

        if (string.IsNullOrWhiteSpace(email))
        {
            ShowError(errorBorder, errorText, "Insira o email para recuperar a palavra-passe");
            return;
        }

        var resultado = await _auth.SolicitarRecuperacaoPalavraPasse(email);

        if (resultado.Sucesso)
        {
            var recoveryWindow = new RecuperacaoPasswordWindow(email);
            await recoveryWindow.ShowDialog(this);
        }
        else
        {
            ShowError(errorBorder, errorText, resultado.Mensagem);
        }
    }

    private void ShowError(Border? border, TextBlock? text, string message)
    {
        if (border == null || text == null) return;
        text.Text = message;
        border.IsVisible = true;

        Dispatcher.UIThread.Post(async () =>
        {
            await Task.Delay(4000);
            border.IsVisible = false;
        });
    }
}