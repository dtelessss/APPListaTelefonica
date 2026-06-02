using Avalonia.Controls;
using Avalonia.Interactivity;
using System;

namespace AppListaTelefonica.Views;

public partial class RecuperacaoPasswordWindow : Window
{
    private readonly Services.AuthService _authService;
    private string? _emailRecuperacao;
    
    public RecuperacaoPasswordWindow()
    {
        InitializeComponent();
        _authService = new Services.AuthService();
    }
    
    private async void BtnSolicitar_Click(object? sender, RoutedEventArgs e)
    {
        EsconderMensagensSolicitar();
        DesabilitarBotoesSolicitar();
        
        string email = TxtEmail.Text?.Trim() ?? "";
        
        if (string.IsNullOrWhiteSpace(email))
        {
            MostrarErroSolicitar("Por favor, introduza o seu email.");
            HabilitarBotoesSolicitar();
            return;
        }
        
        var (sucesso, mensagemErro) = await _authService.SolicitarRecuperacaoPasswordAsync(email);
        
        if (sucesso)
        {
            _emailRecuperacao = email;
            MostrarSucessoSolicitar(mensagemErro ?? "Codigo enviado!");
            
            var timer = new Avalonia.Threading.DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(1.5);
            timer.Tick += (s, args) => 
            { 
                timer.Stop(); 
                PainelSolicitar.IsVisible = false;
                PainelNovaPassword.IsVisible = true;
            };
            timer.Start();
        }
        else
        {
            MostrarErroSolicitar(mensagemErro ?? "Erro ao enviar codigo.");
            HabilitarBotoesSolicitar();
        }
    }
    
    private async void BtnAlterarPassword_Click(object? sender, RoutedEventArgs e)
    {
        EsconderMensagensNova();
        DesabilitarBotoesNova();
        
        string codigo = TxtCodigo.Text?.Trim() ?? "";
        string novaPassword = TxtNovaPassword.Text ?? "";
        string confirmarPassword = TxtConfirmarPassword.Text ?? "";
        
        if (string.IsNullOrWhiteSpace(codigo) || string.IsNullOrWhiteSpace(novaPassword) || string.IsNullOrWhiteSpace(confirmarPassword))
        {
            MostrarErroNova("Por favor, preencha todos os campos.");
            HabilitarBotoesNova();
            return;
        }
        
        if (novaPassword != confirmarPassword)
        {
            MostrarErroNova("As palavras-passe nao coincidem.");
            HabilitarBotoesNova();
            return;
        }
        
        if (string.IsNullOrEmpty(_emailRecuperacao))
        {
            MostrarErroNova("Erro: email de recuperacao nao encontrado. Recomece o processo.");
            HabilitarBotoesNova();
            return;
        }
        
        var (sucesso, mensagemErro) = await _authService.RecuperarPasswordAsync(_emailRecuperacao, codigo, novaPassword);
        
        if (sucesso)
        {
            MostrarSucessoNova("Password alterada com sucesso! Ja pode fazer login.");
            
            var timer = new Avalonia.Threading.DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(2);
            timer.Tick += (s, args) => { timer.Stop(); this.Close(); };
            timer.Start();
        }
        else
        {
            MostrarErroNova(mensagemErro ?? "Erro ao alterar password.");
            HabilitarBotoesNova();
        }
    }
    
    private void BtnCancelar_Click(object? sender, RoutedEventArgs e)
    {
        this.Close();
    }
    
    // Helpers Painel Solicitar
    private void MostrarErroSolicitar(string msg) { TxtErroSolicitar.Text = msg; BorderErroSolicitar.IsVisible = true; }
    private void MostrarSucessoSolicitar(string msg) { TxtSucessoSolicitar.Text = msg; BorderSucessoSolicitar.IsVisible = true; }
    private void EsconderMensagensSolicitar() { BorderErroSolicitar.IsVisible = false; BorderSucessoSolicitar.IsVisible = false; }
    private void DesabilitarBotoesSolicitar() { BtnSolicitar.IsEnabled = false; BtnCancelar1.IsEnabled = false; }
    private void HabilitarBotoesSolicitar() { BtnSolicitar.IsEnabled = true; BtnCancelar1.IsEnabled = true; }
    
    // Helpers Painel Nova Password
    private void MostrarErroNova(string msg) { TxtErroNova.Text = msg; BorderErroNova.IsVisible = true; }
    private void MostrarSucessoNova(string msg) { TxtSucessoNova.Text = msg; BorderSucessoNova.IsVisible = true; }
    private void EsconderMensagensNova() { BorderErroNova.IsVisible = false; BorderSucessoNova.IsVisible = false; }
    private void DesabilitarBotoesNova() { BtnAlterarPassword.IsEnabled = false; BtnCancelar2.IsEnabled = false; }
    private void HabilitarBotoesNova() { BtnAlterarPassword.IsEnabled = true; BtnCancelar2.IsEnabled = true; }
}