using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NvhRealty.Web.Data;
using NvhRealty.Web.Models.Entities;
using NvhRealty.Web.Models.ViewModels;

namespace NvhRealty.Web.Controllers;

/// <summary>Üye girişi / kayıt ("Log In", "Create your profile") ve favoriler.</summary>
[Route("account")]
public class AccountController : Controller
{
    private readonly SignInManager<ApplicationUser> _signIn;
    private readonly UserManager<ApplicationUser> _users;
    private readonly AppDbContext _db;

    public AccountController(SignInManager<ApplicationUser> signIn, UserManager<ApplicationUser> users, AppDbContext db)
    {
        _signIn = signIn;
        _users = users;
        _db = db;
    }

    [HttpGet("login")]
    public IActionResult Login(string? returnUrl) => View(new LoginViewModel { ReturnUrl = returnUrl });

    [HttpPost("login"), ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid) return View(model);
        var result = await _signIn.PasswordSignInAsync(model.Email, model.Password, model.RememberMe, lockoutOnFailure: true);
        if (result.Succeeded)
            return model.ReturnUrl is { } r && Url.IsLocalUrl(r) ? LocalRedirect(r) : RedirectToAction(nameof(Favorites));
        ModelState.AddModelError("", result.IsLockedOut ? "Çok fazla hatalı deneme. Lütfen daha sonra tekrar deneyin." : "E-posta veya şifre hatalı.");
        return View(model);
    }

    [HttpGet("register")]
    public IActionResult Register() => View(new RegisterViewModel());

    [HttpPost("register"), ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid) return View(model);
        var user = new ApplicationUser
        {
            UserName = model.Email,
            Email = model.Email,
            FirstName = model.FirstName,
            LastName = model.LastName,
            InterestedIn = model.InterestedIn
        };
        var result = await _users.CreateAsync(user, model.Password);
        if (result.Succeeded)
        {
            await _signIn.SignInAsync(user, isPersistent: false);
            return model.InterestedIn == InterestType.Sales
                ? RedirectToAction("Sales", "Properties")
                : RedirectToAction("Rentals", "Properties");
        }
        foreach (var e in result.Errors) ModelState.AddModelError("", e.Description);
        return View(model);
    }

    [HttpPost("logout"), ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _signIn.SignOutAsync();
        return Redirect("/");
    }

    [Authorize, HttpGet("favorites")]
    public async Task<IActionResult> Favorites()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var listings = await _db.FavoriteListings.AsNoTracking()
            .Where(f => f.UserId == userId).OrderByDescending(f => f.CreatedAt)
            .Select(f => f.Listing!)
            .Include(l => l.Neighborhood).Include(l => l.Photos)
            .ToListAsync();
        return View(listings);
    }
}
