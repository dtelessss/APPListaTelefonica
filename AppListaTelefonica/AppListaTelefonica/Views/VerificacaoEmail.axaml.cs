using System;
using Avalonia;
using Avalonia.Controls;
using AppListaTelefonica.Services;
using System.Threading.Tasks;

namespace AppListaTelefonica.Views;

public partial class VerificacaoEmailWindow : Window
{
    private readonly AuthService _auth;
    private readonly string _email;

    public VerificacaoEmailWindow(string email, Guid? userId)
    {
        InitializeComponent();
        _auth = new AuthService();
        _email = email;

        var emailText = this.FindControl<TextBlock>("EmailText");
        if (emailText != null) emailText.Text = email;

        var verifyBtn = this.FindControl<Button>("VerificarButton");
        if (verifyBtn != null) verifyBtn.Click += OnVerifyClick;

        var resendBtn = this.FindControl<Button>("ReenviarButton");
        if (resendBtn != null) resendBtn.Click += OnResendClick;
    }

    private async void OnVerifyClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        var codigoBox = this.FindControl<TextBox>("CodigoBox");
        var errorBorder = this.FindControl<Border>("ErrorBorder");
        var errorText = this.FindControl<TextBlock>("ErrorText");

        string codigo = codigoBox?.Text ?? "";

        if (codigo.Length != 6)
        {
            MostrarErro(errorBorder, errorText, "Digite o código de 6 dígitos");
            return;
        }

        var resultado = await _auth.VerificarEmail(_email, codigo);

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

    private async void OnResendClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        var errorBorder = this.FindControl<Border>("ErrorBorder");
        var errorText = this.FindControl<TextBlock>("ErrorText");

        var resultado = await _auth.ReenviarCodigoVerificacao(_email);
        MostrarErro(errorBorder, errorText, resultado.Mensagem);
    }

    private void MostrarErro(Border? border, TextBlock? text, string message)
    {
        if (border == null || text == null) return;
        text.Text = message;
        border.IsVisible = true;

        // Esconder após 4 segundos (opcional)
        Task.Run(async () =>
        {
            await Task.Delay(4000);
            Avalonia.Threading.Dispatcher.UIThread.Post(() => border.IsVisible = false);
        });
    }
}