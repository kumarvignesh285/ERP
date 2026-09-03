using System.Text.RegularExpressions;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace ERP.Helpers;

public static class DbExceptionHelper
{
    public static (string Message, Dictionary<string, string> Errors) ToUserFriendlyError(Exception ex, string entityName = "record")
    {
        var errors = new Dictionary<string, string>();

        if (ex is DbUpdateException dbUpdateEx)
        {
            var sqlEx = dbUpdateEx.GetBaseException() as SqlException;
            if (sqlEx != null)
            {
                switch (sqlEx.Number)
                {
                    // Unique constraint violation (Error 2627 / 2601)
                    case 2601:
                    case 2627:
                        var match = Regex.Match(sqlEx.Message, @"index '([^']+)'|with unique index '([^']+)'");
                        var indexName = match.Success ? (match.Groups[1].Value != "" ? match.Groups[1].Value : match.Groups[2].Value) : "";
                        var fieldName = ExtractFieldNameFromIndex(indexName);

                        string duplicateMsg = !string.IsNullOrEmpty(fieldName)
                            ? $"{fieldName} already exists. Please use a different value."
                            : $"A duplicate {entityName} already exists. Please check unique fields (Code, Name, Number).";

                        if (!string.IsNullOrEmpty(fieldName))
                        {
                            errors[fieldName] = duplicateMsg;
                        }

                        return (duplicateMsg, errors);

                    // Foreign key constraint violation (Error 547)
                    case 547:
                        if (sqlEx.Message.Contains("DELETE", StringComparison.OrdinalIgnoreCase))
                        {
                            return ($"This {entityName} cannot be deleted because it is currently used by other records or transactions (e.g. Invoices, Vouchers, Stock entries).", errors);
                        }
                        else
                        {
                            return ($"Cannot save {entityName} because one of the referenced items (e.g. Category, Customer, Supplier, Account) does not exist or has been deactivated.", errors);
                        }

                    // Cannot insert NULL (Error 515)
                    case 515:
                        var nullMatch = Regex.Match(sqlEx.Message, @"column '([^']+)'");
                        var colName = nullMatch.Success ? nullMatch.Groups[1].Value : "";
                        if (!string.IsNullOrEmpty(colName))
                        {
                            errors[colName] = $"{colName} is required.";
                            return ($"Required field '{colName}' is missing. Please fill in all mandatory fields.", errors);
                        }
                        return ($"Mandatory fields are missing for {entityName}. Please complete all required inputs.", errors);

                    // Deadlock / Timeout (Error 1205 / -2)
                    case 1205:
                    case -2:
                        return ("The database server is currently busy. Please retry your request.", errors);
                }
            }

            // Fallback for general DbUpdateException
            return ($"Could not save {entityName} due to a database constraint or data conflict. Please review your entries.", errors);
        }

        if (ex is InvalidOperationException invalidOpEx)
        {
            return (invalidOpEx.Message, errors);
        }

        if (ex is UnauthorizedAccessException unauthEx)
        {
            return (unauthEx.Message, errors);
        }

        if (ex is ArgumentException argEx)
        {
            if (!string.IsNullOrEmpty(argEx.ParamName))
            {
                errors[argEx.ParamName] = argEx.Message;
            }
            return (argEx.Message, errors);
        }

        // Generic unexpected exception
        return ($"An unexpected error occurred while processing the {entityName}. Please try again.", errors);
    }

    private static string ExtractFieldNameFromIndex(string indexName)
    {
        if (string.IsNullOrEmpty(indexName)) return string.Empty;

        var lower = indexName.ToLowerInvariant();
        if (lower.Contains("code")) return "Code";
        if (lower.Contains("name")) return "Name";
        if (lower.Contains("email")) return "Email";
        if (lower.Contains("number") || lower.Contains("no")) return "Number";
        if (lower.Contains("gst")) return "GSTNumber";
        if (lower.Contains("pan")) return "PANNumber";

        return string.Empty;
    }
}
