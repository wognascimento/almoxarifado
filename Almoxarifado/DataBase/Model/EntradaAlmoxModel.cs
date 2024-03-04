using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Almoxarifado.DataBase.Model
{
    [Table("t_entrada_almox", Schema = "almoxarifado_jac")]
    public class EntradaAlmoxModel
    {
        public long? cod_movimentacao { get; set; }
        [Key]
        public long? cod_entrada_almox { get; set; }
        public string? barcode { get; set; }
        public double? qtde { get; set; }
        //public string? hora { get; set; }
        public DateTime? data { get; set; }
        public string? resp { get; set; }
        public long? requisicao { get; set; }
        public string? origem { get; set; }
        public string? incluido_por { get; set; }
        public DateTime? incluido_data { get; set; }
        public string? estado { get; set; }
        public string? funcionario { get; set; }
        public string? unidade { get; set; }
        public string? nf { get; set; }
        public string? obs { get; set; }
        public string? expurgo { get; set; }
        public long? num_os { get; set; }
        public string? endereco { get; set; }
        public long? id_det_solicitacao_compras { get; set; }
        public long? codcompladicional { get; set; }
        public long? codfun { get; set; }
    }
}
