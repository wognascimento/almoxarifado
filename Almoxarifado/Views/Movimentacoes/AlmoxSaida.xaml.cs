using Almoxarifado.DataBase;
using Almoxarifado.DataBase.Model;
using Dapper;
using Npgsql;
using SharpDX;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Telerik.Windows.Controls;
using Telerik.Windows.Controls.AutoSuggestBox;

namespace Almoxarifado.Views.Movimentacoes
{
    /// <summary>
    /// Interação lógica para AlmoxSaida.xam
    /// </summary>
    public partial class AlmoxSaida : UserControl
    {
        public AlmoxSaida()
        {
            InitializeComponent();
            DataContext = new AlmoxSaidaViewModel();

            this.autoCompletePlanilha.ClearButtonCommand = new DelegateCommand(OnClearPlanilhaExecuted);
            this.autoCompleteDescricao.ClearButtonCommand = new DelegateCommand(OnClearDescricaoExecuted);
            this.autoCompleteDescricaoAdicional.ClearButtonCommand = new DelegateCommand(OnClearDescricaoAdicionalExecuted);
            this.autoCompleteComplementoAdicional.ClearButtonCommand = new DelegateCommand(OnClearComplementoAdicionalExecuted);
        }

        private async void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            Loaded -= UserControl_Loaded;
            try
            {
                Application.Current.Dispatcher.Invoke(() => { Mouse.OverrideCursor = Cursors.Wait; });
                AlmoxSaidaViewModel vm = (AlmoxSaidaViewModel)DataContext;
                vm.Atendentes = await Task.Run(vm.GetAtendentesAsync);
                vm.Funcionarios = await Task.Run(vm.GetFuncionariosAsync);
                vm.Planilhas = await Task.Run(vm.GetPlanilhasAsync);
                Application.Current.Dispatcher.Invoke(() => { Mouse.OverrideCursor = null; });
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                Application.Current.Dispatcher.Invoke(() => { Mouse.OverrideCursor = null; });
            }
        }

        private async void OnBuscaProduto(object sender, KeyEventArgs e)
        {
            try
            {
                //sender = {System.Windows.Controls.TextBox}

                if (e.Key == Key.Enter)
                {
                    //string txtProduto = (sender as TextBox).Text;
                    //sender = {Telerik.Windows.Controls.RadAutoSuggestBox}
                    string txtProduto = ((RadAutoSuggestBox)sender).Text;
                    Application.Current.Dispatcher.Invoke(() => { Mouse.OverrideCursor = Cursors.Wait; });
                    AlmoxSaidaViewModel vm = (AlmoxSaidaViewModel)DataContext;
                    vm.Produto = await Task.Run(() => vm.GetProdutoAsync(long.Parse(txtProduto)));
                    if (vm.Produto == null)
                    {
                        Application.Current.Dispatcher.Invoke(() => { Mouse.OverrideCursor = null; });
                        RadWindow.Alert(new DialogParameters()
                        {
                            Content = "O Produto não foi encontrado",
                            Header = "Atenção",
                        });

                        return;
                    }

                    vm.Descricoes = await Task.Run(() => vm.GetDescricoesAsync(vm.Produto.planilha));
                    vm.DescricaoAdicionais = await Task.Run(() => vm.GetDescricoesAdicionaisAsync(vm.Produto.codigo));
                    vm.ComplemntoAdicionais = await Task.Run(() => vm.GetComplementoAdicionaisAsync(vm.Produto.coduniadicional));

                    Application.Current.Dispatcher.Invoke(() => { Mouse.OverrideCursor = null; });
                    txtEstoque.Text = vm.Produto.saldo_estoque.ToString();
                    //txtStatus.Text = vm.Produto.status;
                    if (vm.Produto.saldo_estoque > vm.Produto.estoque_min && (vm.Produto.estoque_min / vm.Produto.saldo_estoque) > 0)
                        txtStatus.Text = "AVALIAR COMPRA";
                    else if (vm.Produto.saldo_estoque < vm.Produto.estoque_min)
                        txtStatus.Text = "SOLICITAR COMPRA";
                    else
                        txtStatus.Text = "SALDO OK";

                    autoCompletePlanilha.Text = vm.Produto.planilha;
                    autoCompleteDescricao.Text = vm.Produto.descricao;
                    autoCompleteDescricaoAdicional.Text = vm.Produto.descricao_adicional;
                    autoCompleteComplementoAdicional.Text = vm.Produto.complementoadicional;
                    radMaskedUnidade.Text = vm.Produto.unidade;
                    txtQuantidade.Focus();

                    if (distino.Text == "ACERTO ESTOQUE")
                    {
                        var bloqueio = await Task.Run(() => vm.GetVerificaBloqueioAcertoEstoqueAsync(vm.Produto.codcompladicional));
                        if (bloqueio != null)
                        {
                            bloq.Visibility = Visibility.Visible;
                            btnGravar.IsEnabled = false;
                        }
                        else
                        {
                            bloq.Visibility = Visibility.Collapsed;
                            btnGravar.IsEnabled = true;
                        }
                    }
                }
                
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                Application.Current.Dispatcher.Invoke(() => { Mouse.OverrideCursor = null; });
            }
        }

