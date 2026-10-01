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
using Telerik.Windows.Controls.GridView;

namespace Almoxarifado.Views.Cadastros
{
    /// <summary>
    /// Interação lógica para Atendentes.xam
    /// </summary>
    public partial class Atendentes : UserControl
    {
    private void OnRowValidating(object sender, Telerik.Windows.Controls.GridViewRowValidatingEventArgs e)
    {
        if (e.Row?.Item is not AtendenteAlmoxModel item) return;
        ValidatedGridSave.Save(sender, e, () => ((AtendentesViewModel)DataContext).SalvarAtendenteAsync(item));
    }
        public Atendentes()
        {
            InitializeComponent();
            DataContext = new AtendentesViewModel();
        }

        private async void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            Loaded -= UserControl_Loaded;
            try
            {
                Application.Current.Dispatcher.Invoke(() => { Mouse.OverrideCursor = Cursors.Wait; });
                AtendentesViewModel vm = (AtendentesViewModel)DataContext;
                vm.Atendentes = await Task.Run(vm.GetAtendentesAsync);
                Application.Current.Dispatcher.Invoke(() => { Mouse.OverrideCursor = null; });
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                Application.Current.Dispatcher.Invoke(() => { Mouse.OverrideCursor = null; });
            }
        }

        private void OnCellValidatingDados(object sender, GridViewCellValidatingEventArgs e)
        {
            if (e.Cell.Column.UniqueName == "nome_funcionario")
            {
                if(string.IsNullOrWhiteSpace(e.NewValue?.ToString()))
                {
                    e.IsValid = false;
                    e.ErrorMessage = "Informe o nome do Atendente.";
                }

            }
            else if(e.Cell.Column.UniqueName == "funcao")
            {
                if (string.IsNullOrWhiteSpace(e.NewValue?.ToString()))
                {
                    e.IsValid = false;
                    e.ErrorMessage = "Informe a função do Atendente.";
                }
            }
        }



        private void OnDeleting(object sender, GridViewDeletingEventArgs e)
        {
            e.Cancel = true;
        }
    }

    public class AtendentesViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        public void RaisePropertyChanged(string propName) { this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName)); }

        private ObservableCollection<AtendenteAlmoxModel> _atendentes;
        public ObservableCollection<AtendenteAlmoxModel> Atendentes
        {
            get { return _atendentes; }
            set { _atendentes = value; RaisePropertyChanged("Atendentes"); }
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

        public async Task SalvarAtendenteAsync(AtendenteAlmoxModel atendente)
        {
            if (string.IsNullOrWhiteSpace(atendente.nome_funcionario) || string.IsNullOrWhiteSpace(atendente.funcao))
                throw new InvalidOperationException("Informe o funcionario e a funcao.");
            try
            {
                await using var conn = new NpgsqlConnection(DataBaseSettings.Instance.ConnectionString);
                await conn.ExecuteAsync(@"
                    INSERT INTO almoxarifado_jac.tbl_atendentes_almox
                        (nome_funcionario, funcao, username)
                    VALUES
                        (@nome_funcionario, @funcao, @username)
                    ON CONFLICT (nome_funcionario)
                    DO UPDATE SET
                        funcao = EXCLUDED.funcao,
                        username = EXCLUDED.username;", atendente);
            }
            catch (Exception)
            {
                throw;
            }
        }
    }
}
