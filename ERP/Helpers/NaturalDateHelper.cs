using System;
using System.Globalization;
using System.Text.RegularExpressions;

namespace ERP.Helpers;

public static class NaturalDateHelper
{
    public static (DateTime FromDate, DateTime ToDate, string PeriodLabel) ResolveDateRange(string text)
    {
        var now = DateTime.Today;
        var lower = (text ?? string.Empty).ToLowerInvariant();

        // 1. Explicit regex for dates like "from 2026-01-01 to 2026-02-15" or "2026-01-01 to 2026-01-31"
        var isoMatch = Regex.Match(lower, @"(?:\bfrom\s+)?(\d{4}-\d{1,2}-\d{1,2})\s+(?:to|until|-|and)\s+(\d{4}-\d{1,2}-\d{1,2})");
        if (isoMatch.Success && 
            DateTime.TryParse(isoMatch.Groups[1].Value, out var d1) && 
            DateTime.TryParse(isoMatch.Groups[2].Value, out var d2))
        {
            return (d1.Date, d2.Date, $"{d1:dd MMM yyyy} to {d2:dd MMM yyyy}");
        }

        // 2. Relative keywords
        if (lower.Contains("today"))
        {
            return (now, now, "Today");
        }

        if (lower.Contains("yesterday"))
        {
            var yest = now.AddDays(-1);
            return (yest, yest, "Yesterday");
        }

        if (lower.Contains("last 7 days") || lower.Contains("past 7 days"))
        {
            return (now.AddDays(-7), now, "Last 7 Days");
        }

        if (lower.Contains("this week"))
        {
            var diff = (7 + (now.DayOfWeek - DayOfWeek.Monday)) % 7;
            var startOfWeek = now.AddDays(-diff);
            return (startOfWeek, now, "This Week");
        }

        if (lower.Contains("last week") || lower.Contains("previous week"))
        {
            var diff = (7 + (now.DayOfWeek - DayOfWeek.Monday)) % 7;
            var startOfLastWeek = now.AddDays(-diff - 7);
            var endOfLastWeek = startOfLastWeek.AddDays(6);
            return (startOfLastWeek, endOfLastWeek, "Last Week");
        }

        if (lower.Contains("last 30 days") || lower.Contains("past 30 days"))
        {
            return (now.AddDays(-30), now, "Last 30 Days");
        }

        if (lower.Contains("last 60 days") || lower.Contains("past 60 days"))
        {
            return (now.AddDays(-60), now, "Last 60 Days");
        }

        if (lower.Contains("last 90 days") || lower.Contains("past 90 days") || lower.Contains("90 days"))
        {
            return (now.AddDays(-90), now, "Last 90 Days");
        }

        if (lower.Contains("last month") || lower.Contains("previous month"))
        {
            var firstDayCurrentMonth = new DateTime(now.Year, now.Month, 1);
            var lastMonthEnd = firstDayCurrentMonth.AddDays(-1);
            var lastMonthStart = new DateTime(lastMonthEnd.Year, lastMonthEnd.Month, 1);
            return (lastMonthStart, lastMonthEnd, $"{lastMonthStart:MMMM yyyy}");
        }

        if (lower.Contains("this month") || lower.Contains("current month"))
        {
            var startOfMonth = new DateTime(now.Year, now.Month, 1);
            var endOfMonth = startOfMonth.AddMonths(1).AddDays(-1);
            return (startOfMonth, endOfMonth, $"{startOfMonth:MMMM yyyy}");
        }

        if (lower.Contains("this year") || lower.Contains("current year"))
        {
            var startOfYear = new DateTime(now.Year, 1, 1);
            return (startOfYear, now, $"{now.Year}");
        }

        if (lower.Contains("last year") || lower.Contains("previous year"))
        {
            var startOfLastYear = new DateTime(now.Year - 1, 1, 1);
            var endOfLastYear = new DateTime(now.Year - 1, 12, 31);
            return (startOfLastYear, endOfLastYear, $"{now.Year - 1}");
        }

        // 3. Quarter matching
        if (lower.Contains("q1"))
        {
            return (new DateTime(now.Year, 1, 1), new DateTime(now.Year, 3, 31), $"Q1 {now.Year}");
        }
        if (lower.Contains("q2"))
        {
            return (new DateTime(now.Year, 4, 1), new DateTime(now.Year, 6, 30), $"Q2 {now.Year}");
        }
        if (lower.Contains("q3"))
        {
            return (new DateTime(now.Year, 7, 1), new DateTime(now.Year, 9, 30), $"Q3 {now.Year}");
        }
        if (lower.Contains("q4"))
        {
            return (new DateTime(now.Year, 10, 1), new DateTime(now.Year, 12, 31), $"Q4 {now.Year}");
        }

        // 4. Default: Current Month
        var defStart = new DateTime(now.Year, now.Month, 1);
        var defEnd = defStart.AddMonths(1).AddDays(-1);
        return (defStart, defEnd, $"{defStart:MMMM yyyy}");
    }

    public static (DateTime CurrentFrom, DateTime CurrentTo, DateTime PrevFrom, DateTime PrevTo, string Label) ResolveComparisonPeriods(string text)
    {
        var lower = (text ?? string.Empty).ToLowerInvariant();
        var now = DateTime.Today;

        if (lower.Contains("month"))
        {
            var thisMonthStart = new DateTime(now.Year, now.Month, 1);
            var thisMonthEnd = thisMonthStart.AddMonths(1).AddDays(-1);

            var lastMonthEnd = thisMonthStart.AddDays(-1);
            var lastMonthStart = new DateTime(lastMonthEnd.Year, lastMonthEnd.Month, 1);

            return (thisMonthStart, thisMonthEnd, lastMonthStart, lastMonthEnd, $"{thisMonthStart:MMM yyyy} vs {lastMonthStart:MMM yyyy}");
        }

        if (lower.Contains("week"))
        {
            var diff = (7 + (now.DayOfWeek - DayOfWeek.Monday)) % 7;
            var thisWeekStart = now.AddDays(-diff);
            var thisWeekEnd = now;

            var lastWeekStart = thisWeekStart.AddDays(-7);
            var lastWeekEnd = thisWeekStart.AddDays(-1);

            return (thisWeekStart, thisWeekEnd, lastWeekStart, lastWeekEnd, "This Week vs Last Week");
        }

        // Default: This Month vs Last Month
        var curStart = new DateTime(now.Year, now.Month, 1);
        var curEnd = curStart.AddMonths(1).AddDays(-1);
        var pEnd = curStart.AddDays(-1);
        var pStart = new DateTime(pEnd.Year, pEnd.Month, 1);
        return (curStart, curEnd, pStart, pEnd, $"{curStart:MMM yyyy} vs {pStart:MMM yyyy}");
    }
}
