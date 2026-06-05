using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AnlikMekanCore.Models;
using AnlikMekanCore.Models.Entities;
using AnlikMekanCore.Services;

namespace AnlikMekanCore.Controllers;

[Authorize]
public class YorumController : Controller
{
    private readonly AppDbContext _db;
    private readonly UserManager<AppUser> _userManager;
    private readonly CloudinaryService _cloudinary;

    public YorumController(AppDbContext db, UserManager<AppUser> userManager, CloudinaryService cloudinary)
    {
        _db = db;
        _userManager = userManager;
        _cloudinary = cloudinary;
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Sil(int id)
    {
        var user = await _userManager.GetUserAsync(User);
        var yorum = await _db.Yorumlar.FirstOrDefaultAsync(y => y.Id == id && y.YazarId == user!.Id);
        if (yorum == null) return NotFound();
        var mekanId = yorum.MekanId;
        _db.Yorumlar.Remove(yorum);
        await _db.SaveChangesAsync();
        return RedirectToAction("Detay", "Mekan", new { id = mekanId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Begen(int id)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Unauthorized();

        var yorum = await _db.Yorumlar.Include(y => y.Begeniler).FirstOrDefaultAsync(y => y.Id == id);
        if (yorum == null) return NotFound();

        var mevcut = yorum.Begeniler.FirstOrDefault(b => b.KullaniciId == user.Id);
        bool begendi;
        if (mevcut != null) { _db.YorumBegeniler.Remove(mevcut); begendi = false; }
        else { _db.YorumBegeniler.Add(new YorumBegeni { YorumId = id, KullaniciId = user.Id }); begendi = true; }

        await _db.SaveChangesAsync();
        var sayi = await _db.YorumBegeniler.CountAsync(b => b.YorumId == id);
        return Json(new { begendi, sayi });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Yanit(int yorumId, string icerik)
    {
        var user = await _userManager.GetUserAsync(User);
        var yorum = await _db.Yorumlar.Include(y => y.Mekan).FirstOrDefaultAsync(y => y.Id == yorumId);
        if (yorum == null || yorum.Mekan.SahibiId != user?.Id) return Forbid();

        var mevcutYanit = await _db.YorumYanitlar.FirstOrDefaultAsync(y => y.YorumId == yorumId);
        if (mevcutYanit != null) { mevcutYanit.Icerik = icerik; mevcutYanit.Guncellendi = DateTime.UtcNow; }
        else _db.YorumYanitlar.Add(new YorumYanit { YorumId = yorumId, YazanId = user!.Id, Icerik = icerik });

        await _db.SaveChangesAsync();
        return RedirectToAction("Detay", "Mekan", new { id = yorum.MekanId });
    }
}
