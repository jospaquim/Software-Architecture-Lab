namespace CleanArchitecture.Application.Common;

/// <summary>
/// Result pattern for handling success/failure scenarios
/// Avoids using exceptions for business logic errors
/// </summary>
public class Result<T>
{
    public bool IsSuccess { get; }
    public T? Value { get; }
    public string Error { get; }
    public List<string> Errors { get; }

    protected Result(bool isSuccess, T? value, string error, List<string>? errors = null)
    {
        IsSuccess = isSuccess;
        Value = value;
        Error = error;
        Errors = errors ?? new List<string>();
    }

    public static Result<T> Success(T value) => new(true, value, string.Empty);

    public static Result<T> Failure(string error) => new(false, default, error);

    public static Result<T> Failure(List<string> errors) => new(false, default, string.Empty, errors);

    public static implicit operator Result<T>(T value) => Success(value);
}

/// <summary>
/// Result pattern without value for void operations
/// </summary>
public class Result
{
    public bool IsSuccess { get; }
    public string Error { get; }
    public List<string> Errors { get; }

    protected Result(bool isSuccess, string error, List<string>? errors = null)
    {
        IsSuccess = isSuccess;
        Error = error;
        Errors = errors ?? new List<string>();
    }

    public static Result Success() => new(true, string.Empty);

    public static Result Failure(string error) => new(false, error);

    public static Result Failure(List<string> errors) => new(false, string.Empty, errors);
}

/// <summary>
/// Paginated result for list queries
/// </summary>
public class PagedResult<T>
{
    public IEnumerable<T> Items { get; set; } = new List<T>();
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
    public bool HasPreviousPage => PageNumber > 1;
    public bool HasNextPage => PageNumber < TotalPages;

    public PagedResult(IEnumerable<T> items, int pageNumber, int pageSize, int totalCount)
    {
        Items = items;
        PageNumber = pageNumber;
        PageSize = pageSize;
        TotalCount = totalCount;
    }
}
