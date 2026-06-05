using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AnlikMekanCore.Models;
using AnlikMekanCore.Models.Entities;
using AnlikMekanCore.Services;

namespace AnlikMekanCore.Controllers;

[Authorize]
public class ProfilController : Controller
{
    private readonly AppDbContext _db;
    private readonly UserManager<AppUser> _userManager;
    private readonly CloudinaryService _cloudinary;

    public ProfilController(AppDbContext db, UserManager<AppUser> userManager, CloudinaryService cloudinary)
    {
        _db = db;
        _userManager = userManager;
        _cloudinary = cloudinary;
    }

    public async Task<IActionResult> Index()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return RedirectToAction("Login", "Auth");

        var yorumlar = await _db.Yorumlar
            .Include(y => y.Mekan)
            .Where(y => y.YazarId == user.Id)
            .OrderByDescending(y => y.Tarih)
            .ToListAsync();

        var favoriler = await _db.Mekanlar
            .Where(m => m.Favorileyenler.Any(u => u.Id == user.Id))
            .ToListAsync();

        ViewBag.Kullanici = user;
        ViewBag.Yorumlar = yorumlar;
        ViewBag.Favoriler = favoriler;
        ViewBag.YorumSayisi = yorumlar.Count;
        ViewBag.FavoriSayisi = favoriler.Count;

        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(string islem, string? email, IFormFile? foto)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return RedirectToAction("Login", "Auth");

        if (islem == "email" && !string.IsNullOrWhiteSpace(email))
        {
            user.Email = email;
            user.NormalizedEmail = email.ToUpperInvariant();
            await _userManager.UpdateAsync(user);
            TempData["Mesaj"] = "E-posta güncellendi.";
        }
        else if (islem == "foto" && foto != null)
        {
            var url = await _cloudinary.YukleAsync(foto);
            if (url != null)
            {
                user.FotoUrl = url;
                await _userManager.UpdateAsync(user);
                TempData["Mesaj"] = "Profil fotoğrafı güncellendi.";
            }
        }

        return RedirectToAction("Index");
    }
}
