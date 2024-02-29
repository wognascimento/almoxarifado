using Almoxarifado.DataBase;
using Almoxarifado.Views.Movimentacoes;
using Syncfusion.SfSkinManager;
using Syncfusion.Windows.Tools.Controls;
using System.Collections.Specialized;
using System.Configuration;
using System.Text;
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

namespace Almoxarifado
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
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
    }
}