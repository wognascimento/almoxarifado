using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Almoxarifado.DataBase.Model
{
    [Table("tabela_desc_adicional", Schema = "producao")]
    public class TabelaDescAdicionalModel
    {
        [Key]
        public long? coduniadicional { get; set; }
        public long? codigoproduto { get; set; }
        public string? descricao_adicional { get; set; }
        public string? cadastradopor { get; set; }
        public DateTime? cadastradoem { get; set; }
        public string? alteradopor { get; set; }
        public DateTime? alteradoem { get; set; }
        public string? revisao { get; set; }
        public string? obsproducaoobrigatoria { get; set; }
        public string? obsmontagem { get; set; }
        public string? unidade { get; set; }
        public string? inativo { get; set; }
    }
}
