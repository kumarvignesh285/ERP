using System.Threading.Tasks;

namespace ERP.Interfaces;

public class SampleDataInitResult
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public int CategoriesCount { get; set; }
    public int BrandsCount { get; set; }
    public int UnitsCount { get; set; }
    public int TaxesCount { get; set; }
    public int PaymentModesCount { get; set; }
    public int AccountGroupsCount { get; set; }
    public int LedgersCount { get; set; }
    public int WarehousesCount { get; set; }
    public int ProductsCount { get; set; }
    public int CustomersCount { get; set; }
    public int SuppliersCount { get; set; }

    public string GetSummary()
    {
        return $"{CategoriesCount} Categories, {BrandsCount} Brands, {UnitsCount} Units, {ProductsCount} Products, {CustomersCount} Customers, {SuppliersCount} Suppliers, {TaxesCount} Taxes, {PaymentModesCount} Payment Modes, {AccountGroupsCount} Groups, {LedgersCount} Ledgers";
    }
}

public interface ICompanySampleDataService
{
    Task<bool> HasSampleDataAsync(int companyId);
    Task<SampleDataInitResult> InitializeSampleDataAsync(int companyId, string createdBy);
}
