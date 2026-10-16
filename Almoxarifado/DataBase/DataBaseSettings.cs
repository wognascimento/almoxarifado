using System.Collections.Specialized;
using System.Configuration;
using System.IO;

namespace Almoxarifado.DataBase;

public sealed class DataBaseSettings
{
    private static readonly DataBaseSettings instance = new();
    private const string EnvHost = "ALMOXARIFADO_DB_HOST";
    private const string EnvDatabase = "ALMOXARIFADO_DB_NAME";
    private const string EnvUsername = "ALMOXARIFADO_DB_USER";
    private const string EnvPassword = "ALMOXARIFADO_DB_PASSWORD";

    public string? Host { get; set; }
    public string? Database { get; set; }
    public string? Username { get; set; }
    public string? Password { get; set; }
    public string? ConnectionString { get; set; }
    public NameValueCollection? AppSetting { get; set; }
    public string CaminhoSistema { get; set; } = @"C:\SIG\Almoxarifado S.I.G\";
    public string? UpdateInfoUrl { get; set; }
    public static DataBaseSettings Instance => instance;

    public void LoadFromConfiguration()
    {
        AppSetting = ConfigurationManager.GetSection("appSettings") as NameValueCollection;

        Host = ReadSetting("DatabaseHost", EnvHost, "192.168.0.23");
        Database = ReadSetting("Database", EnvDatabase, DateTime.Now.Year.ToString());
        Username = ReadSetting("Username", EnvUsername, Environment.UserName);
        Password = ReadSetting("Password", EnvPassword, null);
        CaminhoSistema = ReadSetting("SystemPath", null, CaminhoSistema);
        UpdateInfoUrl = ReadSetting("UpdateInfoUrl", "ALMOXARIFADO_UPDATE_URL", "https://atualizasig.cipolatti.com.br/downloads/almoxarifado/version.json");
        // Resolve legacy settings in memory, including environment overrides, without changing local credentials.
        if (string.Equals(UpdateInfoUrl?.Trim(), "http://192.168.0.49/downloads/almoxarifado/version.json", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(UpdateInfoUrl?.Trim(), "https://192.168.0.49/downloads/almoxarifado/version.json", StringComparison.OrdinalIgnoreCase))
            UpdateInfoUrl = "https://atualizasig.cipolatti.com.br/downloads/almoxarifado/version.json";

        RefreshConnectionString();
    }

    public void RefreshConnectionString()
    {
        if (string.IsNullOrWhiteSpace(Host))
            throw new InvalidOperationException("Host do banco de dados nao configurado.");

        if (string.IsNullOrWhiteSpace(Database))
            throw new InvalidOperationException("Nome do banco de dados nao configurado.");

        if (string.IsNullOrWhiteSpace(Username))
            throw new InvalidOperationException("Usuario do banco de dados nao configurado.");

        if (string.IsNullOrWhiteSpace(Password))
            throw new InvalidOperationException("Senha padrao do banco de dados nao configurada. Configure ALMOXARIFADO_DB_PASSWORD ou a chave Password.");

        ConnectionString = $"Host={Host};Database={Database};Username={Username};Password={Password}";
    }

    public string ResolveModeloPath(string fileName)
    {
        var paths = GetModeloSearchPaths(fileName).ToArray();
        var path = paths.FirstOrDefault(File.Exists);

        if (path is not null)
            return path;

        throw new FileNotFoundException(
            $"Modelo '{fileName}' nao encontrado. Caminhos verificados: {string.Join("; ", paths)}",
            fileName);
    }

    public string ResolveImpressosPath(string fileName)
    {
        return Path.Combine(ResolveImpressosDirectory(), fileName);
    }

    public string ResolveImpressosDirectory()
    {
        var configuredPath = GetConfiguredSystemPath();
        var directory = Directory.Exists(configuredPath)
            ? Path.Combine(configuredPath, "Impressos")
            : Path.Combine(AppContext.BaseDirectory, "Impressos");

        Directory.CreateDirectory(directory);
        return directory;
    }

    private string ReadSetting(string key, string? environmentVariable, string? defaultValue)
    {
        if (!string.IsNullOrWhiteSpace(environmentVariable))
        {
            var environmentValue = Environment.GetEnvironmentVariable(environmentVariable);
            if (!string.IsNullOrWhiteSpace(environmentValue))
                return environmentValue;
        }

        var configValue = AppSetting?[key];
        return string.IsNullOrWhiteSpace(configValue) ? defaultValue ?? string.Empty : configValue;
    }

    private IEnumerable<string> GetModeloSearchPaths(string fileName)
    {
        yield return Path.Combine(GetConfiguredSystemPath(), "Modelos", fileName);
        yield return Path.Combine(AppContext.BaseDirectory, "Modelos", fileName);

        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            yield return Path.Combine(directory.FullName, "Modelos", fileName);
            yield return Path.Combine(directory.FullName, "Almoxarifado", "Almoxarifado", "Modelos", fileName);
            directory = directory.Parent;
        }
    }

    private string GetConfiguredSystemPath()
    {
        return string.IsNullOrWhiteSpace(CaminhoSistema)
            ? AppContext.BaseDirectory
            : CaminhoSistema;
    }
}
