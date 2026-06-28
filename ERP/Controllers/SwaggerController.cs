using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Routing;

namespace ERP.Controllers;

[AllowAnonymous]
[Route("swagger")]
public class SwaggerController : Controller
{
    private readonly EndpointDataSource _endpointDataSource;

    public SwaggerController(EndpointDataSource endpointDataSource)
    {
        _endpointDataSource = endpointDataSource;
    }

    [HttpGet("")]
    public ContentResult Index()
    {
        var endpoints = GetControllerEndpoints().ToList();
        var rows = string.Join(Environment.NewLine, endpoints.Select(e =>
            $"""<section class="endpoint"><span class="method">{e.Method}</span> <code>{e.Path}</code><p>{e.Controller}.{e.Action}</p></section>"""));

        var html = $$"""
<!doctype html>
<html lang="en">
<head>
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1" />
    <title>SmartERP API Docs</title>
    <style>
        body { margin: 0; font-family: Inter, Segoe UI, Arial, sans-serif; background: #f4f8fb; color: #1e3a5f; }
        header { padding: 28px 36px; background: #fff; border-bottom: 1px solid #d5e3ef; }
        main { padding: 28px 36px; max-width: 1120px; }
        h1 { margin: 0 0 8px; font-size: 28px; }
        .endpoint { background: #fff; border: 1px solid #d5e3ef; border-radius: 8px; margin: 12px 0; padding: 14px; }
        .method { display: inline-block; min-width: 56px; padding: 4px 8px; border-radius: 6px; background: #2f7fd1; color: #fff; font-weight: 700; font-size: 12px; text-align: center; }
        code { color: #195a9b; font-weight: 700; }
        a { color: #195a9b; }
    </style>
</head>
<body>
    <header>
        <h1>SmartERP Controller Routes</h1>
        <div>OpenAPI JSON: <a href="/swagger/v1/swagger.json">/swagger/v1/swagger.json</a></div>
    </header>
    <main>
        {{rows}}
    </main>
</body>
</html>
""";

        return Content(html, "text/html");
    }

    [HttpGet("v1/swagger.json")]
    public IActionResult Json()
    {
        var paths = new Dictionary<string, object>();

        foreach (var endpoint in GetControllerEndpoints())
        {
            if (!paths.TryGetValue(endpoint.Path, out var existing))
            {
                existing = new Dictionary<string, object>();
                paths[endpoint.Path] = existing;
            }

            var methodMap = (Dictionary<string, object>)existing;
            methodMap[endpoint.Method.ToLowerInvariant()] = CreateOperation(endpoint);
        }

        return Ok(new
        {
            openapi = "3.0.1",
            info = new
            {
                title = "SmartERP Routes",
                version = "v1",
                description = "All routed SmartERP controller actions. MVC view routes return HTML; API/AJAX routes return JSON or redirects depending on the action."
            },
            paths
        });
    }

    private IEnumerable<RouteInfo> GetControllerEndpoints()
    {
        return _endpointDataSource.Endpoints
            .OfType<RouteEndpoint>()
            .Select(endpoint => new
            {
                Endpoint = endpoint,
                Action = endpoint.Metadata.GetMetadata<ControllerActionDescriptor>(),
                Methods = endpoint.Metadata.GetMetadata<HttpMethodMetadata>()
            })
            .Where(x => x.Action != null && x.Action.ControllerName != "Swagger")
            .SelectMany(x =>
            {
                var methods = x.Methods?.HttpMethods?.Any() == true
                    ? x.Methods.HttpMethods
                    : new[] { "GET" };

                var path = "/" + (x.Endpoint.RoutePattern.RawText ?? string.Empty).TrimStart('/');
                path = path == "/" ? "/" : path.TrimEnd('/');

                return methods.Select(method => new RouteInfo(
                    method,
                    path,
                    x.Action!.ControllerName,
                    x.Action.ActionName,
                    x.Endpoint.RoutePattern.Parameters.Select(p => p.Name).ToArray()));
            })
            .OrderBy(x => x.Path)
            .ThenBy(x => x.Method);
    }

    private static object CreateOperation(RouteInfo endpoint)
    {
        var parameters = endpoint.Parameters
            .Select(name => new
            {
                name,
                @in = "path",
                required = true,
                schema = new { type = "string" }
            })
            .ToArray();

        return new
        {
            tags = new[] { endpoint.Controller },
            summary = $"{endpoint.Controller}.{endpoint.Action}",
            parameters,
            responses = new Dictionary<string, object>
            {
                ["200"] = new { description = "Success" },
                ["302"] = new { description = "Redirect or MVC navigation" },
                ["400"] = new { description = "Bad request" },
                ["401"] = new { description = "Unauthorized" },
                ["404"] = new { description = "Not found" }
            }
        };
    }

    private sealed record RouteInfo(string Method, string Path, string Controller, string Action, string[] Parameters);
}
