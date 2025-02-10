namespace DynastyBeacon.Models
{
    public class StockTransaction
    {
        public Guid TransactionID { get; set; }
        //public int ID { get; set; }
        //public string TransactionCode { get; set; }
        public Guid StockID { get; set; }
        public DateTime TransactionDate { get; set; }
        public string TransactionType { get; set; }
        public string DocumentNo { get; set; }
        public int Qty { get; set; }
        public decimal UnitCost { get; set; }
        public decimal UnitSell { get; set; }
        public DateTime CreatedOn { get; set; }
        public DateTime? UpdatedOn { get; set; }

        // Navigation property
        public Stock Stock { get; set; }
    }
}
