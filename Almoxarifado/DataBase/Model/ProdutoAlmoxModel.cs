using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;

namespace Almoxarifado.DataBase.Model
{
    [Keyless]
    [Table("movimento_almox", Schema = "almoxarifado_jac")]
    public class ProdutoAlmoxModel
    {
        public long? codcompladicional { get; set; }
        public string? barcode { get; set; } 
        public string? planilha { get; set; } 
        public string? descricao { get; set; } 
        public string? descricao_adicional { get; set; } 
        public string? complementoadicional { get; set; } 
        public string? unidade { get; set; } 
        public string? status { get; set; } 
        public double? saldo_estoque { get; set; } 
        public string? descricao_completa { get; set; }
    }
}
