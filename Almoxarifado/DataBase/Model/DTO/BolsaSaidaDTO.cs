namespace Almoxarifado.DataBase.Model.DTO;

public class BolsaSaidaDTO
{
    public long? cod_controlelinha { get; set; }
    public long? codigo_tipobolsa { get; set; }
    public long? codfun { get; set; }
    public long? codcompladicional { get; set; }
    public string? planilha { get; set; }
    public string? descricao { get; set; }
    public string? descricao_adicional { get; set; }
    public string? complementoadicional { get; set; }
    public string? descricao_completa { get; set; }
    public string? unidade { get; set; }
    public double? quantidade { get; set; }
    public double? valor_unitario { get; set; }
    public double? valor_total { get; set; }
}
