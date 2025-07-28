using System.Net;

namespace MyLittleContentEngine.IntegrationTests.Infrastructure;

public static class TestServerExtensions
{
    public static async Task ShouldReturnSuccessWithContent(this HttpResponseMessage response, string expectedContent)
    {
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains(expectedContent, content, StringComparison.OrdinalIgnoreCase);
    }
    
    public static async Task ShouldReturnSuccessWithTitle(this HttpResponseMessage response, string expectedTitle)
    {
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains($"<title>{expectedTitle}</title>", content, StringComparison.OrdinalIgnoreCase);
    }
}