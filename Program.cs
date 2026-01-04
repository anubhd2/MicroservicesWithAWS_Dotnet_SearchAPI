using Elastic.Clients.Elasticsearch;
using Elastic.Transport;
using Polly;
using Polly.CircuitBreaker;
using SearchApi.Models;
using System.Net;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        builder =>
        {
            builder
                .AllowAnyOrigin()
                .AllowAnyMethod()
                .AllowAnyHeader();
        });
});

var app = builder.Build();
app.UseCors("AllowAll");

var circuitBreakerPolicy = Policy<List<Hotel>>
    .Handle<Exception>()
    .CircuitBreakerAsync(3, TimeSpan.FromSeconds(30));

app.MapGet("/search", async (string? city, int? rating) =>
{

    var result = new HttpResponseMessage(HttpStatusCode.OK);
    try
    {
        return await circuitBreakerPolicy.ExecuteAsync(async () => await SearchHotels(city, rating));
    }
    catch (BrokenCircuitException)
    {
        throw new ApplicationException(message: "Circuit is OPEN.");     
    }
    catch (Exception e)
    {
        Console.WriteLine(e);
        throw;
    }
});
async Task<List<Hotel>> SearchHotels(string? city, int? rating)
{
    var host = Environment.GetEnvironmentVariable("host");//"https://my-deployment-aaee25.es.ap-south-1.aws.elastic-cloud.com";
    var userName = Environment.GetEnvironmentVariable("userName"); //"elastic";
    var password = Environment.GetEnvironmentVariable("password"); //"QKsmyurTF9sww5KDbfvvGC3J"; 
    var indexName = Environment.GetEnvironmentVariable("indexName"); //"event";

    var settings = new ElasticsearchClientSettings(new Uri(host));
    settings.Authentication(new BasicAuthentication(userName, password));
    settings.DefaultIndex(indexName);
    settings.DefaultMappingFor<Hotel>(m => m.IdProperty(p => p.Id));
    var client = new ElasticsearchClient(settings);

    if (rating is null)
    {
        rating = 1;
    }
    //match //prefix //Range // Fuzzy Match
    SearchResponse<Hotel> result = null;
    if (city is null)
    {
        result = await client.SearchAsync<Hotel>(s => s
    .Query(q => q
        .Bool(b => b
            .Must(
                m => m.MatchAll(),
                m => m.Range(r => r
                    .Number(nr => nr
                        .Field(f => f.Rating)
                        .Gte(rating)
                    )
                )
            )
        )
    )
);
    }
    else
    {
        result = await client.SearchAsync<Hotel>(s => s.Explain(true)
   .Query(q => q
       .Bool(b => b
           .Must(
               m => m.Prefix(p => p
                   .Field(f => f.City)
                   .Value(city)
                   .CaseInsensitive()
               ),
               m => m.Range(r => r
                   .Number(nr => nr
                       .Field(f => f.Rating)
                       .Gte(rating)
                   )
               )
           )
       )
   )
 );
    }
    return result.Hits.Select(x => x.Source).ToList();
}

app.Run();
