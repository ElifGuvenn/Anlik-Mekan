using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AnlikMekanCore.Models;
using AnlikMekanCore.Models.Entities;

namespace AnlikMekanCore.Controllers;

[Authorize]
public class RezervasyonController : Controller
{
    private readonly AppDbContext _db;
    private readonly UserManager<AppUser> _userManager;

    public RezervasyonController(AppDbContext db, UserManager<AppUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index()
    {
        var user = await _userManager.GetUserAsync(User);
        var rezervasyonlar = await _db.Rezervasyonlar
            .Include(r => r.Mekan)
            .Where(r => r.KullaniciId == user!.Id)
            .OrderByDescending(r => r.Tarih).ThenByDescending(r => r.Saat)
            .ToListAsync();
        return View(rezervasyonlar);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Olustur(int mekanId, DateOnly tarih, TimeOnly saat,
        int kisiSayisi, string? notMesaj)
    {
        var user = await _userManager.GetUserAsync(User);
        var mekan = await _db.Mekanlar.FirstOrDefaultAsync(m => m.Id == mekanId && m.IsApproved);
        if (mekan == null || !mekan.RezervasyonAktif)
        {
            TempData["Hata"] = "Bu mekan şu an rezervasyon kabul etmiyor.";
            return RedirectToAction("Detay", "Mekan", new { id = mekanId });
        }

        if (tarih < DateOnly.FromDateTime(DateTime.Today))
        {
            TempData["Hata"] = "Geçmiş bir tarihe rezervasyon yapamazsınız.";
            return RedirectToAction("Detay", "Mekan", new { id = mekanId });
        }

        _db.Rezervasyonlar.Add(new Rezervasyon
        {
            MekanId = mekanId, KullaniciId = user!.Id,
            Tarih = tarih, Saat = saat,
            KisiSayisi = kisiSayisi, NotMesaj = notMesaj ?? ""
        });
        await _db.SaveChangesAsync();

        TempData["Mesaj"] = "Rezervasyon talebiniz gönderildi!";
        return RedirectToAction("Detay", "Mekan", new { id = mekanId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Iptal(int id)
    {
        var user = await _userManager.GetUserAsync(User);
        var rez = await _db.Rezervasyonlar
            .FirstOrDefaultAsync(r => r.Id == id && r.KullaniciId == user!.Id);
        if (rez == null) return NotFound();

        if (rez.Durum != "BEKLIYOR")
        {
            TempData["Hata"] = "Sadece bekleyen rezervasyonlar iptal edilebilir.";
            return RedirectToAction("Index");
        }

        rez.Durum = "IPTAL";
        await _db.SaveChangesAsync();
        TempData["Mesaj"] = "Rezervasyonunuz iptal edildi.";
        return RedirectToAction("Index");
    }
}
