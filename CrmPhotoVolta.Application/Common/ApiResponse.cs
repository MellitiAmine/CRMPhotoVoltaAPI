namespace CrmPhotoVolta.Application.Common;

public sealed class ApiError
{
    public required string Code { get; init; }
    public required string Message { get; init; }
    public object? Details { get; init; }
}

public sealed class PaginationMeta
{
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int TotalItems { get; init; }
    public int TotalPages { get; init; }
    public bool HasNext { get; init; }
    public bool HasPrevious { get; init; }
}

public sealed class ApiResponse
{
    public bool Success { get; init; }
    public object? Data { get; init; }
    public PaginationMeta? Meta { get; init; }
    public ApiError? Error { get; init; }

    public static ApiResponse Ok(object? data = null) => new() { Success = true, Data = data };

    public static ApiResponse OkPaged(object data, PaginationMeta meta) =>
        new() { Success = true, Data = data, Meta = meta };

    public static ApiResponse Fail(string code, string message, object? details = null) =>
        new() { Success = false, Error = new ApiError { Code = code, Message = message, Details = details } };
}
