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
                using DatabaseContext db = new();
                var data = await db.AtendentesAlmox.OrderBy(c => c.nome_funcionario).ToListAsync();
                return new ObservableCollection<AtendenteAlmoxModel>(data);
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task<ObservableCollection<FuncionarioModel>> GetFuncionariosAsync()
        {
            try
            {
                using DatabaseContext db = new();
                var data = await db.Funcionarios.OrderBy(f => f.nome_apelido).Where(f => f.data_demissao == null).ToListAsync();
                return new ObservableCollection<FuncionarioModel>(data);
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

        public async Task<ObservableCollection<RelplanModel>> GetPlanilhasAsync()
        {
            try
            {
                using DatabaseContext db = new();
                /*
                var data = await db.ProdutosAlmox
                    .GroupBy(produto => produto.planilha)
                    .Select(grupo => grupo.Key)
                    .OrderBy(planilha => planilha)
                    .ToListAsync();
                */
                var data = await db.Relplans
                    .OrderBy(planilha => planilha.planilha)
                    .Where(planilha => planilha.planilha.StartsWith("ALMO") && planilha.ativo.Contains("1"))
                    .ToListAsync();

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
                using DatabaseContext db = new();
                var data = await db.Produtos
                    .Where(produto => produto.planilha == planilha && produto.inativo.Contains("0"))
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
                /*
                var data = await db.ProdutosAlmox
                    .Where(produto => produto.descricao == descricao)
                    .GroupBy(produto => produto.descricao_adicional)
                    .Select(grupo => grupo.Key)
                    .OrderBy(descricaoAdicional => descricaoAdicional)
                    .ToListAsync();
                */
                var data = await db.DescAdicionais
                    .Where(produto => produto.codigoproduto == codigoproduto && produto.inativo.Contains("0"))
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
                /*
                var data = await db.ProdutosAlmox
                    .Where(produto => produto.descricao_adicional == descricaoAdicional)
                    .GroupBy(produto => produto.complementoadicional)
                    .Select(grupo => grupo.Key)
                    .OrderBy(complementoAdicional => complementoAdicional)
                    .ToListAsync();
                */
                var data = await db.ComplementoAdicionais
                    .Where(produto => produto.coduniadicional == coduniadicional && produto.inativo.Contains("0"))
                    .OrderBy(complementoAdicional => complementoAdicional.complementoadicional)
                    .ToListAsync();
                return new ObservableCollection<TblComplementoAdicionalModel>(data);
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task SaidaAsync(SaidaAlmoxModel saida)
        {
            using DatabaseContext db = new();
            var strategy = db.Database.CreateExecutionStrategy();

            await strategy.ExecuteAsync(async () =>
            {
                using var transaction = db.Database.BeginTransaction();
                try
                {
                    //saida = await db.Saidas.SingleInsertAsync(saida);
                    await db.Saidas.AddAsync(saida);
                    await db.SaveChangesAsync();

                    if(saida.destino == "ACERTO ESTOQUE")
                    await db.ControleAlmoxEstoques.AddAsync(
                        new ControleAlmoxEstoqueModel 
                        {
                            cod_movimentacao = saida.cod_movimentacao,
                            cod_almox = saida.cod_saida_almox,
                            barcode = saida.barcode,
                            codcompladicional = saida.codcompladicional,
                            quantidade = saida.qtde,
                            data = saida.data,
                            operacao = saida.destino,
                            processo = "SAIDA",
                            num_os = saida.num_os,
                            incluido_por = saida.incluido_por,
                            incluido_data = saida.data_inclusao,
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

        public async Task<long?> GetCodMovimentacaoAsync()
        {
            try
            {
                using DatabaseContext db = new();
                var lastMov = await db.Saidas
                    .Select(entidade => entidade.cod_movimentacao)
                    .MaxAsync();

                return lastMov + 1;
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
                using DatabaseContext db = new();
                var resultado  = from saida in db.Saidas
                                 join barcode in db.Barcodes on saida.barcode equals barcode.barcode
                                 join descricao in db.Descricoes on barcode.codigo equals descricao.codcompladicional
                                 where saida.cod_movimentacao == CodMovimentacao
                                 select new ResultadoMovimentacao
                                 {
                                     cod_saida_almox = saida.cod_saida_almox,
                                     planilha = descricao.planilha,
                                     descricao_completa = descricao.descricao_completa,
                                     unidade = saida.unidade,
                                     qtde = saida.qtde
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
