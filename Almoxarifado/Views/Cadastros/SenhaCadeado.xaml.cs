using Almoxarifado.DataBase;
using Almoxarifado.DataBase.Model;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Almoxarifado.Views.Cadastros;

/// <summary>
/// Interação lógica para SenhaCadeado.xam
/// </summary>
public partial class SenhaCadeado : UserControl
{
    public SenhaCadeado()
    {
        InitializeComponent();
        DataContext = new SenhaCadeadoViewModel();
    }

    private async void UserControl_Loaded(object sender, System.Windows.RoutedEventArgs e)
    {
        try
        {
            Application.Current.Dispatcher.Invoke(() => { Mouse.OverrideCursor = Cursors.Wait; });
            SenhaCadeadoViewModel vm = (SenhaCadeadoViewModel)DataContext;
            await vm.GetBolsasAsync();
            await vm.GetAprovadosAsync();
            await vm.GetSenhasAsync();
            Application.Current.Dispatcher.Invoke(() => { Mouse.OverrideCursor = null; });
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException pgEx)
        {
            Application.Current.Dispatcher.Invoke(() => { Mouse.OverrideCursor = null; });
            MessageBox.Show($"Erro do banco: {pgEx.MessageText}\nLocal: {pgEx.Where}");
        }
        catch (Exception ex)
        {
            Application.Current.Dispatcher.Invoke(() => { Mouse.OverrideCursor = null; });
            MessageBox.Show(ex.Message);
        }

    }

    private async void RadGridView_RowValidated(object sender, Telerik.Windows.Controls.GridViewRowValidatedEventArgs e)
    {
        SenhaCadeadoViewModel vm = (SenhaCadeadoViewModel)DataContext;
        var dado = e.Row.Item as AlmoxarifadoApoioCadeadoModel;
        try
        {
            Application.Current.Dispatcher.Invoke(() => { Mouse.OverrideCursor = Cursors.Wait; });
            await vm.AdicionarSenhaAsync(dado);
            Application.Current.Dispatcher.Invoke(() => { Mouse.OverrideCursor = null; });
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException pgEx)
        {
            Application.Current.Dispatcher.Invoke(() => { Mouse.OverrideCursor = null; });
            MessageBox.Show($"Erro do banco: {pgEx.MessageText}\nLocal: {pgEx.Where}");
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message);
        }
    }
}

public partial class SenhaCadeadoViewModel : ObservableObject
{

    [ObservableProperty]
    private ObservableCollection<TipoBolsaModel> bolsas;

    [ObservableProperty]
    private ObservableCollection<AprovadoModel> aprovados;

    [ObservableProperty]
    private ObservableCollection<AlmoxarifadoApoioCadeadoModel> senhas;

    public async Task GetSenhasAsync()
    {
        using DatabaseContext db = new();
        var data = await db.ApoioSenhaCadeados.ToListAsync();
        Senhas = new ObservableCollection<AlmoxarifadoApoioCadeadoModel>(data);
    }

    public async Task GetBolsasAsync()
    {
        using DatabaseContext db = new();
        var data = await db.TipoBolsas.OrderBy(c => c.codigo_tipobolsa).ToListAsync();
        Bolsas = new ObservableCollection<TipoBolsaModel>(data);
    }

    public async Task GetAprovadosAsync()
    {
        using DatabaseContext db = new();
        var result = await db.Aprovados
            .OrderBy(f => f.sigla)
            .ThenBy(f => f.sigla_serv)
            .ToListAsync();
        Aprovados = new ObservableCollection<AprovadoModel>(result);
    }

    public async Task AdicionarSenhaAsync(AlmoxarifadoApoioCadeadoModel model)
    {
        using DatabaseContext db = new();
        var modelExistente = await db.ApoioSenhaCadeados.FindAsync(model.cod_linha_cadeado);
        if (modelExistente == null)
            await db.ApoioSenhaCadeados.AddAsync(model);
        else
            db.Entry(modelExistente).CurrentValues.SetValues(model);

        await db.SaveChangesAsync();
    }
}
