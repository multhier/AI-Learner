using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NvhRealty.Web.Data;
using NvhRealty.Web.Models.Entities;
using NvhRealty.Web.Models.ViewModels;

namespace NvhRealty.Web.Controllers;

/// <summary>İletişim, Kariyer (Interview Application) ve "List With Us" (owners) formları.</summary>
public class FormsController : Controller
{
    private readonly AppDbContext _db;
    public FormsController(AppDbContext db) => _db = db;

    private static bool IsBot(BotProtectedForm f) => !string.IsNullOrEmpty(f.Website);

    [HttpGet("contact")]
    public IActionResult Contact() => View(new ContactFormModel());

    [HttpPost("contact"), ValidateAntiForgeryToken]
    public async Task<IActionResult> Contact(ContactFormModel model)
    {
        var returnUrl = model.ReturnUrl is { } r && Url.IsLocalUrl(r) ? r : null;
        if (!ModelState.IsValid)
        {
            if (returnUrl is null) return View(model);
            TempData["Error"] = "Lütfen formdaki zorunlu alanları doldurun.";
            return LocalRedirect(returnUrl);
        }

        if (!IsBot(model))
        {
            // Yalnızca gerçekten var olan danışman/ilan kayıtlarına bağla
            int? agentId = model.AgentId is int a && await _db.Agents.AnyAsync(x => x.Id == a) ? a : null;
            int? listingId = model.ListingId is int l && await _db.Listings.AnyAsync(x => x.Id == l) ? l : null;
            _db.ContactMessages.Add(new ContactMessage
            {
                Type = listingId != null ? MessageType.ListingInquiry : agentId != null ? MessageType.AgentContact : MessageType.Contact,
                Name = model.Name,
                Email = model.Email,
                Phone = model.Phone,
                Message = model.Message,
                AgentId = agentId,
                ListingId = listingId
            });
            await _db.SaveChangesAsync();
        }

        TempData["Success"] = "Mesajınız alındı. En kısa sürede size dönüş yapacağız.";
        return returnUrl is null ? RedirectToAction(nameof(Contact)) : LocalRedirect(returnUrl);
    }

    [HttpGet("owners")]
    public async Task<IActionResult> Owners()
    {
        ViewBag.Page = await _db.ContentPages.AsNoTracking().FirstOrDefaultAsync(p => p.Slug == "owners");
        return View(new OwnerFormModel());
    }

    [HttpPost("owners"), ValidateAntiForgeryToken]
    public async Task<IActionResult> Owners(OwnerFormModel model)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.Page = await _db.ContentPages.AsNoTracking().FirstOrDefaultAsync(p => p.Slug == "owners");
            return View(model);
        }
        if (!IsBot(model))
        {
            _db.ContactMessages.Add(new ContactMessage
            {
                Type = MessageType.ListWithUs,
                Name = model.Name,
                Email = model.Email,
                Phone = model.Phone,
                PropertyAddress = model.PropertyAddress,
                Message = $"[{(model.ListingType == ListingType.Sale ? "Satış" : "Kiralama")}] {model.Message}"
            });
            await _db.SaveChangesAsync();
        }
        TempData["Success"] = "Talebiniz alındı. Danışmanlarımız mülkünüzle ilgili sizinle iletişime geçecek.";
        return RedirectToAction(nameof(Owners));
    }

    [HttpGet("careers")]
    public async Task<IActionResult> Careers()
    {
        ViewBag.Page = await _db.ContentPages.AsNoTracking().FirstOrDefaultAsync(p => p.Slug == "careers");
        return View(new CareerFormModel());
    }

    [HttpPost("careers"), ValidateAntiForgeryToken]
    public async Task<IActionResult> Careers(CareerFormModel model)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.Page = await _db.ContentPages.AsNoTracking().FirstOrDefaultAsync(p => p.Slug == "careers");
            return View(model);
        }
        if (!IsBot(model))
        {
            _db.CareerApplications.Add(new CareerApplication
            {
                FirstName = model.FirstName,
                LastName = model.LastName,
                Email = model.Email,
                Phone = model.Phone,
                LicenseStatus = model.LicenseStatus,
                Message = model.Message
            });
            await _db.SaveChangesAsync();
        }
        TempData["Success"] = "Başvurunuz alındı. Teşekkür ederiz!";
        return RedirectToAction(nameof(Careers));
    }
}
