using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using AppListaTelefonica.Services;

namespace AppListaTelefonica.Views;

public partial class RecuperacaoPasswordWindow : Window
{
    private readonly AuthService _auth;
    private readonly string _email;

    public RecuperacaoPasswordWindow(string email)
    {
        InitializeComponent();
        _auth = new AuthService();
        _email = email;

        var recuperarBtn = this.FindControl<Button>("RecuperarButton");
        if (recuperarBtn != null) recuperarBtn.Click += OnRecuperarClick;

        var novaPasswordBox = this.FindControl<PasswordBox>("NovaPasswordBox");
        if (novaPasswordBox != null) novaPasswordBox.TextChanged += OnPasswordStrengthChanged;
    }

    private void OnPasswordStrengthChanged(object? sender, TextChangedEventArgs e)
    {
        var passwordBox = this.FindControl<PasswordBox>("NovaPasswordBox");
        var strengthBar = this.FindControl<ProgressBar>("StrengthBar");
        var strengthText = this.FindControl<TextBlock>("StrengthText");

        if (strengthBar == null || strengthText == null) return;

        string password = passwordBox?.Password ?? "";
        int strength = CalcularForcaPalavraPasse(password);
        strengthBar.Value = strength;

        if (strength < 40)
            strengthText.Text = "Fraca - Use 12+ caracteres, maiúsculas e números";
        else if (strength < 70)
            strengthText.Text = "Média - Adicione caracteres especiais";
        else
            strengthText.Text = "Forte - Palavra-passe segura";
    }

    private int CalcularForcaPalavraPasse(string password)
    {
        if (string.IsNullOrEmpty(password)) return 0;

        int score = 0;
        if (password.Length >= 12) score += 30;
        if (System.Text.RegularExpressions.Regex.IsMatch(password, "[A-Z]")) score += 20;
        if (System.Text.RegularExpressions.Regex.IsMatch(password, "[a-z]")) score += 10;
        if (System.Text.RegularExpressions.Regex.IsMatch(password, "[0-9]")) score += 20;
        if (System.Text.RegularExpressions.Regex.IsMatch(password, "[^a-zA-Z0-9]")) score += 20;

        return Math.Min(100, score);
    }

    private async void OnRecuperarClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        var codigoBox = this.FindControl<TextBox>("CodigoBox");
        var novaPassBox = this.FindControl<PasswordBox>("NovaPasswordBox");
        var confirmBox = this.FindControl<PasswordBox>("ConfirmPasswordBox");
        var errorBorder = this.FindControl<Border>("ErrorBorder");
        var errorText = this.FindControl<TextBlock>("ErrorText");

        string codigo = codigoBox?.Text ?? "";
        string novaPassword = novaPassBox?.Password ?? "";
        string confirm = confirmBox?.Password ?? "";

        if (codigo.Length != 6)
        {
            MostrarErro(errorBorder, errorText, "Digite o código de 6 dígitos");
            return;
        }

        if (novaPassword.Length < 12)
        {
            MostrarErro(errorBorder, errorText, "Palavra-passe deve ter pelo menos 12 caracteres");
            return;
        }

        if (novaPassword != confirm)
        {
            MostrarErro(errorBorder, errorText, "As palavras-passe não coincidem");
            return;
        }

        var resultado = await _auth.RedefinirPalavraPasse(_email, codigo, novaPassword);

        if (resultado.Sucesso)
        {
            var loginWindow = new LoginWindow();
            loginWindow.Show();
            Close();
        }
        else
        {
            MostrarErro(errorBorder, errorText, resultado.Mensagem);
        }
    }

    private void MostrarErro(Border? border, TextBlock? text, string message)
    {
        if (border == null || text == null) return;
        text.Text = message;
        border.IsVisible = true;
    }
}