        private void OnClearPlanilhaExecuted(object obj)
        {
            AlmoxSaidaViewModel vm = (AlmoxSaidaViewModel)DataContext;

            this.autoCompletePlanilha.Text = string.Empty;

            vm.Descricoes.Clear();
            autoCompleteDescricao.Text = string.Empty;

            vm.DescricaoAdicionais.Clear();
            autoCompleteDescricaoAdicional.Text = string.Empty;

            vm.ComplemntoAdicionais.Clear();
            autoCompleteComplementoAdicional.Text = string.Empty;

            radMaskedUnidade.Text = string.Empty;

        }

        private void OnFilterPlanilha(object sender, Telerik.Windows.Controls.AutoSuggestBox.TextChangedEventArgs e)
        {
            if (e.Reason == TextChangeReason.UserInput)
                this.autoCompletePlanilha.ItemsSource = GetPlanilhasByText(this.autoCompletePlanilha.Text);
        }

        private ObservableCollection<RelplanModel> GetPlanilhasByText(string searchText)
        {
            AlmoxSaidaViewModel vm = (AlmoxSaidaViewModel)DataContext;
            var lowerText = searchText.ToLowerInvariant();
            return new ObservableCollection<RelplanModel>(vm.Planilhas.Where(item => item.planilha.ToLowerInvariant().Contains(lowerText)).ToList());
        }

        private async void OnPlanilhaQuerySubmitted(object sender, QuerySubmittedEventArgs e)
        {
            try
            {
                AlmoxSaidaViewModel vm = (AlmoxSaidaViewModel)DataContext;
                Application.Current.Dispatcher.Invoke(() => { Mouse.OverrideCursor = Cursors.Wait; });
                vm.Descricoes = await Task.Run(() => vm.GetDescricoesAsync(e.QueryText));
                Application.Current.Dispatcher.Invoke(() => { Mouse.OverrideCursor = null; });
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                Application.Current.Dispatcher.Invoke(() => { Mouse.OverrideCursor = null; });
            }
        }

        private void OnClearDescricaoExecuted(object obj)
        {
            AlmoxSaidaViewModel vm = (AlmoxSaidaViewModel)DataContext;

            autoCompleteDescricao.Text = string.Empty;

            autoCompleteDescricaoAdicional.Text = string.Empty;
            vm.DescricaoAdicionais.Clear();

            autoCompleteComplementoAdicional.Text = string.Empty;
            vm.ComplemntoAdicionais.Clear();

            radMaskedUnidade.Text = string.Empty;
        }

        private void OnFilterDescricoes(object sender, Telerik.Windows.Controls.AutoSuggestBox.TextChangedEventArgs e)
        {
            if (e.Reason == TextChangeReason.UserInput)
                this.autoCompleteDescricao.ItemsSource = GetDescricoessByText(this.autoCompleteDescricao.Text);
        }

