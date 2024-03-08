using Almoxarifado.DataBase;
using Almoxarifado.DataBase.Model;
using Almoxarifado.Views.Movimentacoes;
using Microsoft.EntityFrameworkCore;
using Syncfusion.Data.Extensions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Telerik.Windows.Controls;
using Telerik.Windows.Controls.GridView;

namespace Almoxarifado.Views.Cadastros
{
    /// <summary>
    /// Interação lógica para Atendentes.xam
    /// </summary>
    public partial class Atendentes : UserControl
    {
        public Atendentes()
        {
            InitializeComponent();
            DataContext = new AtendentesViewModel();
        }

        private async void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
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
                if(e.NewValue.ToString() == string.Empty)
                {
                    e.IsValid = false;
                    e.ErrorMessage = "Informe o nome do Atendente.";
                }

            }
            else if(e.Cell.Column.UniqueName == "funcao")
            {
                if (e.NewValue.ToString() == string.Empty)
                {
                    e.IsValid = false;
                    e.ErrorMessage = "Informe a função do Atendente.";
                }
            }
        }

        private async void OnRowInsertDados(object sender, GridViewRowEditEndedEventArgs e)
        {

            try
            {
                AtendentesViewModel vm = (AtendentesViewModel)DataContext;

                if (e.EditAction == GridViewEditAction.Cancel)
                {
                    return;
                }
                if (e.EditOperationType == GridViewEditOperationType.Insert)
                {
                    await Task.Run(() => vm.SalvarAtendenteAsync((AtendenteAlmoxModel)e.NewData));
                    //NewData = {Almoxarifado.DataBase.Model.AtendenteAlmoxModel}
                }
                if (e.EditOperationType == GridViewEditOperationType.Edit)
                {
                    //Edit the entry to the data base. 
                }
            }
            catch (Exception ex)
            {

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
                using DatabaseContext db = new();
                var data = await db.AtendentesAlmox.OrderBy(c => c.nome_funcionario).ToListAsync();
                return new ObservableCollection<AtendenteAlmoxModel>(data);
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task SalvarAtendenteAsync(AtendenteAlmoxModel atendente)
        {
            try
            {
                using DatabaseContext db = new();
                await db.AtendentesAlmox.SingleMergeAsync(atendente);
                await db.SaveChangesAsync();
            }
            catch (Exception)
            {
                throw;
            }
        }
    }
}
