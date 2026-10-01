using Almoxarifado.DataBase;
using Almoxarifado.DataBase.Model;
using Almoxarifado.DataBase.Model.DTO;
using Almoxarifado.Interfaces;
using Almoxarifado.Views.Cadastros;
using Almoxarifado.Views.Consultas;
using Almoxarifado.Views.Movimentacoes;
using ClosedXML.Excel;
using Dapper;
using Npgsql;
using System.Configuration;
using System.Diagnostics;
using System.IO;
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
    DataBaseSettings BaseSettings = DataBaseSettings.Instance;

    public MainWindow()
    {
        InitializeComponent();
        this.Loaded += OnLoaded;
        PreviewKeyDown += OnRefreshKey;
        //StyleManager.ApplicationTheme = new Windows11Theme() ;
        //StyleManager.ApplicationTheme = new CrystalTheme();

        var username = ConfigurationManager.AppSettings["Username"];
        if (!string.IsNullOrWhiteSpace(username))
        {
            BaseSettings.Username = username;
            BaseSettings.RefreshConnectionString();
        }

        txtUsername.Text = BaseSettings.Username;
        txtDataBase.Text = BaseSettings.Database;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
    }

    private async void OnAtualizarSistemaClick(object sender, RoutedEventArgs e)
    {
        await ((App)Application.Current).CheckForUpdatesAsync();
    }

    private void OnRefreshKey(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.F5) return;
        e.Handled = true;
        if (documentGroup.SelectedItem is not RadPane pane || pane.Content is not UserControl current) return;
        if (MessageBox.Show("Recarregar esta tela? Edicoes ainda nao gravadas serao descartadas.",
            "Atualizar", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;
        try
        {
            if (Activator.CreateInstance(current.GetType()) is UserControl refreshed)
                pane.Content = refreshed;
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Erro ao atualizar");
        }
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
    public void adicionarFilho(object filho, string title, string name)
    {
        var pane = ExistDocumentInDocumentContainer(name);
        if (pane == null)
        {
            pane = new RadPane
            {
                Header = title,
                Name = name.ToLower(),
                Content = filho,
                CanUserClose = true,
                IsSelected = true
            };

            documentGroup.Items.Add(pane);
            pane.IsActive = true;
        }
        else
        {
            pane.IsSelected = true;
            pane.IsActive = true;
        }
    }

    private RadPane? ExistDocumentInDocumentContainer(string name_)
    {
        return documentGroup.Items
            .OfType<RadPane>()
            .FirstOrDefault(p => string.Equals(p.Name, name_.ToLower(), StringComparison.OrdinalIgnoreCase));
    }

    private async Task<List<T>> QueryAsync<T>(string sql, object? parametros = null)
    {
        await using var connection = new NpgsqlConnection(BaseSettings.ConnectionString);
        var data = await connection.QueryAsync<T>(sql, parametros);
        return data.ToList();
    }

    private static void ExportarXlsx<T>(IEnumerable<T> data, string caminho)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(caminho) ?? ".");
        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Dados");
        worksheet.Cell(1, 1).InsertTable(data, "Dados", false);
        workbook.SaveAs(caminho);
    }

    private static void AbrirArquivo(string caminho)
    {
        Process.Start(new ProcessStartInfo(caminho)
        {
            UseShellExecute = true
        });
    }

    private void OnAlterarUsuario(object sender, MouseButtonEventArgs e)
    {
        Login window = new();
        window.ShowDialog();

        try
        {
            var username = ConfigurationManager.AppSettings["Username"];

            if (string.IsNullOrWhiteSpace(username))
            {
                return;
            }
            BaseSettings.Username = username;
            BaseSettings.RefreshConnectionString();
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
                    BaseSettings.RefreshConnectionString();
                    documentGroup.Items.Clear();
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

            var data = await QueryAsync<SaldoEstoqueModel>("SELECT * FROM almoxarifado_jac.saldo_estoque_final;");
            var caminho = Path.Combine(BaseSettings.CaminhoSistema, "Impressos", "SALDO-ESTOQUE.xlsx");
            ExportarXlsx(data, caminho);
            AbrirArquivo(caminho);

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

            var data = await QueryAsync<SaldoFuncionarioDebito>("SELECT * FROM almoxarifado_jac.sig_saldo_funcionario_debito;");
            var caminho = Path.Combine(BaseSettings.CaminhoSistema, "Impressos", "SALDO-FUNCIONARIO.xlsx");
            ExportarXlsx(data, caminho);
            AbrirArquivo(caminho);

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

            var data = await QueryAsync<AnalisePontoPedidoModel>("SELECT * FROM almoxarifado_jac.qry_analise_saldo_estoque_ponto_pedido;");
            var caminho = Path.Combine(BaseSettings.CaminhoSistema, "Impressos", "PONTO-PEDIDO.xlsx");
            ExportarXlsx(data, caminho);
            AbrirArquivo(caminho);

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

            var resultado = await QueryAsync<dynamic>(@"
                SELECT
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
                FROM almoxarifado_jac.t_saida_almox sa
                JOIN producao.qry3descricoes qd ON qd.codcompladicional = sa.codcompladicional
                JOIN almoxarifado_jac.view_funcionarios_terceiros vf ON vf.codfun = sa.codfun
                ORDER BY sa.data, vf.nome_apelido, qd.planilha, qd.descricao_completa;");

            var caminho = Path.Combine(BaseSettings.CaminhoSistema, "Impressos", "MOVIMENTACOES-SAIDAS.xlsx");
            ExportarXlsx(resultado, caminho);
            AbrirArquivo(caminho);

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

            var resultado = await QueryAsync<dynamic>(@"
                SELECT
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
                FROM almoxarifado_jac.t_entrada_almox sa
                JOIN producao.qry3descricoes qd ON qd.codcompladicional = sa.codcompladicional
                JOIN almoxarifado_jac.view_funcionarios_terceiros vf ON vf.codfun = sa.codfun
                ORDER BY sa.data, vf.nome_apelido, qd.planilha, qd.descricao_completa;");

            var caminho = Path.Combine(BaseSettings.CaminhoSistema, "Impressos", "MOVIMENTACOES-ENTRADA.xlsx");
            ExportarXlsx(resultado, caminho);
            AbrirArquivo(caminho);

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
            await using var connection = new NpgsqlConnection(BaseSettings.ConnectionString);
            await connection.OpenAsync();
            await using var transaction = await connection.BeginTransactionAsync();
            await connection.ExecuteAsync("LOCK TABLE almoxarifado_jac.t_estoqueinicial, almoxarifado_jac.t_barcodes_almox IN SHARE ROW EXCLUSIVE MODE;", transaction: transaction);
            await connection.ExecuteAsync(@"
                    INSERT INTO almoxarifado_jac.t_estoqueinicial (codcompladicional, estoque_inicial, estoque_inicial_processado)
                    SELECT DISTINCT p.codcompladicional, 0, 0
                    FROM almoxarifado_jac.qry_descricoes_produtos p
                    LEFT JOIN almoxarifado_jac.t_estoqueinicial e 
                        ON p.codcompladicional = e.codcompladicional
                    WHERE e.codcompladicional IS NULL;
                ", transaction: transaction);

            await connection.ExecuteAsync(@"
                    INSERT INTO almoxarifado_jac.t_barcodes_almox (barcode, codigo, impresso, compra)
                    SELECT DISTINCT b.barcode, b.codigo, b.impresso, b.compra
                    FROM (almoxarifado_jac.qry_descricoes_produtos p
                    LEFT JOIN producao.tbl_barcodes b 
                        ON p.codcompladicional = b.codigo)
                    LEFT JOIN almoxarifado_jac.t_barcodes_almox a 
                        ON b.barcode = a.barcode
                    WHERE a.barcode IS NULL AND b.barcode IS NOT NULL;
                ", transaction: transaction);
            await transaction.CommitAsync();

            Application.Current.Dispatcher.Invoke(() => { Mouse.OverrideCursor = null; });
        }
        catch (Exception ex)
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

            string sql = @"
              SELECT
                 f.nome_apelido,
                 f.setor,
                 b.cod_controlelinha,
                 tp.descricao AS bolsa,
	             b.destino_shop,
                 b.codcompladicional,
                 p.planilha,
                 p.descricao_completa,
                 p.unidade,
                 b.quantidade AS quantidade_saida,   
                 b.quantidade_retorno,
                 p.custo AS valor_unitario,
                 (b.quantidade - COALESCE(b.quantidade_retorno, 0)) * p.custo AS saldo_devedor,
	             b.retorno_por,
	             b.retorno_em
             FROM almoxarifado_apoio.tblsaidabolsa AS b
             JOIN producao.qry3descricoes AS p ON b.codcompladicional = p.codcompladicional
             JOIN almoxarifado_jac.view_funcionarios_terceiros AS f ON f.codfun = b.codfun
             JOIN almoxarifado_apoio.tbltipobolsa AS tp ON b.codigo_bolsa = tp.codigo_tipobolsa
             ORDER BY f.nome_apelido, f.setor, b.codigo_bolsa, p.planilha, p.descricao_completa;
            ";
            var resultado = await QueryAsync<MovimentacaoBolsaDTO>(sql);
            var caminho = Path.Combine(BaseSettings.CaminhoSistema, "Impressos", "MOVIMENTACOES-BOLSA.xlsx");
            ExportarXlsx(resultado, caminho);
            AbrirArquivo(caminho);

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
