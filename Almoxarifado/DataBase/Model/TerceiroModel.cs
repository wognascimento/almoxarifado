using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Almoxarifado.DataBase.Model
{
    [Table("tbl_terceiro", Schema = "almoxarifado_jac")]
    public class TerceiroModel
    {
       
        public string? nome { get; set; }
        public string? apelido { get; set; }
        public string? razao_social { get; set; }
        public string? rg { get; set; }
        public string? municipio { get; set; }
        public string? tipo_prestacao_servico { get; set; }
        [Key]
        public long? codfun { get; set; }
    }
}

