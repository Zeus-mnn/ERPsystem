using System.ComponentModel.DataAnnotations;

namespace DynastyBeacon.Models
{
    public class InvoiceInputModel
    {
        [Required]
        public Guid DebtorID { get; set; }

        [Required]
        public DateTime InvoiceDate { get; set; }

        [Required]
        [MinLength(1, ErrorMessage = "At least one line item is required")]
        public List<InvoiceLineItemInput> LineItems { get; set; } = new();
    }

    public class InvoiceLineItemInput
    {
        [Required]
        public Guid StockID { get; set; }

        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "Quantity must be greater than 0")]
        public int Quantity { get; set; }

        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Unit price must be greater than 0")]
        public decimal UnitPrice { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Discount cannot be negative")]
        public decimal Discount { get; set; }
    }
}