using System.ComponentModel;
using System.Windows;
using Almoxarifado.DataBase;
using Dapper;
using Npgsql;

namespace Almoxarifado.Views.Movimentacoes;

public partial class BaixaBolsaWindow : Window
{
    private readonly int funcionario;
    private readonly BaixaBolsaService service = new();
    private List<BaixaBolsaItem> previa = [];
    private bool ocupado;
    private readonly bool entrada;
    private readonly EntradaBolsaService entradaService = new();
    private List<EntradaBolsaItem> previaEntrada = [];
    public int? Movimento { get; private set; }
    public BaixaBolsaWindow(int funcionario, string nome, bool entrada = false)
    {
        InitializeComponent();
        this.funcionario = funcionario;
        this.entrada = entrada;
        funcionarioTexto.Text = nome;
        destino.ItemsSource = BaixaBolsaService.Destinos;
        data.SelectedDate = DateTime.Today;
        if (entrada)
        {
            Title = "Entrada em estoque";
            destinoTitulo.Content = "Origem";
            destino.ItemsSource = EntradaBolsaService.Origens;
            estadoPanel.Visibility = Visibility.Visible;
            estado.ItemsSource = EntradaBolsaService.Estados;
            confirmar.Content = "Finalizar entrada";
            quantidadeColuna.Header = "Entrada pendente";
            aviso.Text = "Sera registrada somente a diferenca entre o retorno acumulado e o que ja entrou no estoque. Novos retornos parciais permanecem permitidos; valores ja registrados nao podem ser reduzidos.";
        }
    }
    private async void Carregar(object sender, RoutedEventArgs e)
    {
        Loaded -= Carregar;
        ocupado = true; conteudo.IsEnabled = false;
        status.Text = "Carregando bolsas...";
        try
        {
            if (entrada)
            {
                previaEntrada = await entradaService.Carregar(funcionario);
                produtos.ItemsSource = EntradaBolsaService.Consolidar(previaEntrada);
            }
            else
            {
                previa = await service.Carregar(funcionario);
                produtos.ItemsSource = BaixaBolsaService.Consolidar(previa);
            }
            await using var db = new NpgsqlConnection(DataBaseSettings.Instance.ConnectionString);
            atendente.ItemsSource = (await db.QueryAsync<string>("SELECT DISTINCT nome_funcionario FROM almoxarifado_jac.tbl_atendentes_almox WHERE nome_funcionario IS NOT NULL ORDER BY nome_funcionario")).ToList();
            confirmar.IsEnabled = true;
            status.Text = $"{(entrada ? previaEntrada.Count(i => i.Pendente > 0) : previa.Count)} item(ns) pendente(s), todas as siglas.";
        }
        catch (Exception ex) { status.Text = ex.Message; }
        finally { ocupado = false; conteudo.IsEnabled = true; }
    }
    private async void Confirmar(object sender, RoutedEventArgs e)
    {
        if (ocupado) return;
        if (destino.SelectedItem is not string d || atendente.SelectedItem is not string a ||
            !int.TryParse(ordem.Text, out int os) || os < 0)
        { status.Text = $"Selecione {(entrada ? "origem" : "destino")}, atendente e informe uma O.S. valida."; return; }
        if (entrada && estado.SelectedItem is not string)
        { status.Text = "Selecione o estado dos materiais."; return; }
        if (MessageBox.Show(this, entrada
                ? "Confirma a entrada das diferencas exibidas? As quantidades ja registradas nao poderao ser reduzidas. Novos retornos parciais poderao ser lancados."
                : "Confirma a saida consolidada? Os itens processados ficarao bloqueados para alteracao e nova baixa.",
            Title, MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes) return;
        ocupado = true; conteudo.IsEnabled = false; status.Text = entrada ? "Gravando entrada..." : "Gravando baixa...";
        try
        {
            Movimento = entrada
                ? await entradaService.Salvar(funcionario, previaEntrada, d, a, os, endereco.Text.Trim(), (string)estado.SelectedItem)
                : await service.Salvar(funcionario, previa, d, a, os, endereco.Text.Trim());
            ocupado = false;
            DialogResult = true;
        }
        catch (Exception ex)
        {
            status.Text = ex.Message;
            // Exige nova consulta, inclusive quando nao foi possivel confirmar a resposta do banco.
            confirmar.IsEnabled = false;
        }
        finally { ocupado = false; conteudo.IsEnabled = true; }
    }
    private void AoFechar(object? sender, CancelEventArgs e) { if (ocupado) e.Cancel = true; }
    private void Cancelar(object sender, RoutedEventArgs e) { if (!ocupado) DialogResult = false; }
}
