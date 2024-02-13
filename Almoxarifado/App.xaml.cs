using Almoxarifado.DataBase;
using System.Windows;

namespace Almoxarifado
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public App()
        {
            // Add your Syncfusion license key for WPF platform with corresponding Syncfusion NuGet version referred in project. For more information about license key see https://help.syncfusion.com/common/essential-studio/licensing/license-key.
            // Syncfusion.Licensing.SyncfusionLicenseProvider.RegisterLicense("Add your license key here"); 

            //Syncfusion.Licensing.SyncfusionLicenseProvider.RegisterLicense("NzI0OUAzMjMzMkUzMTJFMzljWE9mZml0ZnFGQ2xaaUFSUXBGeVdweDhiOCtBbUlDSWptZEgyVTYrcU1ZPQ==");
            Syncfusion.Licensing.SyncfusionLicenseProvider.RegisterLicense("MjM5MkAzMjM0MkUzMTJFMzlZRnNmeEdKa0haRGU0S0MyZUR3b05vcDJFNURBbnFRTi9STUVidExydWswPQ==");

            DataBaseSettings BaseSettings = DataBaseSettings.Instance;
            BaseSettings.Database = DateTime.Now.Year.ToString();
            BaseSettings.Host = "postgresql-server";
            BaseSettings.Username = Environment.UserName;
            BaseSettings.Password = "123mudar";

        }
    }

}
