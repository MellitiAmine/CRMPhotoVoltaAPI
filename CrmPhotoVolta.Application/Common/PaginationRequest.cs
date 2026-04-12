namespace CrmPhotoVolta.Application.Common;

public sealed class PaginationRequest
{
    private int _page = 1;
    private int _pageSize = 20;

    public int Page
    {
        get => _page;
        set => _page = value < 1 ? 1 : value;
    }

    public int PageSize
    {
        get => _pageSize;
        set
        {
            if (value < 10) _pageSize = 10;
            else if (value > 50) _pageSize = 50;
            else _pageSize = value;
        }
    }

    public string? SortBy { get; set; }
    public string SortOrder { get; set; } = "desc";
    public string? Search { get; set; }

    public PaginationMeta ToMeta(int totalItems)
    {
        var totalPages = (int)Math.Ceiling(totalItems / (double)PageSize);
        return new PaginationMeta
        {
            Page = Page,
            PageSize = PageSize,
            TotalItems = totalItems,
            TotalPages = totalPages,
            HasNext = Page < totalPages,
            HasPrevious = Page > 1
        };
    }
}
