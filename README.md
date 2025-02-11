# Dynasty Beacon Business Management System

## Overview
Dynasty Beacon is a comprehensive business management system designed for small companies in South Africa, focusing on debtor management, stock control, and invoicing. The system provides essential functionality for managing customer accounts, inventory, and sales transactions while maintaining detailed audit trails and transaction history.

## Core Features

### 1. Debtors Management
- Create and maintain debtor master records
- Track debtor balances and year-to-date sales
- View detailed transaction history
- Filter transactions by value and date
- Real-time balance updates
- Advanced search functionality

### 2. Stock Management
- Maintain stock master records
- Track stock levels and transactions
- Process stock adjustments and receipts
- View stock movement history
- Filter by recent sales and quantities
- Low stock alerts

### 3. Invoicing System
- Create and process sales invoices
- Dynamic debtor selection
- Real-time stock availability checking
- Automatic calculations for totals
- VAT handling (15%)
- Multi-line item support
- Automatic updates to stock and debtor balances

## Technical Specifications

### Database Structure

#### Debtors Master
```sql
CREATE TABLE DebtorsMaster (
    DebtorID UNIQUEIDENTIFIER DEFAULT NEWID() PRIMARY KEY,
    AccountCode VARCHAR(20) NOT NULL UNIQUE,
    Address1 VARCHAR(100),
    Address2 VARCHAR(100),
    Address3 VARCHAR(100),
    Balance DECIMAL(18,2) DEFAULT 0,
    SalesYearToDate DECIMAL(18,2) DEFAULT 0
)
```

#### Stock Master
```sql
CREATE TABLE StockMaster (
    StockID UNIQUEIDENTIFIER DEFAULT NEWID() PRIMARY KEY,
    StockCode VARCHAR(20) NOT NULL UNIQUE,
    Description VARCHAR(100),
    UnitPrice DECIMAL(18,2),
    QuantityOnHand INT DEFAULT 0
)
```

#### Transaction Files
```sql
CREATE TABLE TransactionFile (
    TransactionID UNIQUEIDENTIFIER DEFAULT NEWID() PRIMARY KEY,
    InvoiceNumber VARCHAR(20),
    DebtorID UNIQUEIDENTIFIER,
    StockID UNIQUEIDENTIFIER,
    Quantity INT,
    UnitPrice DECIMAL(18,2),
    TransactionDate DATETIME DEFAULT GETDATE(),
    FOREIGN KEY (DebtorID) REFERENCES DebtorsMaster(DebtorID),
    FOREIGN KEY (StockID) REFERENCES StockMaster(StockID)
)
```

### Key Components

#### 1. Lookup Functionality
- Dynamic search capabilities
- Partial code/name matching
- Real-time filtering
- Custom SQL implementation (no data-aware controls)

#### 2. Transaction Processing
- Atomic operations
- Rollback support
- Audit trail maintenance
- Balance updates

#### 3. User Interface
- Clean, intuitive design
- Responsive layouts
- Clear error messaging
- Consistent navigation

## Implementation Guidelines

### Code Standards
- Clear and consistent variable naming
- Comprehensive code comments
- Structured function organization
- Modular design patterns
- Error handling implementation

### Business Rules
1. Stock Management
   - No negative stock levels
   - Automatic low stock alerts
   - Required stock adjustment reasons

2. Debtor Controls
   - Unique account codes
   - Balance validation
   - Transaction history tracking

3. Invoicing
   - Required debtor selection
   - Stock availability checking
   - Automatic total calculations
   - VAT handling

## Setup Instructions

### Prerequisites
- Visual Studio 2022 or later
- SQL Server 2019+
- .NET 8.0 SDK
- Windows OS

### Installation Steps
1. Clone repository
2. Update database connection string
3. Run migration scripts
4. Build and run application

### Configuration
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=.;Database=DynastyBeacon;Trusted_Connection=True"
  },
  "AppSettings": {
    "VATRate": 15,
    "LowStockThreshold": 5
  }
}
```

## Usage Guide

### Creating a New Debtor
1. Navigate to Debtors Master
2. Click "New Debtor"
3. Enter required information
4. Save record

### Processing Stock Adjustments
1. Access Stock Adjustments screen
2. Select stock item
3. Enter quantity and reason
4. Process adjustment

### Creating an Invoice
1. Open Invoicing screen
2. Select debtor
3. Add stock items
4. Review totals
5. Process invoice

## Support and Maintenance

### Troubleshooting
1. Check connection strings
2. Verify user permissions
3. Review error logs
4. Validate data integrity


## Futhure Enhancements
- Export functionality
- Advanced reporting
- Bulk operations
- Mobile interface
- API integration

## License
Proprietary software. All rights reserved.
Copyright © 2025 Dynasty Beacon

## Contact
For support:
- Email: support@dynastybeacon.com
- Phone: +27-672-8900-396
- Hours: 24/7
