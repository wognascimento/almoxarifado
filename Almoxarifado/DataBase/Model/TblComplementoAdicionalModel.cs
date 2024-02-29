using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Almoxarifado.DataBase.Model
{
    [Table("tblcomplementoadicional", Schema = "producao")]
    public class TblComplementoAdicionalModel
    {
        [Key]
        public long? codcompladicional { get; set; }
        public string? complementoadicional { get; set; }
        public string? status { get; set; }
        public double? estoque_inicial { get; set; }
        public string? desc_process { get; set; }
        public double? estoque_inicial_processado { get; set; }
        public double? altura { get; set; }
        public double? largura { get; set; }
        public double? profundidade { get; set; }
        public int? vida_util { get; set; }
        public double? diametro { get; set; }
        public double? peso { get; set; }
        public string? unidade { get; set; }
        public string? cadastradopor { get; set; }
        public DateTime? cadastradoem { get; set; }
        public string? alterado_por { get; set; }
        public DateTime? alterado_em { get; set; }
        public int? custo_real { get; set; }
        public string? prodcontrolado { get; set; }
        public double? volume { get; set; }
        public double? area { get; set; }
        public double? precolocacao { get; set; }
        public string? descricaofiscal { get; set; }
        public string? descricaoespanhol { get; set; }
        public double? estoque_min { get; set; }
        public double? v_unit { get; set; }
        public double? v_unit_dolar { get; set; }
        public string? ncm { get; set; }
        public string? tipo { get; set; }
        public double? custoestimado { get; set; }
        public double? indicecorrecao { get; set; }
        public string? nf { get; set; }
        public double? pesobruto { get; set; }
        public long? coduniadicional { get; set; }
        public string? codfornecedor { get; set; }
        public string? foralinhafornecedor { get; set; }
        public string? origemcusto { get; set; }
        public DateTime? datafichatecnica { get; set; }
        public string? respfichatenica { get; set; }
        public DateTime? datainiciofichatecnica { get; set; }
        public string? respcusto { get; set; }
        public DateTime? datacusto { get; set; }
        public string? contabil { get; set; }
        public string? produto_novo { get; set; }
        public string? acompanhamento { get; set; }
        public string? responsavel_acompanha { get; set; }
        public DateTime? concluido_acompanha { get; set; }
        public string? obs_acompanhamento { get; set; }
        public string? importado { get; set; }
        public string? contabil_pldc { get; set; }
        public string? narrativa { get; set; }
        public string? alx { get; set; }
        public string? inativo { get; set; }
        public int? qtd_etiqueta { get; set; }
        public string? fracao { get; set; }
        public string? dividir_qtd_volume { get; set; }
        public string? conta_aplica_contabil { get; set; }
        public string? centro_custo_contabil { get; set; }
        public string? especial { get; set; }
        public string? foto { get; set; }
        public double? custodescadicional_custo { get; set; }
        public int? custodescadicional_codcompladicional { get; set; }
        public string? tamanho_construcao { get; set; }
        public string? diverso { get; set; }
        public string? dificuldade { get; set; }
        public double? saldo_patrimonial_ano_anterior { get; set; }
        public double? saldo_disponivel_ano_anterior { get; set; }
        public string? custo_despesa { get; set; }
        public string? link_foto { get; set; }
        public double? preco_shopping { get; set; }
        public string? exportado_folhamatic { get; set; }
        public double? saldo_estoque { get; set; }
    }
}
