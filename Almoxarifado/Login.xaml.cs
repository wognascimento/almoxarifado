using Almoxarifado.DataBase;
using System.Configuration;
using System.DirectoryServices;
using System.Windows;
using Telerik.Windows.Controls;

namespace Almoxarifado
{
    /// <summary>
    /// Interação lógica para Login.xam
    /// </summary>
    public partial class Login : RadWindow
    {
        DataBaseSettings BaseSettings = DataBaseSettings.Instance;

        public Login()
        {
            InitializeComponent();
            txtLogin.Focus();
        }

        private void OnSair(object sender, System.Windows.RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }

        private void OnLogar(object sender, System.Windows.RoutedEventArgs e)
        {

            if (!string.IsNullOrWhiteSpace(txtLogin.Text) && !string.IsNullOrWhiteSpace(txtSenha.Password))
            {
                try
                {
                    DirectoryEntry directoryEntry = new DirectoryEntry("LDAP://cipodominio.com.br:389", txtLogin.Text, txtSenha.Password);
                    DirectorySearcher directorySearcher = new DirectorySearcher(directoryEntry);
                    directorySearcher.Filter = "(SAMAccountName=" + txtLogin.Text + ")";
                    SearchResult searchResult = directorySearcher.FindOne();
                    //if ((Int32)searchResult.Properties["userAccountControl"][0] == 512)
                    //{

                        //var appSettings = ConfigurationManager.GetSection("appSettings") as NameValueCollection;
                        //ConfigurationManager.AppSettings["Username"] = txtLogin.Text;
                        /*
                        Configuration config = ConfigurationManager.OpenExeConfiguration("Producao.dll.config");
                        config.AppSettings.Settings["Username"].Value = txtLogin.Text;
                        config.Save(ConfigurationSaveMode.Modified);
                        ConfigurationManager.RefreshSection("appSettings");
                        */

                        Configuration config = ConfigurationManager.OpenExeConfiguration("Almoxarifado.dll");

                        //config.AppSettings.SectionInformation.ConfigSource = "app.config";

                        config.AppSettings.Settings["Username"].Value = txtLogin.Text;
                        config.Save(ConfigurationSaveMode.Modified);

                        ConfigurationManager.RefreshSection("appSettings");

                    BaseSettings.Username = txtLogin.Text;
                    BaseSettings.ConnectionString = $"Host={BaseSettings.Host};Database={BaseSettings.Database};Username={BaseSettings.Username};Password={BaseSettings.Password}";

                    this.DialogResult = true;
                        this.Close();
                    //}
                    //else
                    //{
                    //    MessageBox.Show("ERRO: Usuário/Senha Inválido!");
                    //}
                }
                catch (Exception)
                {
                    MessageBox.Show("Usuário não encontrado!");
                }
            }
        }
    }
}