        private ObservableCollection<ProdutoModel> GetDescricoessByText(string searchText)
        {
            AlmoxSaidaViewModel vm = (AlmoxSaidaViewModel)DataContext;
            var lowerText = searchText.ToLowerInvariant();
            return new ObservableCollection<ProdutoModel>(vm.Descricoes.Where(item => item.descricao.ToLowerInvariant().Contains(lowerText)).ToList());
        }

        private async void OnDescricaoQuerySubmitted(object sender, QuerySubmittedEventArgs e)
        {
            try
            {
                AlmoxSaidaViewModel vm = (AlmoxSaidaViewModel)DataContext;
                Application.Current.Dispatcher.Invoke(() => { Mouse.OverrideCursor = Cursors.Wait; });
                vm.DescricaoAdicionais = await Task.Run(() => vm.GetDescricoesAdicionaisAsync((e.Suggestion as ProdutoModel).codigo));
                Application.Current.Dispatcher.Invoke(() => { Mouse.OverrideCursor = null; });
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                Application.Current.Dispatcher.Invoke(() => { Mouse.OverrideCursor = null; });
            }
        }

        private void OnClearDescricaoAdicionalExecuted(object obj)
        {
            AlmoxSaidaViewModel vm = (AlmoxSaidaViewModel)DataContext;

            autoCompleteDescricaoAdicional.Text = string.Empty;

            autoCompleteComplementoAdicional.Text = string.Empty;
            vm.ComplemntoAdicionais.Clear();

            radMaskedUnidade.Text = string.Empty;
        }

        private void OnFilterDescricaoAdicionais(object sender, Telerik.Windows.Controls.AutoSuggestBox.TextChangedEventArgs e)
        {
            if (e.Reason == TextChangeReason.UserInput)
                this.autoCompleteDescricaoAdicional.ItemsSource = GetDescricaoAdicionaisByText(this.autoCompleteDescricaoAdicional.Text);
        }

        private ObservableCollection<TabelaDescAdicionalModel> GetDescricaoAdicionaisByText(string searchText)
        {
            AlmoxSaidaViewModel vm = (AlmoxSaidaViewModel)DataContext;
            var lowerText = searchText.ToLowerInvariant();
            return new ObservableCollection<TabelaDescAdicionalModel>(vm.DescricaoAdicionais.Where(item => item.descricao_adicional.ToLowerInvariant().Contains(lowerText)).ToList());
        }

        private async void OnDescricaoAdicionaisQuerySubmitted(object sender, QuerySubmittedEventArgs e)
        {
            try
            {
                AlmoxSaidaViewModel vm = (AlmoxSaidaViewModel)DataContext;
                Application.Current.Dispatcher.Invoke(() => { Mouse.OverrideCursor = Cursors.Wait; });
                vm.ComplemntoAdicionais = await Task.Run(() => vm.GetComplementoAdicionaisAsync((e.Suggestion as TabelaDescAdicionalModel).coduniadicional));
                Application.Current.Dispatcher.Invoke(() => { Mouse.OverrideCursor = null; });
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                Application.Current.Dispatcher.Invoke(() => { Mouse.OverrideCursor = null; });
            }
        }

        private void OnClearComplementoAdicionalExecuted(object obj)
        {
            AlmoxSaidaViewModel vm = (AlmoxSaidaViewModel)DataContext;
            autoCompleteComplementoAdicional.Text = string.Empty;

            txtEstoque.Text = string.Empty;
            txtStatus.Text = string.Empty;
            radMaskedUnidade.Text = string.Empty;
        }

        private void OnFilterComplemntoAdicionais(object sender, Telerik.Windows.Controls.AutoSuggestBox.TextChangedEventArgs e)
        {
            if (e.Reason == TextChangeReason.UserInput)
                this.autoCompleteComplementoAdicional.ItemsSource = GetOnFilterComplemntoAdicionaisByText(this.autoCompleteComplementoAdicional.Text);
        }

        private ObservableCollection<TblComplementoAdicionalModel> GetOnFilterComplemntoAdicionaisByText(string searchText)
        {
            AlmoxSaidaViewModel vm = (AlmoxSaidaViewModel)DataContext;
            var lowerText = searchText.ToLowerInvariant();
            return new ObservableCollection<TblComplementoAdicionalModel>(vm.ComplemntoAdicionais.Where(item => item.complementoadicional.ToLowerInvariant().Contains(lowerText)).ToList());
        }

