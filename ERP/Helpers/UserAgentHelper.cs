using System;
using System.Text.RegularExpressions;

namespace ERP.Helpers;

public static class UserAgentHelper
{
    public static (string Browser, string OperatingSystem, string Device) Parse(string? userAgent)
    {
        if (string.IsNullOrWhiteSpace(userAgent))
        {
            return ("Unknown", "Unknown", "Unknown");
        }

        var ua = userAgent.Trim();

        // 1. Device detection
        string device;
        if (Regex.IsMatch(ua, @"\b(iPad|Tablet|PlayBook)\b", RegexOptions.IgnoreCase))
        {
            device = "Tablet";
        }
        else if (Regex.IsMatch(ua, @"\b(Mobi|Android|iPhone|iPod|BlackBerry|IEMobile|Opera Mini)\b", RegexOptions.IgnoreCase))
        {
            device = "Mobile";
        }
        else
        {
            device = "Desktop";
        }

        // 2. Operating System detection
        string os;
        if (ua.Contains("Windows NT 10.0", StringComparison.OrdinalIgnoreCase))
            os = "Windows 10/11";
        else if (ua.Contains("Windows NT 6.3", StringComparison.OrdinalIgnoreCase))
            os = "Windows 8.1";
        else if (ua.Contains("Windows NT 6.1", StringComparison.OrdinalIgnoreCase))
            os = "Windows 7";
        else if (ua.Contains("Windows", StringComparison.OrdinalIgnoreCase))
            os = "Windows";
        else if (ua.Contains("Android", StringComparison.OrdinalIgnoreCase))
            os = "Android";
        else if (ua.Contains("iPhone", StringComparison.OrdinalIgnoreCase) || ua.Contains("iPad", StringComparison.OrdinalIgnoreCase))
            os = "iOS";
        else if (ua.Contains("Mac OS X", StringComparison.OrdinalIgnoreCase))
            os = "macOS";
        else if (ua.Contains("Linux", StringComparison.OrdinalIgnoreCase))
            os = "Linux";
        else if (ua.Contains("CrOS", StringComparison.OrdinalIgnoreCase))
            os = "Chrome OS";
        else
            os = "Unknown OS";

        // 3. Browser detection
        string browser;
        if (ua.Contains("Edg/", StringComparison.OrdinalIgnoreCase) || ua.Contains("Edge/", StringComparison.OrdinalIgnoreCase))
            browser = "Microsoft Edge";
        else if (ua.Contains("OPR/", StringComparison.OrdinalIgnoreCase) || ua.Contains("Opera/", StringComparison.OrdinalIgnoreCase))
            browser = "Opera";
        else if (ua.Contains("Chrome/", StringComparison.OrdinalIgnoreCase))
            browser = "Google Chrome";
        else if (ua.Contains("Firefox/", StringComparison.OrdinalIgnoreCase))
            browser = "Mozilla Firefox";
        else if (ua.Contains("Safari/", StringComparison.OrdinalIgnoreCase) && !ua.Contains("Chrome", StringComparison.OrdinalIgnoreCase))
            browser = "Apple Safari";
        else
            browser = "Web Browser";

        return (browser, os, device);
    }
}
