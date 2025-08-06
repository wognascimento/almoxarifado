using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Almoxarifado.DataBase.Model;

[Table("tbl_cadeados", Schema = "almoxarifado_apoio")]
public class AlmoxarifadoApoioCadeadoModel
{
    [Key]
    public long? cod_linha_cadeado { get; set; }
    [Required]
    public string? nome_bolsa { get; set; }
    [Required]
    public int? cadeado { get; set; }
    [Required]
    public string? senha { get; set; }
    [Required]
    public string? montador { get; set; }
    [Required]
    public string? sigla { get; set; }
    public string? lacre { get; set; }
}
