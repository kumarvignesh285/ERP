namespace ERP.Models;

public interface ICompanyOwned
{
    int CompanyId { get; set; }
    Company? Company { get; set; }
}
