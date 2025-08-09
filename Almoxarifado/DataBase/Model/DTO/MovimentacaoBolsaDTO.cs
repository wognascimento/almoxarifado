namespace Almoxarifado.DataBase.Model.DTO;

public class MovimentacaoBolsaDTO
{
    public string nome_apelido { get; set; }
    public string setor { get; set; }
    public int cod_controlelinha { get; set; }
    public string bolsa { get; set; }
    public string destino_shop { get; set; }
    public int codcompladicional { get; set; }
    public string planilha { get; set; }
    public string descricao_completa { get; set; }
    public string unidade { get; set; }
    public double quantidade_saida { get; set; }
    public double? quantidade_retorno { get; set; }
    public double valor_unitario { get; set; }
    public double saldo_devedor { get; set; }
    public string retorno_por { get; set; }
    public DateTime? retorno_em { get; set; }
}
