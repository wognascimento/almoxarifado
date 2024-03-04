using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;

namespace Almoxarifado.DataBase.Model
{
    [Keyless]
    [Table("sig_saldo_funcionario_total", Schema = "almoxarifado_jac")]
    public class SaldoFuncionarioModel
    {
        public long? codcompladicional { get; set; }
        public long? codfun { get; set; }
        public double? saidas { get; set; }
        public double? entradas { get; set; }
        public double? saldo { get; set; }
    }
}
