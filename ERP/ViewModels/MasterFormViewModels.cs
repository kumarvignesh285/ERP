using System.ComponentModel.DataAnnotations;
using ERP.Models;

namespace ERP.ViewModels;

public class CustomerInputModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Customer code is required"), MaxLength(50)]
    [Display(Name = "Customer Code")]
    public string CustomerCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "Customer name is required"), MaxLength(200)]
    [Display(Name = "Customer Name")]
    public string CustomerName { get; set; } = string.Empty;

    [MaxLength(100)]
    [Display(Name = "Contact Person")]
    public string? ContactPerson { get; set; }

    [MaxLength(20)]
    [Phone(ErrorMessage = "Invalid phone number format")]
    [Display(Name = "Mobile Number")]
    public string? Mobile { get; set; }

    public string? MobileNumber { get => Mobile; set => Mobile = value; }

    [MaxLength(100)]
    [EmailAddress(ErrorMessage = "Invalid email address format")]
    [Display(Name = "Email Address")]
    public string? Email { get; set; }

    [MaxLength(20)]
    [Display(Name = "GSTIN / Tax ID")]
    public string? GSTNumber { get; set; }

    [MaxLength(500)]
    [Display(Name = "Billing Address")]
    public string? Address { get; set; }

    [MaxLength(100)]
    public string? City { get; set; }

    [MaxLength(100)]
    public string? State { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "Credit limit must be a positive number")]
    [Display(Name = "Credit Limit")]
    public decimal CreditLimit { get; set; }

    [Display(Name = "Opening Balance")]
    public decimal OpeningBalance { get; set; }

    [MaxLength(10)]
    public string BalanceType { get; set; } = "Debit";

    public bool IsActive { get; set; } = true;
}

public class SupplierInputModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Supplier code is required"), MaxLength(50)]
    [Display(Name = "Supplier Code")]
    public string SupplierCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "Supplier name is required"), MaxLength(200)]
    [Display(Name = "Supplier Name")]
    public string SupplierName { get; set; } = string.Empty;

    [MaxLength(100)]
    [Display(Name = "Contact Person")]
    public string? ContactPerson { get; set; }

    [MaxLength(20)]
    [Display(Name = "Mobile Number")]
    public string? Mobile { get; set; }

    public string? MobileNumber { get => Mobile; set => Mobile = value; }

    [MaxLength(100)]
    [EmailAddress(ErrorMessage = "Invalid email format")]
    public string? Email { get; set; }

    [MaxLength(20)]
    [Display(Name = "GSTIN")]
    public string? GSTNumber { get; set; }

    [MaxLength(500)]
    public string? Address { get; set; }

    [MaxLength(100)]
    public string? City { get; set; }

    [MaxLength(100)]
    public string? State { get; set; }

    [Display(Name = "Opening Balance")]
    public decimal OpeningBalance { get; set; }

    [MaxLength(10)]
    public string BalanceType { get; set; } = "Credit";

    public bool IsActive { get; set; } = true;
}

public class ProductInputModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Product code is required"), MaxLength(50)]
    [Display(Name = "Product Code / SKU")]
    public string ProductCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "Product name is required"), MaxLength(200)]
    [Display(Name = "Product Name")]
    public string ProductName { get; set; } = string.Empty;

    [Display(Name = "Category")]
    public int? CategoryId { get; set; }

    [Display(Name = "Brand")]
    public int? BrandId { get; set; }

    [Display(Name = "Unit of Measure")]
    public int? UnitId { get; set; }

    [Display(Name = "Warehouse")]
    public int? WarehouseId { get; set; }

    [MaxLength(50)]
    [Display(Name = "HSN Code")]
    public string? HSNCode { get; set; }

    [MaxLength(100)]
    public string? Barcode { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "Purchase price must be positive")]
    [Display(Name = "Purchase Price")]
    public decimal PurchasePrice { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "Sales price must be positive")]
    [Display(Name = "Sales Price")]
    public decimal SalesPrice { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "MRP must be positive")]
    public decimal MRP { get; set; }

    [Range(0, 100, ErrorMessage = "GST rate must be between 0% and 100%")]
    [Display(Name = "GST Rate (%)")]
    public decimal GSTPercentage { get; set; } = 18;

    [Display(Name = "Minimum Stock Alert")]
    public decimal MinimumStock { get; set; }

    [Display(Name = "Reorder Level")]
    public decimal ReorderLevel { get; set; }

    [MaxLength(500)]
    public string? Description { get; set; }

    public string? DocumentPath { get; set; }

    public bool IsActive { get; set; } = true;
}

