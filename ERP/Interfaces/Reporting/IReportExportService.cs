using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using ERP.ViewModels.Reporting;

namespace ERP.Interfaces.Reporting;

public interface IReportExportService
{
    byte[] ExportToExcel<T>(string reportTitle, List<string> columnHeaders, List<T> data, ReportFilterViewModel filter, string companyName, string companyAddress, string? companyGst);
    byte[] ExportToPdf<T>(string reportTitle, List<string> columnHeaders, List<T> data, ReportFilterViewModel filter, string companyName, string companyAddress, string? companyGst, string? logoBase64 = null);
}
