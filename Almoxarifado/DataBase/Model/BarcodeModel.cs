using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Almoxarifado.DataBase.Model
{
    [Table("tbl_barcodes", Schema = "producao")]
    public class BarcodeModel
    {
        [Key]
        public long? codigo { get; set; }
        public string? barcode { get; set; }
        public string? impresso { get; set; }
        public string? compra { get; set; }
    }
}
