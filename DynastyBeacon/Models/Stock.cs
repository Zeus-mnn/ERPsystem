using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace DynastyBeacon.Models
{
    public class Stock
    {
        public Guid StockID { get; set; }
        public int ID { get; set; }
        public string StockCode { get; set; }
        public string StockDescription { get; set; }
        public decimal Cost { get; set; }
        public decimal SellingPrice { get; set; }
        public string Category { get; set; }
        public decimal TotalPurchasesExclVat { get; set; }
        public decimal TotalSalesExclVat { get; set; }
        public int QtyPurchased { get; set; }
        public int QtySold { get; set; }
        public int StockOnHand { get; set; }
        public string CreatedBy { get; set; }
        public DateTime CreatedOn { get; set; }
        public string? UpdatedBy { get; set; }
        public DateTime? UpdatedOn { get; set; }

        // Navigation properties
        public ICollection<StockTransaction> StockTransactions { get; set; }
        public ICollection<InvoiceDetail> InvoiceDetails { get; set; }
    }


   

}
