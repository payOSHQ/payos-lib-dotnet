using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using PayOS.Models.V1.Payouts;

namespace PayOS.Internal;

/// <summary>
/// Represents a page of paginated results with navigation and iteration capabilities
/// </summary>
/// <typeparam name="TItem">The type of items being paginated</typeparam>
/// <typeparam name="TResponse">The type of the paginated response</typeparam>
public class Page<TItem, TResponse> : IAsyncEnumerable<TItem>
    where TResponse : class
{
    private readonly Func<int, int, CancellationToken, Task<TResponse>> _fetchPage;
    private readonly Func<TResponse, Pagination> _getPagination;
    private readonly Func<TResponse, IEnumerable<TItem>> _getItems;
    private readonly TResponse _response;
    private readonly IReadOnlyList<TItem> _data;
    private readonly Pagination _pagination;

    /// <summary>
    /// Initializes a new instance of the Page class
    /// </summary>
    /// <param name="response">The response data</param>
    /// <param name="fetchPage">Function to fetch a page of data</param>
    /// <param name="getPagination">Function to extract pagination info from response</param>
    /// <param name="getItems">Function to extract items from response</param>
    public Page(
        TResponse response,
        Func<int, int, CancellationToken, Task<TResponse>> fetchPage,
        Func<TResponse, Pagination> getPagination,
        Func<TResponse, IEnumerable<TItem>> getItems)
    {
        _response = response ?? throw new ArgumentNullException(nameof(response));
        _fetchPage = fetchPage ?? throw new ArgumentNullException(nameof(fetchPage));
        _getPagination = getPagination ?? throw new ArgumentNullException(nameof(getPagination));
        _getItems = getItems ?? throw new ArgumentNullException(nameof(getItems));

        _pagination = _getPagination(_response);
        _data = _getItems(_response).ToList().AsReadOnly();
    }

    /// <summary>
    /// The items in the current page
    /// </summary>
    public IReadOnlyList<TItem> Data => _data;

    /// <summary>
    /// Pagination information for the current page
    /// </summary>
    public Pagination Pagination => _pagination;

    /// <summary>
    /// Check if there are more pages available
    /// </summary>
    public bool HasNextPage => _pagination.HasMore;

    /// <summary>
    /// Check if there are previous pages available
    /// </summary>
    public bool HasPreviousPage => _pagination.Offset > 0;

    /// <summary>
    /// Get the next page of results
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The next page</returns>
    public async Task<Page<TItem, TResponse>> GetNextPageAsync(CancellationToken cancellationToken = default)
    {
        if (!HasNextPage)
        {
            throw new InvalidOperationException("No more pages available");
        }

        var nextOffset = _pagination.Offset + _pagination.Count;
        var response = await _fetchPage(nextOffset, _pagination.Limit, cancellationToken);
        return new Page<TItem, TResponse>(response, _fetchPage, _getPagination, _getItems);
    }

    /// <summary>
    /// Get the previous page of results
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The previous page</returns>
    public async Task<Page<TItem, TResponse>> GetPreviousPageAsync(CancellationToken cancellationToken = default)
    {
        if (!HasPreviousPage)
        {
            throw new InvalidOperationException("No previous pages available");
        }

        var prevOffset = Math.Max(0, _pagination.Offset - _pagination.Limit);
        var response = await _fetchPage(prevOffset, _pagination.Limit, cancellationToken);
        return new Page<TItem, TResponse>(response, _fetchPage, _getPagination, _getItems);
    }

    /// <summary>
    /// Async iterator implementation for automatic pagination through all pages
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Async enumerator</returns>
    public async IAsyncEnumerator<TItem> GetAsyncEnumerator(CancellationToken cancellationToken = default)
    {
        var currentPage = this;

        while (!cancellationToken.IsCancellationRequested)
        {
            foreach (var item in currentPage.Data)
            {
                cancellationToken.ThrowIfCancellationRequested();
                yield return item;
            }

            if (!currentPage.HasNextPage)
            {
                break;
            }

            currentPage = await currentPage.GetNextPageAsync(cancellationToken);
        }
    }

    /// <summary>
    /// Collect all items from all pages into an array
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Array of all items</returns>
    public async Task<List<TItem>> ToListAsync(CancellationToken cancellationToken = default)
    {
        var items = new List<TItem>();
        await foreach (var item in this.WithCancellation(cancellationToken))
        {
            items.Add(item);
        }
        return items;
    }
}

/// <summary>
/// Extension methods for creating pagination iterators
/// </summary>
public static class PaginationExtensions
{
    /// <summary>
    /// Creates a page from paginated response
    /// </summary>
    /// <typeparam name="TItem">The type of items being paginated</typeparam>
    /// <typeparam name="TResponse">The type of the paginated response</typeparam>
    /// <param name="response">The initial response</param>
    /// <param name="fetchPage">Function to fetch a page of data</param>
    /// <param name="getPagination">Function to extract pagination info from response</param>
    /// <param name="getItems">Function to extract items from response</param>
    /// <returns>Page instance</returns>
    public static Page<TItem, TResponse> CreatePage<TItem, TResponse>(
        TResponse response,
        Func<int, int, CancellationToken, Task<TResponse>> fetchPage,
        Func<TResponse, Pagination> getPagination,
        Func<TResponse, IEnumerable<TItem>> getItems)
        where TResponse : class
    {
        return new Page<TItem, TResponse>(response, fetchPage, getPagination, getItems);
    }

    /// <summary>
    /// Creates a pagination iterator for paginated responses (legacy support)
    /// </summary>
    /// <typeparam name="TItem">The type of items being paginated</typeparam>
    /// <typeparam name="TResponse">The type of the paginated response</typeparam>
    /// <param name="fetchPage">Function to fetch a page of data</param>
    /// <param name="getPagination">Function to extract pagination info from response</param>
    /// <param name="getItems">Function to extract items from response</param>
    /// <param name="pageSize">Number of items per page</param>
    /// <param name="startingOffset">Starting offset for pagination</param>
    /// <returns>Async enumerable for iteration</returns>
    public static async IAsyncEnumerable<TItem> CreateIterator<TItem, TResponse>(
        Func<int, int, CancellationToken, Task<TResponse>> fetchPage,
        Func<TResponse, Pagination> getPagination,
        Func<TResponse, IEnumerable<TItem>> getItems,
        int pageSize = 50,
        int startingOffset = 0)
        where TResponse : class
    {
        var response = await fetchPage(startingOffset, pageSize, default);
        var page = new Page<TItem, TResponse>(response, fetchPage, getPagination, getItems);

        await foreach (var item in page)
        {
            yield return item;
        }
    }
}