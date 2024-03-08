using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;

namespace Almoxarifado.DataBase.Model
{
    [Keyless]
    [Table("qry_analise_saldo_estoque_ponto_pedido", Schema = "almoxarifado_jac")]
    public class AnalisePontoPedidoModel
    {
        public long? codcompladicional { get; set; }
        public string? planilha  { get; set; }
        //public string? descricao  { get; set; }
        //public string? descricao_adicional  { get; set; }
        //public string? complementoadicional  { get; set; }
        public string? descricao_completa  { get; set; }
        public string? unidade  { get; set; }
        public double? custo  { get; set; }
        //public double? saldos_atuais_almox_apoio  { get; set; }
        //public double? saldos_atuais_almox_450  { get; set; }
        //public double? saldos_atuais_almox_jac  { get; set; }
        public double? saldos_atuais_total  { get; set; }
        public double? saidas_atuais  { get; set; }
        public double? est_min_15dd  { get; set; }
        public double? jan_01 { get; set; }
        public double? fer_02 { get; set; }
        public double? mar_03 { get; set; }
        public double? abr_04 { get; set; }
        public double? mai_05 { get; set; }
        public double? jun_06 { get; set; }
        public double? jul_07 { get; set; }
        public double? ago_08 { get; set; }
        public double? set_09 { get; set; }
        public double? out_10 { get; set; }
        public double? nov_11 { get; set; }
        public double? dez_12 { get; set; }
    }
}
