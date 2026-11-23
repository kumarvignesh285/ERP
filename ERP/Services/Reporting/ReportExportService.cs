using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using ClosedXML.Excel;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using ERP.Helpers;
using ERP.Interfaces.Reporting;
using ERP.ViewModels.Reporting;

namespace ERP.Services.Reporting;

public class ReportExportService : IReportExportService
{
    static ReportExportService()
    {
        try
        {
            QuestPDF.Settings.License = LicenseType.Community;
        }
        catch { }
    }

    public byte[] ExportToExcel<T>(
        string reportTitle,
        List<string> columnHeaders,
        List<T> data,
        ReportFilterViewModel filter,
        string companyName,
        string companyAddress,
        string? companyGst)
    {
        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add(SanitizeSheetName(reportTitle));

        // Company Header
        worksheet.Cell(1, 1).Value = companyName;
        worksheet.Cell(1, 1).Style.Font.Bold = true;
        worksheet.Cell(1, 1).Style.Font.FontSize = 16;
        worksheet.Cell(1, 1).Style.Font.FontColor = XLColor.FromArgb(13, 110, 253);

        int row = 2;
        if (!string.IsNullOrWhiteSpace(companyAddress))
        {
            worksheet.Cell(row, 1).Value = companyAddress;
            worksheet.Cell(row, 1).Style.Font.FontSize = 10;
            worksheet.Cell(row, 1).Style.Font.FontColor = XLColor.DarkGray;
            row++;
        }

        if (!string.IsNullOrWhiteSpace(companyGst))
        {
            worksheet.Cell(row, 1).Value = $"GSTIN: {companyGst}";
            worksheet.Cell(row, 1).Style.Font.FontSize = 10;
            worksheet.Cell(row, 1).Style.Font.Bold = true;
            row++;
        }

        var (fromDate, toDate) = ReportFilterHelper.ResolveDateRange(filter);
        worksheet.Cell(row, 1).Value = $"Report: {reportTitle} | Period: {fromDate:dd-MMM-yyyy} to {toDate:dd-MMM-yyyy} | Generated: {DateTime.Now:dd-MMM-yyyy HH:mm}";
        worksheet.Cell(row, 1).Style.Font.Italic = true;
        worksheet.Cell(row, 1).Style.Font.FontSize = 10;
        row += 2;

        int headerRow = row;
        // Table Headers
        for (int c = 0; c < columnHeaders.Count; c++)
        {
            var cell = worksheet.Cell(headerRow, c + 1);
            cell.Value = columnHeaders[c];
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.FromArgb(235, 240, 250);
            cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            cell.Style.Border.OutsideBorderColor = XLColor.LightGray;
            cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        }

        row++;
        int startDataRow = row;

        // Data Rows via reflection or ToString
        var properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);

        foreach (var item in data)
        {
            int col = 1;
            for (int p = 0; p < Math.Min(columnHeaders.Count, properties.Length); p++)
            {
                var prop = properties[p];
                var val = prop.GetValue(item);
                var cell = worksheet.Cell(row, col);

                if (val == null)
                {
                    cell.Value = "-";
                }
                else if (val is decimal decVal)
                {
                    cell.Value = decVal;
                    cell.Style.NumberFormat.Format = "#,##0.00";
                    cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
                }
                else if (val is int intVal)
                {
                    cell.Value = intVal;
                    cell.Style.NumberFormat.Format = "#,##0";
                    cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
                }
                else if (val is DateTime dateVal)
                {
                    cell.Value = dateVal.ToString("dd-MMM-yyyy");
                    cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                }
                else if (val is bool boolVal)
                {
                    cell.Value = boolVal ? "Yes" : "No";
                    cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                }
                else
                {
                    cell.Value = val.ToString() ?? "";
                }

                cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                cell.Style.Border.OutsideBorderColor = XLColor.FromArgb(230, 230, 230);
                col++;
            }
            row++;
        }

        int lastDataRow = row - 1;

        // Auto-total summary row for decimal columns
        if (data.Any())
        {
            var summaryRow = worksheet.Cell(row, 1);
            summaryRow.Value = "TOTAL";
            summaryRow.Style.Font.Bold = true;

            for (int p = 0; p < Math.Min(columnHeaders.Count, properties.Length); p++)
            {
                var prop = properties[p];
                var cell = worksheet.Cell(row, p + 1);
                cell.Style.Font.Bold = true;
                cell.Style.Fill.BackgroundColor = XLColor.FromArgb(245, 245, 245);
                cell.Style.Border.TopBorder = XLBorderStyleValues.Thin;
                cell.Style.Border.BottomBorder = XLBorderStyleValues.Double;

                if (prop.PropertyType == typeof(decimal) || prop.PropertyType == typeof(decimal?))
                {
                    string colLetter = XLHelper.GetColumnLetterFromNumber(p + 1);
                    cell.FormulaA1 = $"SUM({colLetter}{startDataRow}:{colLetter}{lastDataRow})";
                    cell.Style.NumberFormat.Format = "#,##0.00";
                    cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
                }
            }
        }

