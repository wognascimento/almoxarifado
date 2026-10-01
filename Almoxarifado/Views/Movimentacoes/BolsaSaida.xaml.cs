using Almoxarifado.DataBase;
using Almoxarifado.DataBase.Model;
using Almoxarifado.DataBase.Model.DTO;
using Almoxarifado.Views.Cadastros;
using Almoxarifado.Views.Util;
using ClosedXML.Excel;
using CommunityToolkit.Mvvm.ComponentModel;
using Dapper;
using Npgsql;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;
using Telerik.Windows.Controls;
using Telerik.Windows.Controls.GridView;
using static Almoxarifado.Interfaces.IModalService;

namespace Almoxarifado.Views.Movimentacoes;

/// <summary>
/// Interação lógica para BolsaSaida.xam
/// </summary>
public partial class BolsaSaida : UserControl
{

    private ProductSearchModal _currentModal;
    private Window _parentWindow;
    private readonly DataBaseSettings BaseSettings = DataBaseSettings.Instance;

    public BolsaSaida()
    {
        InitializeComponent();
        DataContext = new BolsaSaidaViewModel();
    }

    private async void UserControl_Loaded(object sender, RoutedEventArgs e)
    {
        Loaded -= UserControl_Loaded;
        BolsaSaidaViewModel vm = (BolsaSaidaViewModel)DataContext;
        List<string> erros = [];

        string? erro1 = await vm.GetFuncionariosAsync();
        if (!string.IsNullOrEmpty(erro1)) erros.Add(erro1);

        string? erro2 = await vm.CarregarBolsasAsync();
        if (!string.IsNullOrEmpty(erro2)) erros.Add(erro2);

        string? erro3 = await vm.GetAprovadosAsync();
        if (!string.IsNullOrEmpty(erro3)) erros.Add(erro3);

        if (erros.Count > 0)
        {
            MessageBox.Show(string.Join("\n\n", erros), "Erro ao carregar dados", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void bolsa_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        var codBolsa = this.bolsa.SelectedItem is TipoBolsaModel bolsa ? bolsa.codigo_tipobolsa : null;
        var func = funcionarios.SelectedItem is FuncionarioModel funcionario ? funcionario.codfun : null;
        var sigla = siglas.SelectedItem is AprovadoModel aprovado ? aprovado.sigla_serv : null;

        BolsaSaidaViewModel vm = (BolsaSaidaViewModel)DataContext;
        List<string> erros = [];
        string? erro1 = await vm.GetBolsaProdutosAsync(codBolsa, func, sigla);
        if (!string.IsNullOrEmpty(erro1)) erros.Add(erro1);

        string? erro2 = await vm.CarregarBolsasAsync();
        if (!string.IsNullOrEmpty(erro2)) erros.Add(erro2);

        if (erros.Count > 0)
        {
            MessageBox.Show(string.Join("\n\n", erros), "Erro ao carregar dados", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void funcionarios_SelectionChanged(object sender, SelectionChangeEventArgs e)
    {
        var codFunc = funcionarios.SelectedItem is FuncionarioModel funcionario ? funcionario.codfun : null;
        BolsaSaidaViewModel vm = (BolsaSaidaViewModel)DataContext;
        List<string> erros = [];
        string? erro1 = await vm.GetBolsasFuncAsync(codFunc);
        
        if (!string.IsNullOrEmpty(erro1)) erros.Add(erro1);
        if (erros.Count > 0)
        {
            MessageBox.Show(string.Join("\n\n", erros), "Erro ao carregar dados", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        
    }

    private async void funcionarios_GotFocus(object sender, RoutedEventArgs e)
    {
        
    }

    private async void OnAdicionarItemBolsaClick(object sender, RoutedEventArgs e)
    {
        var codBolsa = this.bolsa.SelectedItem is TipoBolsaModel bolsa ? bolsa.codigo_tipobolsa : null;
        var codFunc = funcionarios.SelectedItem is FuncionarioModel funcionario ? funcionario.codfun : null;
        var sigla = siglas.SelectedItem is AprovadoModel aprovado ? aprovado.sigla_serv : null;

        BolsaSaidaViewModel vm = (BolsaSaidaViewModel)DataContext;
        if (codBolsa == null || codFunc == null || sigla == null)
        {
            RadWindow.Alert(new DialogParameters
            {
                Header = "Aviso",
                Content = "Selecione uma bolsa, funcionário e sigla antes de adicionar itens.",
                OkButtonContent = "Ok"
            });
            return;
        }

        List<string> erros = [];
        string? erro1 = await vm.AddItensBolsaProdutosAsync(codBolsa, codFunc, sigla);
        string? erro2 = await vm.GetBolsaProdutosAsync(codBolsa, codFunc, sigla);
        if (!string.IsNullOrEmpty(erro2)) erros.Add(erro2);

        if (!string.IsNullOrEmpty(erro1)) erros.Add(erro1);
        if (erros.Count > 0)
        {
            MessageBox.Show(string.Join("\n\n", erros), "Erro ao carregar dados", MessageBoxButton.OK, MessageBoxImage.Error);;
        }
    }

    private async void OnBuscaProduto(object sender, KeyEventArgs e)
    {
        var codBolsa = this.bolsa.SelectedItem is TipoBolsaModel bolsa ? bolsa.codigo_tipobolsa : null;
        var codFunc = funcionarios.SelectedItem is FuncionarioModel funcionario ? funcionario.codfun : null;
        var sigla = siglas.SelectedItem is AprovadoModel aprovado ? aprovado.sigla_serv : null;

        BolsaSaidaViewModel vm = (BolsaSaidaViewModel)DataContext;
        List<string> erros = [];
        if (e.Key == Key.F2 || e.Key == Key.Enter)
        {
            if (codBolsa == null || codFunc == null || sigla == null)
            {
                RadWindow.Alert(new DialogParameters
                {
                    Header = "Aviso",
                    Content = "Selecione uma bolsa, funcionário e sigla antes de adicionar itens.",
                    OkButtonContent = "Ok"
                });
                return;
            }

            string? erro1 = await vm.GetProdutoAsync(!string.IsNullOrWhiteSpace((e.Source as RadAutoSuggestBox).Text) ? System.Convert.ToInt64((e.Source as RadAutoSuggestBox).Text) : (long?)null);
            if (!string.IsNullOrEmpty(erro1)) erros.Add(erro1);
            if (erros.Count > 0)
            {
                MessageBox.Show(string.Join("\n\n", erros), "Erro ao carregar dados", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            var item = vm.Produto;

            RadWindow.Prompt(new DialogParameters
            {
                Header = "Quantidade",
                Content = "Informe a quantidade:",
                OkButtonContent = "Confirmar",
                CancelButtonContent = "Cancelar",
                Closed = async (sender, args) =>
                {
                    if (args.PromptResult != null)
                    {
                        if (double.TryParse(args.PromptResult, NumberStyles.Any, new CultureInfo("pt-BR"), out double quantidade))
                        {
                            if (quantidade <= 0)
                            {
                                MessageBox.Show("Quantidade deve ser maior que zero.");
                                return;
                            }
                            string? erro1 = await vm.AddItemBolsaProdutosAsync(codBolsa, codFunc, sigla, item.codcompladicional, quantidade);
                            if (!string.IsNullOrEmpty(erro1)) erros.Add(erro1);
                            string? erro2 = await vm.GetBolsaProdutosAsync(codBolsa, codFunc, sigla);
                            if (!string.IsNullOrEmpty(erro2)) erros.Add(erro2);
                        }
                        else
                        {
                            MessageBox.Show("Quantidade inválida. Digite um número com vírgula ou ponto.");
                        }
                    }
                }
            });
            if (erros.Count > 0)
            {
                MessageBox.Show(string.Join("\n\n", erros), "Erro ao carregar dados", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

        }
    }

    private void OnAdicionarItemClick(object sender, RoutedEventArgs e)
    {
        var codBolsa = this.bolsa.SelectedItem is TipoBolsaModel bolsa ? bolsa.codigo_tipobolsa : null;
        var codFunc = this.funcionarios.SelectedItem is FuncionarioModel funcionario ? funcionario.codfun : null;
        var sigla = siglas.SelectedItem is AprovadoModel aprovado ? aprovado.sigla_serv : null;

        BolsaSaidaViewModel vm = (BolsaSaidaViewModel)DataContext;
        if (codBolsa == null || codFunc == null || sigla == null)
        {
            RadWindow.Alert(new DialogParameters
            {
                Header = "Aviso",
                Content = "Selecione uma bolsa, funcionário e sigla antes de adicionar itens.",
                OkButtonContent = "Ok"
            });
            return;
        }
        ShowProductSearchModal();
    }

    private async void OnImprimirReciboBolsaClick(object sender, RoutedEventArgs e)
    {
        BolsaSaidaViewModel vm = (BolsaSaidaViewModel)DataContext;
        var descricaoBolsa = this.bolsa.SelectedItem is TipoBolsaModel bolsa ? bolsa.descricao : null;
        var codBolsa = this.bolsa.SelectedItem is TipoBolsaModel b ? b.codigo_tipobolsa : null;
        var destino = this.siglas.SelectedItem is AprovadoModel sigla ? sigla.sigla_serv : null;
        var nomeFuncionario = this.funcionarios.SelectedItem is FuncionarioModel funcionario ? funcionario.nome_apelido : null;
        var codFuncionario = this.funcionarios.SelectedItem is FuncionarioModel f ? f.codfun : null;


        try
        {
            var modelo = Path.Combine(BaseSettings.CaminhoSistema, "Modelos", "RECIBO_SAIDA.xlsx");
            var destinoArquivo = Path.Combine(BaseSettings.CaminhoSistema, "Impressos", $"RECIBO_SAIDA-{codFuncionario}-{codBolsa}.xlsx");
            Directory.CreateDirectory(Path.GetDirectoryName(destinoArquivo) ?? ".");

            using var workbook = new XLWorkbook(modelo);
            var worksheet = workbook.Worksheet(1);

            // Preenche células fixas
            worksheet.Cell("B5").Value = @$"Prazo máximo para devolução das ferramentas: 31/01/{DateTime.Now.Year + 1}";
            worksheet.Cell("B6").Value = $"{descricaoBolsa} - {destino}";
            worksheet.Cell("E11").Value = nomeFuncionario;

            int linhaInicial = 8; // Inserir a partir da linha 11

            foreach (var item in vm.BolsaItens.Where(b => b.quantidade > 0))
            {
                worksheet.Cell($"A{linhaInicial}").Value = Convert.ToDouble(item.codcompladicional);

                worksheet.Range($"B{linhaInicial}:C{linhaInicial}").Merge();
                worksheet.Cell($"B{linhaInicial}").Value = item.planilha;

                worksheet.Range($"D{linhaInicial}:H{linhaInicial}").Merge();
                worksheet.Cell($"D{linhaInicial}").Value = item?.descricao_completa?.Replace("ÚNICO", null);

                worksheet.Cell($"I{linhaInicial}").Value = item.unidade;

                worksheet.Cell($"J{linhaInicial}").Value = Convert.ToDouble(item.quantidade);

                worksheet.Cell($"K{linhaInicial}").Value = Convert.ToDouble(item.valor_unitario);

                worksheet.Cell($"L{linhaInicial}").Value = Convert.ToDouble(item.valor_total);

                linhaInicial++;
                worksheet.Row(linhaInicial).InsertRowsAbove(1);
            }

            linhaInicial += 1;
            worksheet.Cell($"L{linhaInicial}").FormulaA1 = $"SUM(L7:L{linhaInicial - 1})";

            workbook.SaveAs(destinoArquivo);
            Process.Start(new ProcessStartInfo(destinoArquivo) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message);
        }

    }

    private void ModalOverlay_MouseDown(object sender, MouseButtonEventArgs e)
    {

    }

    private async void RadGridView_CellEditEnded(object sender, GridViewCellEditEndedEventArgs e)
    {
        if (sender is not RadGridView gridView)
            return;

        var itemBeingEdited = gridView.CurrentCellInfo.Item as BolsaSaidaDTO; 
        var columnName = e.Cell.Column.Header.ToString();
        List<string> erros = [];
        BolsaSaidaViewModel vm = (BolsaSaidaViewModel)DataContext;

        switch (columnName)
        {
            case "Qtde":
                //AtualizarQuantidade(itemBeingEdited, e.NewData);
                string? erroQtde = await vm.AlterarItemBolsaAsync(itemBeingEdited);
                if (!string.IsNullOrEmpty(erroQtde)) erros.Add(erroQtde);
                break;
            case "VL. Unit.":
                AtualizarValorUnitario(itemBeingEdited, e.NewData);
                break;
        }

        
        if (erros.Count > 0)
        {
            MessageBox.Show(string.Join("\n\n", erros), "Erro ao carregar dados", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }
    }

    private void AtualizarQuantidade(BolsaSaidaDTO item, object newValue)
    {
        var quantidade = newValue;
    }

    private void AtualizarValorUnitario(BolsaSaidaDTO item, object newValue)
    {
        var valorUnitario = newValue;
    }


    private async void ShowProductSearchModal()
    {
        try
        {
            // Verificar se já existe uma modal aberta
            if (_currentModal != null)
                return;

            // Calcular posição do modal
            var position = CalculateModalPosition();

            // Criar nova instância da modal
            _currentModal = new ProductSearchModal();
            _currentModal.ProductSelected += OnProductSelected;
            _currentModal.ModalClosed += OnModalClosed;

            // Configurar conteúdo
            ModalContent.Content = _currentModal;

            // Posicionar modal
            PositionModal(position);

            // Mostrar modal com animação
            await ShowModalWithAnimation();

            // Focar na modal
            _currentModal.Focus();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Erro ao abrir busca de produtos: {ex.Message}",
                           "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }


    private Point CalculateModalPosition()
    {
        try
        {
            // Obter posição do campo COD. PRODUTO em relação ao UserControl
            var fieldPosition = btnAdicionarItem.TransformToAncestor(this)
                                            .Transform(new Point(0, 0));

            // Obter posição do UserControl em relação à janela principal
            Point userControlPosition = new Point(0, 0);
            if (_parentWindow != null)
            {
                userControlPosition = this.TransformToAncestor(_parentWindow)
                                         .Transform(new Point(0, 0));
            }

            // Calcular posição absoluta do campo
            var absoluteFieldPosition = new Point(
                userControlPosition.X + fieldPosition.X,
                userControlPosition.Y + fieldPosition.Y
            );

            // Dimensões da modal
            const double modalWidth = 700;
            const double modalHeight = 500;
            const double margin = 10;

            // Dimensões disponíveis
            double availableWidth = _parentWindow?.ActualWidth ?? this.ActualWidth;
            double availableHeight = _parentWindow?.ActualHeight ?? this.ActualHeight;

            // Posição inicial (abaixo do campo)
            double x = absoluteFieldPosition.X;
            double y = absoluteFieldPosition.Y + btnAdicionarItem.ActualHeight + margin;

            // Ajustar posição X se sair da tela
            if (x + modalWidth > availableWidth)
            {
                x = Math.Max(margin, availableWidth - modalWidth - margin);
            }

            // Ajustar posição Y se sair da tela
            if (y + modalHeight > availableHeight)
            {
                // Tentar posicionar acima do campo
                double yAbove = absoluteFieldPosition.Y - modalHeight - margin;

                if (yAbove >= margin)
                {
                    y = yAbove;
                }
                else
                {
                    // Centralizar verticalmente se não couber nem acima nem abaixo
                    y = Math.Max(margin, (availableHeight - modalHeight) / 2);
                }
            }

            return new Point(Math.Max(margin, x), Math.Max(margin, y));
        }
        catch (Exception ex)
        {
            // Em caso de erro, centralizar modal
            return new Point(50, 50);
        }
    }


    private async void OnProductSelected(object sender, ProductSelectedEventArgs e)
    {
        try
        {
            // Preencher campo com produto selecionado
            //txtCodProduto.Text = e.Product.codcompladicional?.ToString() ?? string.Empty;

            // Preencher outros campos automaticamente se disponível
            //if (!string.IsNullOrEmpty(e.Product.unidade))
            //{
            //    radMaskedUnidade.Text = e.Product.unidade;
            //}
            // Buscar dados complementares do produto se necessário
            await LoadProductDetails(e.Product);

            // Fechar modal
            await CloseModal();

            // Focar no próximo campo
            //txtQuantidade.Focus();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Erro ao processar produto selecionado: {ex.Message}",
                           "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async Task LoadProductDetails(QryDescricaoModel product)
    {
        try
        {
            // Implementar carregamento de detalhes adicionais do produto
            // Por exemplo: descrições, complementos, etc.

            // Exemplo de preenchimento automático baseado no produto
            // if (!string.IsNullOrEmpty(product.Descricao))
            // {
            //     autoCompleteDescricao.Text = product.Descricao;
            // }

            var codBolsa = this.bolsa.SelectedItem is TipoBolsaModel bolsa ? bolsa.codigo_tipobolsa : null;
            var codFunc = funcionarios.SelectedItem is FuncionarioModel funcionario ? funcionario.codfun : null;
            var sigla = siglas.SelectedItem is AprovadoModel aprovado ? aprovado.sigla_serv : null;

            BolsaSaidaViewModel vm = (BolsaSaidaViewModel)DataContext;
            List<string> erros = [];

            if (codBolsa == null || codFunc == null || sigla == null)
            {
                RadWindow.Alert(new DialogParameters
                {
                    Header = "Aviso",
                    Content = "Selecione uma bolsa, funcionário e sigla antes de adicionar itens.",
                    OkButtonContent = "Ok"
                });
                return;
            }
            var item = product;

            RadWindow.Prompt(new DialogParameters
            {
                Header = "Quantidade",
                Content = "Informe a quantidade:",
                OkButtonContent = "Confirmar",
                CancelButtonContent = "Cancelar",
                Closed = async (sender, args) =>
                {
                    if (args.PromptResult != null)
                    {
                        if (double.TryParse(args.PromptResult, NumberStyles.Any, new CultureInfo("pt-BR"), out double quantidade))
                        {
                            if (quantidade <= 0)
                            {
                                MessageBox.Show("Quantidade deve ser maior que zero.");
                                return;
                            }
                            string? erro1 = await vm.AddItemBolsaProdutosAsync(codBolsa, codFunc, sigla, item.codcompladicional, quantidade);
                            if (!string.IsNullOrEmpty(erro1)) erros.Add(erro1);
                            string? erro2 = await vm.GetBolsaProdutosAsync(codBolsa, codFunc, sigla);
                            if (!string.IsNullOrEmpty(erro2)) erros.Add(erro2);
                        }
                        else
                        {
                            MessageBox.Show("Quantidade inválida. Digite um número com vírgula ou ponto.");
                        }
                    }
                }
            });
            if (erros.Count > 0)
            {
                MessageBox.Show(string.Join("\n\n", erros), "Erro ao carregar dados", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }


        }
        catch (Exception ex)
        {
            // Log do erro sem interromper o fluxo
            System.Diagnostics.Debug.WriteLine($"Erro ao carregar detalhes do produto: {ex.Message}");
        }
    }

    private async void OnModalClosed(object sender, EventArgs e)
    {
        await CloseModal();
    }

    private async Task CloseModal()
    {
        if (_currentModal != null)
        {
            _currentModal.ProductSelected -= OnProductSelected;
            _currentModal.ModalClosed -= OnModalClosed;
            _currentModal = null;
        }

        await HideModalWithAnimation();
        ModalContent.Content = null;
    }


    private async Task HideModalWithAnimation()
    {
        var storyboard = Resources["ModalCloseAnimation"] as Storyboard;
        if (storyboard != null)
        {
            storyboard.Begin();
            await Task.Delay(200);
        }

        ModalContainer.Visibility = Visibility.Collapsed;
        ModalOverlay.Visibility = Visibility.Collapsed;
    }


    private void PositionModal(Point position)
    {
        const double modalWidth = 700;
        const double modalHeight = 500;

        Canvas.SetLeft(ModalContainer, position.X);
        Canvas.SetTop(ModalContainer, position.Y);

        ModalContainer.Width = modalWidth;
        ModalContainer.Height = modalHeight;
    }


    private async Task ShowModalWithAnimation()
    {
        // Mostrar overlay
        ModalOverlay.Visibility = Visibility.Visible;
        ModalContainer.Visibility = Visibility.Visible;

        // Executar animação de abertura
        var storyboard = Resources["ModalOpenAnimation"] as Storyboard;
        if (storyboard != null)
        {
            storyboard.Begin();

            // Aguardar conclusão da animação
            await Task.Delay(250);
        }
    }

    private async void SiglaBolsaGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        BolsaSaidaViewModel vm = (BolsaSaidaViewModel)DataContext;

        var row = (e.OriginalSource as FrameworkElement)?.ParentOfType<GridViewRow>();
        if (row != null)
        {
            vm.BolsaFuncionario = row.Item as BolsaSaidaFuncDestinoDTO;
            siglas.SelectedItem = vm.Aprovados.FirstOrDefault(a => a.sigla_serv == vm.BolsaFuncionario?.destino_shop);
            bolsa.SelectedItem = vm.Bolsas.FirstOrDefault(b => b.codigo_tipobolsa == vm.BolsaFuncionario?.codigo_bolsa);

            
            var codBolsa = this.bolsa.SelectedItem is TipoBolsaModel b ? b.codigo_tipobolsa : null;
            var func = funcionarios.SelectedItem is FuncionarioModel funcionario ? funcionario.codfun : null;
            var sigla = siglas.SelectedItem is AprovadoModel aprovado ? aprovado.sigla_serv : null;


            List<string> erros = [];
            string? erro1 = await vm.GetBolsaProdutosAsync(codBolsa, func, sigla);
            if (!string.IsNullOrEmpty(erro1)) erros.Add(erro1);

            string? erro2 = await vm.CarregarBolsasAsync();
            if (!string.IsNullOrEmpty(erro2)) erros.Add(erro2);

            if (erros.Count > 0)
            {
                MessageBox.Show(string.Join("\n\n", erros), "Erro ao carregar dados", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            
        }
    }
}

public partial class BolsaSaidaViewModel : ObservableObject
{
    private readonly DataBaseSettings _dataBaseSettings;

    public BolsaSaidaViewModel()
    {
        _dataBaseSettings = DataBaseSettings.Instance;
        Funcionarios = [];
        Bolsas = [];
        BolsaItens = [];
        Aprovados = [];
        BolsasFuncionario = [];
    }

    [ObservableProperty]
    private bool isBusy;

    [ObservableProperty]
    private ObservableCollection<FuncionarioModel> funcionarios;

    [ObservableProperty]
    private ObservableCollection<TipoBolsaModel> bolsas;

    [ObservableProperty]
    private ObservableCollection<BolsaSaidaDTO> bolsaItens;

    [ObservableProperty]
    private QryDescricaoModel? produto;

    [ObservableProperty]
    private ObservableCollection<AprovadoModel> aprovados;

    [ObservableProperty]
    private ObservableCollection<BolsaSaidaFuncDestinoDTO> bolsasFuncionario;

    [ObservableProperty]
    private BolsaSaidaFuncDestinoDTO bolsaFuncionario;


    public async Task<string?> GetFuncionariosAsync()
    {
        try
        {
            await using var conn = new NpgsqlConnection(_dataBaseSettings.ConnectionString);
            var result = await conn.QueryAsync<FuncionarioModel>(@"
                SELECT *
                FROM almoxarifado_jac.view_funcionarios_terceiros
                ORDER BY nome_apelido, setor;");
            Funcionarios =  new ObservableCollection<FuncionarioModel>(result);
            return null; // sucesso
        }
        catch (PostgresException pgEx)
        {
            return $"Erro do banco: {pgEx.MessageText}\nLocal: {pgEx.Where}";
        }
        catch (Exception ex)
        {
            return $"Erro inesperado: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    public async Task<string?> CarregarBolsasAsync()
    {
        try
        {
            IsBusy = true;
            await using var conn = new NpgsqlConnection(_dataBaseSettings.ConnectionString);
            var data = await conn.QueryAsync<TipoBolsaModel>(@"
                SELECT codigo_tipobolsa, descricao, codcompladicional
                FROM almoxarifado_apoio.tbltipobolsa
                ORDER BY descricao;");

            Bolsas = new ObservableCollection<TipoBolsaModel>(data);
            return null; // sucesso
        }
        catch (PostgresException pgEx)
        {
            return $"Erro do banco: {pgEx.MessageText}\nLocal: {pgEx.Where}";
        }
        catch (Exception ex)
        {
            return $"Erro inesperado: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    public async Task<string?> GetAprovadosAsync()
    {
        try
        {
            await using var conn = new NpgsqlConnection(_dataBaseSettings.ConnectionString);
            var result = await conn.QueryAsync<AprovadoModel>(@"
                SELECT id_aprovado, sigla, sigla_serv, nome
                FROM producao.t_aprovados
                ORDER BY sigla, sigla_serv;");
            Aprovados = new ObservableCollection<AprovadoModel>(result);
            return null; // sucesso
        }
        catch (PostgresException pgEx)
        {
            return $"Erro do banco: {pgEx.MessageText}\nLocal: {pgEx.Where}";
        }
        catch (Exception ex)
        {
            return $"Erro inesperado: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    public async Task<string?> GetProdutoAsync(long? codcompleadicional)
    {
        try
        {
            IsBusy = true;
            await using var conn = new NpgsqlConnection(_dataBaseSettings.ConnectionString);
            Produto = await conn.QueryFirstOrDefaultAsync<QryDescricaoModel>(@"
                SELECT *
                FROM producao.qry3descricoes
                WHERE codcompladicional = @codcompleadicional;", new { codcompleadicional });
            return null; // sucesso
        }
        catch (NpgsqlException ex) when (ex.InnerException is PostgresException pgEx)
        {
            return $"Erro do banco: {pgEx.MessageText}\nLocal: {pgEx.Where}";
        }
        catch (Exception ex)
        {
            return $"Erro inesperado: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    public async Task<string?> GetBolsaProdutosAsync(long? codBolsa, long? codFunc, string? sigla)
    {
        try
        {


            IsBusy = true;
            var parameters = new { bolsa = codBolsa, func = codFunc, sigla };
            string sql = @"
                SELECT
                    b.cod_controlelinha,
	                b.codigo_bolsa,
	                b.codcompladicional,
	                p.planilha,
                    p.descricao,
                    p.descricao_adicional,
                    p.complementoadicional,
	                p.descricao_completa,
	                p.unidade,
	                b.quantidade,
	                p.custo AS valor_unitario,
	                b.quantidade * p.custo AS valor_total
                FROM almoxarifado_apoio.tblsaidabolsa AS b
                JOIN producao.qry3descricoes AS p ON b.codcompladicional = p.codcompladicional
                WHERE b.codigo_bolsa = @bolsa AND b.codfun = @func AND b.destino_shop = @sigla
                ORDER BY b.codigo_bolsa, p.planilha, p.descricao_completa;
            ";
            using var connection = new NpgsqlConnection(_dataBaseSettings.ConnectionString);
            await connection.OpenAsync();
            var resultado = (await connection.QueryAsync<BolsaSaidaDTO>(sql, parameters)).ToList();
            await connection.CloseAsync();

            BolsaItens = new ObservableCollection<BolsaSaidaDTO>(resultado);
            return null; // sucesso
        }
        catch (NpgsqlException ex) when (ex.InnerException is PostgresException pgEx)
        {
            return $"Erro do banco: {pgEx.MessageText}\nLocal: {pgEx.Where}";
        }
        catch (Exception ex)
        {
            return $"Erro inesperado: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    public async Task<string?> AddItensBolsaProdutosAsync(long? codBolsa, long? codFunc, string? sigla)
    {
        IsBusy = true;

        try
        {
            await using var connection = new NpgsqlConnection(_dataBaseSettings.ConnectionString);
            await connection.OpenAsync();
            await using var transaction = await connection.BeginTransactionAsync();
            try
            {
                var totalItens = await connection.ExecuteScalarAsync<int>(@"
                    SELECT COUNT(*)
                    FROM almoxarifado_apoio.tblbolsafixa
                    WHERE codigo_tipobolsa = @codBolsa;",
                    new { codBolsa }, transaction);

                if (totalItens == 0)
                    return "Nenhum item encontrado para a bolsa selecionada.";

                await connection.ExecuteAsync(@"
                    INSERT INTO almoxarifado_apoio.tblsaidabolsa
                        (codfun, codigo_bolsa, destino_shop, codcompladicional, quantidade)
                    SELECT
                        @codFunc,
                        @codBolsa,
                        @sigla,
                        f.codcompladicional,
                        f.quantidade
                    FROM almoxarifado_apoio.tblbolsafixa f
                    WHERE f.codigo_tipobolsa = @codBolsa
                      AND NOT EXISTS (
                          SELECT 1
                          FROM almoxarifado_apoio.tblsaidabolsa s
                          WHERE s.codigo_bolsa = @codBolsa
                            AND s.codfun = @codFunc
                            AND s.destino_shop = @sigla
                            AND s.codcompladicional = f.codcompladicional
                      );", new { codBolsa, codFunc, sigla }, transaction);

                await transaction.CommitAsync();
                return null; // sucesso
            }
            catch (PostgresException pgEx)
            {
                await transaction.RollbackAsync();
                return $"Erro do banco: {pgEx.MessageText}\nLocal: {pgEx.Where}";
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return $"Erro inesperado: {ex.Message}";
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

    public async Task<string?> AddItemBolsaProdutosAsync(long? codBolsa, long? codFunc, string? sigla, long? codcompladicional, double quantidade)
    {
        IsBusy = true;

        try
        {
            await using var connection = new NpgsqlConnection(_dataBaseSettings.ConnectionString);
            await connection.OpenAsync();
            await using var transaction = await connection.BeginTransactionAsync();
            try
            {
                await connection.ExecuteAsync(@"
                    INSERT INTO almoxarifado_apoio.tblsaidabolsa
                        (codfun, codigo_bolsa, destino_shop, codcompladicional, quantidade)
                    VALUES
                        (@codFunc, @codBolsa, @sigla, @codcompladicional, @quantidade);",
                    new { codBolsa, codFunc, sigla, codcompladicional, quantidade }, transaction);

                await transaction.CommitAsync();
                return null; // sucesso
            }
            catch (PostgresException pgEx)
            {
                await transaction.RollbackAsync();
                return $"Erro do banco: {pgEx.MessageText}\nLocal: {pgEx.Where}";
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return $"Erro inesperado: {ex.Message}";
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

    public async Task<string?> AlterarItemBolsaAsync(BolsaSaidaDTO bolsaSaida)
    {
        try
        {
            IsBusy = true;
            await using var conn = new NpgsqlConnection(_dataBaseSettings.ConnectionString);
            await conn.ExecuteAsync(@"
                UPDATE almoxarifado_apoio.tblsaidabolsa
                SET quantidade = @quantidade
                WHERE cod_controlelinha = @cod_controlelinha;",
                new
                {
                    bolsaSaida.cod_controlelinha,
                    quantidade = bolsaSaida.quantidade ?? 0
                });
            return null; // sucesso
        }
        catch (NpgsqlException ex) when (ex.InnerException is PostgresException pgEx)
        {
            return $"Erro do banco: {pgEx.MessageText}\nLocal: {pgEx.Where}";
        }
        catch (Exception ex)
        {
            return $"Erro inesperado: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }


    public async Task<string?> GetBolsasFuncAsync(long? codFunc)
    {
        try
        {


            IsBusy = true;
            var parameters = new { func = codFunc };
            string sql = @"
                SELECT 
                    codfun, 
                    destino_shop, 
                    codigo_bolsa, 
                    tbltipobolsa.descricao
                FROM almoxarifado_apoio.tblsaidabolsa
                JOIN almoxarifado_apoio.tbltipobolsa ON tblsaidabolsa.codigo_bolsa = tbltipobolsa.codigo_tipobolsa
                JOIN producao.qry3descricoes ON tblsaidabolsa.codcompladicional = qry3descricoes.codcompladicional
                WHERE codfun = @func
                GROUP BY tbltipobolsa.descricao, codigo_bolsa, codfun, destino_shop
                ORDER BY destino_shop, tbltipobolsa.descricao
            ";
            using var connection = new NpgsqlConnection(_dataBaseSettings.ConnectionString);
            await connection.OpenAsync();
            var resultado = (await connection.QueryAsync<BolsaSaidaFuncDestinoDTO>(sql, parameters)).ToList();
            await connection.CloseAsync();

            BolsasFuncionario = new ObservableCollection<BolsaSaidaFuncDestinoDTO>(resultado);
            return null; // sucesso
        }
        catch (NpgsqlException ex) when (ex.InnerException is PostgresException pgEx)
        {
            return $"Erro do banco: {pgEx.MessageText}\nLocal: {pgEx.Where}";
        }
        catch (Exception ex)
        {
            return $"Erro inesperado: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }
}
