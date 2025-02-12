using DynastyBeacon.Models;
using Microsoft.EntityFrameworkCore;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Debtor> Debtors { get; set; }
    public DbSet<Stock> Stocks { get; set; }
    public DbSet<DebtorTransaction> DebtorTransactions { get; set; }
    public DbSet<StockTransaction> StockTransactions { get; set; }
    public DbSet<InvoiceHeader> InvoiceHeaders { get; set; }
    public DbSet<InvoiceDetail> InvoiceDetails { get; set; }



    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure Debtor
        modelBuilder.Entity<Debtor>(entity =>
        {
            entity.ToTable("DebtorsMaster");

            entity.HasKey(e => e.DebtorID);

            // Configured ID as database-generated
            entity.Property(e => e.ID)
                .UseIdentityColumn()
                .IsRequired();

            // Configured AccountCode as computed column
            entity.Property(e => e.AccountCode)
                .HasComputedColumnSql("'ACC-' + RIGHT('00000' + CAST(ID AS NVARCHAR(10)), 5)", stored: true)
                .IsRequired();

            entity.Property(e => e.Name)
                .IsRequired()
                .HasMaxLength(255);

            entity.Property(e => e.Address).IsRequired().HasMaxLength(255);
            entity.Property(e => e.AlternativeAddress).HasMaxLength(255);
            entity.Property(e => e.Phone).IsRequired().HasMaxLength(20);
            entity.Property(e => e.Email).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Balance).HasPrecision(18, 2);
            entity.Property(e => e.SalesYearToDate).HasPrecision(18, 2);
            entity.Property(e => e.CostYearToDate).HasPrecision(18, 2);
            entity.HasIndex(e => e.Email).IsUnique();
            entity.HasIndex(e => e.Name);
            entity.HasIndex(e => e.AccountCode);
        });

        // Configure Stock
        modelBuilder.Entity<Stock>(entity =>
        {
            entity.ToTable("StockMaster");

            // Primary key
            entity.HasKey(e => e.StockID);

            // Configured ID as database-generated
            entity.Property(e => e.ID)
                .UseIdentityColumn()
                .IsRequired();

            // Marked StockCode as computed and stored
            entity.Property(e => e.StockCode)
                .HasComputedColumnSql("'STK-' + RIGHT('00000' + CAST(ID AS NVARCHAR(10)), 5)", stored: true)
                .IsRequired();

            entity.Property(e => e.StockDescription)
                .IsRequired()
                .HasMaxLength(255);

            entity.Property(e => e.Cost)
                .HasPrecision(18, 2)
                .IsRequired();

            entity.Property(e => e.SellingPrice)
                .HasPrecision(18, 2)
                .IsRequired();

            entity.Property(e => e.Category)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(e => e.TotalPurchasesExclVat)
                .HasPrecision(18, 2)
                .IsRequired();

            entity.Property(e => e.TotalSalesExclVat)
                .HasPrecision(18, 2)
                .IsRequired();

            entity.Property(e => e.QtyPurchased)
                .IsRequired();

            entity.Property(e => e.QtySold)
                .IsRequired();

            entity.Property(e => e.StockOnHand)
                .IsRequired();

            entity.Property(e => e.CreatedBy)
                .IsRequired()
                .HasMaxLength(50);

            entity.Property(e => e.CreatedOn)
                .IsRequired();

            entity.Property(e => e.UpdatedBy)
                .HasMaxLength(50);

            // Indexes for better performance
            entity.HasIndex(e => e.StockCode).IsUnique();
            entity.HasIndex(e => e.StockDescription);
            entity.HasIndex(e => e.Category);
        });

        // Configure DebtorTransaction
        modelBuilder.Entity<DebtorTransaction>(entity =>
        {
            entity.ToTable("DebtorsTransactionFile");
            entity.HasKey(e => e.TransactionID);
            entity.Property(e => e.DocumentNo).IsRequired().HasMaxLength(450);
            entity.Property(e => e.GrossTransactionValue).HasPrecision(18, 2);
            entity.Property(e => e.VatValue).HasPrecision(18, 2);
            entity.HasIndex(e => e.TransactionDate);
            entity.HasIndex(e => e.TransactionType);
            entity.HasIndex(e => e.DebtorID);

            entity.HasOne(d => d.Debtor)
                .WithMany(p => p.DebtorTransactions)
                .HasForeignKey(d => d.DebtorID)
                .OnDelete(DeleteBehavior.Cascade);

            entity.Property(e => e.ID)
       .UseIdentityColumn()
       .IsRequired();

            entity.Property(e => e.TransactionCode)
                .HasComputedColumnSql("'TRX-' + RIGHT('00000' + CAST(ID AS NVARCHAR(10)), 5)", stored: true)
                .IsRequired();
        });

        // Configure StockTransaction
        modelBuilder.Entity<StockTransaction>(entity =>
        {
            entity.ToTable("StockTransactionFile");

            // Configure TransactionID as the primary key
            entity.HasKey(e => e.TransactionID);



            entity.Property(e => e.TransactionID)
                .HasDefaultValueSql("NEWID()");

            entity.Property(e => e.DocumentNo).IsRequired().HasMaxLength(450);
            entity.Property(e => e.UnitCost).HasPrecision(18, 2);
            entity.Property(e => e.UnitSell).HasPrecision(18, 2);

            entity.HasIndex(e => e.TransactionDate);
            entity.HasIndex(e => e.StockID);

            entity.HasOne(d => d.Stock)
                .WithMany(p => p.StockTransactions)
                .HasForeignKey(d => d.StockID)
                .OnDelete(DeleteBehavior.Cascade);

            entity.Property(e => e.ID)
       .UseIdentityColumn()
       .IsRequired();

            entity.Property(e => e.TransactionCode)
                .HasComputedColumnSql("'STX-' + RIGHT('00000' + CAST(ID AS NVARCHAR(10)), 5)", stored: true)
                .IsRequired();
        });

        // Configure InvoiceHeader
        modelBuilder.Entity<InvoiceHeader>(entity =>
        {
            entity.ToTable("InvoiceHeader");

            // Primary key
            entity.HasKey(e => e.InvoiceID);

            // Configure ID as identity column
            entity.Property(e => e.ID)
                .UseIdentityColumn()
                .IsRequired();

            // Configure InvoiceNo as computed column
            entity.Property(e => e.InvoiceNo)
                .HasComputedColumnSql("'INV-' + RIGHT('00000' + CAST(ID AS NVARCHAR(10)), 5)", stored: true)
                .IsRequired();

            // Configure decimal precision
            entity.Property(e => e.TotalSellAmountExclVAT)
                .HasPrecision(18, 2)
                .IsRequired();

            entity.Property(e => e.VAT)
                .HasPrecision(18, 2)
                .IsRequired();

            entity.Property(e => e.TotalCost)
                .HasPrecision(18, 2)
                .IsRequired();

            // Configure dates
            entity.Property(e => e.CreatedOn)
                .HasDefaultValueSql("SYSDATETIME()")
                .IsRequired();

            entity.Property(e => e.UpdatedOn);

            // Relationships
            entity.HasOne(d => d.Debtor)
                .WithMany(p => p.Invoices)
                .HasForeignKey(d => d.DebtorID)
                .OnDelete(DeleteBehavior.Cascade);

            // Indexes
            entity.HasIndex(e => e.InvoiceDate);
            entity.HasIndex(e => e.DebtorID);
            entity.HasIndex(e => e.InvoiceNo).IsUnique();
        });

        // Configure InvoiceDetail
        modelBuilder.Entity<InvoiceDetail>(entity =>
        {
            entity.ToTable("InvoiceDetail");

            // Primary key
            entity.HasKey(e => e.InvoiceDetailID);

            // Configure decimal precision
            entity.Property(e => e.UnitCost)
                .HasPrecision(18, 2)
                .IsRequired();

            entity.Property(e => e.UnitSell)
                .HasPrecision(18, 2)
                .IsRequired();

            entity.Property(e => e.Disc)
                .HasPrecision(18, 2)
                .IsRequired();

            entity.Property(e => e.Total)
                .HasPrecision(18, 2)
                .IsRequired();

            // Configure dates
            entity.Property(e => e.CreatedOn)
                .HasDefaultValueSql("SYSDATETIME()")
                .IsRequired();

            entity.Property(e => e.UpdatedOn);

            // Configure quantity validation
            entity.Property(e => e.QtySold)
                .IsRequired();

            // Relationships
            entity.HasOne(d => d.InvoiceHeader)
                .WithMany(p => p.InvoiceDetails)
                .HasForeignKey(d => d.InvoiceID)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(d => d.Stock)
                .WithMany(p => p.InvoiceDetails)
                .HasForeignKey(d => d.StockID)
                .OnDelete(DeleteBehavior.Cascade);

            // Indexes
            entity.HasIndex(e => e.InvoiceID);
            entity.HasIndex(e => e.StockID);
        });
    }
}