        // Auto-filter
        if (columnHeaders.Count > 0 && lastDataRow >= startDataRow)
        {
            worksheet.Range(headerRow, 1, lastDataRow, columnHeaders.Count).SetAutoFilter();
        }

        worksheet.Columns().AdjustToContents();

        using var ms = new MemoryStream();
        workbook.SaveAs(ms);
        return ms.ToArray();
    }

    public byte[] ExportToPdf<T>(
        string reportTitle,
        List<string> columnHeaders,
        List<T> data,
        ReportFilterViewModel filter,
        string companyName,
        string companyAddress,
        string? companyGst,
        string? logoBase64 = null)
    {
        var (fromDate, toDate) = ReportFilterHelper.ResolveDateRange(filter);
        var properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);
        bool isLandscape = columnHeaders.Count > 6;

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(isLandscape ? PageSizes.A4.Landscape() : PageSizes.A4);
                page.Margin(20);
                page.DefaultTextStyle(x => x.FontSize(9).FontFamily("Arial"));

                page.Header().Element(headerContainer =>
                {
                    headerContainer.Column(col =>
                    {
                        col.Item().Row(row =>
                        {
                            row.RelativeItem().Column(c =>
                            {
                                c.Item().Text(companyName).FontSize(14).Bold().FontColor("#0d6efd");
                                if (!string.IsNullOrWhiteSpace(companyAddress))
                                    c.Item().Text(companyAddress).FontSize(8).FontColor(Colors.Grey.Medium);
                                if (!string.IsNullOrWhiteSpace(companyGst))
                                    c.Item().Text($"GSTIN: {companyGst}").FontSize(8).Bold();
                            });

                            row.RelativeItem().AlignRight().Column(c =>
                            {
                                c.Item().Text(reportTitle).FontSize(13).Bold().FontColor("#212529");
                                c.Item().Text($"Period: {fromDate:dd-MMM-yyyy} to {toDate:dd-MMM-yyyy}").FontSize(8).Italic();
                                c.Item().Text($"Generated: {DateTime.Now:dd-MMM-yyyy HH:mm}").FontSize(7).FontColor(Colors.Grey.Medium);
                            });
                        });

                        col.Item().PaddingVertical(5).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
                    });
                });

                page.Content().PaddingTop(5).Table(table =>
                {
                    // Define columns
                    table.ColumnsDefinition(columns =>
                    {
                        for (int i = 0; i < columnHeaders.Count; i++)
                        {
                            columns.RelativeColumn();
                        }
                    });

                    // Header row
                    table.Header(header =>
                    {
                        for (int c = 0; c < columnHeaders.Count; c++)
                        {
                            header.Cell().Background("#f1f5f9").Padding(4).BorderBottom(1).BorderColor("#cbd5e1")
                                .Text(columnHeaders[c]).Bold().FontSize(8);
                        }
                    });

                    // Data rows
                    int rIdx = 0;
                    foreach (var item in data.Take(1500))
                    {
                        string bg = rIdx % 2 == 0 ? "#ffffff" : "#f8fafc";
                        for (int p = 0; p < Math.Min(columnHeaders.Count, properties.Length); p++)
                        {
                            var prop = properties[p];
                            var val = prop.GetValue(item);
                            string textVal = val switch
                            {
                                null => "-",
                                decimal dec => dec.ToString("N2"),
                                int intVal => intVal.ToString("N0"),
                                DateTime dt => dt.ToString("dd-MMM-yyyy"),
                                bool b => b ? "Yes" : "No",
                                _ => val.ToString() ?? ""
                            };

                            var cell = table.Cell().Background(bg).Padding(3).BorderBottom(0.5f).BorderColor("#e2e8f0");
                            if (val is decimal || val is int)
                            {
                                cell.AlignRight().Text(textVal).FontSize(7.5f);
                            }
                            else
                            {
                                cell.Text(textVal).FontSize(7.5f);
                            }
                        }
                        rIdx++;
                    }
                });

                page.Footer().AlignCenter().Text(x =>
                {
                    x.Span("Page ");
                    x.CurrentPageNumber();
                    x.Span(" of ");
                    x.TotalPages();
                    x.Span($" | VMR Qubrix ERP - {companyName}");
                });
            });
        });

        using var ms = new MemoryStream();
        document.GeneratePdf(ms);
        return ms.ToArray();
    }

    private static string SanitizeSheetName(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return "Report";
        var invalid = new[] { ':', '\\', '/', '?', '*', '[', ']' };
        foreach (var c in invalid) name = name.Replace(c, '_');
        return name.Length > 31 ? name.Substring(0, 31) : name;
    }
}
