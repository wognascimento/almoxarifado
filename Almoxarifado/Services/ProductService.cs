using Almoxarifado.DataBase;
using Almoxarifado.DataBase.Model;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using System.Collections.ObjectModel;

namespace Almoxarifado.Services;

public class ProductService : IProductService
{

    private readonly DatabaseContext _dbContext;

    public ProductService()
    {
        _dbContext = new DatabaseContext();
    }

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
            var data = await _dbContext
                .Descricoes
                .Where(p => p.planilha != null && p.planilha.StartsWith("ALMOX")) // Verifica se 'planilha' não é nulo antes de chamar StartsWith
                .OrderBy(c => c.planilha)
                .ToListAsync();

            return data;
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException pgEx)
        {
            throw new Exception($"Erro do banco: {pgEx.MessageText}\nLocal: {pgEx.Where}");
        }
        catch (Exception ex)
        {
            throw new Exception($"Erro inesperado: {ex.Message}");
        }
    }
}
