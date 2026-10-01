using Almoxarifado.DataBase;
using Almoxarifado.DataBase.Model;
using Dapper;
using Npgsql;
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
    private void OnRowValidating(object sender, Telerik.Windows.Controls.GridViewRowValidatingEventArgs e)
    {
        if (e.Row?.Item is not TipoBolsaModel item) return;
        ValidatedGridSave.Save(sender, e, () => ((BolsaFerramentaViewModel)DataContext).AdicionarBolsasAsync(item));
    }
    public BolsaFerramenta()
    {
        InitializeComponent();
        DataContext = new BolsaFerramentaViewModel();
    }

    private async void UserControl_Loaded(object sender, RoutedEventArgs e)
    {
        Loaded -= UserControl_Loaded;
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

    private void OnCellValidatingDados(object sender, Telerik.Windows.Controls.GridViewCellValidatingEventArgs e)
    {
        BolsaFerramentaViewModel vm = (BolsaFerramentaViewModel)DataContext;
        var dado = e.Row.Item as TipoBolsaModel;
        try
        {
            if (e.Cell.Column.UniqueName == "descricao")
            {
                if (string.IsNullOrWhiteSpace(e.NewValue?.ToString()))
                {
                    e.IsValid = false;
                    e.ErrorMessage = "Informe a Descrição.";
                    return;
                }
            }
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
            await using var conn = new NpgsqlConnection(DataBaseSettings.Instance.ConnectionString);
            var data = await conn.QueryAsync<TipoBolsaModel>(@"
                SELECT codigo_tipobolsa, descricao, codcompladicional
                FROM almoxarifado_apoio.tbltipobolsa
                ORDER BY codigo_tipobolsa;");
            return new ObservableCollection<TipoBolsaModel>(data);
        }
        catch (Exception)
        {
            throw;
        }
    }

    public async Task AdicionarBolsasAsync(TipoBolsaModel tipoBolsa)
    {
        if (string.IsNullOrWhiteSpace(tipoBolsa.descricao))
            throw new InvalidOperationException("Informe a descricao.");
        try
        {
            await using var conn = new NpgsqlConnection(DataBaseSettings.Instance.ConnectionString);
            if (tipoBolsa.codigo_tipobolsa is null or 0)
            {
                tipoBolsa.codigo_tipobolsa = await conn.ExecuteScalarAsync<long>(@"
                    INSERT INTO almoxarifado_apoio.tbltipobolsa (descricao, codcompladicional)
                    VALUES (@descricao, @codcompladicional)
                    RETURNING codigo_tipobolsa;", tipoBolsa);
                return;
            }

            await conn.ExecuteAsync(@"
                INSERT INTO almoxarifado_apoio.tbltipobolsa (codigo_tipobolsa, descricao, codcompladicional)
                VALUES (@codigo_tipobolsa, @descricao, @codcompladicional)
                ON CONFLICT (codigo_tipobolsa) DO UPDATE SET
                    descricao = EXCLUDED.descricao,
                    codcompladicional = EXCLUDED.codcompladicional;", tipoBolsa);
        }
        catch (Exception)
        {
            throw;
        }
    }
}
