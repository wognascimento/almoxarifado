using Almoxarifado.DataBase;
using Almoxarifado.DataBase.Model;
using Microsoft.EntityFrameworkCore;
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
                    funcionario = ((FuncionarioModel)funcionario.SelectedItem).nome_apelido,
                    codfun = ((FuncionarioModel)funcionario.SelectedItem).codfun,
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
                using DatabaseContext db = new();
                var data = await db.AtendentesAlmox.OrderBy(c => c.nome_funcionario).ToListAsync();
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
                using DatabaseContext db = new();

                var funcs = db.SaldoFuncionarios.GroupBy(saldo => saldo.codfun).Select(grupo => grupo.Key).ToListAsync();

                var queryRH = db.Funcionarios
                       .Select(a => new FuncionarioUnionModel
                       {
                           codfun = a.codfun,
                           nome = a.nome_apelido,
                           setor = a.setor
                       });

                var queryTerceiro = db.Terceiros
                        .Select(b => new FuncionarioUnionModel
                        {
                            codfun = b.codfun,
                            nome = b.nome,
                            setor = "TERCEIRO"
                        });

                var unionQuery = queryRH.Union(queryTerceiro);
                var result = await unionQuery.OrderBy(f => f.nome).Where(x => funcs.Result.Contains(x.codfun)).ToListAsync();

                //var data = await db.Funcionarios.OrderBy(f => f.nome_apelido).Where(x => funcs.Result.Contains(x.codfun)).ToListAsync();
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
                using DatabaseContext db = new();

                var produtos = await db.SaldoFuncionarios.Where(where => where.codfun == codfun).GroupBy(saldo => saldo.codcompladicional).Select(grupo => grupo.Key).ToListAsync();
                var planilhas = await db.Descricoes.Where(where => produtos.Contains(where.codcompladicional)).GroupBy(desc => desc.planilha).Select(grupo => grupo.Key).ToListAsync();

                var data = await db.Relplans
                    .OrderBy(planilha => planilha.planilha)
                    .Where(planilha => planilhas.Contains(planilha.planilha))
                    .ToListAsync();

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
                using DatabaseContext db = new();
                var data = await db.Descricoes
                    .Where(p => p.codcompladicional == codcompladicional && p.planilha.StartsWith("ALMO") && !p.inativo.Contains("-1"))
                    .FirstOrDefaultAsync();
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
                using DatabaseContext db = new();

                var prodsDevedor = db.SaldoFuncionarios.Where(where => where.codfun == Funcionario.codfun).GroupBy(saldo => saldo.codcompladicional).Select(grupo => grupo.Key).ToListAsync();
                var prodis = db.Descricoes.Where(where => prodsDevedor.Result.Contains(where.codcompladicional)).GroupBy(desc => desc.codigo).Select(grupo => grupo.Key).ToListAsync();

                var data = await db.Produtos
                    .Where(produto => produto.planilha == planilha && prodis.Result.Contains(produto.codigo))
                    .OrderBy(descricao => descricao)
                    .ToListAsync();
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
                using DatabaseContext db = new();

                var prodsDevedor = db.SaldoFuncionarios.Where(where => where.codfun == Funcionario.codfun).GroupBy(saldo => saldo.codcompladicional).Select(grupo => grupo.Key).ToListAsync();
                var prodis = db.Descricoes.Where(where => prodsDevedor.Result.Contains(where.codcompladicional)).GroupBy(desc => desc.coduniadicional).Select(grupo => grupo.Key).ToListAsync();

                var data = await db.DescAdicionais
                    .Where(produto => produto.codigoproduto == codigoproduto && prodis.Result.Contains(produto.coduniadicional))
                    .OrderBy(descricaoAdicional => descricaoAdicional)
                    .ToListAsync();
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
                using DatabaseContext db = new();

                var prodsDevedor = db.SaldoFuncionarios.Where(where => where.codfun == Funcionario.codfun).GroupBy(saldo => saldo.codcompladicional).Select(grupo => grupo.Key).ToListAsync();
                var prodis = db.Descricoes.Where(where => prodsDevedor.Result.Contains(where.codcompladicional)).GroupBy(desc => desc.codcompladicional).Select(grupo => grupo.Key).ToListAsync();

                var data = await db.ComplementoAdicionais
                    .Where(produto => produto.coduniadicional == coduniadicional && prodis.Result.Contains(produto.codcompladicional))
                    .OrderBy(complementoAdicional => complementoAdicional.complementoadicional)
                    .ToListAsync();
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
                using DatabaseContext db = new();
                var data = await db.ControleAlmoxEstoques
                    .Where(p => p.bloqueado == "-1" && p.codcompladicional == codcompladicional)
                    .FirstOrDefaultAsync();
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
                using DatabaseContext db = new();
                var lastMov = await db.Entradas
                    .Select(entidade => entidade.cod_movimentacao)
                    .MaxAsync();

                return lastMov + 1;
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
                using DatabaseContext db = new();
                return await db.Barcodes.FindAsync(codigo);
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task EntradaAsync(EntradaAlmoxModel entrada)
        {
            using DatabaseContext db = new();
            var strategy = db.Database.CreateExecutionStrategy();

            await strategy.ExecuteAsync(async () =>
            {
                using var transaction = db.Database.BeginTransaction();
                try
                {
                    await db.Entradas.AddAsync(entrada);
                    await db.SaveChangesAsync();

                    if (entrada.origem == "ACERTO ESTOQUE")
                        await db.ControleAlmoxEstoques.AddAsync(
                            new ControleAlmoxEstoqueModel
                            {
                                cod_movimentacao = entrada.cod_movimentacao,
                                cod_almox = entrada.cod_entrada_almox,
                                barcode = entrada.barcode,
                                codcompladicional = entrada.codcompladicional,
                                quantidade = entrada.qtde,
                                data = entrada.data,
                                operacao = entrada.origem,
                                processo = "ENTRADA",
                                num_os = entrada.num_os,
                                incluido_por = entrada.incluido_por,
                                incluido_data = entrada.incluido_data,
                                bloqueado = "-1"
                            });
                    await db.SaveChangesAsync();

                    transaction.Commit();
                }
                catch (Exception)
                {
                    transaction.Rollback();
                    throw;
                }
            });
        }

        public async Task<ObservableCollection<ResultadoMovimentacao>> GetItensMovimentacaoAsync(long CodMovimentacao)
        {
            try
            {
                using DatabaseContext db = new();
                var resultado = from entrada in db.Entradas
                                join barcode in db.Barcodes on entrada.barcode equals barcode.barcode
                                join descricao in db.Descricoes on barcode.codigo equals descricao.codcompladicional
                                where entrada.cod_movimentacao == CodMovimentacao
                                select new ResultadoMovimentacao
                                {
                                    cod_saida_almox = entrada.cod_entrada_almox,
                                    planilha = descricao.planilha,
                                    descricao_completa = descricao.descricao_completa,
                                    unidade = entrada.unidade,
                                    qtde = entrada.qtde
                                };

                return new ObservableCollection<ResultadoMovimentacao>(await resultado.ToListAsync());
            }
            catch (Exception)
            {
                throw;
            }
        }
    }
}
