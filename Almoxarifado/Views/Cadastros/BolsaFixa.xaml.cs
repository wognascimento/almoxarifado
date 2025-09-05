using Almoxarifado.DataBase;
using Almoxarifado.DataBase.Model;
using Almoxarifado.DataBase.Model.DTO;
using Almoxarifado.Views.Util;
using CommunityToolkit.Mvvm.ComponentModel;
using Dapper;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;
using Telerik.Windows.Controls;
using Telerik.Windows.Controls.AutoSuggestBox;
using Telerik.Windows.Controls.GridView;
using static Almoxarifado.Interfaces.IModalService;


namespace Almoxarifado.Views.Cadastros;

/// <summary>
/// Interação lógica para BolsaFixa.xam
/// </summary>
public partial class BolsaFixa : UserControl
{
    private ProductSearchModal _currentModal;
    private Window _parentWindow;
    private readonly DataBaseSettings _dataBaseSettings;

    public BolsaFixa()
    {
        InitializeComponent();
        DataContext = new BolsaFixaViewModel();
        _dataBaseSettings = DataBaseSettings.Instance;
    }

    private async void UserControl_Loaded(object sender, RoutedEventArgs e)
    {
        BolsaFixaViewModel vm = (BolsaFixaViewModel)DataContext;
        List<string> erros = [];
        string? erro1 = await vm.CarregarBolsasAsync();
        if (!string.IsNullOrEmpty(erro1)) erros.Add(erro1);

        string? erro2 = await vm.CarregarPlanilhasAsync();
        if (!string.IsNullOrEmpty(erro2)) erros.Add(erro2);

        if (erros.Count > 0)
        {
            MessageBox.Show(string.Join("\n\n", erros), "Erro ao carregar dados", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        // Encontrar a janela pai
        _parentWindow = Window.GetWindow(this);

        if (_parentWindow != null)
        {
            _parentWindow.SizeChanged += ParentWindow_SizeChanged;
            _parentWindow.LocationChanged += ParentWindow_LocationChanged;
        }
    }

    private async void origem_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        BolsaFixaViewModel vm = (BolsaFixaViewModel)DataContext;
        var bolsa = e.AddedItems[0] as TipoBolsaModel; //((object[])e.AddedItems)[0]
        List<string> erros = [];
        string? erro1 = await vm.GetBolsaProdutosAsync(bolsa.codigo_tipobolsa);

        //var itens = vm.BolsaFixas;
        if (!string.IsNullOrEmpty(erro1)) erros.Add(erro1);
        if (erros.Count > 0)
        {
            MessageBox.Show(string.Join("\n\n", erros), "Erro ao carregar dados", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void OnBuscaProduto(object sender, KeyEventArgs e)
    {
        BolsaFixaViewModel vm = (BolsaFixaViewModel)DataContext;
        List<string> erros = [];
        if (e.Key == Key.F2 || e.Key == Key.Enter)
        {
            string? erro1 = await vm.GetProdutoAsync(!string.IsNullOrWhiteSpace((e.Source as RadAutoSuggestBox).Text) ? System.Convert.ToInt64((e.Source as RadAutoSuggestBox).Text) : (long?)null);
            if (!string.IsNullOrEmpty(erro1)) erros.Add(erro1);
            if (erros.Count > 0)
            {
                MessageBox.Show(string.Join("\n\n", erros), "Erro ao carregar dados", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            var item = vm.Produto;
            txtCodProduto.Text = item.codcompladicional?.ToString() ?? string.Empty;
            autoCompletePlanilha.Text = item.planilha ?? string.Empty;
            autoCompleteDescricao.Text = item.descricao ?? string.Empty;
            autoCompleteDescricaoAdicional.Text = item.descricao_adicional ?? string.Empty;
            autoCompleteComplementoAdicional.Text = item.complementoadicional ?? string.Empty;
            radMaskedUnidade.Text = item.unidade;

            string? erroPlanilha = await vm.GetDescricoesAsync(item.planilha);
            if (!string.IsNullOrEmpty(erroPlanilha)) erros.Add(erroPlanilha);
            string? erroDescricao = await vm.GetDescricoesAdicionaisAsync(item.codigo);
            if (!string.IsNullOrEmpty(erroDescricao)) erros.Add(erroDescricao);
            string? erroComplemento = await vm.GetComplementoAdicionaisAsync(item.coduniadicional);
            if (!string.IsNullOrEmpty(erroComplemento)) erros.Add(erroComplemento);

            if (erros.Count > 0)
            {
                MessageBox.Show(string.Join("\n\n", erros), "Erro ao carregar dados", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

        }
    }

    private void BtnBuscarProduto_Click(object sender, RoutedEventArgs e)
    {
        ShowProductSearchModal();
    }

    private async void RadGridView_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        BolsaFixaViewModel vm = (BolsaFixaViewModel)DataContext;
        List<string> erros = [];
        var row = (e.OriginalSource as FrameworkElement)?.ParentOfType<GridViewRow>();
        if (row != null)
        {
            vm.ItemBolsa = row.Item as BolsaFixaDTO;
            txtCodProduto.Text = vm.ItemBolsa.codcompladicional?.ToString() ?? string.Empty;
            autoCompletePlanilha.Text = vm.ItemBolsa.planilha ?? string.Empty;
            autoCompleteDescricao.Text = vm.ItemBolsa.descricao ?? string.Empty;
            autoCompleteDescricaoAdicional.Text = vm.ItemBolsa.descricao_adicional ?? string.Empty;
            autoCompleteComplementoAdicional.Text = vm.ItemBolsa.complementoadicional ?? string.Empty;
            txtQuantidade.Text = vm.ItemBolsa.quantidade?.ToString("F2") ?? string.Empty;
            radMaskedUnidade.Text = vm.ItemBolsa.unidade;

            string? erro1 = await vm.GetProdutoAsync(vm.ItemBolsa.codcompladicional);
            if (!string.IsNullOrEmpty(erro1)) erros.Add(erro1);
            if (erros.Count > 0)
            {
                MessageBox.Show(string.Join("\n\n", erros), "Erro ao carregar dados", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            var product = vm.Produto;


            string? erroPlanilha = await vm.GetDescricoesAsync(product.planilha);
            if (!string.IsNullOrEmpty(erroPlanilha)) erros.Add(erroPlanilha);
            string? erroDescricao = await vm.GetDescricoesAdicionaisAsync(product.codigo);
            if (!string.IsNullOrEmpty(erroDescricao)) erros.Add(erroDescricao);
            string? erroComplemento = await vm.GetComplementoAdicionaisAsync(product.coduniadicional);
            if (!string.IsNullOrEmpty(erroComplemento)) erros.Add(erroComplemento);

            if (erros.Count > 0)
            {
                MessageBox.Show(string.Join("\n\n", erros), "Erro ao carregar dados", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
        }
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
            var fieldPosition = txtCodProduto.TransformToAncestor(this)
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
            double y = absoluteFieldPosition.Y + txtCodProduto.ActualHeight + margin;

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

    private async void OnProductSelected(object sender, ProductSelectedEventArgs e)
    {
        try
        {
            // Preencher campo com produto selecionado
            txtCodProduto.Text = e.Product.codcompladicional?.ToString() ?? string.Empty;

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
            txtQuantidade.Focus();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Erro ao processar produto selecionado: {ex.Message}",
                           "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
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

            BolsaFixaViewModel vm = (BolsaFixaViewModel)DataContext;
            List<string> erros = [];

            autoCompletePlanilha.Text = product.planilha ?? string.Empty;
            autoCompleteDescricao.Text = product.descricao ?? string.Empty;
            autoCompleteDescricaoAdicional.Text = product.descricao_adicional ?? string.Empty;
            autoCompleteComplementoAdicional.Text = product.complementoadicional ?? string.Empty;
            radMaskedUnidade.Text = product.unidade;

            string? erroPlanilha = await vm.GetDescricoesAsync(product.planilha);
            if (!string.IsNullOrEmpty(erroPlanilha)) erros.Add(erroPlanilha);
            string? erroDescricao = await vm.GetDescricoesAdicionaisAsync(product.codigo);
            if (!string.IsNullOrEmpty(erroDescricao)) erros.Add(erroDescricao);
            string? erroComplemento = await vm.GetComplementoAdicionaisAsync(product.coduniadicional);
            if (!string.IsNullOrEmpty(erroComplemento)) erros.Add(erroComplemento);

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

    // Event handlers para reposicionamento
    private void ParentWindow_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (_currentModal != null && ModalContainer.Visibility == Visibility.Visible)
        {
            var newPosition = CalculateModalPosition();
            PositionModal(newPosition);
        }
    }

    private void ParentWindow_LocationChanged(object sender, EventArgs e)
    {
        if (_currentModal != null && ModalContainer.Visibility == Visibility.Visible)
        {
            var newPosition = CalculateModalPosition();
            PositionModal(newPosition);
        }
    }

    private async void ModalOverlay_MouseDown(object sender, MouseButtonEventArgs e)
    {
        // Fechar modal se clicar no overlay (fora da modal)
        if (e.Source == ModalOverlay)
        {
            await CloseModal();
        }
    }

    // Cleanup quando o UserControl é descarregado
    private void UserControl_Unloaded(object sender, RoutedEventArgs e)
    {
        if (_parentWindow != null)
        {
            _parentWindow.SizeChanged -= ParentWindow_SizeChanged;
            _parentWindow.LocationChanged -= ParentWindow_LocationChanged;
        }

        if (_currentModal != null)
        {
            CloseModal().ConfigureAwait(false);
        }
    }

    private void RadAutoSuggestBox_GotFocus(object sender, RoutedEventArgs e)
    {
        var SuggestBox = e.Source as RadAutoSuggestBox;
        SuggestBox.IsDropDownOpen = true;
    }

    private async void OnQuerySubmitted(object sender, QuerySubmittedEventArgs e)
    {
        List<string> erros = [];
        BolsaFixaViewModel vm = (BolsaFixaViewModel)DataContext;
        System.Windows.Application.Current.Dispatcher.Invoke(() => { Mouse.OverrideCursor = Cursors.Wait; });

        if (sender is RadAutoSuggestBox suggestBox)
        {
            switch (suggestBox.DisplayMemberPath)
            {
                case "planilha":
                    autoCompleteDescricao.Text = string.Empty;
                    autoCompleteDescricaoAdicional.Text = string.Empty;
                    txtCodProduto.Text = string.Empty;
                    autoCompleteComplementoAdicional.Text = string.Empty;
                    radMaskedUnidade.Text = string.Empty;
                    string? erroPlanilha = await vm.GetDescricoesAsync(suggestBox.Text);
                    if (!string.IsNullOrEmpty(erroPlanilha)) erros.Add(erroPlanilha);
                    break;
                case "descricao":
                    autoCompleteDescricaoAdicional.Text = string.Empty;
                    txtCodProduto.Text = string.Empty;
                    autoCompleteComplementoAdicional.Text = string.Empty;
                    radMaskedUnidade.Text = string.Empty;
                    long? codigoproduto = ((ProdutoModel)e.Suggestion).codigo;
                    string? erroDescricao = await vm.GetDescricoesAdicionaisAsync(codigoproduto);
                    if (!string.IsNullOrEmpty(erroDescricao)) erros.Add(erroDescricao);
                    break;
                case "descricao_adicional":
                    txtCodProduto.Text = string.Empty;
                    autoCompleteComplementoAdicional.Text = string.Empty;
                    radMaskedUnidade.Text = string.Empty;
                    long ? coduniadicional = ((TabelaDescAdicionalModel)e.Suggestion).coduniadicional;
                    string? erroComplemento = await vm.GetComplementoAdicionaisAsync(coduniadicional);
                    if (!string.IsNullOrEmpty(erroComplemento)) erros.Add(erroComplemento);
                    break;
                case "complementoadicional":
                    txtCodProduto.Text = ((TblComplementoAdicionalModel)e.Suggestion).codcompladicional?.ToString() ?? string.Empty;
                    radMaskedUnidade.Text = ((TblComplementoAdicionalModel)e.Suggestion).unidade ?? string.Empty;
                    txtQuantidade.Focus();
                    break;
            }
        }

        if (erros.Count > 0)
        {
            MessageBox.Show(string.Join("\n\n", erros), "Erro ao carregar dados", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        System.Windows.Application.Current.Dispatcher.Invoke(() => { Mouse.OverrideCursor = null; });
    }

    private async void OnSaveClick(object sender, RoutedEventArgs e)
    {

        if(bolsa.SelectedItem == null)
        {
            MessageBox.Show("Selecione uma bolsa.", "Atenção", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        else if (string.IsNullOrEmpty(txtCodProduto.Text))
        {
            MessageBox.Show("Informe o código do produto.", "Atenção", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        else if (string.IsNullOrEmpty(txtQuantidade.Text) || !double.TryParse(txtQuantidade.Text, out double quantidade) || quantidade <= 0)
        {
            MessageBox.Show("Informe uma quantidade válida.", "Atenção", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        BolsaFixaViewModel vm = (BolsaFixaViewModel)DataContext;
        List<string> erros = [];
        string? erro = await vm.SalvarBolsaFixaAsync(
            new BolsaFixaDTO { 
                codcompladicional = !string.IsNullOrWhiteSpace(txtCodProduto.Text) ? System.Convert.ToInt64(txtCodProduto.Text) : (long?)null, 
                quantidade = !string.IsNullOrWhiteSpace(txtQuantidade.Text) ? System.Convert.ToDouble(txtQuantidade.Text) : (double?)null,
                codigo_tipobolsa = (bolsa.SelectedValue as TipoBolsaModel)?.codigo_tipobolsa
            });
        if (!string.IsNullOrEmpty(erro)) erros.Add(erro);
        string? erro1 = await vm.GetBolsaProdutosAsync((bolsa.SelectedValue as TipoBolsaModel)?.codigo_tipobolsa);
        if (!string.IsNullOrEmpty(erro1)) erros.Add(erro1);
        if (erros.Count > 0)
        {
            MessageBox.Show(string.Join("\n\n", erros), "Erro ao carregar dados", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }
        autoCompletePlanilha.Text = string.Empty;
        autoCompleteDescricao.Text = string.Empty;
        autoCompleteDescricaoAdicional.Text = string.Empty;
        txtCodProduto.Text = string.Empty;
        autoCompleteComplementoAdicional.Text = string.Empty;
        radMaskedUnidade.Text = string.Empty;
    }

    private async void OnAlterarClick(object sender, RoutedEventArgs e)
    {
        if (bolsa.SelectedItem == null)
        {
            MessageBox.Show("Selecione uma bolsa.", "Atenção", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        else if (string.IsNullOrEmpty(txtCodProduto.Text))
        {
            MessageBox.Show("Informe o código do produto.", "Atenção", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        else if (string.IsNullOrEmpty(txtQuantidade.Text) || !double.TryParse(txtQuantidade.Text, out double quantidade) || quantidade <= 0)
        {
            MessageBox.Show("Informe uma quantidade válida.", "Atenção", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        BolsaFixaViewModel vm = (BolsaFixaViewModel)DataContext;
        List<string> erros = [];

        RadWindow.Confirm(
            new DialogParameters
            {
                Content = "Deseja realmente excluir este item?",
                Header = "Confirmação",
                OkButtonContent = "Sim",
                CancelButtonContent = "Não",
                Closed = async (sender, e) => 
                { 
                    if (e.DialogResult == true)
                    {
                        vm.ItemBolsa.codcompladicional = !string.IsNullOrWhiteSpace(txtCodProduto.Text) ? Convert.ToInt64(txtCodProduto.Text) : (long?)null;
                        vm.ItemBolsa.quantidade = !string.IsNullOrWhiteSpace(txtQuantidade.Text) ? Convert.ToDouble(txtQuantidade.Text) : (double?)null;
                        string? erro = await vm.AlterarBolsaFixaAsync(vm.ItemBolsa);
                        if (!string.IsNullOrEmpty(erro)) erros.Add(erro);
                        string? erro1 = await vm.GetBolsaProdutosAsync((bolsa.SelectedValue as TipoBolsaModel)?.codigo_tipobolsa);
                        if (!string.IsNullOrEmpty(erro1)) erros.Add(erro1);
                        if (erros.Count > 0)
                        {
                            MessageBox.Show(string.Join("\n\n", erros), "Erro ao carregar dados", MessageBoxButton.OK, MessageBoxImage.Error);
                            return;
                        }
                        autoCompletePlanilha.Text = string.Empty;
                        autoCompleteDescricao.Text = string.Empty;
                        autoCompleteDescricaoAdicional.Text = string.Empty;
                        txtCodProduto.Text = string.Empty;
                        autoCompleteComplementoAdicional.Text = string.Empty;
                        radMaskedUnidade.Text = string.Empty;
                        txtQuantidade.Text = string.Empty;
                    }
                }
            }); 
    }

    private async void RadGridView_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        BolsaFixaViewModel vm = (BolsaFixaViewModel)DataContext;
        List<string> erros = [];
        if (e.Key == Key.Delete)
        {
            var gridView = sender as RadGridView;
            if (gridView?.SelectedItem != null)
            {
                bool DialogResult = false;
                DialogParameters param = new()
                {
                    Content = "Deseja excluir o produto selecionado da bolsa fixa?",
                    Owner = App.Current.MainWindow
                };
                param.Closed += (sender, e) => {
                    DialogResult = e.DialogResult == true;
                };

                RadWindow.Confirm(param);

                if (DialogResult)
                {
                    string? erro = await vm.DeletarBolsaFixaAsync(gridView.SelectedItem as BolsaFixaDTO);
                    if (!string.IsNullOrEmpty(erro)) erros.Add(erro);
                    string? erro1 = await vm.GetBolsaProdutosAsync((gridView.SelectedItem as BolsaFixaDTO).codigo_tipobolsa);
                    if (!string.IsNullOrEmpty(erro1)) erros.Add(erro1);
                    if (erros.Count > 0)
                    {
                        MessageBox.Show(string.Join("\n\n", erros), "Erro ao carregar dados", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }
                }
            }
        }
    }

    private async void RadGridView_CellEditEnded(object sender, GridViewCellEditEndedEventArgs e)
    {
        var linha = e.Cell.DataContext as BolsaFixaDTO;
        try
        {
            //using var connection = new NpgsqlConnection(_connectionString);
            using var connection = new NpgsqlConnection(_dataBaseSettings.ConnectionString);
            string sql = @"
                UPDATE comercial.tblcustodescadicional
                SET custo = @custo
                WHERE codcompladicional = @codcompladicional;
            ";

            var rowsAffected = await connection.ExecuteAsync(sql, new
            {
                custo = linha.valor_unitario,
                linha.codcompladicional
            });
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Erro ao atualizar custo: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}

public partial class BolsaFixaViewModel : ObservableObject
{
    private readonly DatabaseContext _dbContext;
    private readonly DataBaseSettings _dataBaseSettings;

    public BolsaFixaViewModel()
    {
        _dbContext = new DatabaseContext();
        _dataBaseSettings = DataBaseSettings.Instance;
        Bolsas = [];
        Planilhas = [];
        Produtos = [];
        DescricaoAdicionais = [];
        ComplemntoAdicionais = [];
        Produto = new QryDescricaoModel();
    }

    [ObservableProperty]
    private bool isBusy;

    [ObservableProperty]
    private ObservableCollection<TipoBolsaModel> bolsas;

    [ObservableProperty]
    private ObservableCollection<RelplanModel> planilhas;

    [ObservableProperty]
    private ObservableCollection<ProdutoModel> produtos;

    [ObservableProperty]
    private ObservableCollection<TabelaDescAdicionalModel> descricaoAdicionais;

    [ObservableProperty]
    private ObservableCollection<TblComplementoAdicionalModel> complemntoAdicionais;

    [ObservableProperty]
    private ObservableCollection<BolsaFixaDTO> bolsaFixas;

    [ObservableProperty]
    private QryDescricaoModel? produto;

    [ObservableProperty]
    private BolsaFixaDTO? itemBolsa;

    public async Task<string?> CarregarBolsasAsync()
    {
        try
        {
            IsBusy = true;
            var data = await _dbContext
                .TipoBolsas
                .OrderBy(c => c.descricao)
                .ToListAsync();

            Bolsas = new ObservableCollection<TipoBolsaModel>(data);
            return null; // sucesso
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException pgEx)
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

    public async Task<string?> CarregarPlanilhasAsync()
    {
        try
        {
            IsBusy = true;
            var data = await _dbContext
                .Relplans
                .Where(p => p.planilha != null && p.planilha.StartsWith("ALMOX")) // Verifica se 'planilha' não é nulo antes de chamar StartsWith
                .OrderBy(c => c.planilha)
                .ToListAsync();

            Planilhas = new ObservableCollection<RelplanModel>(data);
            return null; // sucesso
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException pgEx)
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

    public async Task<string?> GetBolsaProdutosAsync(long? codBolsa)
    {
        try
        {


            IsBusy = true;
            var parameters = new { bolsa = codBolsa };
            string sql = @"
                SELECT
                    b.cod_bolsafixa,
	                b.codigo_tipobolsa,
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
                FROM almoxarifado_apoio.tblbolsafixa AS b
                JOIN producao.qry3descricoes AS p ON b.codcompladicional = p.codcompladicional
                WHERE b.codigo_tipobolsa = @bolsa
                ORDER BY b.codigo_tipobolsa, p.planilha, p.descricao_completa;
            ";
            using var connection = new NpgsqlConnection(_dataBaseSettings.ConnectionString);
            await connection.OpenAsync();
            var resultado = (await connection.QueryAsync<BolsaFixaDTO>(sql, parameters)).ToList();
            await connection.CloseAsync();

            BolsaFixas = new ObservableCollection<BolsaFixaDTO>(resultado);
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

    public async Task<string?> GetDescricoesAsync(string planilha)
    {
        try
        {
            IsBusy = true;
            using DatabaseContext db = new();
            var data = await db.Produtos
                .Where(produto => produto.planilha == planilha && produto.inativo.Contains("0"))
                .OrderBy(descricao => descricao)
                .ToListAsync();
            Produtos = new ObservableCollection<ProdutoModel>(data);    
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

    public async Task<string?> GetDescricoesAdicionaisAsync(long? codigoproduto)
    {
        try
        {
            IsBusy = true;
            using DatabaseContext db = new();
            var data = await db.DescAdicionais
                .Where(produto => produto.codigoproduto == codigoproduto && produto.inativo.Contains("0"))
                .OrderBy(descricaoAdicional => descricaoAdicional)
                .ToListAsync();
            DescricaoAdicionais = new ObservableCollection<TabelaDescAdicionalModel>(data);
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

    public async Task<string?> GetComplementoAdicionaisAsync(long? coduniadicional)
    {
        try
        {
            IsBusy = true;
            using DatabaseContext db = new();
            var data = await db.ComplementoAdicionais
                .Where(produto => produto.coduniadicional == coduniadicional && produto.inativo.Contains("0"))
                .OrderBy(complementoAdicional => complementoAdicional.complementoadicional)
                .ToListAsync();
            ComplemntoAdicionais = new ObservableCollection<TblComplementoAdicionalModel>(data);
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

    public async Task<string?> SalvarBolsaFixaAsync(BolsaFixaDTO bolsaFixa)
    {
        try
        {
            IsBusy = true;
            using DatabaseContext db = new();
            var novaBolsa = new BolsaFixaModel
            {
                codcompladicional = bolsaFixa.codcompladicional,
                codigo_tipobolsa = bolsaFixa.codigo_tipobolsa,
                quantidade = bolsaFixa.quantidade ?? 0
            };
            await db.BolsaFixas.AddAsync(novaBolsa);
            await db.SaveChangesAsync();
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

    public async Task<string?> AlterarBolsaFixaAsync(BolsaFixaDTO bolsaFixa)
    {
        try
        {
            IsBusy = true;
            using DatabaseContext db = new();
            var tipoBolsaExistente = await db.BolsaFixas.FindAsync(bolsaFixa.cod_bolsafixa);
            var novaBolsa = tipoBolsaExistente;
            novaBolsa.codcompladicional = bolsaFixa.codcompladicional;
            novaBolsa.quantidade = bolsaFixa.quantidade ?? 0;
            db.Entry(tipoBolsaExistente).CurrentValues.SetValues(novaBolsa);
            await db.SaveChangesAsync();
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

    public async Task<string?> DeletarBolsaFixaAsync(BolsaFixaDTO bolsaFixa)
    {
        try
        {
            IsBusy = true;
            using DatabaseContext db = new();
            var tipoBolsaExistente = await db.BolsaFixas.FindAsync(bolsaFixa.cod_bolsafixa);
            db.BolsaFixas.Remove(tipoBolsaExistente);
            await db.SaveChangesAsync();
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

    public async Task<string?> GetProdutoAsync(long? codcompleadicional)
    {
        try
        {
            IsBusy = true;
            using DatabaseContext db = new();
            Produto = await db.Descricoes.FirstOrDefaultAsync(p => p.codcompladicional == codcompleadicional);
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
