using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using ERP.Interfaces;
using System;
using System.Threading.Tasks;

namespace ERP.Filters;

public class PermissionAttribute : TypeFilterAttribute
{
    public PermissionAttribute(string screenName, string action) : base(typeof(PermissionFilter))
    {
        Arguments = new object[] { screenName, action };
    }
}

public class PermissionFilter : IAsyncActionFilter
{
    private readonly IPermissionService _permissionService;
    private readonly string _screenName;
    private readonly string _action;

    public PermissionFilter(IPermissionService permissionService, string screenName, string action)
    {
        _permissionService = permissionService;
        _screenName = screenName;
        _action = action;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var user = context.HttpContext.User;
        var controller = context.RouteData.Values["controller"]?.ToString() ?? string.Empty;
        var actionName = context.RouteData.Values["action"]?.ToString() ?? string.Empty;

        var pageName = ResolvePageName(controller, actionName);
        if (string.IsNullOrEmpty(pageName))
        {
            pageName = _screenName;
        }

        var permissionAction = ResolveAction(actionName, _action);

        var hasPermission = await _permissionService.HasPermissionAsync(user, pageName, permissionAction);

        if (!hasPermission)
        {
            var request = context.HttpContext.Request;
            var isAjax = request.Headers["X-Requested-With"] == "XMLHttpRequest" || 
                         (request.Headers["Accept"].ToString().Contains("application/json", StringComparison.OrdinalIgnoreCase));

            if (isAjax)
            {
                context.Result = new JsonResult(new { success = false, message = $"Access Denied: You do not have permission to perform this action ({permissionAction} on {pageName})." })
                {
                    StatusCode = 403
                };
            }
            else
            {
                context.Result = new RedirectToActionResult("AccessDenied", "Account", null);
            }
            return;
        }

        await next();
    }

    private string ResolvePageName(string controller, string action)
    {
        if (string.IsNullOrEmpty(controller) || string.IsNullOrEmpty(action)) return string.Empty;

        controller = controller.ToLowerInvariant();
        action = action.ToLowerInvariant();

        if (controller == "crm")
        {
            if (action.Contains("lead")) return "Leads";
            if (action.Contains("follow")) return "Follow Ups";
            if (action.Contains("opp")) return "Opportunities";
            if (action.Contains("pipeline")) return "Pipeline View";
        }
        else if (controller == "masters")
        {
            if (action.Contains("company")) return "Company Master";
            if (action.Contains("customer")) return "Customer Master";
            if (action.Contains("supplier")) return "Supplier Master";
            if (action.Contains("product")) return "Product Master";
            if (action.Contains("category")) return "Category Master";
            if (action.Contains("brand")) return "Brand Master";
            if (action.Contains("unit")) return "Unit Master";
            if (action.Contains("warehouse")) return "Warehouse Master";
            if (action.Contains("ledger")) return "Ledger Master";
            if (action.Contains("employee")) return "Employee Master";
            if (action.Contains("group") || action.Contains("accountgroup")) return "Account Groups";
            if (action.Contains("bank")) return "Bank Master";
            if (action.Contains("tax")) return "Tax Settings";
            if (action.Contains("mode") || action.Contains("paymentmode")) return "Payment Modes";
        }
        else if (controller == "sales")
        {
            if (action.Contains("quotation")) return "Quotation";
            if (action.Contains("order")) return "Sales Order";
            if (action.Contains("challan")) return "Delivery Challan";
            if (action.Contains("invoice")) return "Sales Invoice";
            if (action.Contains("return")) return "Sales Return";
        }
        else if (controller == "purchase")
        {
            if (action.Contains("order")) return "Purchase Order";
            if (action.Contains("grn")) return "Goods Receipt Note";
            if (action.Contains("invoice")) return "Purchase Invoice";
            if (action.Contains("return")) return "Purchase Return";
        }
        else if (controller == "inventory")
        {
            if (action.Contains("opening")) return "Stock Opening";
            if (action.Contains("transfer")) return "Stock Transfer";
            if (action.Contains("adjustment")) return "Stock Adjustment";
            if (action.Contains("verification") || action.Contains("physical")) return "Physical Stock";
        }
        else if (controller == "accounts")
        {
            if (action.Contains("receipt")) return "Receipt Voucher";
            if (action.Contains("payment")) return "Payment Voucher";
            if (action.Contains("contra")) return "Contra Voucher";
            if (action.Contains("journal")) return "Journal Voucher";
            if (action.Contains("debit")) return "Debit Note";
            if (action.Contains("credit")) return "Credit Note";
            if (action.Contains("cash")) return "Cash Book";
            if (action.Contains("bank")) return "Bank Book";
        }
        else if (controller == "reports")
        {
            if (action.Contains("sales")) return "Sales Reports";
            if (action.Contains("purchase")) return "Purchase Reports";
            if (action.Contains("inventory")) return "Inventory Reports";
            if (action.Contains("accounting")) return "Accounting Reports";
        }
        else if (controller == "settings")
        {
            if (action.Contains("user") || action.Contains("deleteuser") || action.Contains("saveuser")) return "User Management";
            if (action.Contains("role")) return "Role Configuration";
            if (action.Contains("company")) return "Company Setup";
            if (action.Contains("system") || action.Contains("config")) return "System Settings";
        }

        return string.Empty;
    }

    private string ResolveAction(string actionName, string defaultAction)
    {
        if (string.IsNullOrEmpty(actionName)) return defaultAction;
        actionName = actionName.ToLowerInvariant();

        if (actionName.StartsWith("delete") || actionName.StartsWith("remove"))
        {
            return "delete";
        }
        if (actionName.StartsWith("save") || actionName.StartsWith("update") || actionName.StartsWith("edit") || actionName.StartsWith("add"))
        {
            return "edit";
        }

        return defaultAction.ToLowerInvariant();
    }
}
