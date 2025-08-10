using System.Net;
using Shouldly;

namespace MyLittleContentEngine.IntegrationTests.Infrastructure;

public static class TestServerExtensions
{
    public static async Task ShouldReturnSuccessWithContent(this HttpResponseMessage response, string expectedContent)
    {
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        
        var content = await response.Content.ReadAsStringAsync();
        content.ShouldContain(expectedContent, Case.Insensitive);
    }
    
    public static async Task ShouldReturnSuccessWithTitle(this HttpResponseMessage response, string expectedTitle)
    {
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        
        var content = await response.Content.ReadAsStringAsync();
        content.ShouldContain($"<title>{expectedTitle}</title>", Case.Insensitive);
    }
}