        private async void OnComplemntoAdicionaisQuerySubmitted(object sender, QuerySubmittedEventArgs e)
        {
            try
            {
                AlmoxSaidaViewModel vm = (AlmoxSaidaViewModel)DataContext;
                Application.Current.Dispatcher.Invoke(() => { Mouse.OverrideCursor = Cursors.Wait; });
                var compAdicional = (e.Suggestion as TblComplementoAdicionalModel);
                txtEstoque.Text = compAdicional.saldo_estoque.ToString();
                if(compAdicional.saldo_estoque > compAdicional.estoque_min && (compAdicional.estoque_min / compAdicional.saldo_estoque) > 0)
                    txtStatus.Text = "AVALIAR COMPRA";
                else if(compAdicional.saldo_estoque < compAdicional.estoque_min)
                    txtStatus.Text = "SOLICITAR COMPRA";
                else
                    txtStatus.Text = "SALDO OK";

                radMaskedUnidade.Text = compAdicional.unidade;

                vm.Produto = await Task.Run(() => vm.GetProdutoAsync(compAdicional.codcompladicional));

                if(distino.Text == "ACERTO ESTOQUE")
                {
                    var bloqueio = await Task.Run(() => vm.GetVerificaBloqueioAcertoEstoqueAsync(vm.Produto.codcompladicional));
                    if (bloqueio != null)
                    {
                        bloq.Visibility = Visibility.Visible;
                        btnGravar.IsEnabled = false;
                    }
                    else
                    {
                        bloq.Visibility = Visibility.Collapsed;
                        btnGravar.IsEnabled = true;
                    }
                }
                

                Application.Current.Dispatcher.Invoke(() => { Mouse.OverrideCursor = null; });
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                Application.Current.Dispatcher.Invoke(() => { Mouse.OverrideCursor = null; });
            }
        }

        private void autoCompletePlanilha_GotFocus(object sender, RoutedEventArgs e)
        {
            autoCompletePlanilha.IsDropDownOpen = true;
        }

        private void autoCompleteDescricao_GotFocus(object sender, RoutedEventArgs e)
        {
            autoCompleteDescricao.IsDropDownOpen = true;
        }

        private void autoCompleteDescricaoAdicional_GotFocus(object sender, RoutedEventArgs e)
        {
            autoCompleteDescricaoAdicional.IsDropDownOpen = true;
        }

        private void autoCompleteComplementoAdicional_GotFocus(object sender, RoutedEventArgs e)
        {
            autoCompleteComplementoAdicional.IsDropDownOpen = true;
        }

