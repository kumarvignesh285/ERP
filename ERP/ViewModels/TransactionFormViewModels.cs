using System.ComponentModel.DataAnnotations;

namespace ERP.ViewModels;

public class TransactionLineItemInputModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Product is required")]
    public int ProductId { get; set; }

    [Range(0.0001, double.MaxValue, ErrorMessage = "Quantity must be greater than zero")]
    public decimal Quantity { get; set; } = 1;

    [Range(0, double.MaxValue, ErrorMessage = "Rate cannot be negative")]
    public decimal UnitPrice { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "Discount cannot be negative")]
    public decimal Discount { get; set; }

    [Range(0, 100, ErrorMessage = "GST rate must be between 0 and 100")]
    public decimal GSTPercentage { get; set; }

    public decimal GSTAmount { get; set; }
    public decimal TotalAmount { get; set; }
}

public class SalesDocumentInputModel
{
    public int Id { get; set; }

    [Display(Name = "Document Number")]
    public string DocumentNumber { get; set; } = string.Empty;

    [Required(ErrorMessage = "Date is required")]
    [Display(Name = "Date")]
    public DateTime DocumentDate { get; set; } = DateTime.Today;

    [Required(ErrorMessage = "Customer is required")]
    [Display(Name = "Customer")]
    public int CustomerId { get; set; }

    [Display(Name = "Warehouse")]
    public int? WarehouseId { get; set; }

    [MaxLength(50)]
    public string Status { get; set; } = "Pending";

    [MaxLength(50)]
    public string? PaymentMode { get; set; }

    public decimal SubTotal { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal RoundOff { get; set; }
    public decimal GrandTotal { get; set; }

    [MaxLength(500)]
    public string? Remarks { get; set; }

    public List<TransactionLineItemInputModel> Items { get; set; } = new();
}

public class PurchaseDocumentInputModel
{
    public int Id { get; set; }

    [Display(Name = "Document Number")]
    public string DocumentNumber { get; set; } = string.Empty;

    [Required(ErrorMessage = "Date is required")]
    [Display(Name = "Date")]
    public DateTime DocumentDate { get; set; } = DateTime.Today;

    [Required(ErrorMessage = "Supplier is required")]
    [Display(Name = "Supplier")]
    public int SupplierId { get; set; }

    [Display(Name = "Warehouse")]
    public int? WarehouseId { get; set; }

    [MaxLength(50)]
    public string Status { get; set; } = "Pending";

    [MaxLength(50)]
    public string? SupplierInvoiceNumber { get; set; }

    public decimal SubTotal { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal RoundOff { get; set; }
    public decimal GrandTotal { get; set; }

    [MaxLength(500)]
    public string? Remarks { get; set; }

    public List<TransactionLineItemInputModel> Items { get; set; } = new();
}

public class VoucherFormViewModel
{
    public int Id { get; set; }

    [Display(Name = "Voucher Number")]
    public string VoucherNumber { get; set; } = string.Empty;

    [Required(ErrorMessage = "Date is required")]
    public DateTime VoucherDate { get; set; } = DateTime.Today;

    [Required(ErrorMessage = "Voucher type is required")]
    public string VoucherType { get; set; } = "Payment";

    [MaxLength(500)]
    public string? Narration { get; set; }

    public decimal TotalAmount { get; set; }

    public List<VoucherItemInputModel> Items { get; set; } = new();
}

public class VoucherItemInputModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Ledger is required")]
    public int LedgerId { get; set; }

    [Range(0, double.MaxValue)]
    public decimal DebitAmount { get; set; }

    [Range(0, double.MaxValue)]
    public decimal CreditAmount { get; set; }

    [MaxLength(250)]
    public string? Description { get; set; }
}

public class LeadFormViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Lead name is required"), MaxLength(100)]
    public string LeadName { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? CompanyName { get; set; }

    [MaxLength(20)]
    public string? Phone { get; set; }

    [MaxLength(100), EmailAddress]
    public string? Email { get; set; }

    [MaxLength(50)]
    public string Status { get; set; } = "New";

    [MaxLength(50)]
    public string Source { get; set; } = "Website";

    [MaxLength(500)]
    public string? Notes { get; set; }

    public decimal EstimatedValue { get; set; }
}
