using Almoxarifado.DataBase;
using Almoxarifado.DataBase.Model;
using Microsoft.EntityFrameworkCore;
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
        public Terceiro()
        {
            InitializeComponent();
            DataContext = new TerceiroViewModel();
        }

        private async void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
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
                if (e.NewValue.ToString() == string.Empty)
                {
                    e.IsValid = false;
                    e.ErrorMessage = "Informe o nome do Terceiro.";
                }

            }
            else if (e.Cell.Column.UniqueName == "apelido")
            {
                if (e.NewValue.ToString() == string.Empty)
                {
                    e.IsValid = false;
                    e.ErrorMessage = "Informe o apelido do Terceiro.";
                }
            }
            /*
            else if (e.Cell.Column.UniqueName == "razao_social")
            {
                if (e.NewValue.ToString() == string.Empty)
                {
                    e.IsValid = false;
                    e.ErrorMessage = "Informe a razao_social do Terceiro.";
                }
            }
            */
            else if (e.Cell.Column.UniqueName == "rg")
            {
                if (e.NewValue.ToString() == string.Empty)
                {
                    e.IsValid = false;
                    e.ErrorMessage = "Informe o R.G do Terceiro.";
                }
            }
            else if (e.Cell.Column.UniqueName == "municipio")
            {
                if (e.NewValue.ToString() == string.Empty)
                {
                    e.IsValid = false;
                    e.ErrorMessage = "Informe o municipio do Terceiro.";
                }
            }
            else if (e.Cell.Column.UniqueName == "tipo_prestacao_servico")
            {
                if (e.NewValue.ToString() == string.Empty)
                {
                    e.IsValid = false;
                    e.ErrorMessage = "Informe o tipo de prestação de serviço do Terceiro.";
                }
            }
        }

        private async void OnRowInsertDados(object sender, Telerik.Windows.Controls.GridViewRowEditEndedEventArgs e)
        {
            try
            {
                TerceiroViewModel vm = (TerceiroViewModel)DataContext;

                if (e.EditAction == GridViewEditAction.Cancel)
                {
                    return;
                }
                if (e.EditOperationType == GridViewEditOperationType.Insert)
                {
                    var terceiro = await vm.SalvarTerceiroAsync((TerceiroModel)e.NewData);
                    var updatedObject = (TerceiroModel)this.TerceirosGridView.CurrentItem;

                    this.TerceirosGridView.CurrentItem = terceiro;
                    TerceirosGridView.Items.Refresh();
                }
                if (e.EditOperationType == GridViewEditOperationType.Edit)
                {
                    var terceiro = await vm.SalvarTerceiroAsync((TerceiroModel)e.NewData);
                    var updatedObject = (TerceiroModel)this.TerceirosGridView.CurrentItem;

                    this.TerceirosGridView.CurrentItem = terceiro;
                    TerceirosGridView.Items.Refresh();
                }
            }
            catch (PostgresException ex)
            {
                MessageBox.Show(ex.Message);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
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
                using DatabaseContext db = new();
                var data = await db.Terceiros.OrderBy(c => c.nome).ToListAsync();
                return new ObservableCollection<TerceiroModel>(data);
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task<TerceiroModel> SalvarTerceiroAsync(TerceiroModel terceiro)
        {
            try
            {
                using DatabaseContext db = new();
                await db.Terceiros.SingleMergeAsync(terceiro);
                await db.SaveChangesAsync();

                return terceiro;
            }
            catch (Exception)
            {
                throw;
            }
        }
    }
}
