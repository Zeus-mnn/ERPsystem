using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;


namespace DynastyBeacon.Models
{
    public class InvoiceDetail
    {
        public Guid InvoiceDetailID { get; set; }
        public Guid InvoiceID { get; set; }
        public Guid StockID { get; set; }

        // Added check constraint in database
        [Range(1, int.MaxValue)]
        public int QtySold { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal UnitCost { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal UnitSell { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Disc { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Total { get; set; }

        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public DateTime CreatedOn { get; set; }

        public DateTime? UpdatedOn { get; set; }

        // Navigation properties  
        public virtual InvoiceHeader InvoiceHeader { get; set; }
        public virtual Stock Stock { get; set; }
    }
}