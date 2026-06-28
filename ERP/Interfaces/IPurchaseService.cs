using ERP.Models;

namespace ERP.Interfaces;

public interface IPurchaseService
{
    // Purchase Order
    Task<List<PurchaseOrder>> GetPurchaseOrdersAsync();
    Task<PurchaseOrder?> GetPurchaseOrderByIdAsync(int id);
    Task<PurchaseOrder> SavePurchaseOrderAsync(PurchaseOrder order);
    Task UpdatePurchaseOrderStatusAsync(int id, string status);
    Task DeletePurchaseOrderAsync(int id);

    // Goods Receipt Note
    Task<List<GoodsReceiptNote>> GetGRNsAsync();
    Task<GoodsReceiptNote?> GetGRNByIdAsync(int id);
    Task<GoodsReceiptNote> SaveGRNAsync(GoodsReceiptNote grn);
    Task DeleteGRNAsync(int id);

    // Purchase Invoice
    Task<List<PurchaseInvoice>> GetInvoicesAsync();
    Task<PurchaseInvoice?> GetInvoiceByIdAsync(int id);
    Task<PurchaseInvoice> SaveInvoiceAsync(PurchaseInvoice invoice);
    Task DeleteInvoiceAsync(int id);

    // Purchase Return
    Task<List<PurchaseReturn>> GetReturnsAsync();
    Task<PurchaseReturn?> GetReturnByIdAsync(int id);
    Task<PurchaseReturn> SaveReturnAsync(PurchaseReturn purchaseReturn);
    Task DeleteReturnAsync(int id);
}
