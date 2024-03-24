using TestAssignmentRouting;
using TestAssignmentRouting.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();

// Register ISearchService with its implementation
builder.Services.AddScoped<ISearchService, SearchService>();
// Register HttpClientFactory to be used for making HTTP requests
builder.Services.AddHttpClient();
// Register MemoryCache to be used for caching
builder.Services.AddMemoryCache();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
   app.UseSwagger();
   app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
