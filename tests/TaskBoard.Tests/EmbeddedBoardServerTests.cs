using TaskBoard.Client.Services;

namespace TaskBoard.Tests;

public sealed class EmbeddedBoardServerTests
{
    [Fact]
    public async Task StartAsync_ExposesHealthEndpoint()
    {
        await using var server = new EmbeddedBoardServer();
        using var http = new HttpClient();
        var port = 5099;

        await server.StartAsync(port);
        var response = await http.GetAsync($"http://127.0.0.1:{port}");

        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("Team Task Board server is running", body);
    }
}
