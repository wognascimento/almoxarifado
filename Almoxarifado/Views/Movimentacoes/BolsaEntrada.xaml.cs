using Almoxarifado.DataBase;
using Almoxarifado.DataBase.Model;
using Almoxarifado.DataBase.Model.DTO;
using CommunityToolkit.Mvvm.ComponentModel;
using Dapper;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Syncfusion.ExcelToPdfConverter;
using Syncfusion.Pdf;
using Syncfusion.XlsIO;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Telerik.Windows.Controls;

namespace Almoxarifado.Views.Movimentacoes;

/// <summary>
/// Interação lógica para BolsaEntrada.xam
/// </summary>
public partial class BolsaEntrada : UserControl
{
    private readonly DataBaseSettings BaseSettings = DataBaseSettings.Instance;

    public BolsaEntrada()
    {
        InitializeComponent();
        DataContext = new BolsaEntradaViewModel();
    }

    private async void UserControl_Loaded(object sender, RoutedEventArgs e)
    {
        BolsaEntradaViewModel vm = (BolsaEntradaViewModel)DataContext;
        List<string> erros = [];

        string? erro1 = await vm.GetFuncionariosAsync();
        if (!string.IsNullOrEmpty(erro1)) erros.Add(erro1);

        

        if (erros.Count > 0)
        {
            MessageBox.Show(string.Join("\n\n", erros), "Erro ao carregar dados", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void funcionarios_GotFocus(object sender, RoutedEventArgs e)
    {

    }

    private async void funcionarios_SelectionChanged(object sender, Telerik.Windows.Controls.SelectionChangeEventArgs e)
    {
        var codFunc = funcionarios.SelectedItem is FuncionarioModel funcionario ? funcionario.codfun : null;
        BolsaEntradaViewModel vm = (BolsaEntradaViewModel)DataContext;
        List<string> erros = [];
        string? erro1 = await vm.GetBolsasFuncAsync(codFunc);

        if (!string.IsNullOrEmpty(erro1)) erros.Add(erro1);
        if (erros.Count > 0)
        {
            MessageBox.Show(string.Join("\n\n", erros), "Erro ao carregar dados", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void SiglaBolsaGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        var bolsa = SiglaBolsaGrid.Items.CurrentItem as BolsaSaidaFuncDestinoDTO;

        //var codBolsa = this.bolsa.SelectedItem is TipoBolsaModel bolsa ? bolsa.codigo_tipobolsa : null;
        //var codFunc = funcionarios.SelectedItem is FuncionarioModel funcionario ? funcionario.codfun : null;
        //var sigla = siglas.SelectedItem is AprovadoModel aprovado ? aprovado.sigla_serv : null;

        BolsaEntradaViewModel vm = (BolsaEntradaViewModel)DataContext;
        if (bolsa == null)
        {
            RadWindow.Alert(new DialogParameters
            {
                Header = "Aviso",
                Content = "Selecione uma bolsa, funcionário e sigla antes de adicionar itens.",
                OkButtonContent = "Ok"
            });
            return;
        }

        List<string> erros = [];
        string? erro2 = await vm.GetBolsaProdutosAsync(bolsa.codigo_bolsa, bolsa.codfun, bolsa.destino_shop);
        if (!string.IsNullOrEmpty(erro2)) erros.Add(erro2);

        if (erros.Count > 0)
        {
            MessageBox.Show(string.Join("\n\n", erros), "Erro ao carregar dados", MessageBoxButton.OK, MessageBoxImage.Error); ;
        }
    }

    private void OnImprimirReciboBolsaClick(object sender, RoutedEventArgs e)
    {
        BolsaEntradaViewModel vm = (BolsaEntradaViewModel)DataContext;
        var descricaoBolsa = SiglaBolsaGrid.Items.CurrentItem as BolsaSaidaFuncDestinoDTO;
        var nomeFuncionario = this.funcionarios.SelectedItem is FuncionarioModel funcionario ? funcionario.nome_apelido : null;
        try
        {
            using ExcelEngine excelEngine = new();
            IApplication application = excelEngine.Excel;
            application.DefaultVersion = ExcelVersion.Excel2016;
            // Abre o arquivo modelo
            FileStream inputStream = new(@$"{BaseSettings.CaminhoSistema}Modelos\RECIBO_SAIDA.xlsx", FileMode.Open, FileAccess.Read);
            IWorkbook workbook = application.Workbooks.Open(inputStream);
            IWorksheet worksheet = workbook.Worksheets[0];
            // Preenche células fixas
            worksheet.Range["A5"].Text = descricaoBolsa.descricao;
            worksheet.Range["D10"].Text = nomeFuncionario;

            int linhaInicial = 8; // Inserir a partir da linha 11

            var lista = vm.BolsaItens
                .Where(b => (b.quantidade_retorno ?? 0) < b.quantidade)
                .ToList();

            foreach (var item in lista)
            {
                worksheet.Range[@$"A{linhaInicial}:B{linhaInicial}"].Merge();
                worksheet.Range[@$"A{linhaInicial}:B{linhaInicial}"].Text = item.planilha;

                worksheet.Range[@$"C{linhaInicial}:G{linhaInicial}"].Merge();
                worksheet.Range[@$"C{linhaInicial}:G{linhaInicial}"].Text = item?.descricao_completa?.Replace("ÚNICO", null);

                worksheet.Range[@$"H{linhaInicial}"].Text = item.unidade;

                worksheet.Range[@$"I{linhaInicial}"].Number = Convert.ToDouble((item.quantidade - Convert.ToDouble(item.quantidade_retorno)));

                worksheet.Range[@$"J{linhaInicial}"].Number = Convert.ToDouble(item.valor_unitario);

                worksheet.Range[@$"K{linhaInicial}"].Number = Convert.ToDouble((item.quantidade - Convert.ToDouble(item.quantidade_retorno)) * item.valor_unitario);

                linhaInicial++;
                worksheet.InsertRow(linhaInicial, 1, ExcelInsertOptions.FormatAsBefore);
            }

            linhaInicial += 1;
            worksheet.Range[@$"K{linhaInicial}"].Formula = $"SUM(K7:K{linhaInicial - 1})";

            // Cria o conversor
            ExcelToPdfConverter converter = new(workbook);
            ExcelToPdfConverterSettings settings = new()
            {
                LayoutOptions = LayoutOptions.FitSheetOnOnePage
            };

            // Converte para documento PDF
            PdfDocument pdfDocument = converter.Convert(settings);

            // Salva o PDF
            using (FileStream outputStream = new(@$"{BaseSettings.CaminhoSistema}Impressos\RECIBO_RETORNO-{nomeFuncionario}-{descricaoBolsa}.PDF", FileMode.Create, FileAccess.Write))
            {
                pdfDocument.Save(outputStream);
            }

            workbook.Close();
            inputStream.Close();
            pdfDocument.Close(true);
            Process.Start("explorer", @$"{BaseSettings.CaminhoSistema}Impressos\RECIBO_RETORNO-{nomeFuncionario}-{descricaoBolsa}.PDF");
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message);
        }
    }

    private async void RadGridView_CellEditEnded(object sender, Telerik.Windows.Controls.GridViewCellEditEndedEventArgs e)
    {
        if (sender is not RadGridView gridView)
            return;

        var itemBeingEdited = gridView.CurrentCellInfo.Item as BolsaSaidaDTO;
        itemBeingEdited.retorno_em = DateTime.Now;
        itemBeingEdited.retorno_por = Environment.UserName;

        var columnName = e.Cell.Column.Header.ToString();
        List<string> erros = [];
        BolsaEntradaViewModel vm = (BolsaEntradaViewModel)DataContext;

        switch (columnName)
        {
            case "Qtde Retorno":
                //AtualizarQuantidade(itemBeingEdited, e.NewData);
                string? erroQtde = await vm.AlterarItemBolsaAsync(itemBeingEdited);
                if (!string.IsNullOrEmpty(erroQtde)) erros.Add(erroQtde);
                break;
        }


        if (erros.Count > 0)
        {
            MessageBox.Show(string.Join("\n\n", erros), "Erro ao carregar dados", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }
    }

    private void ModalOverlay_MouseDown(object sender, MouseButtonEventArgs e)
    {

    }
}

public partial class BolsaEntradaViewModel : ObservableObject
{
    private readonly DataBaseSettings _dataBaseSettings;

    [ObservableProperty]
    private bool isBusy;

    [ObservableProperty]
    private ObservableCollection<FuncionarioModel> funcionarios;

    [ObservableProperty]
    private ObservableCollection<BolsaSaidaFuncDestinoDTO> bolsasFuncionario;

    [ObservableProperty]
    private ObservableCollection<BolsaSaidaDTO> bolsaItens;

    public BolsaEntradaViewModel()
    {
        _dataBaseSettings = DataBaseSettings.Instance;
    }

    public async Task<string?> GetFuncionariosAsync()
    {
        try
        {
            using DatabaseContext db = new();

            var funcSaida = db.BolsaSaidas
                .Select(bs => bs.codfun)
                .Distinct()
                .ToList();

            var result = await db.Funcionarios
                .OrderBy(f => f.nome_apelido)
                .ThenBy(f => f.setor)
                .Where(item => funcSaida.Contains(item.codfun))
                .ToListAsync();

            Funcionarios = new ObservableCollection<FuncionarioModel>(result);
            return null; // sucesso
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException pgEx)
        {
            return $"Erro do banco: {pgEx.MessageText}\nLocal: {pgEx.Where}";
        }
        catch (Exception ex)
        {
            return $"Erro inesperado: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    public async Task<string?> GetBolsasFuncAsync(long? codFunc)
    {
        try
        {
            IsBusy = true;
            var parameters = new { func = codFunc };
            string sql = @"
                SELECT 
                    codfun, 
                    destino_shop, 
                    codigo_bolsa, 
                    tbltipobolsa.descricao
                FROM almoxarifado_apoio.tblsaidabolsa
                JOIN almoxarifado_apoio.tbltipobolsa ON tblsaidabolsa.codigo_bolsa = tbltipobolsa.codigo_tipobolsa
                JOIN producao.qry3descricoes ON tblsaidabolsa.codcompladicional = qry3descricoes.codcompladicional
                WHERE codfun = @func
                GROUP BY tbltipobolsa.descricao, codigo_bolsa, codfun, destino_shop
                ORDER BY destino_shop, tbltipobolsa.descricao
            ";
            using var connection = new NpgsqlConnection(_dataBaseSettings.ConnectionString);
            await connection.OpenAsync();
            var resultado = (await connection.QueryAsync<BolsaSaidaFuncDestinoDTO>(sql, parameters)).ToList();
            await connection.CloseAsync();

            BolsasFuncionario = new ObservableCollection<BolsaSaidaFuncDestinoDTO>(resultado);
            return null; // sucesso
        }
        catch (NpgsqlException ex) when (ex.InnerException is PostgresException pgEx)
        {
            return $"Erro do banco: {pgEx.MessageText}\nLocal: {pgEx.Where}";
        }
        catch (Exception ex)
        {
            return $"Erro inesperado: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    public async Task<string?> GetBolsaProdutosAsync(long? codBolsa, long? codFunc, string? sigla)
    {
        try
        {


            IsBusy = true;
            var parameters = new { bolsa = codBolsa, func = codFunc, sigla };
            string sql = @"
                SELECT
                    b.cod_controlelinha,
	                b.codigo_bolsa,
	                b.codcompladicional,
	                p.planilha,
                    p.descricao,
                    p.descricao_adicional,
                    p.complementoadicional,
	                p.descricao_completa,
	                p.unidade,
                    b.quantidade,
	                b.quantidade_retorno,
	                p.custo AS valor_unitario,
	                (b.quantidade - COALESCE(b.quantidade_retorno, 0)) * p.custo AS valor_total
                FROM almoxarifado_apoio.tblsaidabolsa AS b
                JOIN producao.qry3descricoes AS p ON b.codcompladicional = p.codcompladicional
                WHERE b.codigo_bolsa = @bolsa AND b.codfun = @func AND b.destino_shop = @sigla
                ORDER BY b.codigo_bolsa, p.planilha, p.descricao_completa;
            ";
            using var connection = new NpgsqlConnection(_dataBaseSettings.ConnectionString);
            await connection.OpenAsync();
            var resultado = (await connection.QueryAsync<BolsaSaidaDTO>(sql, parameters)).ToList();
            await connection.CloseAsync();

            BolsaItens = new ObservableCollection<BolsaSaidaDTO>(resultado);
            return null; // sucesso
        }
        catch (NpgsqlException ex) when (ex.InnerException is PostgresException pgEx)
        {
            return $"Erro do banco: {pgEx.MessageText}\nLocal: {pgEx.Where}";
        }
        catch (Exception ex)
        {
            return $"Erro inesperado: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    public async Task<string?> AlterarItemBolsaAsync(BolsaSaidaDTO bolsaSaida)
    {
        try
        {
            IsBusy = true;
            using DatabaseContext db = new();
            var bolsaExistente = await db.BolsaSaidas.FindAsync(bolsaSaida.cod_controlelinha);
            var novaBolsa = bolsaExistente;
            novaBolsa.quantidade_retorno = bolsaSaida.quantidade_retorno ?? 0;
            novaBolsa.retorno_por =Environment.UserName;
            novaBolsa.data_retorno = DateTime.Now;
            db.Entry(bolsaExistente).CurrentValues.SetValues(novaBolsa);
            await db.SaveChangesAsync();
            return null; // sucesso
        }
        catch (NpgsqlException ex) when (ex.InnerException is PostgresException pgEx)
        {
            return $"Erro do banco: {pgEx.MessageText}\nLocal: {pgEx.Where}";
        }
        catch (Exception ex)
        {
            return $"Erro inesperado: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }
}
