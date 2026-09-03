namespace ERP.Models;

/// <summary>
/// Standardized response contract for all ERP CRUD operations and API endpoints.
/// </summary>
public class ApiResponse<T>
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public T? Data { get; set; }
    public Dictionary<string, string>? Errors { get; set; }

    public static ApiResponse<T> Ok(string message, T? data = default)
    {
        return new ApiResponse<T>
        {
            Success = true,
            Message = message,
            Data = data
        };
    }

    public static ApiResponse<T> Fail(string message, Dictionary<string, string>? errors = null, T? data = default)
    {
        return new ApiResponse<T>
        {
            Success = false,
            Message = message,
            Errors = errors,
            Data = data
        };
    }
}

public class ApiResponse : ApiResponse<object>
{
    public static new ApiResponse Ok(string message, object? data = null)
    {
        return new ApiResponse
        {
            Success = true,
            Message = message,
            Data = data
        };
    }

    public static new ApiResponse Fail(string message, Dictionary<string, string>? errors = null, object? data = null)
    {
        return new ApiResponse
        {
            Success = false,
            Message = message,
            Errors = errors,
            Data = data
        };
    }
}
