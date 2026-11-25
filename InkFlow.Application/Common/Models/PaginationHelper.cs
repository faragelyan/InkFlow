namespace InkFlow.Application.Common.Models;

public static class PaginationHelper
{
    public static (int PageNumber, int PageSize) Normalize(
        int pageNumber,
        int pageSize,
        int defaultPageSize = 10,
        int maximumPageSize = 50)
    {
        pageSize = Math.Clamp(pageSize < 1 ? defaultPageSize : pageSize, 1, maximumPageSize);
        var maximumPageNumber = (int)Math.Min(int.MaxValue, (long)int.MaxValue / pageSize + 1);
        pageNumber = Math.Clamp(pageNumber, 1, maximumPageNumber);
        return (pageNumber, pageSize);
    }
}