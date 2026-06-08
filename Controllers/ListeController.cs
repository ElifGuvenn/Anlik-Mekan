using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AnlikMekanCore.Models;
using AnlikMekanCore.Models.Entities;

namespace AnlikMekanCore.Controllers;

[Authorize]
public class ListeController : Controller
{
    private readonly AppDbContext _db;
    private readonly UserManager<AppUser> _userManager;

    public ListeController(AppDbContext db, UserManager<AppUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index()
    {
        var user = await _userManager.GetUserAsync(User);
        var listeler = await _db.MekanListeleri
            .Include(l => l.Mekanlar)
            .Where(l => l.OlusturanId == user!.Id)
            .OrderByDescending(l => l.Olusturuldu)
            .ToListAsync();
        return View(listeler);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Olustur(string ad, string? aciklama, bool herkesAcik)
    {
        var user = await _userManager.GetUserAsync(User);
        _db.MekanListeleri.Add(new MekanListesi
        {
            OlusturanId = user!.Id, Ad = ad,
            Aciklama = aciklama ?? "", HerkesAcik = herkesAcik
        });
        await _db.SaveChangesAsync();
        TempData["Mesaj"] = $"\"{ad}\" listesi oluşturuldu.";
        return RedirectToAction("Index");
    }

    public async Task<IActionResult> Detay(int id)
    {
        var user = await _userManager.GetUserAsync(User);
        var liste = await _db.MekanListeleri.Include(l => l.Mekanlar)
            .FirstOrDefaultAsync(l => l.Id == id);
        if (liste == null) return NotFound();
        if (!liste.HerkesAcik && liste.OlusturanId != user?.Id) return Forbid();
        return View(liste);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MekanEkle(int listeId, int mekanId)
    {
        var user = await _userManager.GetUserAsync(User);
        var liste = await _db.MekanListeleri.Include(l => l.Mekanlar)
            .FirstOrDefaultAsync(l => l.Id == listeId && l.OlusturanId == user!.Id);
        var mekan = await _db.Mekanlar.FindAsync(mekanId);
        if (liste == null || mekan == null) return NotFound();
        if (liste.Mekanlar.Any(m => m.Id == mekanId))
            return Json(new { ok = false, mesaj = "Bu mekan zaten listede." });
        liste.Mekanlar.Add(mekan);
        await _db.SaveChangesAsync();
        return Json(new { ok = true, mesaj = $"\"{mekan.Ad}\" listeye eklendi." });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MekanCikar(int listeId, int mekanId)
    {
        var user = await _userManager.GetUserAsync(User);
        var liste = await _db.MekanListeleri.Include(l => l.Mekanlar)
            .FirstOrDefaultAsync(l => l.Id == listeId && l.OlusturanId == user!.Id);
        if (liste == null) return NotFound();
        var mekan = liste.Mekanlar.FirstOrDefault(m => m.Id == mekanId);
        if (mekan != null) liste.Mekanlar.Remove(mekan);
        await _db.SaveChangesAsync();
        return Json(new { ok = true });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Sil(int id)
    {
        var user = await _userManager.GetUserAsync(User);
        var liste = await _db.MekanListeleri.FirstOrDefaultAsync(l => l.Id == id && l.OlusturanId == user!.Id);
        if (liste == null) return NotFound();
        _db.MekanListeleri.Remove(liste);
        await _db.SaveChangesAsync();
        return RedirectToAction("Index");
    }
}