        private async void OnGravarClick(object sender, RoutedEventArgs e)
        {
            try
            {
                AlmoxSaidaViewModel vm = (AlmoxSaidaViewModel)DataContext;

                bool validar = false;
                validar = GetValidarAsync();

                if (!validar)
                    return;

                if (distino.Text == "ACERTO ESTOQUE")
                {
                    var bloqueio = await Task.Run(() => vm.GetVerificaBloqueioAcertoEstoqueAsync(vm.Produto.codcompladicional));
                    if (bloqueio != null)
                    {
                        bloq.Visibility = Visibility.Visible;
                        btnGravar.IsEnabled = false;
                        return;
                    }
                    else
                    {
                        bloq.Visibility = Visibility.Collapsed;
                        btnGravar.IsEnabled = true;
                    }
                }

                    Application.Current.Dispatcher.Invoke(() => { Mouse.OverrideCursor = Cursors.Wait; });

                var barcode = await Task.Run(() => vm.GetBarcodeAsync((int)vm.Produto.codcompladicional));
                var saida = new SaidaAlmoxModel 
                {
                    barcode = barcode.barcode,
                    codcompladicional = vm.Produto.codcompladicional,
                    qtde = double.Parse(txtQuantidade.Text),
                    data = dtSaida.SelectedDate.Value.Date,
                    //hora = DateTime.Now.Hour,
                    resp = atendentes.Text,
                    requisicao = 0,
                    destino = distino.Text,
                    incluido_por = Environment.UserName,
                    data_inclusao = DateTime.Now,
                    funcionario = ((FuncionarioModel)funcionario.SelectedItem).nome_apelido,
                    codfun = ((FuncionarioModel)funcionario.SelectedItem).codfun,
                    setor = ((FuncionarioModel)funcionario.SelectedItem).setor,
                    unidade = vm.Produto.unidade,
                    num_os = long.Parse(ordemServico.Text),
                    endereco = endereco.Text,
                    cod_movimentacao = long.Parse(codMovimentacao.Text),
                };

                await Task.Run(() => vm.SaidaAsync(saida));
                vm.MovSaidaItens = await Task.Run(() => vm.GetItensMovimentacaoAsync((long)saida.cod_movimentacao));

                this.autoCompletePlanilha.Text = string.Empty;

                vm.Descricoes.Clear();
                autoCompleteDescricao.Text = string.Empty;

                vm.DescricaoAdicionais.Clear();
                autoCompleteDescricaoAdicional.Text = string.Empty;

                vm.ComplemntoAdicionais.Clear();
                autoCompleteComplementoAdicional.Text = string.Empty;

                ordemServico.Text = string.Empty;
                txtQuantidade.Text = string.Empty;

                radMaskedUnidade.Text = string.Empty;

                Application.Current.Dispatcher.Invoke(() => { Mouse.OverrideCursor = null; });
            }
            catch (Exception ex)
            {
                Application.Current.Dispatcher.Invoke(() => { Mouse.OverrideCursor = null; });
                RadWindow.Alert(new DialogParameters()
                {
                    Content = ex.Message,
                    Header = "Error",
                });
            }
        }

        private bool GetValidarAsync()
        {
            AlmoxSaidaViewModel vm = (AlmoxSaidaViewModel)DataContext;

            if (codMovimentacao.Text == null || codMovimentacao.Text == "")
            {
                RadWindow.Alert(new DialogParameters() { Content = "Cria Código de Movimentação.", Header = "Atenção", });
                return false;
            }
            else if (funcionario.SelectedItem == null)
            {
                RadWindow.Alert(new DialogParameters() { Content = "Seleciona o Funcionário.", Header = "Atenção", });
                return false;
            }
            else if(distino.SelectedItem == null)
            {
                RadWindow.Alert(new DialogParameters() { Content = "Seleciona o Destino.", Header = "Atenção", });
                return false;
            }
            else if(atendentes.SelectedItem == null)
            {
                RadWindow.Alert(new DialogParameters() { Content = "Seleciona o Atendente.", Header = "Atenção", });
                return false;
            }
            else if (dtSaida.SelectedDate == null)
            {
                RadWindow.Alert(new DialogParameters() { Content = "Seleciona a Data.", Header = "Atenção", });
                return false;
            }
            else if (vm.Produto == null)
            {
                RadWindow.Alert(new DialogParameters() { Content = "Seleciona o Produto.", Header = "Atenção", });
                return false;
            }
            else if (txtQuantidade.Text == null || txtQuantidade.Text == "")
            {
                RadWindow.Alert(new DialogParameters() { Content = "Informe a Quantidade.", Header = "Atenção", });
                return false;
            }
            else if (ordemServico.Text == null || ordemServico.Text == "")
            {
                RadWindow.Alert(new DialogParameters() { Content = "Informe o número da Ordem de Serviço.", Header = "Atenção", });
                return false;
            }

            return true;
        }

