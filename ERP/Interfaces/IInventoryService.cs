using ERP.Models;

namespace ERP.Interfaces;

public interface IInventoryService
{
    // Stock Opening
    Task<List<Product>> GetProductsForOpeningStockAsync();
    Task UpdateOpeningStockAsync(int productId, decimal quantity);

    // Stock Transfer
    Task<List<StockTransfer>> GetStockTransfersAsync();
    Task<StockTransfer?> GetStockTransferByIdAsync(int id);
    Task<StockTransfer> SaveStockTransferAsync(StockTransfer transfer);
    Task<(bool Success, string Message)> DeleteStockTransferAsync(int id);

    // Stock Adjustment
    Task<List<StockAdjustment>> GetStockAdjustmentsAsync();
    Task<StockAdjustment?> GetStockAdjustmentByIdAsync(int id);
    Task<StockAdjustment> SaveStockAdjustmentAsync(StockAdjustment adjustment);
    Task<(bool Success, string Message)> DeleteStockAdjustmentAsync(int id);

    // Physical Stock Verification
    Task<List<PhysicalStockVerification>> GetPhysicalStockVerificationsAsync();
    Task<PhysicalStockVerification?> GetPhysicalStockVerificationByIdAsync(int id);
    Task<PhysicalStockVerification> SavePhysicalStockVerificationAsync(PhysicalStockVerification verification);
    Task<(bool Success, string Message)> DeletePhysicalStockVerificationAsync(int id);
}
