using Almoxarifado.DataBase;
using Almoxarifado.Localization;
using BibliotecasSIG;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Text.Json;
using System.Threading;
using System.Windows;
using System.Windows.Markup;
using Telerik.Windows.Controls;

namespace Almoxarifado
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private readonly DataBaseSettings baseSettings = DataBaseSettings.Instance;
        private readonly string currentVersion = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "0.0.0.0";
        private bool checkingForUpdates;

        public App()
        {
            EventManager.RegisterClassHandler(typeof(RadGridView), FrameworkElement.LoadedEvent,
                new RoutedEventHandler(OnGridLoaded));
            baseSettings.LoadFromConfiguration();
            StyleManager.ApplicationTheme = new Office2016Theme();

            LocalizationManager.Manager = new LocalizationManager()
            {
                ResourceManager = GridViewResources.ResourceManager
            };

        }

        protected override async void OnStartup(StartupEventArgs e)
        {
            AplicarCulturaPadrao();

            base.OnStartup(e);

            await System.Windows.Threading.Dispatcher.Yield(System.Windows.Threading.DispatcherPriority.ApplicationIdle);
            await CheckForUpdatesAsync(showMessages: false);
        }

        private static void OnGridLoaded(object sender, RoutedEventArgs e)
        {
            if (sender is not RadGridView grid) return;
            grid.FilterOperatorsLoading -= OnFilterOperatorsLoading;
            grid.FilterOperatorsLoading += OnFilterOperatorsLoading;
        }

        private static void OnFilterOperatorsLoading(object sender, Telerik.Windows.Controls.GridView.FilterOperatorsLoadingEventArgs e)
        {
            if (e.AvailableOperators.Contains(Telerik.Windows.Data.FilterOperator.Contains))
            {
                e.DefaultOperator1 = Telerik.Windows.Data.FilterOperator.Contains;
                e.DefaultOperator2 = Telerik.Windows.Data.FilterOperator.Contains;
            }
        }


        internal async Task CheckForUpdatesAsync(bool showMessages = true)
        {
            if (checkingForUpdates) return;
            checkingForUpdates = true;
            try
            {
                if (string.IsNullOrWhiteSpace(baseSettings.UpdateInfoUrl))
                {
                    if (showMessages)
                        MessageBox.Show("O endereco de atualizacao nao esta configurado.", "Atualizacao do sistema");
                    return;
                }

                var updateChecker = new UpdateChecker(baseSettings.UpdateInfoUrl, currentVersion);
                var updateInfo = await updateChecker.CheckForUpdatesAsync();

                if (updateInfo == null)
                {
                    if (showMessages)
                        MessageBox.Show($"O sistema ja esta atualizado.\n\nVersao instalada: {currentVersion}",
                            "Atualizacao do sistema", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                if (updateInfo != null)
                {
                    // Pergunta ao usuário se deseja atualizar
                    var result = MessageBox.Show(
                        $"Nova versão disponível!\n\n" +
                        $"Versão atual: {currentVersion}\n" +
                        $"Nova versão: {updateInfo.updateVersion}\n\n" +
                        "Changelog:\n" +
                        string.Join("\n", updateInfo.changelog) +
                        "\n\nDeseja baixar a atualização?",
                        "Atualização Disponível",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Information
                    );

                    if (result == MessageBoxResult.Yes)
                    {
                        showMessages = true;

                        string updateExecutable = Path.Combine(AppContext.BaseDirectory, "Update.exe");
                        if (!File.Exists(updateExecutable))
                            throw new FileNotFoundException("O atualizador Update.exe nao foi encontrado.", updateExecutable);

                        var startInfo = new ProcessStartInfo(updateExecutable)
                        {
                            UseShellExecute = true,
                            WorkingDirectory = AppContext.BaseDirectory
                        };
                        startInfo.ArgumentList.Add(JsonSerializer.Serialize(updateInfo));
                        startInfo.ArgumentList.Add("Almoxarifado.exe");
                        if (Process.Start(startInfo) == null)
                            throw new InvalidOperationException("Nao foi possivel iniciar o atualizador.");
                        this.Shutdown();

                    }
                }
            }
            catch (Exception ex) when (!showMessages)
            {
                Trace.TraceWarning("Falha na verificacao automatica de atualizacao: {0}", ex.Message);
            }
            catch (HttpRequestException ex)
            {
                // Log do erro ou tratamento de exceção
                MessageBox.Show(
                    $"Erro ao verificar atualizações: {ex.Message}",
                    "Erro",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Erro ao verificar atualizações: {ex.Message}",
                    "Erro",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
            }
            finally
            {
                checkingForUpdates = false;
            }
        }

        private static void AplicarCulturaPadrao()
        {
            var culture = CultureInfo.GetCultureInfo("pt-BR");

            CultureInfo.DefaultThreadCurrentCulture = culture;
            CultureInfo.DefaultThreadCurrentUICulture = culture;
            Thread.CurrentThread.CurrentCulture = culture;
            Thread.CurrentThread.CurrentUICulture = culture;

            FrameworkElement.LanguageProperty.OverrideMetadata(
                typeof(FrameworkElement),
                new FrameworkPropertyMetadata(XmlLanguage.GetLanguage(culture.IetfLanguageTag)));

            GridViewResources.Culture = culture;
        }
    }
}
