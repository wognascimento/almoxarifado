using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Almoxarifado.DataBase.Model
{
    [Table("t_saida_almox", Schema = "almoxarifado_jac")]
    public class SaidaAlmoxModel
    {
        public long? cod_movimentacao { get; set; }
        [Key]
        public long? cod_saida_almox { get; set; } 
        public string? barcode { get; set; } 
        public double? qtde { get; set; } 
        public DateTime? data { get; set; } 
        //public DateTime? hora { get; set; } 
        public string? resp { get; set; } 
        public long? requisicao { get; set; } 
        public string? destino { get; set; } 
        public string? incluido_por { get; set; } 
        public DateTime? data_inclusao { get; set; } 
        public string? funcionario { get; set; } 
        public string? setor { get; set; } 
        public string? unidade { get; set; } 
        public string? expurgo { get; set; } 
        public string? obs { get; set; } 
        public long? num_os { get; set; } 
        public string? endereco { get; set; }
        public long? codcompladicional { get; set; }
    }
}
