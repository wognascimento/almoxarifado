using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Almoxarifado.DataBase.Model;

[Table("tbl_atendentes_almox", Schema = "almoxarifado_jac")]
public class AtendenteAlmoxModel
{
    [Key]
    public string? nome_funcionario { get; set; }
    public string? funcao { get; set; }
    public string? username { get; set; }
}
