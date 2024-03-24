namespace TestAssignmentRouting
{
   using System;
   using System.Net.Http;
   using System.Text;
   using System.Threading;
   using System.Threading.Tasks;
   using Microsoft.Extensions.Caching.Memory;
   using Newtonsoft.Json;
   using TestAssignmentRouting.Models; // Use the correct namespace

   public class SearchService : ISearchService
   {
      private IHttpClientFactory _httpClientFactory;
      private const string providerOnePingUrl = "http://provider-one/api/v1/ping";
      private const string providerTwoPingUrl = "http://provider-two/api/v1/ping";
      private const string ProviderTwoBaseUrl = "http://provider-two/api/v1/search";
      private const string ProviderOneBaseUrl = "http://provider-one/api/v1/search";
      private readonly IMemoryCache _cache;

      // Constructor, consider injecting any required services here, e.g., HttpClient, caching services
      public SearchService(IHttpClientFactory httpClientFactory, IMemoryCache cache)
      {
         _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
         _cache = cache ?? throw new ArgumentNullException(nameof(cache));
      }

      public async Task<SearchResponse> SearchAsync(SearchRequest request, CancellationToken cancellationToken)
      {
         // Attempt to retrieve cached results first
         var cacheKey = CreateCacheKey(request);
         if (_cache.TryGetValue(cacheKey, out SearchResponse cachedResponse))
         {
            return cachedResponse;
         }

         // Call both providers concurrently
         var providerOneTask = FetchProviderOneResultsAsync(request, cancellationToken);
         var providerTwoTask = FetchProviderTwoResultsAsync(request, cancellationToken);
         await Task.WhenAll(providerOneTask, providerTwoTask);

         var aggregatedRoutes = providerOneTask.Result.Concat(providerTwoTask.Result);

         // Apply filters (not implemented here)
         // ...

         // Cache and return the response
         var response = new SearchResponse
         {
            Routes = aggregatedRoutes.ToArray(),
            // Calculate MinPrice, MaxPrice, MinMinutesRoute, MaxMinutesRoute based on aggregatedRoutes
         };

         // Cache the response
         _cache.Set(cacheKey, response, TimeSpan.FromMinutes(5)); // Example: cache for 5 minutes

         return response;
      }

      public async Task<bool> IsAvailableAsync(CancellationToken cancellationToken)
      {
         var client = _httpClientFactory.CreateClient();

         try
         {
            // Ping Provider One
            var responseOne = await client.GetAsync(providerOnePingUrl, cancellationToken);
            if (!responseOne.IsSuccessStatusCode) return false;

            // Ping Provider Two
            var responseTwo = await client.GetAsync(providerTwoPingUrl, cancellationToken);
            if (!responseTwo.IsSuccessStatusCode) return false;

            // Both providers are available
            return true;
         }
         catch
         {
            // An error occurred (e.g., network error, timeout), treat as unavailable
            return false;
         }
      }

      private async Task<IEnumerable<Route>> FetchProviderOneResultsAsync(SearchRequest request, CancellationToken cancellationToken)
      {
         var httpClient = _httpClientFactory.CreateClient();
         var providerRequest = new ProviderOneSearchRequest
         {
            From = request.Origin,
            To = request.Destination,
            DateFrom = request.OriginDateTime,
            DateTo = request.Filters?.DestinationDateTime,
            MaxPrice = request.Filters?.MaxPrice
            // Populate other necessary fields from SearchRequest
         };

         string jsonRequest = JsonConvert.SerializeObject(providerRequest);
         var content = new StringContent(jsonRequest, Encoding.UTF8, "application/json");

         try
         {
            var response = await httpClient.PostAsync(ProviderOneBaseUrl, content, cancellationToken);
            if (response.IsSuccessStatusCode)
            {
               var jsonResponse = await response.Content.ReadAsStringAsync();
               var providerResponse = JsonConvert.DeserializeObject<ProviderOneSearchResponse>(jsonResponse);

               return providerResponse.Routes.Select(r => new Route
               {
                  Id = Guid.NewGuid(), // Create a new GUID for each route
                  Origin = r.From,
                  Destination = r.To,
                  OriginDateTime = r.DateFrom,
                  DestinationDateTime = r.DateTo,
                  Price = r.Price,
                  TimeLimit = r.TimeLimit
               });
            }
            else
            {
               // Log or handle the unsuccessful response as needed
               return Enumerable.Empty<Route>();
            }
         }
         catch (Exception ex)
         {
            // Log or handle the exception as needed
            return Enumerable.Empty<Route>();
         }
      }

      private async Task<IEnumerable<Route>> FetchProviderTwoResultsAsync(SearchRequest request, CancellationToken cancellationToken)
      {
         var httpClient = _httpClientFactory.CreateClient();
         var providerRequest = new ProviderTwoSearchRequest
         {
            Departure = request.Origin,
            Arrival = request.Destination,
            DepartureDate = request.OriginDateTime,
            MinTimeLimit = request.Filters?.MinTimeLimit
            // Populate other necessary fields from SearchRequest
         };

         string jsonRequest = JsonConvert.SerializeObject(providerRequest);
         var content = new StringContent(jsonRequest, Encoding.UTF8, "application/json");

         try
         {
            var response = await httpClient.PostAsync(ProviderTwoBaseUrl, content, cancellationToken);
            if (response.IsSuccessStatusCode)
            {
               var jsonResponse = await response.Content.ReadAsStringAsync(cancellationToken);
               var providerResponse = JsonConvert.DeserializeObject<ProviderTwoSearchResponse>(jsonResponse);

               return providerResponse.Routes.Select(r => new Route
               {
                  Id = Guid.NewGuid(), // Create a new GUID for each route
                  Origin = r.Departure.Point,
                  Destination = r.Arrival.Point,
                  OriginDateTime = r.Departure.Date,
                  DestinationDateTime = r.Arrival.Date,
                  Price = r.Price,
                  TimeLimit = r.TimeLimit
               });
            }
            else
            {
               // Log the unsuccessful response or handle errors as needed
               return Enumerable.Empty<Route>();
            }
         }
         catch (Exception ex)
         {
            // Log the exception or handle it as needed
            return Enumerable.Empty<Route>();
         }
      }

      private string CreateCacheKey(SearchRequest request)
      {
         // Create a unique key based on the request's properties
         // Use a combination of origin, destination, dates, etc.
         return $"Search_{request.Origin}_{request.Destination}_{request.OriginDateTime.Ticks}";
      }
   }
}
