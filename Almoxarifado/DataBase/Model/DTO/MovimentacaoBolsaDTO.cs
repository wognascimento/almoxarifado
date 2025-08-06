namespace Almoxarifado.DataBase.Model.DTO;

public class MovimentacaoBolsaDTO
{
    public string nome_apelido { get; set; }
    public string setor { get; set; }
    public int cod_controlelinha { get; set; }
    public int codigo_bolsa { get; set; }
    public int codcompladicional { get; set; }
    public string planilha { get; set; }
    public string descricao_completa { get; set; }
    public string unidade { get; set; }
    public decimal quantidade_saida { get; set; }
    public decimal quantidade_retorno { get; set; }
    public decimal valor_unitario { get; set; }
    public decimal saldo_devedor { get; set; }
}