public class SimpleMasterInputModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Name is required"), MaxLength(100)]
    [Display(Name = "Name")]
    public string Name { get; set; } = string.Empty;

    [MaxLength(50)]
    [Display(Name = "Code / Symbol")]
    public string? Code { get; set; }

    [MaxLength(250)]
    [Display(Name = "Description")]
    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;
}

public class WarehouseInputModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Warehouse code is required"), MaxLength(50)]
    [Display(Name = "Warehouse Code")]
    public string WarehouseCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "Warehouse name is required"), MaxLength(100)]
    [Display(Name = "Warehouse Name")]
    public string WarehouseName { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Address { get; set; }

    [MaxLength(100)]
    public string? City { get; set; }

    [MaxLength(100)]
    public string? State { get; set; }

    [MaxLength(20)]
    public string? Phone { get; set; }

    public string? ContactNumber { get => Phone; set => Phone = value; }

    public bool IsActive { get; set; } = true;
}

public class LedgerInputModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Ledger code is required"), MaxLength(50)]
    [Display(Name = "Ledger Code")]
    public string LedgerCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "Ledger name is required"), MaxLength(150)]
    [Display(Name = "Ledger Name")]
    public string LedgerName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Account group is required")]
    [Display(Name = "Account Group")]
    public int AccountGroupId { get; set; }

    [Display(Name = "Opening Balance")]
    public decimal OpeningBalance { get; set; }

    [Required]
    [MaxLength(10)]
    [Display(Name = "Balance Type")]
    public string BalanceType { get; set; } = "Debit";

    [MaxLength(500)]
    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;
}

public class CategoryInputModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Category name is required"), MaxLength(100)]
    [Display(Name = "Category Name")]
    public string CategoryName { get; set; } = string.Empty;

    public string Name { get => CategoryName; set => CategoryName = value; }

    [MaxLength(500)]
    [Display(Name = "Description")]
    public string? Description { get; set; }

    public int? ParentCategoryId { get; set; }

    public bool IsActive { get; set; } = true;
}

public class BrandInputModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Brand name is required"), MaxLength(100)]
    [Display(Name = "Brand Name")]
    public string BrandName { get; set; } = string.Empty;

    public string Name { get => BrandName; set => BrandName = value; }

    [MaxLength(500)]
    [Display(Name = "Description")]
    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;
}

public class UnitInputModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Unit name is required"), MaxLength(50)]
    [Display(Name = "Unit Name")]
    public string UnitName { get; set; } = string.Empty;

    public string Name { get => UnitName; set => UnitName = value; }

    [MaxLength(10)]
    [Display(Name = "Unit Symbol")]
    public string UnitSymbol { get; set; } = string.Empty;

    public string? Code { get => UnitSymbol; set => UnitSymbol = value ?? string.Empty; }

    [MaxLength(200)]
    [Display(Name = "Description")]
    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;
}

public class EmployeeInputModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Employee code is required"), MaxLength(20)]
    [Display(Name = "Employee Code")]
    public string EmployeeCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "Employee name is required"), MaxLength(200)]
    [Display(Name = "Employee Name")]
    public string EmployeeName { get; set; } = string.Empty;

    [MaxLength(100)]
    [Display(Name = "Designation")]
    public string? Designation { get; set; }

    [MaxLength(100)]
    [Display(Name = "Department")]
    public string? Department { get; set; }

    [MaxLength(20)]
    [Display(Name = "Mobile Number")]
    public string? Mobile { get; set; }

    [MaxLength(100)]
    [EmailAddress(ErrorMessage = "Invalid email format")]
    [Display(Name = "Email Address")]
    public string? Email { get; set; }

    public bool IsActive { get; set; } = true;
}

public class AccountGroupInputModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Group name is required"), MaxLength(100)]
    [Display(Name = "Group Name")]
    public string GroupName { get; set; } = string.Empty;

    public string Name { get => GroupName; set => GroupName = value; }

    [Required(ErrorMessage = "Group type is required"), MaxLength(20)]
    [Display(Name = "Group Type")]
    public string GroupType { get; set; } = "Asset";

    [MaxLength(500)]
    [Display(Name = "Description")]
    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;
}

