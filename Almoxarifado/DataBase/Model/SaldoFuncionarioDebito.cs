using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;

namespace Almoxarifado.DataBase.Model
{
    [Keyless]
    [Table("sig_saldo_funcionario_debito", Schema = "almoxarifado_jac")]
    public class SaldoFuncionarioDebito
    {
        public long? codcompladicional { get; set; }
        public string? planilha  { get; set; }
        public string? descricao_completa  { get; set; }
        public string? nome_apelido  { get; set; }
        public string? setor  { get; set; }
        public double? saidas  { get; set; }
        public double? entradas  { get; set; }
        public double? saldo { get; set; }
    }
}
