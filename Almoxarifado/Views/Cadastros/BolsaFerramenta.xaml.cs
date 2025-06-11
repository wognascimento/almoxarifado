using Almoxarifado.DataBase;
using Almoxarifado.DataBase.Model;
using Microsoft.EntityFrameworkCore;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Almoxarifado.Views.Cadastros;

/// <summary>
/// Interação lógica para BolsaFerramenta.xam
/// </summary>
public partial class BolsaFerramenta : UserControl
{
    public BolsaFerramenta()
    {
        InitializeComponent();
        DataContext = new BolsaFerramentaViewModel();
    }

    private async void UserControl_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            Application.Current.Dispatcher.Invoke(() => { Mouse.OverrideCursor = Cursors.Wait; });
            BolsaFerramentaViewModel vm = (BolsaFerramentaViewModel)DataContext;
            vm.Bolsas = await vm.GetBolsasAsync();
            Application.Current.Dispatcher.Invoke(() => { Mouse.OverrideCursor = null; });
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message);
            Application.Current.Dispatcher.Invoke(() => { Mouse.OverrideCursor = null; });
        }
    }

    private async void OnCellValidatingDados(object sender, Telerik.Windows.Controls.GridViewCellValidatingEventArgs e)
    {
        BolsaFerramentaViewModel vm = (BolsaFerramentaViewModel)DataContext;
        var dado = e.Row.Item as TipoBolsaModel;
        try
        {
            if (e.Cell.Column.UniqueName == "descricao")
            {
                if (e.NewValue.ToString() == string.Empty)
                {
                    e.IsValid = false;
                    e.ErrorMessage = "Informe a Descrição.";
                    return;
                }
            }
           await vm.AdicionarBolsasAsync(dado);
        }
        catch (Exception ex)
        {
            e.IsValid = false;
            MessageBox.Show(ex.Message);
        }
    }

    private void OnRowInsertDados(object sender, Telerik.Windows.Controls.GridViewRowEditEndedEventArgs e)
    {
        
    }
}

public class BolsaFerramentaViewModel : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;
    public void RaisePropertyChanged(string propName) { this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName)); }

    private ObservableCollection<TipoBolsaModel> _bolsas;
    public ObservableCollection<TipoBolsaModel> Bolsas
    {
        get { return _bolsas; }
        set { _bolsas = value; RaisePropertyChanged("Bolsas"); }
    }

    public async Task<ObservableCollection<TipoBolsaModel>> GetBolsasAsync()
    {
        try
        {
            using DatabaseContext db = new();
            var data = await db.TipoBolsas.OrderBy(c => c.codigo_tipobolsa).ToListAsync();
            return new ObservableCollection<TipoBolsaModel>(data);
        }
        catch (Exception)
        {
            throw;
        }
    }

    public async Task AdicionarBolsasAsync(TipoBolsaModel tipoBolsa)
    {
        try
        {
            using DatabaseContext db = new();
            var tipoBolsaExistente = await db.TipoBolsas.FindAsync(tipoBolsa.codigo_tipobolsa);
            if (tipoBolsaExistente == null)
               await db.TipoBolsas.AddAsync(tipoBolsa);
            else
                db.Entry(tipoBolsaExistente).CurrentValues.SetValues(tipoBolsa);

            await db.SaveChangesAsync();
        }
        catch (Exception)
        {
            throw;
        }
    }
}
