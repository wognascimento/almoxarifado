using Almoxarifado.DataBase;
using Almoxarifado.DataBase.Model;
using Dapper;
using Npgsql;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Telerik.Windows.Controls.GridView;

namespace Almoxarifado.Views.Cadastros
{
    /// <summary>
    /// Interação lógica para Terceiro.xam
    /// </summary>
    public partial class Terceiro : UserControl
    {
    private void OnRowValidating(object sender, Telerik.Windows.Controls.GridViewRowValidatingEventArgs e)
    {
        if (e.Row?.Item is not TerceiroModel item) return;
        ValidatedGridSave.Save(sender, e, () => ((TerceiroViewModel)DataContext).SalvarTerceiroAsync(item));
    }
        public Terceiro()
        {
            InitializeComponent();
            DataContext = new TerceiroViewModel();
        }

        private async void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            Loaded -= UserControl_Loaded;
            try
            {
                Application.Current.Dispatcher.Invoke(() => { Mouse.OverrideCursor = Cursors.Wait; });
                TerceiroViewModel vm = (TerceiroViewModel)DataContext;
                vm.Terceiros = await Task.Run(vm.GetTerceirosAsync);

                Application.Current.Dispatcher.Invoke(() => { Mouse.OverrideCursor = null; });
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                Application.Current.Dispatcher.Invoke(() => { Mouse.OverrideCursor = null; });
            }
        }

        private void OnGravarClick(object sender, RoutedEventArgs e)
        {

        }

        private void OnCellValidatingDados(object sender, Telerik.Windows.Controls.GridViewCellValidatingEventArgs e)
        {
            if (e.Cell.Column.UniqueName == "nome")
            {
                if (string.IsNullOrWhiteSpace(e.NewValue?.ToString()))
                {
                    e.IsValid = false;
                    e.ErrorMessage = "Informe o nome do Terceiro.";
                }

            }
            else if (e.Cell.Column.UniqueName == "apelido")
            {
                if (string.IsNullOrWhiteSpace(e.NewValue?.ToString()))
                {
                    e.IsValid = false;
                    e.ErrorMessage = "Informe o apelido do Terceiro.";
                }
            }
            /*
            else if (e.Cell.Column.UniqueName == "razao_social")
            {
                if (string.IsNullOrWhiteSpace(e.NewValue?.ToString()))
                {
                    e.IsValid = false;
                    e.ErrorMessage = "Informe a razao_social do Terceiro.";
                }
            }
            */
            else if (e.Cell.Column.UniqueName == "rg")
            {
                if (string.IsNullOrWhiteSpace(e.NewValue?.ToString()))
                {
                    e.IsValid = false;
                    e.ErrorMessage = "Informe o R.G do Terceiro.";
                }
            }
            else if (e.Cell.Column.UniqueName == "municipio")
            {
                if (string.IsNullOrWhiteSpace(e.NewValue?.ToString()))
                {
                    e.IsValid = false;
                    e.ErrorMessage = "Informe o municipio do Terceiro.";
                }
            }
            else if (e.Cell.Column.UniqueName == "tipo_prestacao_servico")
            {
                if (string.IsNullOrWhiteSpace(e.NewValue?.ToString()))
                {
                    e.IsValid = false;
                    e.ErrorMessage = "Informe o tipo de prestação de serviço do Terceiro.";
                }
            }
        }



    }

    public class TerceiroViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        public void RaisePropertyChanged(string propName) { this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName)); }

        private TerceiroModel _terceiro;
        public TerceiroModel Terceiro
        {
            get { return _terceiro; }
            set { _terceiro = value; RaisePropertyChanged("Terceiro"); }
        }

        private ObservableCollection<TerceiroModel> _terceiros;
        public ObservableCollection<TerceiroModel> Terceiros
        {
            get { return _terceiros; }
            set { _terceiros = value; RaisePropertyChanged("Terceiros"); }
        }

        public async Task<ObservableCollection<TerceiroModel>> GetTerceirosAsync()
        {
            try
            {
                await using var conn = new NpgsqlConnection(DataBaseSettings.Instance.ConnectionString);
                var data = await conn.QueryAsync<TerceiroModel>(@"
                    SELECT nome, apelido, razao_social, rg, municipio, tipo_prestacao_servico, codfun
                    FROM almoxarifado_jac.tbl_terceiro
                    ORDER BY nome;");
                return new ObservableCollection<TerceiroModel>(data);
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task<TerceiroModel> SalvarTerceiroAsync(TerceiroModel terceiro)
        {
            if (string.IsNullOrWhiteSpace(terceiro.nome) || string.IsNullOrWhiteSpace(terceiro.apelido) ||
                string.IsNullOrWhiteSpace(terceiro.rg) || string.IsNullOrWhiteSpace(terceiro.municipio) ||
                string.IsNullOrWhiteSpace(terceiro.tipo_prestacao_servico))
                throw new InvalidOperationException("Preencha nome, apelido, RG, municipio e tipo de prestacao de servico.");
            try
            {
                await using var conn = new NpgsqlConnection(DataBaseSettings.Instance.ConnectionString);
                if (!terceiro.codfun.HasValue)
                {
                    terceiro.codfun = await conn.QuerySingleAsync<long>(@"
                        INSERT INTO almoxarifado_jac.tbl_terceiro
                            (nome, apelido, razao_social, rg, municipio, tipo_prestacao_servico)
                        VALUES
                            (@nome, @apelido, @razao_social, @rg, @municipio, @tipo_prestacao_servico)
                        RETURNING codfun;", terceiro);
                }
                else
                {
                    var affected = await conn.ExecuteAsync(@"
                        UPDATE almoxarifado_jac.tbl_terceiro
                        SET nome = @nome,
                            apelido = @apelido,
                            razao_social = @razao_social,
                            rg = @rg,
                            municipio = @municipio,
                            tipo_prestacao_servico = @tipo_prestacao_servico
                        WHERE codfun = @codfun;", terceiro);
                    if (affected != 1)
                        throw new InvalidOperationException("O terceiro não foi encontrado. Reabra o cadastro antes de alterar.");
                }

                return terceiro;
            }
            catch (Exception)
            {
                throw;
            }
        }
    }
}
