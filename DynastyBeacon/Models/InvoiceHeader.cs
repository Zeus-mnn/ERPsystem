namespace DynastyBeacon.Models
{
    public class InvoiceHeader
    {
        public Guid InvoiceID { get; set; }
        public int ID { get; set; }
        public string InvoiceNo { get; set; }
        public Guid DebtorID { get; set; }
        public DateTime InvoiceDate { get; set; }
        public decimal TotalSellAmountExclVAT { get; set; }
        public decimal VAT { get; set; }
        public decimal TotalCost { get; set; }
        public DateTime CreatedOn { get; set; }
        public DateTime? UpdatedOn { get; set; }

        // Navigation properties
        public Debtor Debtor { get; set; }
        public ICollection<InvoiceDetail> InvoiceDetails { get; set; }
    }
}
