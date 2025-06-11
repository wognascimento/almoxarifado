using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Almoxarifado.DataBase.Model;

[Table("tblsaidabolsa", Schema = "almoxarifado_apoio")]
public class BolsaSaidaModel
{
    [Key]
    public long?        cod_controlelinha { get; set; }
    public long?        codigo_bolsa { get; set; }
    public long?        codfun { get; set; }
    public long?        codcompladicional { get; set; }
    public string?      destino_shop	{ get; set; }
    public double?      quantidade { get; set; }
    public double?      valor_unitario { get; set; }
    public double?      valor_total { get; set; }
    public string?      emitido_por	{ get; set; }
    public DateTime?	emitido_data	{ get; set; }
    public string?      alterado_por    { get; set; }
    public DateTime?    alterado_data	{ get; set; }
    public bool?        retorno { get; set; }
    public DateTime?    data_retorno { get; set; }
    public double?      quantidade_retorno { get; set; }
    public string?      retorno_por { get; set; }
    public DateTime?    retorno_em	{ get; set; }

}
