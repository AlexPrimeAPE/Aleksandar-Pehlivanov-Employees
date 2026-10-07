using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace Employees.Tests;

public class AnalyzeEndpointTests(WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client = factory.CreateClient();

    private Task<HttpResponseMessage> Upload(string csv, string fileName = "data.csv", string query = "")
    {
        var content = new MultipartFormDataContent
        {
            { new ByteArrayContent(Encoding.UTF8.GetBytes(csv)), "file", fileName },
        };
        return _client.PostAsync($"/api/analyze{query}", content);
    }

    [Fact]
    public async Task Valid_file_returns_the_longest_pair()
    {
        var response = await Upload("1,1,2020-01-01,2020-01-10\n2,1,2020-01-06,2020-01-20");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        var pair = json.GetProperty("pairs").EnumerateArray().Single();
        Assert.Equal(5, pair.GetProperty("totalDays").GetInt32());
    }

    [Fact]
    public async Task Tied_pairs_are_all_returned()
    {
        var response = await Upload("1,1,2020-01-01,2020-01-10\n2,1,2020-01-01,2020-01-10\n3,2,2021-01-01,2021-01-10\n4,2,2021-01-01,2021-01-10");

        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(2, json.GetProperty("pairs").GetArrayLength());
    }

    [Fact]
    public async Task Empty_file_is_rejected_with_problem_details()
    {
        var response = await Upload("");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task Wrong_file_type_is_rejected()
    {
        var response = await Upload("1,1,2020-01-01,2020-01-10", fileName: "data.pdf");

        Assert.Equal(HttpStatusCode.UnsupportedMediaType, response.StatusCode);
    }

    [Fact]
    public async Task Unknown_date_order_is_rejected()
    {
        var response = await Upload("1,1,2020-01-01,2020-01-10", query: "?dateOrder=banana");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
