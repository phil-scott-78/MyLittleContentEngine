using Shouldly;

namespace MyLittleContentEngine.IntegrationTests;

public class BasicTest
{
    [Fact]
    public void TestFramework_ShouldWork()
    {
        true.ShouldBeTrue();
    }
}