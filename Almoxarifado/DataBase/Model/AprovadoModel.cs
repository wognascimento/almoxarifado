using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Almoxarifado.DataBase.Model;

[Table("t_aprovados", Schema = "producao")]
public class AprovadoModel
{
    [Key]
    public long? id_aprovado { get; set; }
    public string? sigla { get; set; }
    public string? sigla_serv { get; set; }
    public string? nome { get; set; }
}