public class BankInputModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Bank name is required"), MaxLength(200)]
    [Display(Name = "Bank Name")]
    public string BankName { get; set; } = string.Empty;

    public string Name { get => BankName; set => BankName = value; }

    [MaxLength(100)]
    [Display(Name = "Branch Name")]
    public string? BranchName { get; set; }

    [Required(ErrorMessage = "Account number is required"), MaxLength(50)]
    [Display(Name = "Account Number")]
    public string? AccountNumber { get; set; }

    [Required(ErrorMessage = "IFSC code is required"), MaxLength(20)]
    [Display(Name = "IFSC Code")]
    public string? IFSCCode { get; set; }

    [Required(ErrorMessage = "Accounting ledger is required")]
    [Display(Name = "Mapped Accounting Ledger")]
    public int? LedgerId { get; set; }

    public bool IsActive { get; set; } = true;
}

public class TaxInputModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Tax name is required"), MaxLength(50)]
    [Display(Name = "Tax Name")]
    public string TaxName { get; set; } = string.Empty;

    public string Name { get => TaxName; set => TaxName = value; }

    [Required(ErrorMessage = "Tax percentage is required")]
    [Range(0, 100, ErrorMessage = "Tax percentage must be between 0 and 100")]
    [Display(Name = "Tax Percentage")]
    public decimal TaxPercentage { get; set; }

    [MaxLength(20)]
    [Display(Name = "Tax Type")]
    public string TaxType { get; set; } = "GST";

    [Range(0, 100, ErrorMessage = "IGST percentage must be between 0 and 100")]
    [Display(Name = "IGST Percentage")]
    public decimal? IGSTPercentage { get; set; }

    [Range(0, 100, ErrorMessage = "CGST percentage must be between 0 and 100")]
    [Display(Name = "CGST Percentage")]
    public decimal? CGSTPercentage { get; set; }

    [Range(0, 100, ErrorMessage = "SGST percentage must be between 0 and 100")]
    [Display(Name = "SGST Percentage")]
    public decimal? SGSTPercentage { get; set; }

    public bool IsActive { get; set; } = true;
}

public class PaymentModeInputModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Mode name is required"), MaxLength(50)]
    [Display(Name = "Mode Name")]
    public string ModeName { get; set; } = string.Empty;

    public string Name { get => ModeName; set => ModeName = value; }

    [Required(ErrorMessage = "Mode type is required"), MaxLength(20)]
    [Display(Name = "Mode Type")]
    public string ModeType { get; set; } = "Cash";

    public bool IsActive { get; set; } = true;
}

public class CustomerPageViewModel
{
    public List<Customer> Customers { get; set; } = new();
    public CustomerInputModel Form { get; set; } = new();
}

public class SupplierPageViewModel
{
    public List<Supplier> Suppliers { get; set; } = new();
    public SupplierInputModel Form { get; set; } = new();
}

public class WarehousePageViewModel
{
    public List<Warehouse> Warehouses { get; set; } = new();
    public WarehouseInputModel Form { get; set; } = new();
}

public class CategoryPageViewModel
{
    public List<Category> Categories { get; set; } = new();
    public CategoryInputModel Form { get; set; } = new();
}

public class BrandPageViewModel
{
    public List<Brand> Brands { get; set; } = new();
    public BrandInputModel Form { get; set; } = new();
}

public class UnitPageViewModel
{
    public List<Unit> Units { get; set; } = new();
    public UnitInputModel Form { get; set; } = new();
}

public class EmployeePageViewModel
{
    public List<Employee> Employees { get; set; } = new();
    public EmployeeInputModel Form { get; set; } = new();
}

public class AccountGroupPageViewModel
{
    public List<AccountGroup> AccountGroups { get; set; } = new();
    public AccountGroupInputModel Form { get; set; } = new();
}

public class TaxPageViewModel
{
    public List<Tax> Taxes { get; set; } = new();
    public TaxInputModel Form { get; set; } = new();
}

public class PaymentModePageViewModel
{
    public List<PaymentMode> PaymentModes { get; set; } = new();
    public PaymentModeInputModel Form { get; set; } = new();
}
