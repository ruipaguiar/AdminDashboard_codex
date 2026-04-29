using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace AdminDashBoard.Tests;

public sealed class SystemStatusEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public SystemStatusEndpointTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetSystemStatus_ReturnsNonSecretOperationalStatus()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/system/status");
        var payload = await response.Content.ReadFromJsonAsync<SystemStatusResponse>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(payload);
        Assert.Equal("ok", payload.Status);
        Assert.NotNull(payload.Database.Health);
        Assert.Equal("OpenAI", payload.Ai.Provider);
        Assert.NotNull(payload.Ai.Model);
        Assert.DoesNotContain("printpro", await response.Content.ReadAsStringAsync());
    }
}
