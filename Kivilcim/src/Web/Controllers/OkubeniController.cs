using Microsoft.AspNetCore.Mvc;

namespace Kivilcim.Web.Controllers;

/// <summary>"Beni oku" sayfası: projenin nasıl yapıldığını adım adım anlatır.</summary>
public class OkubeniController : KivilcimController
{
    [HttpGet("/okubeni")]
    public IActionResult Index() => View();
}
