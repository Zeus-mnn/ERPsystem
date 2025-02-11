using System.ComponentModel.DataAnnotations.Schema;

namespace DynastyBeacon.Models
{
    public class InvoiceHeader
    {
        public Guid InvoiceID { get; set; }

        // ID is database-generated identity column
        public int ID { get; set; }

        // InvoiceNo is a computed column based on ID - DatabaseGenerated
        [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        public string InvoiceNo { get; set; }

        public Guid DebtorID { get; set; }
        public DateTime InvoiceDate { get; set; }
        public decimal TotalSellAmountExclVAT { get; set; }
        public decimal VAT { get; set; }
        public decimal TotalCost { get; set; }

        // CreatedOn should have a default value
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public DateTime CreatedOn { get; set; }

        public DateTime? UpdatedOn { get; set; }

        // Navigation properties
        public virtual Debtor Debtor { get; set; }
        public virtual ICollection<InvoiceDetail> InvoiceDetails { get; set; }
    }
}