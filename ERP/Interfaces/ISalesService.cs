using ERP.Models;

namespace ERP.Interfaces;

public interface ISalesService
{
    // Sales Quotation
    Task<List<SalesQuotation>> GetQuotationsAsync();
    Task<SalesQuotation?> GetQuotationByIdAsync(int id);
    Task<SalesQuotation> SaveQuotationAsync(SalesQuotation quotation);
    Task DeleteQuotationAsync(int id);

    // Sales Order
    Task<List<SalesOrder>> GetSalesOrdersAsync();
    Task<SalesOrder?> GetSalesOrderByIdAsync(int id);
    Task<SalesOrder> SaveSalesOrderAsync(SalesOrder order);
    Task UpdateSalesOrderStatusAsync(int id, string status);
    Task DeleteSalesOrderAsync(int id);

    // Delivery Challan
    Task<List<DeliveryChallan>> GetDeliveryChallansAsync();
    Task<DeliveryChallan?> GetDeliveryChallanByIdAsync(int id);
    Task<DeliveryChallan> SaveDeliveryChallanAsync(DeliveryChallan challan);
    Task DeleteDeliveryChallanAsync(int id);

    // Sales Invoice
    Task<List<SalesInvoice>> GetInvoicesAsync();
    Task<SalesInvoice?> GetInvoiceByIdAsync(int id);
    Task<SalesInvoice> SaveInvoiceAsync(SalesInvoice invoice);
    Task DeleteInvoiceAsync(int id);

    // Sales Return
    Task<List<SalesReturn>> GetReturnsAsync();
    Task<SalesReturn?> GetReturnByIdAsync(int id);
    Task<SalesReturn> SaveReturnAsync(SalesReturn salesReturn);
    Task DeleteReturnAsync(int id);
}
