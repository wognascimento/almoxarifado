using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;

namespace Almoxarifado.DataBase.Model
{
    [Keyless]
    [Table("saldo_estoque_final", Schema = "almoxarifado_jac")]
    public class SaldoEstoqueModel
    {
        public long? codcompladicional { get; set; }
        public string? planilha  { get; set; }
        public string? descricao_completa  { get; set; }
        public string? unidade  { get; set; }
        public double? estoque_min  { get; set; }
        public double? custo  { get; set; }
        public double? estoque_inicial  { get; set; }
        public double? qtde_entrada  { get; set; }
        public double? qtde_saida  { get; set; }
        public double? saldo_estoque  { get; set; }
        public string? status  { get; set; }
        public double? ultimo_valor_compra  { get; set; }
        public string? inventariado  { get; set; }
        public double? ponto_de_pedido  { get; set; }
        public string? inativo { get; set; }
    }
}
