using System.Text.Json.Serialization;

namespace ERP.ViewModels;

public class ImportProductPreviewDto
{
    [JsonPropertyName("productCode")]
    public string ProductCode { get; set; } = string.Empty;

    [JsonPropertyName("productName")]
    public string ProductName { get; set; } = string.Empty;

    [JsonPropertyName("hsnCode")]
    public string HSNCode { get; set; } = string.Empty;

    [JsonPropertyName("categoryName")]
    public string CategoryName { get; set; } = string.Empty;

    [JsonPropertyName("brandName")]
    public string BrandName { get; set; } = string.Empty;

    [JsonPropertyName("unitName")]
    public string UnitName { get; set; } = string.Empty;

    [JsonPropertyName("warehouseName")]
    public string WarehouseName { get; set; } = string.Empty;

    [JsonPropertyName("purchasePrice")]
    public decimal PurchasePrice { get; set; }

    [JsonPropertyName("salesPrice")]
    public decimal SalesPrice { get; set; }

    [JsonPropertyName("mrp")]
    public decimal MRP { get; set; }

    [JsonPropertyName("gstPercentage")]
    public decimal GSTPercentage { get; set; } = 18;

    [JsonPropertyName("discount")]
    public decimal Discount { get; set; }

    [JsonPropertyName("openingStock")]
    public decimal OpeningStock { get; set; }

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("existsInDb")]
    public bool ExistsInDb { get; set; }

    [JsonPropertyName("existingProductId")]
    public int? ExistingProductId { get; set; }

    [JsonPropertyName("matchStatus")]
    public string MatchStatus { get; set; } = "New"; // "New", "ExactMatch", "SimilarMatch"

    [JsonPropertyName("similarMatchedName")]
    public string? SimilarMatchedName { get; set; }
}

public class ImportProductCommitDto
{
    [JsonPropertyName("productCode")]
    public string ProductCode { get; set; } = string.Empty;

    [JsonPropertyName("productName")]
    public string ProductName { get; set; } = string.Empty;

    [JsonPropertyName("hsnCode")]
    public string HSNCode { get; set; } = string.Empty;

    [JsonPropertyName("categoryName")]
    public string CategoryName { get; set; } = string.Empty;

    [JsonPropertyName("brandName")]
    public string BrandName { get; set; } = string.Empty;

    [JsonPropertyName("unitName")]
    public string UnitName { get; set; } = string.Empty;

    [JsonPropertyName("warehouseName")]
    public string WarehouseName { get; set; } = string.Empty;

    [JsonPropertyName("purchasePrice")]
    public decimal PurchasePrice { get; set; }

    [JsonPropertyName("salesPrice")]
    public decimal SalesPrice { get; set; }

    [JsonPropertyName("mrp")]
    public decimal MRP { get; set; }

    [JsonPropertyName("gstPercentage")]
    public decimal GSTPercentage { get; set; }

    [JsonPropertyName("discount")]
    public decimal Discount { get; set; }

    [JsonPropertyName("openingStock")]
    public decimal OpeningStock { get; set; }

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("existingProductId")]
    public int? ExistingProductId { get; set; }

    [JsonPropertyName("actionType")]
    public string ActionType { get; set; } = "CreateNew"; // "CreateNew", "UpdateExisting", "Ignore"
}

public class ProductImportResultDto
{
    [JsonPropertyName("productId")]
    public int ProductId { get; set; }

    [JsonPropertyName("productName")]
    public string ProductName { get; set; } = string.Empty;

    [JsonPropertyName("productCode")]
    public string ProductCode { get; set; } = string.Empty;

    [JsonPropertyName("quantity")]
    public decimal Quantity { get; set; }

    [JsonPropertyName("rate")]
    public decimal Rate { get; set; }

    [JsonPropertyName("categoryId")]
    public int? CategoryId { get; set; }
}
