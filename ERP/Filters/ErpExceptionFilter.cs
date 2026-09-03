using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using ERP.Helpers;
using ERP.Models;
using ERP.Interfaces;

namespace ERP.Filters;

public class ErpExceptionFilter : IAsyncExceptionFilter
{
    private readonly ILogger<ErpExceptionFilter> _logger;
    private readonly IAuditService? _auditService;

    public ErpExceptionFilter(ILogger<ErpExceptionFilter> logger, IAuditService? auditService = null)
    {
        _logger = logger;
        _auditService = auditService;
    }

    public async Task OnExceptionAsync(ExceptionContext context)
    {
        var ex = context.Exception;
        _logger.LogError(ex, "Unhandled exception in {ActionName} at {Path}", context.ActionDescriptor.DisplayName, context.HttpContext.Request.Path);

        var (userMessage, fieldErrors) = DbExceptionHelper.ToUserFriendlyError(ex);

        // Try logging to audit trail if available
        if (_auditService != null)
        {
            try
            {
                await _auditService.LogAsync(
                    action: "EXCEPTION",
                    module: context.RouteData.Values["controller"]?.ToString() ?? "System",
                    entityName: context.RouteData.Values["action"]?.ToString() ?? "Action",
                    description: $"Error: {userMessage} | Technical: {ex.Message}",
                    severity: "Error"
                );
            }
            catch
            {
                // Fallback silently if audit fails
            }
        }

        var isAjax = context.HttpContext.Request.Headers["X-Requested-With"] == "XMLHttpRequest" ||
                     (context.HttpContext.Request.ContentType?.Contains("application/json") ?? false) ||
                     context.HttpContext.Request.Headers["Accept"].ToString().Contains("application/json");

        if (isAjax)
        {
            context.Result = new JsonResult(ApiResponse.Fail(userMessage, fieldErrors))
            {
                StatusCode = StatusCodes.Status400BadRequest
            };
            context.ExceptionHandled = true;
        }
        else
        {
            // For standard view requests, let standard exception pipeline handle or render view
            // but mark user message in TempData if possible
            if (context.HttpContext.Items.ContainsKey("TempData"))
            {
                // Standard flow
            }
        }
    }
}
