using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace AdminDashBoard.Tests;

public static class TestAuthExtensions
{
    public static async Task<HttpClient> CreateAuthenticatedClientAsync(
        this WebApplicationFactory<Program> factory)
    {
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = true
        });

        var response = await client.PostAsJsonAsync(
            "/api/auth/login",
            new
            {
                Email = "ruipaguiar@gmail.com",
                Password = "Password123!"
            });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var csrfResponse = await client.GetAsync("/api/auth/csrf");
        Assert.Equal(HttpStatusCode.OK, csrfResponse.StatusCode);
        var csrfPayload = await csrfResponse.Content.ReadFromJsonAsync<JsonElement>();
        var csrfToken = csrfPayload.GetProperty("token").GetString();
        Assert.False(string.IsNullOrWhiteSpace(csrfToken));

        client.DefaultRequestHeaders.Remove("X-CSRF-TOKEN");
        client.DefaultRequestHeaders.Add("X-CSRF-TOKEN", csrfToken);

        return client;
    }
}
