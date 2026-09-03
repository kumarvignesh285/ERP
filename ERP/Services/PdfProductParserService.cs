using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using ERP.ViewModels;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;

namespace ERP.Services;

public interface IPdfProductParserService
{
    List<ImportProductPreviewDto> ParseProductsFromPdf(Stream pdfStream);
}

public class PdfProductParserService : IPdfProductParserService
{
    public List<ImportProductPreviewDto> ParseProductsFromPdf(Stream pdfStream)
    {
        var items = new List<ImportProductPreviewDto>();

        try
        {
            using (var document = PdfDocument.Open(pdfStream))
            {
                foreach (var page in document.GetPages())
                {
                    var words = page.GetWords();
                    if (words == null || !words.Any()) continue;

                    // Group words by Y coordinate to construct lines
                    // A tolerance of 4.5 points is standard for grouping text on the same line
                    var groupedLines = new List<List<Word>>();
                    foreach (var word in words.OrderByDescending(w => w.BoundingBox.Bottom).ThenBy(w => w.BoundingBox.Left))
                    {
                        bool placed = false;
                        foreach (var line in groupedLines)
                        {
                            var lineAvgY = line.Average(w => w.BoundingBox.Bottom);
                            if (Math.Abs(word.BoundingBox.Bottom - lineAvgY) < 4.5)
                            {
                                line.Add(word);
                                placed = true;
                                break;
                            }
                        }
                        if (!placed)
                        {
                            groupedLines.Add(new List<Word> { word });
                        }
                    }

                    // Sort words left-to-right within each line and get text representation
                    var sortedLines = groupedLines
                        .Select(line => line.OrderBy(w => w.BoundingBox.Left).ToList())
                        .ToList();

                    bool headerFound = false;
                    ImportProductPreviewDto? currentItem = null;

                    foreach (var wordList in sortedLines)
                    {
                        var lineText = string.Join(" ", wordList.Select(w => w.Text)).Trim();
                        if (string.IsNullOrEmpty(lineText)) continue;

                        // 1. Detect Table Start Header
                        if (!headerFound)
                        {
                            if (lineText.Contains("Description of Goods", StringComparison.OrdinalIgnoreCase) ||
                                lineText.Contains("Description", StringComparison.OrdinalIgnoreCase) ||
                                (lineText.Contains("Sl No", StringComparison.OrdinalIgnoreCase) && lineText.Contains("Amount", StringComparison.OrdinalIgnoreCase)))
                            {
                                headerFound = true;
                            }
                            continue;
                        }

                        // 2. Detect Table End Footer
                        if (lineText.StartsWith("Total", StringComparison.OrdinalIgnoreCase) ||
                            lineText.Contains("Grand Total", StringComparison.OrdinalIgnoreCase) ||
                            lineText.Contains("Amount Chargeable", StringComparison.OrdinalIgnoreCase) ||
                            lineText.Contains("continued to page", StringComparison.OrdinalIgnoreCase) ||
                            lineText.Contains("E.&O.E", StringComparison.OrdinalIgnoreCase) ||
                            lineText.Contains("Declaration", StringComparison.OrdinalIgnoreCase) ||
                            lineText.Contains("Company's Bank Details", StringComparison.OrdinalIgnoreCase))
                        {
                            // We hit the end of the item table on this page
                            break;
                        }

                        // 3. Try to parse a main table row
                        var parsedRow = ParseRow(wordList);
                        if (parsedRow != null)
                        {
                            // If we already have a currentItem, add it to list
                            if (currentItem != null)
                            {
                                items.Add(currentItem);
                            }
                            currentItem = parsedRow;
                        }
                        else if (currentItem != null)
                        {
                            // This might be a continuation of the description from the previous line.
                            // If this line does not contain numbers at the end, and contains text, we append it.
                            if (!HasRowEndingPattern(lineText))
                            {
                                // Remove any footer-like text
                                if (!lineText.Contains("Receiver's Signature", StringComparison.OrdinalIgnoreCase) &&
                                    !lineText.Contains("for Everest Machine Tools", StringComparison.OrdinalIgnoreCase) &&
                                    !lineText.Contains("Authorised Signatory", StringComparison.OrdinalIgnoreCase))
                                {
                                    currentItem.ProductName = (currentItem.ProductName + " " + lineText).Trim();
                                }
                            }
                        }
                    }

                    // Add last item from page if any
                    if (currentItem != null)
                    {
                        items.Add(currentItem);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            // Log or handle error - return empty or partially parsed items
            Console.WriteLine("PDF parsing error: " + ex.Message);
        }

        // Clean up extracted product names (e.g. double spaces, parentheses formatting)
        foreach (var item in items)
        {
            item.ProductName = Regex.Replace(item.ProductName, @"\s+", " ").Trim();
            // Try to set default GST and Sales Price / MRP calculations
            if (item.SalesPrice == 0 && item.PurchasePrice > 0)
            {
                item.SalesPrice = Math.Round(item.PurchasePrice * 1.25m, 2); // 25% markup
            }
            if (item.MRP == 0 && item.PurchasePrice > 0)
            {
                item.MRP = Math.Round(item.PurchasePrice * 1.30m, 2); // 30% markup
            }
        }

        return items;
    }

    private ImportProductPreviewDto? ParseRow(List<Word> words)
    {
        var tokens = words.Select(w => w.Text.Trim()).Where(t => !string.IsNullOrEmpty(t)).ToList();
        if (tokens.Count < 4) return null;

        int index = tokens.Count - 1;

        // 1. Amount (at the very end of the line)
        if (index < 0) return null;
        string amountStr = tokens[index].Replace(",", "");
        if (!decimal.TryParse(amountStr, out decimal amount))
        {
            amountStr = Regex.Replace(amountStr, @"[^\d\.]", "");
            if (!decimal.TryParse(amountStr, out amount)) return null;
        }
        index--;

        // 2. Discount % (optional, e.g. "5 %" or "5%")
        decimal discountPct = 0;
        if (index >= 0 && (tokens[index] == "%" || tokens[index].EndsWith("%")))
        {
            string discVal = tokens[index] == "%" ? tokens[index - 1] : tokens[index].TrimEnd('%');
            discVal = discVal.Replace(",", "");
            if (decimal.TryParse(discVal, out decimal dVal))
            {
                discountPct = dVal;
                index = (tokens[index] == "%") ? index - 2 : index - 1;
            }
        }

        // 3. Unit (optional, e.g. "nos", "pcs")
        string unit = "";
        if (index >= 0 && !decimal.TryParse(tokens[index].Replace(",", ""), out _))
        {
            unit = tokens[index];
            index--;
        }

        // 4. Rate (excl. of tax) (decimal)
        if (index < 0) return null;
        string rateExclStr = tokens[index].Replace(",", "");
        if (!decimal.TryParse(rateExclStr, out decimal rateExcl)) return null;
        index--;

        // 5. Rate (incl. of tax) (optional decimal)
        decimal rateIncl = rateExcl;
        if (index >= 0)
        {
            string rateInclStr = tokens[index].Replace(",", "");
            // Check if this token is a valid rate (usually has a dot or is numeric)
            if (decimal.TryParse(rateInclStr, out decimal parsedRateIncl))
            {
                rateIncl = parsedRateIncl;
                index--;
            }
        }

        // 6. Unit again (optional, e.g. "nos")
        if (index >= 0 && !decimal.TryParse(tokens[index].Replace(",", ""), out _))
        {
            if (string.IsNullOrEmpty(unit))
            {
                unit = tokens[index];
            }
            index--;
        }

        // 7. Quantity (decimal)
        if (index < 0) return null;
        string qtyStr = tokens[index].Replace(",", "");
        // Remove trailing unit words if stuck together (e.g. "10nos" -> "10")
        qtyStr = Regex.Match(qtyStr, @"^\d+(?:\.\d+)?").Value;
        if (!decimal.TryParse(qtyStr, out decimal qty)) return null;
        index--;

        // 8. HSN Code (optional, e.g. 84679100)
        string hsnCode = "";
        if (index >= 0)
        {
            string possibleHsn = tokens[index].Replace(",", "");
            if (possibleHsn.Length >= 4 && possibleHsn.Length <= 10 && long.TryParse(possibleHsn, out _))
            {
                hsnCode = possibleHsn;
                index--;
            }
        }

        // Remaining tokens are the description/product name
        if (index < 0) return null;
        var descTokens = tokens.Take(index + 1).ToList();

        // Remove leading serial number (integer) if present
        if (descTokens.Any() && int.TryParse(descTokens[0], out _))
        {
            descTokens.RemoveAt(0);
        }

        string productName = string.Join(" ", descTokens).Trim();
        if (string.IsNullOrEmpty(productName)) return null;

        return new ImportProductPreviewDto
        {
            ProductName = productName,
            HSNCode = hsnCode,
            CategoryName = (productName.Contains("chainsaw", StringComparison.OrdinalIgnoreCase) || 
                            productName.Contains("chain saw", StringComparison.OrdinalIgnoreCase)) 
                            ? "Chain saw" 
                            : "Power Tools",
            OpeningStock = qty,
            UnitName = unit,
            PurchasePrice = rateExcl,
            SalesPrice = Math.Round(rateExcl * 1.25m, 2),
            MRP = Math.Round(rateExcl * 1.30m, 2),
            Discount = discountPct,
            GSTPercentage = 18 // Default GST
        };
    }

    private bool HasRowEndingPattern(string text)
    {
        // Checks if the line ends with a pattern like "... Rate Unit Disc Amount"
        // If it matches numbers at the end, it's probably a row main line, not a wrapped description
        var matches = Regex.Matches(text, @"\d+(?:\.\d+)?");
        return matches.Count >= 3;
    }
}
