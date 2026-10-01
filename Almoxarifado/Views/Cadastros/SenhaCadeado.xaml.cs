using Almoxarifado.DataBase;
using Almoxarifado.DataBase.Model;
using CommunityToolkit.Mvvm.ComponentModel;
using Dapper;
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
        Loaded -= UserControl_Loaded;
        try
        {
            Application.Current.Dispatcher.Invoke(() => { Mouse.OverrideCursor = Cursors.Wait; });
            SenhaCadeadoViewModel vm = (SenhaCadeadoViewModel)DataContext;
            await vm.GetBolsasAsync();
            await vm.GetAprovadosAsync();
            await vm.GetSenhasAsync();
            Application.Current.Dispatcher.Invoke(() => { Mouse.OverrideCursor = null; });
        }
        catch (PostgresException pgEx)
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
        catch (PostgresException pgEx)
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
        await using var conn = new NpgsqlConnection(DataBaseSettings.Instance.ConnectionString);
        var data = await conn.QueryAsync<AlmoxarifadoApoioCadeadoModel>(@"
            SELECT cod_linha_cadeado, nome_bolsa, cadeado, senha, montador, sigla, lacre
            FROM almoxarifado_apoio.tbl_cadeados
            ORDER BY cod_linha_cadeado;");
        Senhas = new ObservableCollection<AlmoxarifadoApoioCadeadoModel>(data);
    }

    public async Task GetBolsasAsync()
    {
        await using var conn = new NpgsqlConnection(DataBaseSettings.Instance.ConnectionString);
        var data = await conn.QueryAsync<TipoBolsaModel>(@"
            SELECT codigo_tipobolsa, descricao, codcompladicional
            FROM almoxarifado_apoio.tbltipobolsa
            ORDER BY codigo_tipobolsa;");
        Bolsas = new ObservableCollection<TipoBolsaModel>(data);
    }

    public async Task GetAprovadosAsync()
    {
        await using var conn = new NpgsqlConnection(DataBaseSettings.Instance.ConnectionString);
        var result = await conn.QueryAsync<AprovadoModel>(@"
            SELECT id_aprovado, sigla, sigla_serv, nome
            FROM producao.t_aprovados
            ORDER BY sigla, sigla_serv;");
        Aprovados = new ObservableCollection<AprovadoModel>(result);
    }

    public async Task AdicionarSenhaAsync(AlmoxarifadoApoioCadeadoModel model)
    {
        await using var conn = new NpgsqlConnection(DataBaseSettings.Instance.ConnectionString);
        if (model.cod_linha_cadeado is null or 0)
        {
            model.cod_linha_cadeado = await conn.ExecuteScalarAsync<long>(@"
                INSERT INTO almoxarifado_apoio.tbl_cadeados
                    (nome_bolsa, cadeado, senha, montador, sigla, lacre)
                VALUES
                    (@nome_bolsa, @cadeado, @senha, @montador, @sigla, @lacre)
                RETURNING cod_linha_cadeado;", model);
            return;
        }

        await conn.ExecuteAsync(@"
            INSERT INTO almoxarifado_apoio.tbl_cadeados
                (cod_linha_cadeado, nome_bolsa, cadeado, senha, montador, sigla, lacre)
            VALUES
                (@cod_linha_cadeado, @nome_bolsa, @cadeado, @senha, @montador, @sigla, @lacre)
            ON CONFLICT (cod_linha_cadeado) DO UPDATE SET
                nome_bolsa = EXCLUDED.nome_bolsa,
                cadeado = EXCLUDED.cadeado,
                senha = EXCLUDED.senha,
                montador = EXCLUDED.montador,
                sigla = EXCLUDED.sigla,
                lacre = EXCLUDED.lacre;", model);
    }
}
