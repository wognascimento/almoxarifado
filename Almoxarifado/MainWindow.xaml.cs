using Almoxarifado.DataBase;
using Almoxarifado.DataBase.Model.DTO;
using Almoxarifado.Interfaces;
using Almoxarifado.Views.Cadastros;
using Almoxarifado.Views.Consultas;
using Almoxarifado.Views.Movimentacoes;
using Dapper;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Syncfusion.SfSkinManager;
using Syncfusion.Windows.Tools.Controls;
using Syncfusion.XlsIO;
using System.Collections.Specialized;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Telerik.Windows.Controls;

namespace Almoxarifado;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window, IModalService
{

    #region Fields
    private string currentVisualStyle;
    private string currentSizeMode;
    #endregion

    #region Properties
    /// <summary>
    /// Gets or sets the current visual style.
    /// </summary>
    /// <value></value>
    /// <remarks></remarks>
    public string CurrentVisualStyle
    {
        get { return currentVisualStyle; }
        set { currentVisualStyle = value; OnVisualStyleChanged(); }
    }

    /// <summary>
    /// Gets or sets the current Size mode.
    /// </summary>
    /// <value></value>
    /// <remarks></remarks>
    public string CurrentSizeMode
    {
        get { return currentSizeMode; }
        set { currentSizeMode = value; OnSizeModeChanged(); }
    }

    #endregion

    /// <summary>
    /// On Visual Style Changed.
    /// </summary>
    /// <remarks></remarks>
    private void OnVisualStyleChanged()
    {
        VisualStyles visualStyle = VisualStyles.Default;
        Enum.TryParse(CurrentVisualStyle, out visualStyle);
        if (visualStyle != VisualStyles.Default)
        {
            SfSkinManager.ApplyStylesOnApplication = true;
            SfSkinManager.SetVisualStyle(this, visualStyle);
            SfSkinManager.ApplyStylesOnApplication = false;
        }
    }

    /// <summary>
    /// On Size Mode Changed event.
    /// </summary>
    /// <remarks></remarks>
    private void OnSizeModeChanged()
    {
        Syncfusion.SfSkinManager.SizeMode sizeMode = Syncfusion.SfSkinManager.SizeMode.Default;
        Enum.TryParse(CurrentSizeMode, out sizeMode);
        if (sizeMode != Syncfusion.SfSkinManager.SizeMode.Default)
        {
            SfSkinManager.ApplyStylesOnApplication = true;
            SfSkinManager.SetSizeMode(this, sizeMode);
            SfSkinManager.ApplyStylesOnApplication = false;
        }
    }

    DataBaseSettings BaseSettings = DataBaseSettings.Instance;

    public MainWindow()
    {
        InitializeComponent();
        this.Loaded += OnLoaded;
        //StyleManager.ApplicationTheme = new Windows11Theme() ;
        //StyleManager.ApplicationTheme = new CrystalTheme();

        var appSettings = ConfigurationManager.GetSection("appSettings") as NameValueCollection;
        if (appSettings[0].Length > 0)
            BaseSettings.Username = appSettings[0];

        txtUsername.Text = BaseSettings.Username;
        txtDataBase.Text = BaseSettings.Database;
    }

