namespace ERP.Interfaces;

public interface ICompanyContext
{
    int? CurrentCompanyId { get; }
    string? CurrentCompanyCode { get; }
    string? CurrentCompanyName { get; }
    int? CompanyId { get; }
    string? CompanyCode { get; }
    string? CompanyName { get; }
    bool HasActiveCompany { get; }
    bool HasCompanyContext { get; }
    bool IsSuperAdmin { get; }
    bool IsAuthenticated { get; }
    void SetCompanyOverride(int? companyId, string? companyCode, string? companyName);
}
