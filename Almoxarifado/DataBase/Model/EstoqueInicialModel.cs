using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Almoxarifado.DataBase.Model
{
    [Table("t_estoqueinicial", Schema = "almoxarifado_jac")]
    public class EstoqueInicialModel
    {
        [Key]
        public double? codcompladicional { get; set; }
        public double? estoque_inicial { get; set; }
        public double? estoque_inicial_processado { get; set; }
    }
}
