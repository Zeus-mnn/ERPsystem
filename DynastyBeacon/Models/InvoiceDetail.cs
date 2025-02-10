namespace DynastyBeacon.Models
{
    public class InvoiceDetail
    {
        public Guid InvoiceDetailID { get; set; }
        public Guid InvoiceID { get; set; }
        public Guid StockID { get; set; }
        public int QtySold { get; set; }
        public decimal UnitCost { get; set; }
        public decimal UnitSell { get; set; }
        public decimal Disc { get; set; }
        public decimal Total { get; set; }
        public DateTime CreatedOn { get; set; }
        public DateTime? UpdatedOn { get; set; }

        // Navigation properties
        public InvoiceHeader InvoiceHeader { get; set; }
        public Stock Stock { get; set; }
    }
}