        private async void OnCodMovimentacao(object sender, RoutedEventArgs e)
        {
            try
            {
                AlmoxSaidaViewModel vm = (AlmoxSaidaViewModel)DataContext;
                Application.Current.Dispatcher.Invoke(() => { Mouse.OverrideCursor = Cursors.Wait; });
                var codMov = await Task.Run(vm.GetCodMovimentacaoAsync);

                codMovimentacao.Text = codMov.ToString();
                Application.Current.Dispatcher.Invoke(() => { Mouse.OverrideCursor = null; });
            }
            catch (Exception ex)
            {
                Application.Current.Dispatcher.Invoke(() => { Mouse.OverrideCursor = null; });
                RadWindow.Alert(new DialogParameters()
                {
                    Content = ex.Message,
                    Header = "Error",
                });
            }
        }
    }

    public class AlmoxSaidaViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        public void RaisePropertyChanged(string propName) { this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName)); }

        private ObservableCollection<string> _destinos = ["ADMINISTRAÇÃO", "ACERTO ESTOQUE", "ACERTO MOMADES", "CLIENTE", "MANUTENÇÃO GALPÃO", "MOMADES", "PRODUÇÃO", "TRANSFERENCIA ALMOX", "TREINAMENTO"];
        public ObservableCollection<string> Destinos
        {
            get { return _destinos; }
            set { _destinos = value; RaisePropertyChanged("Destinos"); }
        }

        private ObservableCollection<AtendenteAlmoxModel> _atendentes;
        public ObservableCollection<AtendenteAlmoxModel> Atendentes
        {
            get { return _atendentes; }
            set { _atendentes = value; RaisePropertyChanged("Atendentes"); }
        }

        private ObservableCollection<FuncionarioModel> _funcionarios;
        public ObservableCollection<FuncionarioModel> Funcionarios
        {
            get { return _funcionarios; }
            set { _funcionarios = value; RaisePropertyChanged("Funcionarios"); }
        }

        private ObservableCollection<ProdutoAlmoxModel> _produtos;
        public ObservableCollection<ProdutoAlmoxModel> Produtos
        {
            get { return _produtos; }
            set { _produtos = value; RaisePropertyChanged("Produtos"); }
        }

        private QryDescricaoModel _produto;
        public QryDescricaoModel Produto
        {
            get { return _produto; }
            set { _produto = value; RaisePropertyChanged("Produto"); }
        }

        private ObservableCollection<RelplanModel> _planilhas;
        public ObservableCollection<RelplanModel> Planilhas
        {
            get { return _planilhas; }
            set { _planilhas = value; RaisePropertyChanged("Planilhas"); }
        }

        private ObservableCollection<ProdutoModel> _descricoes;
        public ObservableCollection<ProdutoModel> Descricoes
        {
            get { return _descricoes; }
            set { _descricoes = value; RaisePropertyChanged("Descricoes"); }
        }

        private ObservableCollection<TabelaDescAdicionalModel> _descricaoAdicionais;
        public ObservableCollection<TabelaDescAdicionalModel> DescricaoAdicionais
        {
            get { return _descricaoAdicionais; }
            set { _descricaoAdicionais = value; RaisePropertyChanged("DescricaoAdicionais"); }
        }

        private ObservableCollection<TblComplementoAdicionalModel> _ComplemntoAdicionais;
        public ObservableCollection<TblComplementoAdicionalModel> ComplemntoAdicionais
        {
            get { return _ComplemntoAdicionais; }
            set { _ComplemntoAdicionais = value; RaisePropertyChanged("ComplemntoAdicionais"); }
        }
        
        private SaidaAlmoxModel _saida;
        public SaidaAlmoxModel Saida
        {
            get { return _saida; }
            set { _saida = value; RaisePropertyChanged("Saida"); }
        }
        
        private BarcodeModel _barcode;
        public BarcodeModel Barcode
        {
            get { return _barcode; }
            set { _barcode = value; RaisePropertyChanged("Barcode"); }
        }

        private ObservableCollection<ResultadoMovimentacao> _movSaidaItens;
        public ObservableCollection<ResultadoMovimentacao> MovSaidaItens
        {
            get { return _movSaidaItens; }
            set { _movSaidaItens = value; RaisePropertyChanged("MovSaidaItens"); }
        }


        public async Task<ObservableCollection<AtendenteAlmoxModel>> GetAtendentesAsync()
        {
            try
            {
                await using var conn = new NpgsqlConnection(DataBaseSettings.Instance.ConnectionString);
                var data = await conn.QueryAsync<AtendenteAlmoxModel>(@"
                    SELECT nome_funcionario, funcao, username
                    FROM almoxarifado_jac.tbl_atendentes_almox
                    ORDER BY nome_funcionario;");
                return new ObservableCollection<AtendenteAlmoxModel>(data);
            }
            catch (Exception)
            {
                throw;
            }
        }

        //public async Task<ObservableCollection<FuncionarioUnionModel>> GetFuncionariosAsync()
        public async Task<ObservableCollection<FuncionarioModel>> GetFuncionariosAsync()
        {
            try
            {
                await using var conn = new NpgsqlConnection(DataBaseSettings.Instance.ConnectionString);
                var result = await conn.QueryAsync<FuncionarioModel>(@"
                    SELECT *
                    FROM almoxarifado_jac.view_funcionarios_terceiros
                    ORDER BY nome_apelido, setor;");
                return new ObservableCollection<FuncionarioModel>(result);
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task<QryDescricaoModel> GetProdutoAsync(long? codcompladicional)
        {
            try
            {
                await using var conn = new NpgsqlConnection(DataBaseSettings.Instance.ConnectionString);
                var data = await conn.QueryFirstOrDefaultAsync<QryDescricaoModel>(@"
                    SELECT *
                    FROM producao.qry3descricoes
                    WHERE codcompladicional = @codcompladicional
                      AND planilha LIKE 'ALMO%'
                      AND COALESCE(inativo, '') NOT LIKE '%-1%';", new { codcompladicional });
                return data;
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task<ControleAlmoxEstoqueModel> GetVerificaBloqueioAcertoEstoqueAsync(long? codcompladicional)
        {
            try
            {
                await using var conn = new NpgsqlConnection(DataBaseSettings.Instance.ConnectionString);
                var data = await conn.QueryFirstOrDefaultAsync<ControleAlmoxEstoqueModel>(@"
                    SELECT *
                    FROM pcp.tbl_controle_almox_estoque
                    WHERE bloqueado = '-1'
                      AND codcompladicional = @codcompladicional;", new { codcompladicional });
                return data;
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task<ObservableCollection<RelplanModel>> GetPlanilhasAsync()
        {
            try
            {
                await using var conn = new NpgsqlConnection(DataBaseSettings.Instance.ConnectionString);
                var data = await conn.QueryAsync<RelplanModel>(@"
                    SELECT *
                    FROM producao.relplan
                    WHERE (planilha LIKE 'ALMO%' OR planilha = 'ESTOQUE TI')
                      AND COALESCE(ativo, '') LIKE '%1%'
                    ORDER BY planilha;");

                return new ObservableCollection<RelplanModel>(data);
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task<ObservableCollection<ProdutoModel>> GetDescricoesAsync(string planilha)
        {
            try
            {
                await using var conn = new NpgsqlConnection(DataBaseSettings.Instance.ConnectionString);
                var data = await conn.QueryAsync<ProdutoModel>(@"
                    SELECT *
                    FROM producao.produtos
                    WHERE planilha = @planilha
                      AND COALESCE(inativo, '') LIKE '%0%'
                    ORDER BY descricao;", new { planilha });
                return new ObservableCollection<ProdutoModel>(data);
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task<ObservableCollection<TabelaDescAdicionalModel>> GetDescricoesAdicionaisAsync(long? codigoproduto)
        {
            try
            {
                await using var conn = new NpgsqlConnection(DataBaseSettings.Instance.ConnectionString);
                var data = await conn.QueryAsync<TabelaDescAdicionalModel>(@"
                    SELECT *
                    FROM producao.tabela_desc_adicional
                    WHERE codigoproduto = @codigoproduto
                      AND COALESCE(inativo, '') LIKE '%0%'
                    ORDER BY descricao_adicional;", new { codigoproduto });
                return new ObservableCollection<TabelaDescAdicionalModel>(data);
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task<ObservableCollection<TblComplementoAdicionalModel>> GetComplementoAdicionaisAsync(long? coduniadicional)
        {
            try
            {
                await using var conn = new NpgsqlConnection(DataBaseSettings.Instance.ConnectionString);
                var data = await conn.QueryAsync<TblComplementoAdicionalModel>(@"
                    SELECT *
                    FROM producao.tblcomplementoadicional
                    WHERE coduniadicional = @coduniadicional
                      AND COALESCE(inativo, '') LIKE '%0%'
                    ORDER BY complementoadicional;", new { coduniadicional });
                return new ObservableCollection<TblComplementoAdicionalModel>(data);
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task SaidaAsync(SaidaAlmoxModel saida)
        {
            await using var conn = new NpgsqlConnection(DataBaseSettings.Instance.ConnectionString);
            await conn.OpenAsync();
            await using var transaction = await conn.BeginTransactionAsync();
            try
            {
                saida.cod_saida_almox = await conn.ExecuteScalarAsync<long>(@"
                    INSERT INTO almoxarifado_jac.t_saida_almox
                        (cod_movimentacao, barcode, qtde, data, resp, requisicao, destino,
                         incluido_por, data_inclusao, funcionario, setor, unidade, expurgo,
                         obs, num_os, endereco, codcompladicional, codfun)
                    VALUES
                        (@cod_movimentacao, @barcode, @qtde, @data, @resp, @requisicao, @destino,
                         @incluido_por, @data_inclusao, @funcionario, @setor, @unidade, @expurgo,
                         @obs, @num_os, @endereco, @codcompladicional, @codfun)
                    RETURNING cod_saida_almox;", saida, transaction);

                if(saida.destino == "ACERTO ESTOQUE")
                {
                    await conn.ExecuteAsync(@"
                        INSERT INTO pcp.tbl_controle_almox_estoque
                            (cod_movimentacao, cod_almox, barcode, codcompladicional, quantidade,
                             data, operacao, processo, num_os, incluido_por, incluido_data, bloqueado)
                        VALUES
                            (@cod_movimentacao, @cod_almox, @barcode, @codcompladicional, @quantidade,
                             @data, @operacao, @processo, @num_os, @incluido_por, @incluido_data, @bloqueado);",
                        new
                        {
                            saida.cod_movimentacao,
                            cod_almox = saida.cod_saida_almox,
                            saida.barcode,
                            saida.codcompladicional,
                            quantidade = saida.qtde,
                            saida.data,
                            operacao = saida.destino,
                            processo = "SAIDA",
                            saida.num_os,
                            saida.incluido_por,
                            incluido_data = saida.data_inclusao,
                            bloqueado = "-1"
                        }, transaction);
                }

                await transaction.CommitAsync();
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<BarcodeModel> GetBarcodeAsync(long codigo)
        {
            try
            {
                await using var conn = new NpgsqlConnection(DataBaseSettings.Instance.ConnectionString);
                return await conn.QueryFirstOrDefaultAsync<BarcodeModel>(@"
                    SELECT codigo, barcode, impresso, compra
                    FROM producao.tbl_barcodes
                    WHERE codigo = @codigo;", new { codigo });
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task<long?> GetCodMovimentacaoAsync()
        {
            try
            {
                await using var conn = new NpgsqlConnection(DataBaseSettings.Instance.ConnectionString);
                var lastMov = await conn.ExecuteScalarAsync<long?>(@"
                    SELECT MAX(cod_movimentacao)
                    FROM almoxarifado_jac.t_saida_almox;");

                return (lastMov ?? 0) + 1;
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task<ObservableCollection<ResultadoMovimentacao>> GetItensMovimentacaoAsync(long CodMovimentacao)
        {
            try
            {
                await using var conn = new NpgsqlConnection(DataBaseSettings.Instance.ConnectionString);
                var resultado = await conn.QueryAsync<ResultadoMovimentacao>(@"
                    SELECT
                        saida.cod_saida_almox,
                        descricao.planilha,
                        descricao.descricao_completa,
                        saida.unidade,
                        saida.qtde
                    FROM almoxarifado_jac.t_saida_almox saida
                    JOIN producao.tbl_barcodes barcode ON saida.barcode = barcode.barcode
                    JOIN producao.qry3descricoes descricao ON barcode.codigo = descricao.codcompladicional
                    WHERE saida.cod_movimentacao = @CodMovimentacao;", new { CodMovimentacao });

                return new ObservableCollection<ResultadoMovimentacao>(resultado);
            }
            catch (Exception)
            {
                throw;
            }
        }
    }
}
