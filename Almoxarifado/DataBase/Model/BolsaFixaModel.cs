using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Almoxarifado.DataBase.Model;

[Table("tblbolsafixa", Schema = "almoxarifado_apoio")]
public class BolsaFixaModel
{
    [Key]
    public long? cod_bolsafixa { get; set; }
    public long? codigo_tipobolsa { get; set; }
    public long? codcompladicional { get; set; }
    public double? quantidade { get; set; }
    public string? observacao { get; set; }
}
