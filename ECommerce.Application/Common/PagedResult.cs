namespace ECommerce.Application.Common;

public record PagedResult<T>(IReadOnlyList<T> Items, int TotalCount, int Page, int PageSize);

public static class Pagination
{
    public static void Validate(int page, int pageSize)
    {
        if (page < 1)
            throw new ArgumentOutOfRangeException(nameof(page), "Page must be positive.");
        if (pageSize is < 1 or > 100)
            throw new ArgumentOutOfRangeException(nameof(pageSize), "Page size must be between 1 and 100.");
        if ((long)(page - 1) * pageSize > int.MaxValue)
            throw new ArgumentOutOfRangeException(nameof(page), "Page is too large.");
    }
}
