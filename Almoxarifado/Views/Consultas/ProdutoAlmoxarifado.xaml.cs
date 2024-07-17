using Almoxarifado.Custom;
using Almoxarifado.DataBase;
using Almoxarifado.DataBase.Model;
using Almoxarifado.Views.Cadastros;
using Microsoft.EntityFrameworkCore;
using SharpDX.DirectWrite;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.DirectoryServices.ActiveDirectory;
using System.Linq;
using System.Runtime.InteropServices;
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

namespace Almoxarifado.Views.Consultas
{

    public class TSCLIB_DLL
    {
        [DllImport("TSCLIB.dll", EntryPoint = "about")]
        public static extern int about();

        [DllImport("TSCLIB.dll", EntryPoint = "openport")]
        public static extern int openport(string printername);

        [DllImport("TSCLIB.dll", EntryPoint = "barcode")]
        public static extern int barcode(string x, string y, string type,
                    string height, string readable, string rotation,
                    string narrow, string wide, string code);

        [DllImport("TSCLIB.dll", EntryPoint = "clearbuffer")]
        public static extern int clearbuffer();

        [DllImport("TSCLIB.dll", EntryPoint = "closeport")]
        public static extern int closeport();

        [DllImport("TSCLIB.dll", EntryPoint = "downloadpcx")]
        public static extern int downloadpcx(string filename, string image_name);

        [DllImport("TSCLIB.dll", EntryPoint = "formfeed")]
        public static extern int formfeed();

        [DllImport("TSCLIB.dll", EntryPoint = "nobackfeed")]
        public static extern int nobackfeed();

        [DllImport("TSCLIB.dll", EntryPoint = "printerfont")]
        public static extern int printerfont(string x, string y, string fonttype,
                        string rotation, string xmul, string ymul,
                        string text);

        [DllImport("TSCLIB.dll", EntryPoint = "printlabel")]
        public static extern int printlabel(string set, string copy);

        [DllImport("TSCLIB.dll", EntryPoint = "sendcommand")]
        public static extern int sendcommand(string printercommand);

        [DllImport("TSCLIB.dll", EntryPoint = "setup")]
        public static extern int setup(string width, string height,
                  string speed, string density,
                  string sensor, string vertical,
                  string offset);

        [DllImport("TSCLIB.dll", EntryPoint = "windowsfont")]
        public static extern int windowsfont(int x, int y, int fontheight,
                        int rotation, int fontstyle, int fontunderline,
                        string szFaceName, string content);
        [DllImport("TSCLIB.dll", EntryPoint = "windowsfontUnicode")]
        public static extern int windowsfontUnicode(int x, int y, int fontheight,
                         int rotation, int fontstyle, int fontunderline,
                         string szFaceName, byte[] content);

        [DllImport("TSCLIB.dll", EntryPoint = "sendBinaryData")]
        public static extern int sendBinaryData(byte[] content, int length);

        [DllImport("TSCLIB.dll", EntryPoint = "usbportqueryprinter")]
        public static extern byte usbportqueryprinter();
    }
    /// <summary>
    /// Interação lógica para ProdutoAlmoxarifado.xam
    /// </summary>
    public partial class ProdutoAlmoxarifado : UserControl
    {

        public ProdutoAlmoxarifado()
        {
            InitializeComponent();
            DataContext = new ProdutoAlmoxarifadoViewModel();
        }

