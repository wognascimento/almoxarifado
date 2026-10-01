using Almoxarifado.DataBase;
using Almoxarifado.DataBase.Model;
using Dapper;
using Npgsql;
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
    /// Interação lógica para AlmoxEntrada.xam
    /// </summary>
    public partial class AlmoxEntrada : UserControl
    {
        public AlmoxEntrada()
        {
            InitializeComponent();
            DataContext = new AlmoxEntradaViewModel();


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
                AlmoxEntradaViewModel vm = (AlmoxEntradaViewModel)DataContext;
                vm.Atendentes = await Task.Run(vm.GetAtendentesAsync);
                vm.Funcionarios = await Task.Run(vm.GetFuncionariosAsync);
                //vm.Planilhas = await Task.Run(vm.GetPlanilhasAsync);
                Application.Current.Dispatcher.Invoke(() => { Mouse.OverrideCursor = null; });
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                Application.Current.Dispatcher.Invoke(() => { Mouse.OverrideCursor = null; });
            }
        }

        private async void OnSelectFuncionario(object sender, SelectionChangeEventArgs e)
        {
            try
            {
                AlmoxEntradaViewModel vm = (AlmoxEntradaViewModel)DataContext;
                
                if (e.AddedItems.Count() == 1)
                {
                    FuncionarioUnionModel func = (FuncionarioUnionModel)e.AddedItems[0];
                    Application.Current.Dispatcher.Invoke(() => { Mouse.OverrideCursor = Cursors.Wait; });
                    vm.Planilhas = await Task.Run(() => vm.GetPlanilhasAsync(func.codfun));
                    Application.Current.Dispatcher.Invoke(() => { Mouse.OverrideCursor = null; });
                }
                else
                    vm.Planilhas = null;
            }
            catch (Exception ex)
            {
                Application.Current.Dispatcher.Invoke(() => { Mouse.OverrideCursor = null; });
                MessageBox.Show(ex.Message);
            }
        }

        private async void OnBuscaProduto(object sender, KeyEventArgs e)
        {
            try
            {

                if (e.Key == Key.Enter)
                {
                    string txtProduto = ((RadAutoSuggestBox)sender).Text;
                    Application.Current.Dispatcher.Invoke(() => { Mouse.OverrideCursor = Cursors.Wait; });
                    AlmoxEntradaViewModel vm = (AlmoxEntradaViewModel)DataContext;
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

                    if (origem.Text == "ACERTO ESTOQUE")
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
            AlmoxEntradaViewModel vm = (AlmoxEntradaViewModel)DataContext;

            this.autoCompletePlanilha.Text = string.Empty;

            vm.Descricoes.Clear();
            autoCompleteDescricao.Text = string.Empty;

            vm.DescricaoAdicionais.Clear();
            autoCompleteDescricaoAdicional.Text = string.Empty;

            vm.ComplemntoAdicionais.Clear();
            autoCompleteComplementoAdicional.Text = string.Empty;

            radMaskedUnidade.Text = string.Empty;

            txtEstoque.Text = string.Empty;
            txtStatus.Text = string.Empty;

        }

        private void autoCompletePlanilha_GotFocus(object sender, RoutedEventArgs e)
        {
            autoCompletePlanilha.IsDropDownOpen = true;
        }

        private async void OnPlanilhaQuerySubmitted(object sender, Telerik.Windows.Controls.AutoSuggestBox.QuerySubmittedEventArgs e)
        {
            try
            {
                AlmoxEntradaViewModel vm = (AlmoxEntradaViewModel)DataContext;
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

        private void OnFilterPlanilha(object sender, Telerik.Windows.Controls.AutoSuggestBox.TextChangedEventArgs e)
        {
            if (e.Reason == TextChangeReason.UserInput)
                this.autoCompletePlanilha.ItemsSource = GetPlanilhasByText(this.autoCompletePlanilha.Text);
        }

        private ObservableCollection<RelplanModel> GetPlanilhasByText(string searchText)
        {
            AlmoxEntradaViewModel vm = (AlmoxEntradaViewModel)DataContext;
            var lowerText = searchText.ToLowerInvariant();
            return new ObservableCollection<RelplanModel>(vm.Planilhas.Where(item => item.planilha.ToLowerInvariant().Contains(lowerText)).ToList());
        }


        private void OnClearDescricaoExecuted(object obj)
        {
            AlmoxEntradaViewModel vm = (AlmoxEntradaViewModel)DataContext;

            autoCompleteDescricao.Text = string.Empty;

            autoCompleteDescricaoAdicional.Text = string.Empty;
            vm.DescricaoAdicionais.Clear();

            autoCompleteComplementoAdicional.Text = string.Empty;
            vm.ComplemntoAdicionais.Clear();

            radMaskedUnidade.Text = string.Empty;

            txtEstoque.Text = string.Empty;
            txtStatus.Text = string.Empty;

        }


        private void autoCompleteDescricao_GotFocus(object sender, RoutedEventArgs e)
        {
            autoCompleteDescricao.IsDropDownOpen = true;
        }

        private async void OnDescricaoQuerySubmitted(object sender, QuerySubmittedEventArgs e)
        {
            try
            {
                AlmoxEntradaViewModel vm = (AlmoxEntradaViewModel)DataContext;
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

        private void OnFilterDescricoes(object sender, Telerik.Windows.Controls.AutoSuggestBox.TextChangedEventArgs e)
        {
            if (e.Reason == TextChangeReason.UserInput)
                this.autoCompleteDescricao.ItemsSource = GetDescricoessByText(this.autoCompleteDescricao.Text);
        }

        private ObservableCollection<ProdutoModel> GetDescricoessByText(string searchText)
        {
            AlmoxEntradaViewModel vm = (AlmoxEntradaViewModel)DataContext;
            var lowerText = searchText.ToLowerInvariant();
            return new ObservableCollection<ProdutoModel>(vm.Descricoes.Where(item => item.descricao.ToLowerInvariant().Contains(lowerText)).ToList());
        }

        private void OnClearDescricaoAdicionalExecuted(object obj)
        {
            AlmoxEntradaViewModel vm = (AlmoxEntradaViewModel)DataContext;

            autoCompleteDescricaoAdicional.Text = string.Empty;

            autoCompleteComplementoAdicional.Text = string.Empty;
            vm.ComplemntoAdicionais.Clear();

            radMaskedUnidade.Text = string.Empty;

            txtEstoque.Text = string.Empty;
            txtStatus.Text = string.Empty;

        }

        private void autoCompleteDescricaoAdicional_GotFocus(object sender, RoutedEventArgs e)
        {
            autoCompleteDescricaoAdicional.IsDropDownOpen = true;
        }

        private async void OnDescricaoAdicionaisQuerySubmitted(object sender, QuerySubmittedEventArgs e)
        {
            try
            {
                AlmoxEntradaViewModel vm = (AlmoxEntradaViewModel)DataContext;
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

        private void OnFilterDescricaoAdicionais(object sender, Telerik.Windows.Controls.AutoSuggestBox.TextChangedEventArgs e)
        {
            if (e.Reason == TextChangeReason.UserInput)
                this.autoCompleteDescricaoAdicional.ItemsSource = GetDescricaoAdicionaisByText(this.autoCompleteDescricaoAdicional.Text);
        }

        private ObservableCollection<TabelaDescAdicionalModel> GetDescricaoAdicionaisByText(string searchText)
        {
            AlmoxEntradaViewModel vm = (AlmoxEntradaViewModel)DataContext;
            var lowerText = searchText.ToLowerInvariant();
            return new ObservableCollection<TabelaDescAdicionalModel>(vm.DescricaoAdicionais.Where(item => item.descricao_adicional.ToLowerInvariant().Contains(lowerText)).ToList());
        }

        private void OnClearComplementoAdicionalExecuted(object obj)
        {
            AlmoxEntradaViewModel vm = (AlmoxEntradaViewModel)DataContext;
            autoCompleteComplementoAdicional.Text = string.Empty;

            txtEstoque.Text = string.Empty;
            txtStatus.Text = string.Empty;
            radMaskedUnidade.Text = string.Empty;
        }

        private void autoCompleteComplementoAdicional_GotFocus(object sender, RoutedEventArgs e)
        {
            autoCompleteComplementoAdicional.IsDropDownOpen = true;
        }

        private async void OnComplemntoAdicionaisQuerySubmitted(object sender, Telerik.Windows.Controls.AutoSuggestBox.QuerySubmittedEventArgs e)
        {
            try
            {
                AlmoxEntradaViewModel vm = (AlmoxEntradaViewModel)DataContext;
                Application.Current.Dispatcher.Invoke(() => { Mouse.OverrideCursor = Cursors.Wait; });
                var compAdicional = (e.Suggestion as TblComplementoAdicionalModel);
                txtEstoque.Text = compAdicional.saldo_estoque.ToString();
                if (compAdicional.saldo_estoque > compAdicional.estoque_min && (compAdicional.estoque_min / compAdicional.saldo_estoque) > 0)
                    txtStatus.Text = "AVALIAR COMPRA";
                else if (compAdicional.saldo_estoque < compAdicional.estoque_min)
                    txtStatus.Text = "SOLICITAR COMPRA";
                else
                    txtStatus.Text = "SALDO OK";

                radMaskedUnidade.Text = compAdicional.unidade;

                vm.Produto = await Task.Run(() => vm.GetProdutoAsync(compAdicional.codcompladicional));

                if (origem.Text == "ACERTO ESTOQUE")
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

        private void OnFilterComplemntoAdicionais(object sender, Telerik.Windows.Controls.AutoSuggestBox.TextChangedEventArgs e)
        {
            if (e.Reason == TextChangeReason.UserInput)
                this.autoCompleteComplementoAdicional.ItemsSource = GetOnFilterComplemntoAdicionaisByText(this.autoCompleteComplementoAdicional.Text);
        }

        private ObservableCollection<TblComplementoAdicionalModel> GetOnFilterComplemntoAdicionaisByText(string searchText)
        {
            AlmoxEntradaViewModel vm = (AlmoxEntradaViewModel)DataContext;
            var lowerText = searchText.ToLowerInvariant();
            return new ObservableCollection<TblComplementoAdicionalModel>(vm.ComplemntoAdicionais.Where(item => item.complementoadicional.ToLowerInvariant().Contains(lowerText)).ToList());
        }

        private async void OnGravarClick(object sender, RoutedEventArgs e)
        {
            try
            {
                AlmoxEntradaViewModel vm = (AlmoxEntradaViewModel)DataContext;

                bool validar = false;
                validar = GetValidarAsync();

                if (!validar)
                    return;

                if (origem.Text == "ACERTO ESTOQUE")
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

                Application.Current.Dispatcher.Invoke(() => { Mouse.OverrideCursor = Cursors.Wait; });

                var barcode = await Task.Run(() => vm.GetBarcodeAsync((int)vm.Produto.codcompladicional));
                var entrada = new EntradaAlmoxModel
                {
                    barcode = barcode.barcode,
                    codcompladicional = vm.Produto.codcompladicional,
                    qtde = double.Parse(txtQuantidade.Text),
                    data = dtEntrada.SelectedDate.Value.Date,
                    //hora = DateTime.Now.Hour,
                    resp = atendentes.Text,
                    requisicao = 0,
                    estado = estado.Text,
                    incluido_por = Environment.UserName,
                    incluido_data = DateTime.Now,
                    funcionario = ((FuncionarioUnionModel)funcionario.SelectedItem).nome,
                    codfun = ((FuncionarioUnionModel)funcionario.SelectedItem).codfun,
                    //setor = ((FuncionarioModel)funcionario.SelectedItem).setor,
                    unidade = vm.Produto.unidade,
                    num_os = long.Parse(ordemServico.Text),
                    endereco = endereco.Text,
                    cod_movimentacao = long.Parse(codMovimentacao.Text),
                    origem = origem.Text,
                };

                await Task.Run(() => vm.EntradaAsync(entrada));
                vm.MovSaidaItens = await Task.Run(() => vm.GetItensMovimentacaoAsync((long)entrada.cod_movimentacao));

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

                txtEstoque.Text = string.Empty;
                txtStatus.Text = string.Empty;

                estado.Text = string.Empty;


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
            AlmoxEntradaViewModel vm = (AlmoxEntradaViewModel)DataContext;

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
            else if (origem.SelectedItem == null)
            {
                RadWindow.Alert(new DialogParameters() { Content = "Seleciona o Destino.", Header = "Atenção", });
                return false;
            }
            else if (atendentes.SelectedItem == null)
            {
                RadWindow.Alert(new DialogParameters() { Content = "Seleciona o Atendente.", Header = "Atenção", });
                return false;
            }
            else if (dtEntrada.SelectedDate == null)
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
            else if (estado.SelectedItem == null)
            {
                RadWindow.Alert(new DialogParameters() { Content = "Informe o estado do retorno.", Header = "Atenção", });
                return false;
            }

            return true;
        }

        private async void OnCodMovimentacao(object sender, RoutedEventArgs e)
        {
            try
            {
                AlmoxEntradaViewModel vm = (AlmoxEntradaViewModel)DataContext;
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

    public class AlmoxEntradaViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        public void RaisePropertyChanged(string propName) { this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName)); }

        private ObservableCollection<string> _origens = ["RETORNO INTERNO", "PRODUÇÃO PEÇA NOVA", "CLIENTE", "ACERTO ESTOQUE", "ACERTO MOMADE", "TRANSFERENCIA ALMOX", "COMPRA"];
        public ObservableCollection<string> Origens
        {
            get { return _origens; }
            set { _origens = value; RaisePropertyChanged("Origens"); }
        }

        private ObservableCollection<string> _estados = ["BOM", "MÉDIA", "RUIM"];
        public ObservableCollection<string> Estados
        {
            get { return _estados; }
            set { _estados = value; RaisePropertyChanged("Estados"); }
        }

        private ObservableCollection<AtendenteAlmoxModel> _atendentes;
        public ObservableCollection<AtendenteAlmoxModel> Atendentes
        {
            get { return _atendentes; }
            set { _atendentes = value; RaisePropertyChanged("Atendentes"); }
        }

        private ObservableCollection<FuncionarioUnionModel> _funcionarios;
        public ObservableCollection<FuncionarioUnionModel> Funcionarios
        {
            get { return _funcionarios; }
            set { _funcionarios = value; RaisePropertyChanged("Funcionarios"); }
        }

        private FuncionarioUnionModel _funcionario;
        public FuncionarioUnionModel Funcionario
        {
            get { return _funcionario; }
            set { _funcionario = value; RaisePropertyChanged("Funcionario"); }
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

        private EntradaAlmoxModel _entrada;
        public EntradaAlmoxModel Entrada
        {
            get { return _entrada; }
            set { _entrada = value; RaisePropertyChanged("Entrada"); }
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

        public async Task<ObservableCollection<FuncionarioUnionModel>> GetFuncionariosAsync()
        {
            try
            {
                await using var conn = new NpgsqlConnection(DataBaseSettings.Instance.ConnectionString);
                var result = await conn.QueryAsync<FuncionarioUnionModel>(@"
                    SELECT f.codfun, f.nome_apelido AS nome, f.setor
                    FROM almoxarifado_jac.view_funcionarios_terceiros f
                    WHERE EXISTS (
                        SELECT 1
                        FROM almoxarifado_jac.sig_saldo_funcionario s
                        WHERE s.codfun = f.codfun
                    )
                    ORDER BY nome;");
                return new ObservableCollection<FuncionarioUnionModel>(result);
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task<ObservableCollection<RelplanModel>> GetPlanilhasAsync(long? codfun)
        {
            try
            {
                await using var conn = new NpgsqlConnection(DataBaseSettings.Instance.ConnectionString);
                var data = await conn.QueryAsync<RelplanModel>(@"
                    SELECT DISTINCT rel.*
                    FROM producao.relplan rel
                    JOIN producao.qry3descricoes descricao ON descricao.planilha = rel.planilha
                    JOIN almoxarifado_jac.sig_saldo_funcionario saldo ON saldo.codcompladicional = descricao.codcompladicional
                    WHERE saldo.codfun = @codfun
                    ORDER BY rel.planilha;", new { codfun });

                return new ObservableCollection<RelplanModel>(data);
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

        public async Task<ObservableCollection<ProdutoModel>> GetDescricoesAsync(string planilha)
        {
            try
            {
                await using var conn = new NpgsqlConnection(DataBaseSettings.Instance.ConnectionString);
                var data = await conn.QueryAsync<ProdutoModel>(@"
                    SELECT DISTINCT produto.*
                    FROM producao.produtos produto
                    JOIN producao.qry3descricoes descricao ON descricao.codigo = produto.codigo
                    JOIN almoxarifado_jac.sig_saldo_funcionario saldo ON saldo.codcompladicional = descricao.codcompladicional
                    WHERE saldo.codfun = @codfun
                      AND produto.planilha = @planilha
                    ORDER BY produto.descricao;",
                    new { codfun = Funcionario.codfun, planilha });
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
                    SELECT DISTINCT adicional.*
                    FROM producao.tabela_desc_adicional adicional
                    JOIN producao.qry3descricoes descricao ON descricao.coduniadicional = adicional.coduniadicional
                    JOIN almoxarifado_jac.sig_saldo_funcionario saldo ON saldo.codcompladicional = descricao.codcompladicional
                    WHERE saldo.codfun = @codfun
                      AND adicional.codigoproduto = @codigoproduto
                    ORDER BY adicional.descricao_adicional;",
                    new { codfun = Funcionario.codfun, codigoproduto });
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
                    SELECT DISTINCT complemento.*
                    FROM producao.tblcomplementoadicional complemento
                    JOIN almoxarifado_jac.sig_saldo_funcionario saldo ON saldo.codcompladicional = complemento.codcompladicional
                    WHERE saldo.codfun = @codfun
                      AND complemento.coduniadicional = @coduniadicional
                    ORDER BY complemento.complementoadicional;",
                    new { codfun = Funcionario.codfun, coduniadicional });
                return new ObservableCollection<TblComplementoAdicionalModel>(data);
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

        public async Task<long?> GetCodMovimentacaoAsync()
        {
            try
            {
                await using var conn = new NpgsqlConnection(DataBaseSettings.Instance.ConnectionString);
                var lastMov = await conn.ExecuteScalarAsync<long?>(@"
                    SELECT MAX(cod_movimentacao)
                    FROM almoxarifado_jac.t_entrada_almox;");

                return (lastMov ?? 0) + 1;
            }
            catch (Exception)
            {
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

        public async Task EntradaAsync(EntradaAlmoxModel entrada)
        {
            await using var conn = new NpgsqlConnection(DataBaseSettings.Instance.ConnectionString);
            await conn.OpenAsync();
            await using var transaction = await conn.BeginTransactionAsync();
            try
            {
                entrada.cod_entrada_almox = await conn.ExecuteScalarAsync<long>(@"
                    INSERT INTO almoxarifado_jac.t_entrada_almox
                        (cod_movimentacao, barcode, qtde, data, resp, requisicao, origem,
                         incluido_por, incluido_data, estado, funcionario, unidade, nf,
                         obs, expurgo, num_os, endereco, id_det_solicitacao_compras,
                         codcompladicional, codfun)
                    VALUES
                        (@cod_movimentacao, @barcode, @qtde, @data, @resp, @requisicao, @origem,
                         @incluido_por, @incluido_data, @estado, @funcionario, @unidade, @nf,
                         @obs, @expurgo, @num_os, @endereco, @id_det_solicitacao_compras,
                         @codcompladicional, @codfun)
                    RETURNING cod_entrada_almox;", entrada, transaction);

                if (entrada.origem == "ACERTO ESTOQUE")
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
                            entrada.cod_movimentacao,
                            cod_almox = entrada.cod_entrada_almox,
                            entrada.barcode,
                            entrada.codcompladicional,
                            quantidade = entrada.qtde,
                            entrada.data,
                            operacao = entrada.origem,
                            processo = "ENTRADA",
                            entrada.num_os,
                            entrada.incluido_por,
                            entrada.incluido_data,
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

        public async Task<ObservableCollection<ResultadoMovimentacao>> GetItensMovimentacaoAsync(long CodMovimentacao)
        {
            try
            {
                await using var conn = new NpgsqlConnection(DataBaseSettings.Instance.ConnectionString);
                var resultado = await conn.QueryAsync<ResultadoMovimentacao>(@"
                    SELECT
                        entrada.cod_entrada_almox AS cod_saida_almox,
                        descricao.planilha,
                        descricao.descricao_completa,
                        entrada.unidade,
                        entrada.qtde
                    FROM almoxarifado_jac.t_entrada_almox entrada
                    JOIN producao.tbl_barcodes barcode ON entrada.barcode = barcode.barcode
                    JOIN producao.qry3descricoes descricao ON barcode.codigo = descricao.codcompladicional
                    WHERE entrada.cod_movimentacao = @CodMovimentacao;", new { CodMovimentacao });

                return new ObservableCollection<ResultadoMovimentacao>(resultado);
            }
            catch (Exception)
            {
                throw;
            }
        }
    }
}
