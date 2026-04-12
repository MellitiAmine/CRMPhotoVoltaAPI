using CrmPhotoVolta.Application.Common;

namespace CrmPhotoVoltaApis.Controllers;

public sealed class PaginationQuery
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public string? SortBy { get; set; }
    public string SortOrder { get; set; } = "desc";
    public string? Search { get; set; }

    public PaginationRequest ToRequest() => new()
    {
        Page = Page,
        PageSize = PageSize,
        SortBy = SortBy,
        SortOrder = SortOrder,
        Search = Search
    };
}