    /// <summary>
    /// Called when [loaded].
    /// </summary>
    /// <param name="sender">The sender.</param>
    /// <param name="e">The <see cref="RoutedEventArgs"/> instance containing the event data.</param>
    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        CurrentVisualStyle = "Metro"; // "FluentLight";
        CurrentSizeMode = "Default";
    }


    public void ShowModalAt(UserControl modalContent, Point position, Size size)
    {
        try
        {
            // Configurar conteúdo
            ModalContent.Content = modalContent;

            // Ajustar posição para coordenadas da janela atual
            var adjustedPosition = AdjustModalPosition(position, size);

            // Posicionar modal
            Canvas.SetLeft(ModalContainer, adjustedPosition.X);
            Canvas.SetTop(ModalContainer, adjustedPosition.Y);
            ModalContainer.Width = size.Width;
            ModalContainer.Height = size.Height;

            // Mostrar modal com animação
            ShowModalWithAnimation();

            // Focar no modal
            modalContent.Focus();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Erro ao abrir modal: {ex.Message}", "Erro",
                           MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    public void CloseModal()
    {
        HideModalWithAnimation();
        ModalContent.Content = null;
    }

    private Point AdjustModalPosition(Point requestedPosition, Size modalSize)
    {
        double x = requestedPosition.X;
        double y = requestedPosition.Y;

        // Obter dimensões da janela
        double windowWidth = ActualWidth;
        double windowHeight = ActualHeight;

        const double margin = 20;

        // Ajustar X se sair da tela pela direita
        if (x + modalSize.Width > windowWidth - margin)
            x = windowWidth - modalSize.Width - margin;

        // Ajustar X se sair da tela pela esquerda
        if (x < margin)
            x = margin;

        // Ajustar Y se sair da tela por baixo
        if (y + modalSize.Height > windowHeight - margin)
        {
            // Tentar posicionar acima do ponto de referência
            y = requestedPosition.Y - modalSize.Height - 10;

            // Se ainda assim não couber, centralizar
            if (y < margin)
                y = (windowHeight - modalSize.Height) / 2;
        }

        return new Point(Math.Max(margin, x), Math.Max(margin, y));
    }

    private void ShowModalWithAnimation()
    {
        ModalOverlay.Visibility = Visibility.Visible;
        ModalContainer.Visibility = Visibility.Visible;

        // Aplicar transformação para animação
        ModalContainer.RenderTransform = new System.Windows.Media.ScaleTransform(0.8, 0.8);
        ModalContainer.RenderTransformOrigin = new Point(0.5, 0.5);
        ModalContainer.Opacity = 0;

        // Criar animação
        var scaleXAnim = new System.Windows.Media.Animation.DoubleAnimation(0.8, 1.0,
            TimeSpan.FromMilliseconds(200));
        var scaleYAnim = new System.Windows.Media.Animation.DoubleAnimation(0.8, 1.0,
            TimeSpan.FromMilliseconds(200));
        var opacityAnim = new System.Windows.Media.Animation.DoubleAnimation(0, 1,
            TimeSpan.FromMilliseconds(200));

        ModalContainer.RenderTransform.BeginAnimation(
            System.Windows.Media.ScaleTransform.ScaleXProperty, scaleXAnim);
        ModalContainer.RenderTransform.BeginAnimation(
            System.Windows.Media.ScaleTransform.ScaleYProperty, scaleYAnim);
        ModalContainer.BeginAnimation(OpacityProperty, opacityAnim);
    }

    private void HideModalWithAnimation()
    {
        var opacityAnim = new System.Windows.Media.Animation.DoubleAnimation(1, 0,
            TimeSpan.FromMilliseconds(150));

        opacityAnim.Completed += (s, e) => {
            ModalOverlay.Visibility = Visibility.Collapsed;
            ModalContainer.Visibility = Visibility.Collapsed;
        };

        ModalContainer.BeginAnimation(OpacityProperty, opacityAnim);
    }

    private void ModalOverlay_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.Source == ModalOverlay)
        {
            CloseModal();
        }
    }
    /*
    protected override void OnSizeChanged(SizeChangedEventArgs e)
    {
        base.OnSizeChanged(e);

        // Reposicionar modal se estiver aberta
        if (ModalContainer.Visibility == Visibility.Visible && ModalContent.Content != null)
        {
            var currentPos = new Point(Canvas.GetLeft(ModalContainer), Canvas.GetTop(ModalContainer));
            var modalSize = new Size(ModalContainer.Width, ModalContainer.Height);
            var adjustedPos = AdjustModalPosition(currentPos, modalSize);

            Canvas.SetLeft(ModalContainer, adjustedPos.X);
            Canvas.SetTop(ModalContainer, adjustedPos.Y);
        }
    }
    */
    private void _mdi_CloseAllTabs(object sender, Syncfusion.Windows.Tools.Controls.CloseTabEventArgs e)
    {
        _mdi.Items.Clear();
    }

    private void _mdi_CloseButtonClick(object sender, Syncfusion.Windows.Tools.Controls.CloseButtonEventArgs e)
    {
        var tab = (DocumentContainer)sender;
        _mdi.Items.Remove(tab.ActiveDocument);
    }

    private FrameworkElement ExistDocumentInDocumentContainer(string name_)
    {
        foreach (FrameworkElement element in _mdi.Items)
        {
            if (name_.ToLower() == element.Name)
            {
                return element;
            }
        }
        return null;
    }

    public void adicionarFilho(object filho, string title, string name)
    {
        var doc = ExistDocumentInDocumentContainer(name);
        if (doc == null)
        {
            doc = (FrameworkElement?)filho;
            DocumentContainer.SetHeader(doc, title);
            doc.Name = name.ToLower();
            _mdi.Items.Add(doc);
        }
        else
        {
            //_mdi.RestoreDocument(doc as UIElement);
            _mdi.ActiveDocument = doc;
        }
    }

    private void OnAlterarUsuario(object sender, MouseButtonEventArgs e)
    {
        Login window = new();
        window.ShowDialog();

        try
        {
            var appSettings = ConfigurationManager.GetSection("appSettings") as NameValueCollection;

            if (appSettings[0] == null || appSettings[0] == "")
            {
                return;
            }
            BaseSettings.Username = appSettings[0];
            txtUsername.Text = BaseSettings.Username;
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message);
        }
    }

    private void OnAlterarAno(object sender, MouseButtonEventArgs e)
    {
        RadWindow.Prompt(new DialogParameters()
        {
            Header = "Ano Sistema",
            Content = "Alterar o Ano do Sistema",
            Closed = (object sender, WindowClosedEventArgs e) =>
            {
                if (e.PromptResult != null)
                {
                    BaseSettings.Database = e.PromptResult;
                    txtDataBase.Text = BaseSettings.Database;
                    _mdi.Items.Clear();
                }
            }
        });
    }

    private void OnAlmoxSaidaClick(object sender, RoutedEventArgs e)
    {
        adicionarFilho(new AlmoxSaida(), "ALMOXARIFADO MOVIMENTAÇÃO DE SAÍDA", "ALMOX_MOV_SAIDA");
    }

    private void OnAlmoxEntradaClick(object sender, RoutedEventArgs e)
    {
        adicionarFilho(new AlmoxEntrada(), "ALMOXARIFADO MOVIMENTAÇÃO DE ENTRADA", "ALMOX_MOV_ENTRADA");
    }

    private void OnBolsaSaidaClick(object sender, RoutedEventArgs e)
    {
        adicionarFilho(new BolsaSaida(), "ALMOXARIFADO BOLSA MOVIMENTAÇÃO DE SAÍDA", "ALMOX_BOLSA_MOV_SAIDA");
    }

    private void OnBolsaEntradaClick(object sender, RoutedEventArgs e)
    {
        adicionarFilho(new BolsaEntrada(), "ALMOXARIFADO BOLSA MOVIMENTAÇÃO DE ENTRADA", "ALMOX_BOLSA_MOV_ENTRADA");
    }

    private async void OnSaldoEstoqueClick(object sender, RoutedEventArgs e)
    {
        try
        {
            Application.Current.Dispatcher.Invoke(() => { Mouse.OverrideCursor = Cursors.Wait; });

            using DatabaseContext db = new();
            //var data = await db.PendenciaProducaos.ToListAsync();
            var data = await db.SaldoEstoques.ToListAsync();

            using ExcelEngine excelEngine = new ExcelEngine();
            IApplication application = excelEngine.Excel;

            application.DefaultVersion = ExcelVersion.Xlsx;

            //Create a workbook
            IWorkbook workbook = application.Workbooks.Create(1);
            IWorksheet worksheet = workbook.Worksheets[0];
            //worksheet.IsGridLinesVisible = false;
            worksheet.ImportData(data, 1, 1, true);

            workbook.SaveAs("Impressos/SALDO-ESTOQUE.xlsx");
            Process.Start(new ProcessStartInfo("Impressos\\SALDO-ESTOQUE.xlsx")
            {
                UseShellExecute = true
            });

            Application.Current.Dispatcher.Invoke(() => { Mouse.OverrideCursor = null; });
        }
        catch (Exception ex)
        {
            Application.Current.Dispatcher.Invoke(() => { Mouse.OverrideCursor = null; });
            MessageBox.Show(ex.Message);
        }
    }

    private async void OnSaldoFuncionarioClick(object sender, RoutedEventArgs e)
    {
        try
        {
            Application.Current.Dispatcher.Invoke(() => { Mouse.OverrideCursor = Cursors.Wait; });

            using DatabaseContext db = new();
            //var data = await db.PendenciaProducaos.ToListAsync();
            var data = await db.SaldoFuncionarioDebitos.ToListAsync();

            using ExcelEngine excelEngine = new ExcelEngine();
            IApplication application = excelEngine.Excel;

            application.DefaultVersion = ExcelVersion.Xlsx;

            //Create a workbook
            IWorkbook workbook = application.Workbooks.Create(1);
            IWorksheet worksheet = workbook.Worksheets[0];
            //worksheet.IsGridLinesVisible = false;
            worksheet.ImportData(data, 1, 1, true);

            workbook.SaveAs("Impressos/SALDO-FUNCIONARIO.xlsx");
            Process.Start(new ProcessStartInfo("Impressos\\SALDO-FUNCIONARIO.xlsx")
            {
                UseShellExecute = true
            });

            Application.Current.Dispatcher.Invoke(() => { Mouse.OverrideCursor = null; });
        }
        catch (Exception ex)
        {
            Application.Current.Dispatcher.Invoke(() => { Mouse.OverrideCursor = null; });
            MessageBox.Show(ex.Message);
        }
    }

    private async void OnPontoPedidoClick(object sender, RoutedEventArgs e)
    {
        try
        {
            Application.Current.Dispatcher.Invoke(() => { Mouse.OverrideCursor = Cursors.Wait; });

            using DatabaseContext db = new();
            //var data = await db.PendenciaProducaos.ToListAsync();
            var data = await db.AnalisePontoPedidos.ToListAsync();

            using ExcelEngine excelEngine = new ExcelEngine();
            IApplication application = excelEngine.Excel;

            application.DefaultVersion = ExcelVersion.Xlsx;

            //Create a workbook
            IWorkbook workbook = application.Workbooks.Create(1);
            IWorksheet worksheet = workbook.Worksheets[0];
            //worksheet.IsGridLinesVisible = false;
            worksheet.ImportData(data, 1, 1, true);

            workbook.SaveAs("Impressos/PONTO-PEDIDO.xlsx");
            Process.Start(new ProcessStartInfo("Impressos\\PONTO-PEDIDO.xlsx")
            {
                UseShellExecute = true
            });

            Application.Current.Dispatcher.Invoke(() => { Mouse.OverrideCursor = null; });
        }
        catch (Exception ex)
        {
            Application.Current.Dispatcher.Invoke(() => { Mouse.OverrideCursor = null; });
            MessageBox.Show(ex.Message);
        }
    }

    private void OnCadastroAtendenteClick(object sender, RoutedEventArgs e)
    {
        adicionarFilho(new Atendentes(), "CADASTRO DE ATENDENTES", "CADASTRO_ATENDENTE");
    }

    private async void OnMovSaida(object sender, RoutedEventArgs e)
    {
        try
        {
            Application.Current.Dispatcher.Invoke(() => { Mouse.OverrideCursor = Cursors.Wait; });

            using DatabaseContext db = new();
            var resultado = (
            from sa in db.Saidas
            join qd in db.Descricoes on sa.codcompladicional equals qd.codcompladicional
            join vf in db.Funcionarios on sa.codfun equals vf.codfun
            orderby sa.data, vf.nome_apelido, qd.planilha, qd.descricao_completa
            select new
            {
                sa.cod_movimentacao,
                sa.cod_saida_almox,
                sa.codfun,
                vf.nome_apelido,
                vf.setor,
                sa.codcompladicional,
                qd.planilha,
                qd.descricao_completa,
                qd.unidade,
                sa.qtde,
                sa.data,
                sa.resp,
                sa.obs,
                sa.num_os
            }).ToList();

            using ExcelEngine excelEngine = new ExcelEngine();
            IApplication application = excelEngine.Excel;

            application.DefaultVersion = ExcelVersion.Xlsx;

            //Create a workbook
            IWorkbook workbook = application.Workbooks.Create(1);
            IWorksheet worksheet = workbook.Worksheets[0];
            //worksheet.IsGridLinesVisible = false;
            worksheet.ImportData(resultado, 1, 1, true);

            workbook.SaveAs("Impressos/MOVIMENTACOES-SAIDAS.xlsx");
            Process.Start(new ProcessStartInfo("Impressos\\MOVIMENTACOES-SAIDAS.xlsx")
            {
                UseShellExecute = true
            });

            Application.Current.Dispatcher.Invoke(() => { Mouse.OverrideCursor = null; });
        }
        catch (Exception ex)
        {
            Application.Current.Dispatcher.Invoke(() => { Mouse.OverrideCursor = null; });
            MessageBox.Show(ex.Message);
        }
    }

    private async void OnMovEntrada(object sender, RoutedEventArgs e)
    {
        try
        {
            Application.Current.Dispatcher.Invoke(() => { Mouse.OverrideCursor = Cursors.Wait; });

            using DatabaseContext db = new();
            var resultado = (
            from sa in db.Entradas
            join qd in db.Descricoes on sa.codcompladicional equals qd.codcompladicional
            join vf in db.Funcionarios on sa.codfun equals vf.codfun
            orderby sa.data, vf.nome_apelido, qd.planilha, qd.descricao_completa
            select new
            {
                sa.cod_movimentacao,
                sa.cod_entrada_almox,
                sa.codfun,
                vf.nome_apelido,
                vf.setor,
                sa.codcompladicional,
                qd.planilha,
                qd.descricao_completa,
                qd.unidade,
                sa.origem,
                sa.qtde,
                sa.data,
                sa.resp,
                sa.obs,
                sa.num_os
            }).ToList();

            using ExcelEngine excelEngine = new ExcelEngine();
            IApplication application = excelEngine.Excel;

            application.DefaultVersion = ExcelVersion.Xlsx;

            //Create a workbook
            IWorkbook workbook = application.Workbooks.Create(1);
            IWorksheet worksheet = workbook.Worksheets[0];
            //worksheet.IsGridLinesVisible = false;
            worksheet.ImportData(resultado, 1, 1, true);

            workbook.SaveAs("Impressos/MOVIMENTACOES-ENTRADA.xlsx");
            Process.Start(new ProcessStartInfo("Impressos\\MOVIMENTACOES-ENTRADA.xlsx")
            {
                UseShellExecute = true
            });

            Application.Current.Dispatcher.Invoke(() => { Mouse.OverrideCursor = null; });
        }
        catch (Exception ex)
        {
            Application.Current.Dispatcher.Invoke(() => { Mouse.OverrideCursor = null; });
            MessageBox.Show(ex.Message);
        }
    }

    private void OnProdutosAlmoxarifado(object sender, RoutedEventArgs e)
    {
        adicionarFilho(new ProdutoAlmoxarifado(), "PRODUTOS ALMOXARIFADO", "PRODUTOS_ALMOXARIFADO"); //ProdutoAlmoxarifado
    }

    private void OnCadastroTerceiro(object sender, RoutedEventArgs e)
    {
        adicionarFilho(new Terceiro(), "CADASTRO DE TERCEIROS", "CADASTRO_TERCEIRO");
    }

    private async void Window_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            Application.Current.Dispatcher.Invoke(() => { Mouse.OverrideCursor = Cursors.Wait; });
            using DatabaseContext db = new();
            await db.Database.ExecuteSqlRawAsync(@"
                    INSERT INTO almoxarifado_jac.t_estoqueinicial (codcompladicional, estoque_inicial, estoque_inicial_processado)
                    SELECT p.codcompladicional, 0, 0
                    FROM almoxarifado_jac.qry_descricoes_produtos p
                    LEFT JOIN almoxarifado_jac.t_estoqueinicial e 
                        ON p.codcompladicional = e.codcompladicional
                    WHERE e.codcompladicional IS NULL;
                ");

            await db.Database.ExecuteSqlRawAsync(@"
                    INSERT INTO almoxarifado_jac.t_barcodes_almox (barcode, codigo, impresso, compra)
                    SELECT b.barcode, b.codigo, b.impresso, b.compra
                    FROM (almoxarifado_jac.qry_descricoes_produtos p
                    LEFT JOIN producao.tbl_barcodes b 
                        ON p.codcompladicional = b.codigo)
                    LEFT JOIN almoxarifado_jac.t_barcodes_almox a 
                        ON b.barcode = a.barcode
                    WHERE a.codigo IS NULL;
                ");

            Application.Current.Dispatcher.Invoke(() => { Mouse.OverrideCursor = null; });
        }
        catch (DbUpdateException ex)
        {
            Application.Current.Dispatcher.Invoke(() => { Mouse.OverrideCursor = null; });
            MessageBox.Show(ex.Message);
        }
    }

    private void OnCadastroBolsa(object sender, RoutedEventArgs e)
    {
        adicionarFilho(new BolsaFerramenta(), "CADASTRAR TIPO DE BOLSA", "CADASTRAR_BOLSA");
    }

    private void OnCadastroBocaFixa(object sender, RoutedEventArgs e)
    {
        adicionarFilho(new BolsaFixa(), "CADASTRAR ITENS BOLSA", "CADASTRAR_BOLSA_FIXA");
    }

    private async void OnMovBolsaClick(object sender, RoutedEventArgs e)
    {
        try
        {
            Application.Current.Dispatcher.Invoke(() => { Mouse.OverrideCursor = Cursors.Wait; });

            using DatabaseContext db = new();

            string sql = @"
               SELECT
	                f.nome_apelido,
	                f.setor,
                    b.cod_controlelinha,
                    b.codigo_bolsa,
                    b.codcompladicional,
                    p.planilha,
                    p.descricao_completa,
                    p.unidade,
                    b.quantidade AS quantidade_saida,   
                    COALESCE(b.quantidade_retorno, 0) AS quantidade_retorno,
                    p.custo AS valor_unitario,
                    (b.quantidade - COALESCE(b.quantidade_retorno, 0)) * p.custo AS saldo_devedor
                FROM almoxarifado_apoio.tblsaidabolsa AS b
                JOIN producao.qry3descricoes AS p ON b.codcompladicional = p.codcompladicional
                JOIN almoxarifado_jac.view_funcionarios_terceiros AS f ON f.codfun = b.codfun
                ORDER BY f.nome_apelido, f.setor, b.codigo_bolsa, p.planilha, p.descricao_completa;
            ";
            using var connection = new NpgsqlConnection(BaseSettings.ConnectionString);
            await connection.OpenAsync();
            var resultado = (await connection.QueryAsync<MovimentacaoBolsaDTO>(sql)).ToList();
            await connection.CloseAsync();

            using ExcelEngine excelEngine = new();
            IApplication application = excelEngine.Excel;
            application.DefaultVersion = ExcelVersion.Excel2016;

            //Create a workbook
            IWorkbook workbook = application.Workbooks.Create(1);
            IWorksheet worksheet = workbook.Worksheets[0];
            worksheet.ImportData(resultado, 1, 1, true);
            workbook.SaveAs(@$"{BaseSettings.CaminhoSistema}Impressos\MOVIMENTACOES-BOLSA.xlsx");
            Process.Start(new ProcessStartInfo(@$"{BaseSettings.CaminhoSistema}Impressos\MOVIMENTACOES-BOLSA.xlsx")
            {
                UseShellExecute = true
            });

            Application.Current.Dispatcher.Invoke(() => { Mouse.OverrideCursor = null; });
        }
        catch (Exception ex)
        {
            Application.Current.Dispatcher.Invoke(() => { Mouse.OverrideCursor = null; });
            MessageBox.Show(ex.Message);
        }
    }

    private void OnSenhaCadeadoClick(object sender, RoutedEventArgs e)
    {
        adicionarFilho(new SenhaCadeado(), "SENHA CADEADO", "SENHA_CADEADO");
    }
}