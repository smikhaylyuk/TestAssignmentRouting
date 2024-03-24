using Microsoft.AspNetCore.Mvc;
using TestAssignmentRouting.Models;

namespace TestAssignmentRouting.Controllers
{
   [Route("api/[controller]")]
   [ApiController]
   public class SearchController : ControllerBase
   {
      private readonly ISearchService _searchService;

      public SearchController(ISearchService searchService)
      {
         _searchService = searchService;
      }

      [HttpPost]
      public async Task<IActionResult> SearchAsync([FromBody] SearchRequest request)
      {
         if (request == null)
         {
            return BadRequest("Invalid request");
         }

         try
         {
            var response = await _searchService.SearchAsync(request, CancellationToken.None);
            return Ok(response);
         }
         catch (Exception ex)
         {
            return StatusCode(500, $"Internal server error: {ex.Message}");
         }
      }

      [HttpGet("ping")]
      public async Task<IActionResult> Ping()
      {
         var isAvailable = await _searchService.IsAvailableAsync(CancellationToken.None);
         return isAvailable ? Ok() : StatusCode(503);
      }
   }
}
