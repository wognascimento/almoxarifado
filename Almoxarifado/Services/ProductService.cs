using Almoxarifado.DataBase;
using Almoxarifado.DataBase.Model;
using Dapper;
using Npgsql;

namespace Almoxarifado.Services;

public class ProductService : IProductService
{

    public async Task<List<QryDescricaoModel>> GetProductsAsync()
    {
        // Simular delay de rede
        //await Task.Delay(500);

        // Retornar dados simulados (substitua pela sua implementação real)
        return await GenerateSampleProducts();
    }

    public async Task<QryDescricaoModel> GetProductByCodeAsync(long code)
    {
        var products = await GetProductsAsync();
        return products.FirstOrDefault(p => p.codcompladicional == code);
    }

    public async Task<List<QryDescricaoModel>> SearchProductsAsync(string searchTerm)
    {
        var products = await GetProductsAsync();

        if (string.IsNullOrEmpty(searchTerm))
            return products;

        var term = searchTerm.ToLower();
        return [.. products.Where(p =>
            p.descricao_completa.Contains(term, StringComparison.CurrentCultureIgnoreCase)
        )];
    }

    private async Task<List<QryDescricaoModel>> GenerateSampleProducts()
    {
        try
        {
            await using var conn = new NpgsqlConnection(DataBaseSettings.Instance.ConnectionString);
            var data = await conn.QueryAsync<QryDescricaoModel>(@"
                SELECT *
                FROM producao.qry3descricoes
                WHERE planilha IS NOT NULL AND planilha LIKE 'ALMOX%'
                ORDER BY planilha;");

            return data.ToList();
        }
        catch (PostgresException pgEx)
        {
            throw new Exception($"Erro do banco: {pgEx.MessageText}\nLocal: {pgEx.Where}");
        }
        catch (Exception ex)
        {
            throw new Exception($"Erro inesperado: {ex.Message}");
        }
    }
}
