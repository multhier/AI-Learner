using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NvhRealty.Web.Data;

namespace NvhRealty.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = DbSeeder.AdminRole)]
[AutoValidateAntiforgeryToken]
public abstract class AdminControllerBase : Controller
{
    protected void Flash(string message) => TempData["Success"] = message;
}
