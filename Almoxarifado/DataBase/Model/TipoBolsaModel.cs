using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Almoxarifado.DataBase.Model;

[Table("tbltipobolsa", Schema = "almoxarifado_apoio")]
public class TipoBolsaModel
{
    [Key]
    public long? codigo_tipobolsa { get; set; }
    public string? descricao { get; set; } 
    public long? codcompladicional { get; set; } = 0;

}
