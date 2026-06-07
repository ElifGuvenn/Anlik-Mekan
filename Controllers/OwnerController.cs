using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AnlikMekanCore.Models;
using AnlikMekanCore.Models.Entities;
using AnlikMekanCore.Models.ViewModels;
using AnlikMekanCore.Services;

namespace AnlikMekanCore.Controllers;

[Authorize]
public class OwnerController : Controller
{
    private readonly AppDbContext _db;
    private readonly UserManager<AppUser> _userManager;
    private readonly CloudinaryService _cloudinary;

    public OwnerController(AppDbContext db, UserManager<AppUser> userManager, CloudinaryService cloudinary)
    {
        _db = db;
        _userManager = userManager;
        _cloudinary = cloudinary;
    }

    private async Task<AppUser?> GetOwnerAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        return user?.Rol == "OWNER" ? user : null;
    }

    public async Task<IActionResult> Dashboard()
    {
        var user = await GetOwnerAsync();
        if (user == null) return RedirectToAction("Dashboard", "Home");

        var mekanlar = await _db.Mekanlar
            .Include(m => m.Yorumlar)
            .Include(m => m.Etkinlikler)
            .Include(m => m.Rezervasyonlar).ThenInclude(r => r.Kullanici)
            .Include(m => m.Kampanyalar)
            .Include(m => m.CalismaGunleri)
            .Include(m => m.Fotolar)
            .Include(m => m.Favorileyenler)
            .Where(m => m.SahibiId == user.Id)
            .ToListAsync();

        return View(mekanlar);
    }

    // ── Mekan CRUD ────────────────────────────────────────────────────────────

    [HttpGet]
    public async Task<IActionResult> MekanOlustur()
    {
        var user = await GetOwnerAsync();
        if (user == null) return RedirectToAction("Dashboard", "Home");
        if (!user.TwoFaDogrulandi) return RedirectToAction("QrKodOlustur", "Auth");
        return View(new MekanFormModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MekanOlustur(MekanFormModel model)
    {
        var user = await GetOwnerAsync();
        if (user == null) return RedirectToAction("Dashboard", "Home");
        if (!ModelState.IsValid) return View(model);

        var imgUrl = model.Img != null ? await _cloudinary.YukleAsync(model.Img) : null;
        var ruhsatUrl = model.RuhsatBelgesi != null ? await _cloudinary.YukleAsync(model.RuhsatBelgesi) : null;

        var mekan = new Mekan
        {
            Ad = model.Ad, Kategori = model.Kategori, Sehir = model.Sehir,
            Adres = model.Adres, Telefon = model.Telefon, Website = model.Website,
            Latitude = model.Latitude, Longitude = model.Longitude,
            ImgUrl = imgUrl, SahibiId = user.Id, RuhsatBelgesiUrl = ruhsatUrl,
            SuAnAcik = model.SuAnAcik, DolulukOrani = model.DolulukOrani,
            AnlikDuyuru = model.AnlikDuyuru, RezervasyonAktif = model.RezervasyonAktif,
            AcilisSaati = !string.IsNullOrEmpty(model.AcilisSaati) ? TimeOnly.Parse(model.AcilisSaati) : null,
            KapanisSaati = !string.IsNullOrEmpty(model.KapanisSaati) ? TimeOnly.Parse(model.KapanisSaati) : null,
            WifiVar = model.WifiVar, PrizVar = model.PrizVar,
            CalismaAlaniVar = model.CalismaAlaniVar, OtoparkVar = model.OtoparkVar,
            SigaraIcinUygun = model.SigaraIcinUygun, BahceVar = model.BahceVar,
            EngelliErizimiVar = model.EngelliErizimiVar, CanliMuzikVar = model.CanliMuzikVar,
            EvcilHayvanIzinli = model.EvcilHayvanIzinli, CocukOyunAlaniVar = model.CocukOyunAlaniVar
        };

        _db.Mekanlar.Add(mekan);
        await _db.SaveChangesAsync();

        TempData["Mesaj"] = $"\"{mekan.Ad}\" eklendi. Admin onayından sonra yayına alınacak.";
        return RedirectToAction("Dashboard");
    }

    [HttpGet]
    public async Task<IActionResult> MekanDuzenle(int id)
    {
        var user = await GetOwnerAsync();
        if (user == null) return RedirectToAction("Dashboard", "Home");

        var mekan = await _db.Mekanlar.FirstOrDefaultAsync(m => m.Id == id && m.SahibiId == user.Id);
        if (mekan == null) return NotFound();

        return View(new MekanFormModel
        {
            Ad = mekan.Ad, Kategori = mekan.Kategori, Sehir = mekan.Sehir,
            Adres = mekan.Adres, Telefon = mekan.Telefon, Website = mekan.Website,
            Latitude = mekan.Latitude, Longitude = mekan.Longitude,
            SuAnAcik = mekan.SuAnAcik, DolulukOrani = mekan.DolulukOrani,
            AnlikDuyuru = mekan.AnlikDuyuru, RezervasyonAktif = mekan.RezervasyonAktif,
            AcilisSaati = mekan.AcilisSaati?.ToString("HH:mm"),
            KapanisSaati = mekan.KapanisSaati?.ToString("HH:mm"),
            WifiVar = mekan.WifiVar, PrizVar = mekan.PrizVar,
            CalismaAlaniVar = mekan.CalismaAlaniVar, OtoparkVar = mekan.OtoparkVar,
            SigaraIcinUygun = mekan.SigaraIcinUygun, BahceVar = mekan.BahceVar,
            EngelliErizimiVar = mekan.EngelliErizimiVar, CanliMuzikVar = mekan.CanliMuzikVar,
            EvcilHayvanIzinli = mekan.EvcilHayvanIzinli, CocukOyunAlaniVar = mekan.CocukOyunAlaniVar
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MekanDuzenle(int id, MekanFormModel model)
    {
        var user = await GetOwnerAsync();
        if (user == null) return RedirectToAction("Dashboard", "Home");

        var mekan = await _db.Mekanlar.FirstOrDefaultAsync(m => m.Id == id && m.SahibiId == user.Id);
        if (mekan == null) return NotFound();
        if (!ModelState.IsValid) return View(model);

        mekan.Ad = model.Ad; mekan.Kategori = model.Kategori; mekan.Sehir = model.Sehir;
        mekan.Adres = model.Adres; mekan.Telefon = model.Telefon; mekan.Website = model.Website;
        mekan.Latitude = model.Latitude; mekan.Longitude = model.Longitude;
        mekan.SuAnAcik = model.SuAnAcik; mekan.DolulukOrani = model.DolulukOrani;
        mekan.AnlikDuyuru = model.AnlikDuyuru; mekan.RezervasyonAktif = model.RezervasyonAktif;
        mekan.AcilisSaati = !string.IsNullOrEmpty(model.AcilisSaati) ? TimeOnly.Parse(model.AcilisSaati) : null;
        mekan.KapanisSaati = !string.IsNullOrEmpty(model.KapanisSaati) ? TimeOnly.Parse(model.KapanisSaati) : null;
        mekan.WifiVar = model.WifiVar; mekan.PrizVar = model.PrizVar;
        mekan.CalismaAlaniVar = model.CalismaAlaniVar; mekan.OtoparkVar = model.OtoparkVar;
        mekan.SigaraIcinUygun = model.SigaraIcinUygun; mekan.BahceVar = model.BahceVar;
        mekan.EngelliErizimiVar = model.EngelliErizimiVar; mekan.CanliMuzikVar = model.CanliMuzikVar;
        mekan.EvcilHayvanIzinli = model.EvcilHayvanIzinli; mekan.CocukOyunAlaniVar = model.CocukOyunAlaniVar;

        if (model.Img != null)
            mekan.ImgUrl = await _cloudinary.YukleAsync(model.Img);
        if (model.RuhsatBelgesi != null)
            mekan.RuhsatBelgesiUrl = await _cloudinary.YukleAsync(model.RuhsatBelgesi);

        await _db.SaveChangesAsync();
        TempData["Mesaj"] = $"\"{mekan.Ad}\" güncellendi.";
        return RedirectToAction("Dashboard");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MekanSil(int id)
    {
        var user = await GetOwnerAsync();
        if (user == null) return RedirectToAction("Dashboard", "Home");

        var mekan = await _db.Mekanlar.FirstOrDefaultAsync(m => m.Id == id && m.SahibiId == user.Id);
        if (mekan == null) return NotFound();

        _db.Mekanlar.Remove(mekan);
        await _db.SaveChangesAsync();
        TempData["Mesaj"] = $"\"{mekan.Ad}\" silindi.";
        return RedirectToAction("Dashboard");
    }

    // ── AJAX Durum Güncelle ────────────────────────────────────────────────────

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DurumGuncelle(int id, string alan, string deger)
    {
        var user = await GetOwnerAsync();
        if (user == null) return Unauthorized();

        var mekan = await _db.Mekanlar.FirstOrDefaultAsync(m => m.Id == id && m.SahibiId == user.Id);
        if (mekan == null) return NotFound();

        if (alan == "su_an_acik")
        {
            mekan.SuAnAcik = deger == "true";
            await _db.SaveChangesAsync();
            return Json(new { ok = true, deger = mekan.SuAnAcik });
        }
        if (alan == "doluluk_orani" && int.TryParse(deger, out var doluluk))
        {
            mekan.DolulukOrani = Math.Clamp(doluluk, 0, 100);
            await _db.SaveChangesAsync();
            return Json(new { ok = true, deger = mekan.DolulukOrani });
        }
        if (alan == "anlik_duyuru")
        {
            mekan.AnlikDuyuru = deger;
            await _db.SaveChangesAsync();
            return Json(new { ok = true });
        }

        return BadRequest(new { ok = false, hata = "Bilinmeyen alan" });
    }

    // ── Rezervasyon ────────────────────────────────────────────────────────────

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RezervasyonToggle(int mekanId)
    {
        var user = await GetOwnerAsync();
        if (user == null) return RedirectToAction("Dashboard", "Home");

        var mekan = await _db.Mekanlar.FirstOrDefaultAsync(m => m.Id == mekanId && m.SahibiId == user.Id);
        if (mekan == null) return NotFound();

        mekan.RezervasyonAktif = !mekan.RezervasyonAktif;
        await _db.SaveChangesAsync();
        TempData["Mesaj"] = $"Rezervasyon alma {(mekan.RezervasyonAktif ? "açıldı" : "kapatıldı")}.";
        return RedirectToAction("Dashboard");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RezervasyonGuncelle(int rezervasyonId, string durum)
    {
        var user = await GetOwnerAsync();
        if (user == null) return RedirectToAction("Dashboard", "Home");

        var rez = await _db.Rezervasyonlar
            .Include(r => r.Mekan).FirstOrDefaultAsync(r => r.Id == rezervasyonId && r.Mekan.SahibiId == user.Id);
        if (rez == null) return NotFound();

        if (durum is "ONAYLANDI" or "REDDEDILDI")
        {
            rez.Durum = durum;
            await _db.SaveChangesAsync();

            var tip = durum == "ONAYLANDI" ? "REZERVASYON" : "REZERVASYON";
            var msg = durum == "ONAYLANDI"
                ? $"Rezervasyonunuz onaylandı: {rez.Mekan.Ad}, {rez.Tarih:dd MMM}."
                : $"Rezervasyonunuz reddedildi: {rez.Mekan.Ad}, {rez.Tarih:dd MMM}.";
            await BildirimHelper.OlusturAsync(_db, rez.KullaniciId, tip, msg,
                "/Rezervasyon/Index");
        }
        return RedirectToAction("Dashboard");
    }

    // ── Çalışma Saatleri ─────────────────────────────────────────────────────

    [HttpGet]
    public async Task<IActionResult> CalismaSaatleri(int mekanId)
    {
        var user = await GetOwnerAsync();
        if (user == null) return RedirectToAction("Dashboard", "Home");

        var mekan = await _db.Mekanlar.Include(m => m.CalismaGunleri)
            .FirstOrDefaultAsync(m => m.Id == mekanId && m.SahibiId == user.Id);
        if (mekan == null) return NotFound();

        return View(mekan);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CalismaSaatleri(int mekanId, IFormCollection form)
    {
        var user = await GetOwnerAsync();
        if (user == null) return RedirectToAction("Dashboard", "Home");

        var mekan = await _db.Mekanlar.Include(m => m.CalismaGunleri)
            .FirstOrDefaultAsync(m => m.Id == mekanId && m.SahibiId == user.Id);
        if (mekan == null) return NotFound();

        for (int gunNo = 0; gunNo < 7; gunNo++)
        {
            var acik = form[$"gun_{gunNo}_acik"] == "on";
            var acilis = form[$"gun_{gunNo}_acilis"].ToString();
            var kapanis = form[$"gun_{gunNo}_kapanis"].ToString();

            var gun = mekan.CalismaGunleri.FirstOrDefault(g => g.Gun == gunNo);
            if (gun == null)
            {
                gun = new CalismaGunu { MekanId = mekanId, Gun = gunNo };
                _db.CalismaGunleri.Add(gun);
            }
            gun.Acik = acik;
            gun.Acilis = !string.IsNullOrEmpty(acilis) ? TimeOnly.Parse(acilis) : null;
            gun.Kapanis = !string.IsNullOrEmpty(kapanis) ? TimeOnly.Parse(kapanis) : null;
        }

        await _db.SaveChangesAsync();
        TempData["Mesaj"] = "Çalışma saatleri güncellendi.";
        return RedirectToAction("Dashboard");
    }

    // ── Kampanya ──────────────────────────────────────────────────────────────

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> KampanyaOlustur(int mekanId, string baslik, string aciklama,
        DateTime baslangic, DateTime bitis, IFormFile? foto)
    {
        var user = await GetOwnerAsync();
        if (user == null) return RedirectToAction("Dashboard", "Home");

        var mekan = await _db.Mekanlar.FirstOrDefaultAsync(m => m.Id == mekanId && m.SahibiId == user.Id);
        if (mekan == null) return NotFound();

        var fotoUrl = foto != null ? await _cloudinary.YukleAsync(foto) : null;

        _db.Kampanyalar.Add(new Kampanya
        {
            MekanId = mekanId, Baslik = baslik, Aciklama = aciklama,
            Baslangic = baslangic, Bitis = bitis, FotoUrl = fotoUrl
        });
        await _db.SaveChangesAsync();

        // Mekan sahibini takip eden kullanıcılara bildirim
        await BildirimHelper.TakipcilareBildirimAsync(_db, user.Id, "KAMPANYA",
            $"{mekan.Ad} yeni kampanya ekledi: {baslik}",
            $"/Mekan/Detay/{mekanId}");

        TempData["Mesaj"] = "Kampanya oluşturuldu.";
        return RedirectToAction("Dashboard");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> KampanyaSil(int kampanyaId)
    {
        var user = await GetOwnerAsync();
        if (user == null) return RedirectToAction("Dashboard", "Home");

        var k = await _db.Kampanyalar.Include(k => k.Mekan)
            .FirstOrDefaultAsync(k => k.Id == kampanyaId && k.Mekan.SahibiId == user.Id);
        if (k == null) return NotFound();

        _db.Kampanyalar.Remove(k);
        await _db.SaveChangesAsync();
        return RedirectToAction("Dashboard");
    }

    // ── Etkinlik ──────────────────────────────────────────────────────────────

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EtkinlikOlustur(int mekanId, string baslik, string? aciklama,
        DateTime baslangic, DateTime bitis, IFormFile? foto)
    {
        var user = await GetOwnerAsync();
        if (user == null) return RedirectToAction("Dashboard", "Home");

        var mekan = await _db.Mekanlar.FirstOrDefaultAsync(m => m.Id == mekanId && m.SahibiId == user.Id);
        if (mekan == null) return NotFound();

        var fotoUrl = foto != null ? await _cloudinary.YukleAsync(foto) : null;

        _db.Etkinlikler.Add(new Etkinlik
        {
            MekanId = mekanId, Baslik = baslik, Aciklama = aciklama ?? "",
            Baslangic = baslangic, Bitis = bitis, FotoUrl = fotoUrl
        });
        await _db.SaveChangesAsync();

        TempData["Mesaj"] = "Etkinlik eklendi.";
        return RedirectToAction("Dashboard");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EtkinlikSil(int etkinlikId)
    {
        var user = await GetOwnerAsync();
        if (user == null) return RedirectToAction("Dashboard", "Home");

        var e = await _db.Etkinlikler.Include(e => e.Mekan)
            .FirstOrDefaultAsync(e => e.Id == etkinlikId && e.Mekan.SahibiId == user.Id);
        if (e == null) return NotFound();

        _db.Etkinlikler.Remove(e);
        await _db.SaveChangesAsync();
        return RedirectToAction("Dashboard");
    }

    // ── Galeri ────────────────────────────────────────────────────────────────

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> FotoYukle(int mekanId)
    {
        var user = await GetOwnerAsync();
        if (user == null) return RedirectToAction("Dashboard", "Home");

        var mekan = await _db.Mekanlar.FirstOrDefaultAsync(m => m.Id == mekanId && m.SahibiId == user.Id);
        if (mekan == null) return NotFound();

        var dosyalar = Request.Form.Files.GetFiles("fotolar");
        foreach (var dosya in dosyalar)
        {
            if (!dosya.ContentType.StartsWith("image/")) continue;
            var url = await _cloudinary.YukleAsync(dosya);
            if (url != null)
                _db.MekanFotolar.Add(new MekanFoto { MekanId = mekanId, FotoUrl = url });
        }
        await _db.SaveChangesAsync();

        TempData["Mesaj"] = "Fotoğraflar yüklendi.";
        return RedirectToAction("Dashboard");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> FotoSil(int fotoId)
    {
        var user = await GetOwnerAsync();
        if (user == null) return RedirectToAction("Dashboard", "Home");

        var foto = await _db.MekanFotolar.Include(f => f.Mekan)
            .FirstOrDefaultAsync(f => f.Id == fotoId && f.Mekan.SahibiId == user.Id);
        if (foto == null) return NotFound();

        _db.MekanFotolar.Remove(foto);
        await _db.SaveChangesAsync();
        return RedirectToAction("Dashboard");
    }

    // ── Menü ─────────────────────────────────────────────────────────────────

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MenuYukle(int mekanId, IFormFile? menu_foto, IFormFile? menu_pdf)
    {
        var user = await GetOwnerAsync();
        if (user == null) return RedirectToAction("Dashboard", "Home");

        var mekan = await _db.Mekanlar.FirstOrDefaultAsync(m => m.Id == mekanId && m.SahibiId == user.Id);
        if (mekan == null) return NotFound();

        if (menu_foto != null)
            mekan.MenuFotoUrl = await _cloudinary.YukleAsync(menu_foto);
        if (menu_pdf != null)
            mekan.MenuPdfUrl = await _cloudinary.YukleAsync(menu_pdf);

        await _db.SaveChangesAsync();
        TempData["Mesaj"] = "Menü güncellendi.";
        return RedirectToAction("Dashboard");
    }
}
