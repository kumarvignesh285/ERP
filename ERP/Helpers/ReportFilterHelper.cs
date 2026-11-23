using System;
using ERP.ViewModels.Reporting;

namespace ERP.Helpers;

public static class ReportFilterHelper
{
    public static (DateTime fromDate, DateTime toDate) ResolveDateRange(ReportFilterViewModel filter)
    {
        var today = DateTime.Today;

        if (filter.FromDate.HasValue && filter.ToDate.HasValue)
        {
            var from = filter.FromDate.Value.Date;
            var to = filter.ToDate.Value.Date.AddDays(1).AddTicks(-1);
            return (from, to);
        }

        switch (filter.DatePreset?.ToLowerInvariant())
        {
            case "today":
                return (today, today.AddDays(1).AddTicks(-1));

            case "yesterday":
                var yesterday = today.AddDays(-1);
                return (yesterday, yesterday.AddDays(1).AddTicks(-1));

            case "this_week":
                int diff = (7 + (today.DayOfWeek - DayOfWeek.Monday)) % 7;
                var weekStart = today.AddDays(-1 * diff).Date;
                return (weekStart, today.AddDays(1).AddTicks(-1));

            case "last_month":
                var firstDayLastMonth = new DateTime(today.Year, today.Month, 1).AddMonths(-1);
                var lastDayLastMonth = firstDayLastMonth.AddMonths(1).AddTicks(-1);
                return (firstDayLastMonth, lastDayLastMonth);

            case "this_year":
                // Standard Indian Financial Year starts April 1st
                int fyYear = today.Month >= 4 ? today.Year : today.Year - 1;
                var fyStart = new DateTime(fyYear, 4, 1);
                var fyEnd = fyStart.AddYears(1).AddTicks(-1);
                return (fyStart, fyEnd);

            case "this_month":
            default:
                var firstDayThisMonth = new DateTime(today.Year, today.Month, 1);
                var lastDayThisMonth = firstDayThisMonth.AddMonths(1).AddTicks(-1);
                return (firstDayThisMonth, lastDayThisMonth);
        }
    }
}
