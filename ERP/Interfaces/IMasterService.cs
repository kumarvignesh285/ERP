using ERP.Models;
using ERP.ViewModels;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;

namespace ERP.Interfaces;

public interface IMasterService
{
    // Company
    Task<Company?> GetCompanyAsync();
    Task<Company> SaveCompanyAsync(Company company);
    Task<Company> SaveCompanyWithLogoAsync(Company company, IFormFile? logoFile, IWebHostEnvironment env);
    Task<string> GetNextBillNumberPreviewAsync(string billType);
    Task<string> ReserveNextBillNumberAsync(string billType);

    // Customer
    Task<List<Customer>> GetCustomersAsync();
    Task<Customer?> GetCustomerByIdAsync(int id);
    Task<Customer> SaveCustomerAsync(Customer customer);
    Task DeleteCustomerAsync(int id);

    // Supplier
    Task<List<Supplier>> GetSuppliersAsync();
    Task<Supplier?> GetSupplierByIdAsync(int id);
    Task<Supplier> SaveSupplierAsync(Supplier supplier);
    Task DeleteSupplierAsync(int id);

    // Product
    Task<List<Product>> GetProductsAsync();
    Task<Product?> GetProductByIdAsync(int id);
    Task<Product> SaveProductAsync(Product product);
    Task DeleteProductAsync(int id);
    Task<(int successCount, List<string> errors)> BulkUploadProductsAsync(System.IO.Stream fileStream);

    // Category
    Task<List<Category>> GetCategoriesAsync();
    Task<Category?> GetCategoryByIdAsync(int id);
    Task<Category> SaveCategoryAsync(Category category);
    Task DeleteCategoryAsync(int id);

    // Brand
    Task<List<Brand>> GetBrandsAsync();
    Task<Brand?> GetBrandByIdAsync(int id);
    Task<Brand> SaveBrandAsync(Brand brand);
    Task DeleteBrandAsync(int id);

    // Unit
    Task<List<Unit>> GetUnitsAsync();
    Task<Unit?> GetUnitByIdAsync(int id);
    Task<Unit> SaveUnitAsync(Unit unit);
    Task DeleteUnitAsync(int id);

    // Warehouse
    Task<List<Warehouse>> GetWarehousesAsync();
    Task<Warehouse?> GetWarehouseByIdAsync(int id);
    Task<Warehouse> SaveWarehouseAsync(Warehouse warehouse);
    Task DeleteWarehouseAsync(int id);

    // Employee
    Task<List<Employee>> GetEmployeesAsync();
    Task<Employee?> GetEmployeeByIdAsync(int id);
    Task<Employee> SaveEmployeeAsync(Employee employee);
    Task DeleteEmployeeAsync(int id);

    // Account Group
    Task<List<AccountGroup>> GetAccountGroupsAsync();
    Task<AccountGroup?> GetAccountGroupByIdAsync(int id);
    Task<AccountGroup> SaveAccountGroupAsync(AccountGroup group);
    Task DeleteAccountGroupAsync(int id);

    // Ledger
    Task<List<Ledger>> GetLedgersAsync();
    Task<Ledger?> GetLedgerByIdAsync(int id);
    Task<Ledger> SaveLedgerAsync(Ledger ledger);
    Task DeleteLedgerAsync(int id);

    // Bank
    Task<List<Bank>> GetBanksAsync();
    Task<Bank?> GetBankByIdAsync(int id);
    Task<Bank> SaveBankAsync(Bank bank);
    Task DeleteBankAsync(int id);

    // Tax
    Task<List<Tax>> GetTaxesAsync();
    Task<Tax?> GetTaxByIdAsync(int id);
    Task<Tax> SaveTaxAsync(Tax tax);
    Task DeleteTaxAsync(int id);

    // Payment Mode
    Task<List<PaymentMode>> GetPaymentModesAsync();
    Task<PaymentMode?> GetPaymentModeByIdAsync(int id);
    Task<PaymentMode> SavePaymentModeAsync(PaymentMode mode);
    Task DeletePaymentModeAsync(int id);

    // Bulk Import and Verification
    Task<List<ImportProductPreviewDto>> PreviewImportAsync(System.IO.Stream fileStream, string fileExtension);
    Task<List<ProductImportResultDto>> CommitImportAsync(List<ImportProductCommitDto> items);
}
