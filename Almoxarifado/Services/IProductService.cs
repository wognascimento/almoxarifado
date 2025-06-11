using Almoxarifado.DataBase.Model;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Almoxarifado.Services;

public interface IProductService
{
    Task<List<QryDescricaoModel>> GetProductsAsync();
    Task<QryDescricaoModel> GetProductByCodeAsync(long code);
    Task<List<QryDescricaoModel>> SearchProductsAsync(string searchTerm);
}
