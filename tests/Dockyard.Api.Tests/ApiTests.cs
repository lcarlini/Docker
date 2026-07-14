using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Dockyard.Api.Tests;

public sealed class ApiTests(WebApplicationFactory<Program> factory)
    : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task LiveHealthEndpoint_ReturnsOk()
    {
        using var response = await _client.GetAsync("/health/live");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Healthy", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task InspectEndpoint_PreservesValidCorrelationId()
    {
        const string correlationId = "portfolio-test-42";
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/inspect");
        request.Headers.Add("X-Correlation-ID", correlationId);

        using var response = await _client.SendAsync(request);
        var payload = await response.Content.ReadFromJsonAsync<InspectionResponse>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(correlationId, response.Headers.GetValues("X-Correlation-ID").Single());
        Assert.Equal(correlationId, payload?.CorrelationId);
    }

    private sealed record InspectionResponse(string CorrelationId);
}
