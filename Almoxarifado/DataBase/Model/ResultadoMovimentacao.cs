namespace Almoxarifado.DataBase.Model
{
    public class ResultadoMovimentacao
    {
        public long? cod_saida_almox { get; set; }
        public string? planilha { get; set; }
        public string? descricao_completa { get; set; }
        public string? unidade { get; set; }
        public double? qtde { get; set; }
    }
}