        private async void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                Application.Current.Dispatcher.Invoke(() => { Mouse.OverrideCursor = Cursors.Wait; });
                ProdutoAlmoxarifadoViewModel vm = (ProdutoAlmoxarifadoViewModel)DataContext;
                vm.Produtos = await Task.Run(vm.GetProdutosAsync);
                Application.Current.Dispatcher.Invoke(() => { Mouse.OverrideCursor = null; });
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                Application.Current.Dispatcher.Invoke(() => { Mouse.OverrideCursor = null; });
            }
        }

        private void OnPrintClick(object sender, RoutedEventArgs e)
        {
            RadButton _button = (RadButton)sender;
            var DESCRICAO = ((QryDescricaoModel)_button.DataContext);
            try
            {
                // Endereço IP e porta da impressora
                string printerIP = "TSC TTP-244CE";
                string port = "9100"; // Porta padrão para impressoras de rede

                // Combina o IP e a porta para o método openport
                string printerConnection = $"{printerIP}";

                // Abrindo a porta para a impressora
                int result = TSCLIB_DLL.openport(printerConnection);

                if (result == 1)
                {
                    /*
                    sendcommand("SIZE 50 mm, 80 mm");
                    sendcommand("SPEED 4");
                    sendcommand("DENSITY 12");
                    sendcommand("DIRECTION 1");
                    sendcommand("SET TEAR ON");
                    sendcommand("CODEPAGE 1252");
                    List<string> stringList1 = QuebraString(DESCRICAO.descricao_completa, 23);
                    sendcommand($@"^XA~TA000~JSN^LT0^MNW^MTT^PON^PMN^LH0,0^JMA^PR7~SD15^JUS^LRN^CI28");
                    sendcommand($@"^FT173,203^BQN,2,8");
                    sendcommand($@"^FH\^FDLA,{DESCRICAO.codcompladicional}^FS");
                    sendcommand($@"^FO71,25^GB34,168,34^FS");
                    sendcommand($@"^FT77,25^A0R,28,28^FB168,1,0,C^FR^FH\^FD{DESCRICAO.codcompladicional}^FS");
                    sendcommand($@"^FT277,231^A0R,28,28^FH\^FD{(stringList1.Count >= 1 ? stringList1[0].Trim() : "")}^FS");
                    sendcommand($@"^FT250,231^A0R,28,28^FH\^FD{(stringList1.Count >= 2 ? stringList1[1].Trim() : "")}^FS");
                    sendcommand($@"^FT222,231^A0R,28,28^FH\^FD{(stringList1.Count >= 3 ? stringList1[2].Trim() : "")}^FS");
                    sendcommand($@"^FT195,231^A0R,28,28^FH\^FD{(stringList1.Count >= 4 ? stringList1[3].Trim() : "")}^FS");
                    sendcommand($@"^FT167,231^A0R,28,28^FH\^FD{(stringList1.Count >= 5 ? stringList1[4].Trim() : "")}^FS");
                    sendcommand($@"^XZ");
                    closeport();
                    */

                    // Comandos específicos para imprimir texto e código de barras
                    //sendcommand("TEXT 100,100,\"3\",0,1,1,\"Hello, World!\"");
                    //sendcommand("BARCODE 100,200,\"128\",100,1,0,2,2,\"1234567890\"");

                    List<string> stringList1 = QuebraString(DESCRICAO.descricao_completa.Replace("\"", "''"), 28);
                    byte status = TSCLIB_DLL.usbportqueryprinter();//0 = idle, 1 = head open, 16 = pause, following <ESC>!? command of TSPL manual
                    TSCLIB_DLL.sendcommand("SIZE 47.5 mm, 80.1 mm");
                    TSCLIB_DLL.sendcommand("GAP 3 mm, 0 mm");
                    TSCLIB_DLL.sendcommand("DIRECTION 0,0");
                    TSCLIB_DLL.sendcommand("REFERENCE 0,0");
                    TSCLIB_DLL.sendcommand("OFFSET 0 mm");
                    TSCLIB_DLL.sendcommand("SET PEEL OFF");
                    TSCLIB_DLL.sendcommand("SET CUTTER OFF");
                    TSCLIB_DLL.sendcommand("SET PARTIAL_CUTTER OFF");
                    TSCLIB_DLL.sendcommand("SET TEAR ON");
                    TSCLIB_DLL.sendcommand("CLS");
                    TSCLIB_DLL.sendcommand("CODEPAGE 1252");
                    TSCLIB_DLL.clearbuffer();
                    TSCLIB_DLL.downloadpcx("UL.PCX", "UL.PCX");
                    TSCLIB_DLL.sendcommand($"TEXT 319,236,\"ROMAN.TTF\",90,1,10,\"{(stringList1.Count >= 1 ? stringList1[0].Trim() : "")}\"");
                    TSCLIB_DLL.sendcommand($"TEXT 285,236,\"ROMAN.TTF\",90,1,10,\"{(stringList1.Count >= 2 ? stringList1[1].Trim() : "")}\"");
                    TSCLIB_DLL.sendcommand($"TEXT 250,236,\"ROMAN.TTF\",90,1,10,\"{(stringList1.Count >= 3 ? stringList1[2].Trim() : "")}\"");
                    TSCLIB_DLL.sendcommand($"TEXT 216,236,\"ROMAN.TTF\",90,1,10,\"{(stringList1.Count >= 4 ? stringList1[3].Trim() : "")}\"");
                    TSCLIB_DLL.sendcommand($"TEXT 181,236,\"ROMAN.TTF\",90,1,10,\"{(stringList1.Count >= 5 ? stringList1[4].Trim() : "")}\"");
                    TSCLIB_DLL.sendcommand($"TEXT 147,236,\"ROMAN.TTF\",90,1,7,\"{(stringList1.Count >= 6 ? stringList1[5].Trim() : "")}\"");
                    TSCLIB_DLL.sendcommand($"TEXT 112,236,\"ROMAN.TTF\",90,1,7,\"{(stringList1.Count >= 7 ? stringList1[6].Trim() : "")}\"");
                    TSCLIB_DLL.sendcommand($"TEXT 78,236,\"ROMAN.TTF\",90,1,7,\"{(stringList1.Count >= 8 ? stringList1[7].Trim() : "")}\"");
                    TSCLIB_DLL.sendcommand($"QRCODE 321,28,Q,8,A,90,M2,S7,\"{DESCRICAO.codcompladicional}\"");
                    TSCLIB_DLL.sendcommand("ERASE 59,23,54,178");
                    TSCLIB_DLL.sendcommand($"TEXT 112,24,\"ROMAN.TTF\",90,1,16,\"{DESCRICAO.codcompladicional}\"");
                    TSCLIB_DLL.sendcommand("REVERSE 59,23,54,178");
                    TSCLIB_DLL.printlabel("1", "1");
                    TSCLIB_DLL.closeport();
                }
                else
                {
                    Console.WriteLine("Falha ao estabelecer conexão com a impressora.");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private static List<string> QuebraString(string valor, int tamanho)
        {
            List<string> stringList = new List<string>();
            int length = tamanho;
            for (int startIndex = 0; startIndex < valor.Length; startIndex += length)
            {
                if (startIndex + length > valor.Length)
                    length = valor.Length - startIndex;
                string str = valor.Substring(startIndex, length);
                stringList.Add(str);
            }
            return stringList;
        }
    }

    public class ProdutoAlmoxarifadoViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        public void RaisePropertyChanged(string propName) { this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName)); }

        private ObservableCollection<QryDescricaoModel> _produtos;
        public ObservableCollection<QryDescricaoModel> Produtos
        {
            get { return _produtos; }
            set { _produtos = value; RaisePropertyChanged("Produtos"); }
        }

        private ICommand rowDataCommand { get; set; }
        public ICommand RowDataCommand
        {
            get
            {
                return rowDataCommand;
            }
            set
            {
                rowDataCommand = value;
            }
        }

        public ProdutoAlmoxarifadoViewModel()
        {
            //this.PrintCommand = new DelegateCommand(OnCustomCommandExecuted);
            rowDataCommand = new RelayCommand(myCommand_Execute);
        }

        public async Task<ObservableCollection<QryDescricaoModel>> GetProdutosAsync()
        {
            try
            {
                using DatabaseContext db = new();
                var data = await db.Descricoes.OrderBy(c => c.planilha).ThenBy(c => c.descricao).Where(c => c.planilha.Contains("ALMOX") && c.inativo.Contains("0")).ToListAsync();
                return new ObservableCollection<QryDescricaoModel>(data);
            }
            catch (Exception)
            {
                throw;
            }
        }

        public void myCommand_Execute(object obj)
        {
            MessageBox.Show("The MyCommand has been executed");
        }
    }

}
