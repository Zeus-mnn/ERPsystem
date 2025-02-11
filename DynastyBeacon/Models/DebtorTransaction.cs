using System.ComponentModel.DataAnnotations.Schema;

namespace DynastyBeacon.Models
{
    public class DebtorTransaction
    {
        public Guid TransactionID { get; set; }
        public int ID { get; set; }

        [DatabaseGenerated(DatabaseGeneratedOption.Computed)] 
        public string TransactionCode { get; set; }
        public Guid DebtorID { get; set; }
        public DateTime TransactionDate { get; set; }
        public string TransactionType { get; set; }
        public string DocumentNo { get; set; }
        public decimal GrossTransactionValue { get; set; }
        public decimal VatValue { get; set; }
        public DateTime CreatedOn { get; set; }
        public DateTime? UpdatedOn { get; set; }

        // Navigation property
        public Debtor Debtor { get; set; }
    }
}
