using WebKoy.Web.Services;

namespace WebKoy.Tests;

public class SocialServiceTests
{
    [Fact]
    public void ParseTags_normalizes_dedupes_and_limits()
    {
        var tags = SocialService.ParseTags("Sanat, #kedi kedi, x, oyun, müzik, retro, fazla, <script>");
        Assert.Equal(new[] { "sanat", "kedi", "oyun", "müzik", "retro" }, tags);
    }
}
