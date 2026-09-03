namespace ERP.ViewModels;

public class CompanyProvisioningResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public int CompanyId { get; set; }
    public string CompanyCode { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public string? AdminUsername { get; set; }
    public string? AdminFullName { get; set; }
    public string AdminRole { get; set; } = "CompanyAdmin";
    public string? InitialUsername { get; set; }
    public string? InitialUserFullName { get; set; }
    public string? InitialUserRole { get; set; }
    public bool IsActive { get; set; } = true;
    public bool SampleDataInitialized { get; set; }
    public string? SampleDataSummary { get; set; }
}
