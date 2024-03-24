﻿namespace TestAssignmentRouting.Models
{
    public interface ISearchService
    {
        Task<SearchResponse> SearchAsync(SearchRequest request, CancellationToken cancellationToken);
        Task<bool> IsAvailableAsync(CancellationToken cancellationToken);
    }
}
