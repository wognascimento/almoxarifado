using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Almoxarifado.DataBase.Model
{
    [Table("relplan", Schema = "producao")]
    public class RelplanModel
    {
        [Key]
        public string? planilha { get; set; }
        public string? ativo { get; set; }
        public string? baia { get; set; }
        public string? local_de_armazenamento { get; set; }
        public string? seguranca { get; set; }
        public string? process { get; set; }
        public string? funcionalidade { get; set; }
        public string? familia_produto { get; set; }
        public string? diretor_estoque { get; set; }
        public string? coordenador { get; set; }
        public string? suporte_outra_unidade { get; set; }
        public string? encarregado { get; set; }
        public string? lider_setor { get; set; }
        public string? producao { get; set; }
        public string? estoque { get; set; }
        public string? retorno { get; set; }
        public string? est { get; set; }
        public string? origem { get; set; }
        public string? ficha_tecnica { get; set; }
        public string? backup { get; set; }
        public string? lead_time { get; set; }
        public string? cce_sob_encomenda { get; set; }
        public string? complexidade { get; set; }
        public long? id { get; set; }
        public string? tipo_saldo { get; set; }
        public string? tipo_custo { get; set; }
        public string? GI { get; set; }
        public string? resp_compras { get; set; }
        public string? agrupamento { get; set; }
    }
}
