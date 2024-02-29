using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Almoxarifado.DataBase.Model
{
    [Table("tbl_controle_almox_estoque", Schema = "pcp")]
    public class ControleAlmoxEstoqueModel
    {
        [Key]
        public long? codigo { get; set; }
        public long? cod_movimentacao { get; set; } 
        public long? cod_almox { get; set; } 
        public string? barcode { get; set; } 
        public long? codcompladicional { get; set; } 
        public double? quantidade { get; set; } 
        public DateTime? data { get; set; } 
        public TimeOnly? hora { get; set; } 
        public string? operacao { get; set; } 
        public string? processo { get; set; } 
        public long? num_os { get; set; } 
        public string? incluido_por { get; set; } 
        public DateTime? incluido_data { get; set; }
        public string? bloqueado { get; set; } 
        public string? liberado_por { get; set; } 
        public DateTime? liberado_em { get; set; }
    }
}
