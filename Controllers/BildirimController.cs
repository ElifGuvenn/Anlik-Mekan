using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AnlikMekanCore.Models;

namespace AnlikMekanCore.Controllers;

[Authorize]
public class BildirimController : Controller
{
    private readonly AppDbContext _db;
    private readonly UserManager<AppUser> _userManager;

    public BildirimController(AppDbContext db, UserManager<AppUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return RedirectToAction("Login", "Auth");

        var bildirimler = await _db.Bildirimler
            .Where(b => b.AliciId == user.Id)
            .OrderByDescending(b => b.Olusturuldu)
            .ToListAsync();

        await _db.Bildirimler
            .Where(b => b.AliciId == user.Id && !b.Okundu)
            .ExecuteUpdateAsync(s => s.SetProperty(b => b.Okundu, true));

        return View(bildirimler);
    }

    public async Task<IActionResult> Sayi()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Json(new { sayi = 0 });
        var sayi = await _db.Bildirimler.CountAsync(b => b.AliciId == user.Id && !b.Okundu);
        return Json(new { sayi });
    }
}
