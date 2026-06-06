using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using AnlikMekanCore.Models;
using AnlikMekanCore.Models.Entities;
using AnlikMekanCore.Models.ViewModels;
using AnlikMekanCore.Services;

namespace AnlikMekanCore.Controllers;

public class MekanController : Controller
{
    private readonly AppDbContext _db;
    private readonly UserManager<AppUser> _userManager;
    private readonly CloudinaryService _cloudinary;

    public MekanController(AppDbContext db, UserManager<AppUser> userManager, CloudinaryService cloudinary)
    {
        _db = db;
        _userManager = userManager;
        _cloudinary = cloudinary;
    }

    public async Task<IActionResult> Detay(int id)
    {
        var mekan = await _db.Mekanlar
            .Include(m => m.Yorumlar).ThenInclude(y => y.Yazar)
            .Include(m => m.Yorumlar).ThenInclude(y => y.Fotolar)
            .Include(m => m.Yorumlar).ThenInclude(y => y.Begeniler)
            .Include(m => m.Yorumlar).ThenInclude(y => y.Yanit)
            .Include(m => m.Fotolar)
            .Include(m => m.Etkinlikler)
            .Include(m => m.Kampanyalar)
            .Include(m => m.CalismaGunleri)
            .Include(m => m.Sahibi)
            .FirstOrDefaultAsync(m => m.Id == id);

        if (mekan == null) return NotFound();

        var user = await _userManager.GetUserAsync(User);
        if (user != null)
            await _db.Mekanlar.Where(m => m.Id == id)
                .ExecuteUpdateAsync(s => s.SetProperty(m => m.GoruntulmeSayisi, m => m.GoruntulmeSayisi + 1));

        var kullaniciBegendigi = new HashSet<int>();
        var takipEdiyor = false;

        if (user != null)
        {
            kullaniciBegendigi = (await _db.YorumBegeniler
                .Where(yb => yb.KullaniciId == user.Id && yb.Yorum.MekanId == id)
                .Select(yb => yb.YorumId).ToListAsync()).ToHashSet();

            if (mekan.SahibiId != null)
                takipEdiyor = await _db.Takipler.AnyAsync(t =>
                    t.TakipciId == user.Id && t.TakipEdilenId == mekan.SahibiId);
        }

        var now = DateTime.UtcNow;
        var aktifKampanyalar = mekan.Kampanyalar.Where(k => k.Baslangic <= now && now <= k.Bitis).ToList();

        return View(new MekanDetayViewModel
        {
            Mekan = mekan,
            Yorumlar = mekan.Yorumlar.OrderByDescending(y => y.Tarih).ToList(),
            Etkinlikler = mekan.Etkinlikler.Where(e => e.Bitis >= now).OrderBy(e => e.Baslangic).ToList(),
            Fotolar = mekan.Fotolar.OrderBy(f => f.Siralama).ToList(),
            KullaniciBegendigi = kullaniciBegendigi,
            TakipEdiyor = takipEdiyor,
            AktifKampanyalar = aktifKampanyalar,
            BugunNo = ((int)DateTime.Now.DayOfWeek + 6) % 7
        });
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> YorumEkle(int id, YorumFormModel model)
    {
        if (!ModelState.IsValid) return RedirectToAction("Detay", new { id });

        var user = await _userManager.GetUserAsync(User);
        if (user == null) return RedirectToAction("Login", "Auth");

        var yorum = new Yorum
        {
            MekanId = id,
            YazarId = user.Id,
            Icerik = model.Icerik,
            Puan = model.Puan
        };
        _db.Yorumlar.Add(yorum);
        await _db.SaveChangesAsync();

        // Yorum fotoğrafları
        var dosyalar = Request.Form.Files.GetFiles("yorum_fotolar");
        foreach (var dosya in dosyalar)
        {
            if (dosya.ContentType.StartsWith("image/"))
            {
                var url = await _cloudinary.YukleAsync(dosya);
                if (url != null)
                    _db.YorumFotolar.Add(new YorumFoto { YorumId = yorum.Id, FotoUrl = url });
            }
        }
        await _db.SaveChangesAsync();

        return RedirectToAction("Detay", new { id });
    }

    // ── Harita ───────────────────────────────────────────────────────────────

    [Authorize]
    public async Task<IActionResult> Harita(string? sehir, string? kategori, string? acik,
        string? wifi, string? priz, string? bahce, string? pet, string? engelli,
        string? muzik, string? sigara, string? lat, string? lng)
    {
        var q = _db.Mekanlar.Include(m => m.Yorumlar).Include(m => m.CalismaGunleri)
            .Where(m => m.IsApproved);

        if (!string.IsNullOrEmpty(sehir)) q = q.Where(m => m.Sehir == sehir);
        if (!string.IsNullOrEmpty(kategori)) q = q.Where(m => m.Kategori == kategori);
        if (!string.IsNullOrEmpty(wifi)) q = q.Where(m => m.WifiVar);
        if (!string.IsNullOrEmpty(priz)) q = q.Where(m => m.PrizVar);
        if (!string.IsNullOrEmpty(bahce)) q = q.Where(m => m.BahceVar);
        if (!string.IsNullOrEmpty(pet)) q = q.Where(m => m.EvcilHayvanIzinli);
        if (!string.IsNullOrEmpty(engelli)) q = q.Where(m => m.EngelliErizimiVar);
        if (!string.IsNullOrEmpty(muzik)) q = q.Where(m => m.CanliMuzikVar);
        if (!string.IsNullOrEmpty(sigara)) q = q.Where(m => m.SigaraIcinUygun);

        var mekanlar = await q.ToListAsync();

        if (!string.IsNullOrEmpty(acik))
            mekanlar = mekanlar.Where(m => m.AcikMi).ToList();

        // Konuma göre sıralama
        if (double.TryParse(lat, out var ulat) && double.TryParse(lng, out var ulng))
        {
            foreach (var m in mekanlar)
                m.GoruntulmeSayisi = m.Latitude.HasValue
                    ? (int)(HaversineM(ulat, ulng, (double)m.Latitude, (double)m.Longitude!.Value))
                    : 999999;
            mekanlar = mekanlar.OrderBy(m => m.GoruntulmeSayisi).ToList();
        }

        var haritaVerisi = mekanlar
            .Where(m => m.Latitude.HasValue && m.Longitude.HasValue)
            .Select(m => new HaritaMekanJson
            {
                Id = m.Id, Ad = m.Ad, Kategori = m.Kategori,
                KategoriDisplay = m.KategoriAdi, Adres = m.Adres,
                Lat = (double)m.Latitude!, Lng = (double)m.Longitude!,
                SuAnAcik = m.AcikMi, DolulukOrani = m.DolulukOrani,
                OrtPuan = m.OrtalamaYorum,
                YorumSayisi = m.Yorumlar.Count,
                DetayUrl = Url.Action("Detay", "Mekan", new { id = m.Id }) ?? "",
                ImgUrl = m.ImgUrl, WifiVar = m.WifiVar, PrizVar = m.PrizVar,
                BahceVar = m.BahceVar, EvcilHayvanIzinli = m.EvcilHayvanIzinli,
                EngelliErizimiVar = m.EngelliErizimiVar, CanliMuzikVar = m.CanliMuzikVar,
                SigaraIcinUygun = m.SigaraIcinUygun
            }).ToList();

        ViewBag.HaritaVerisiJson = JsonSerializer.Serialize(haritaVerisi,
            new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        ViewBag.ToplamMekan = mekanlar.Count;
        ViewBag.HaritadaMekan = haritaVerisi.Count;
        ViewBag.AktifKonum = !string.IsNullOrEmpty(lat) && !string.IsNullOrEmpty(lng);
        ViewBag.UserLat = lat ?? "";
        ViewBag.UserLng = lng ?? "";

        return View(mekanlar);
    }

    // ── Etkinlik Takvimi ─────────────────────────────────────────────────────

    [Authorize]
    public async Task<IActionResult> Takvim(int? mekanId)
    {
        var etkinlikler = await _db.Etkinlikler
            .Include(e => e.Mekan)
            .Where(e => e.Bitis >= DateTime.UtcNow.AddMonths(-1))
            .OrderBy(e => e.Baslangic)
            .ToListAsync();

        if (mekanId.HasValue)
            etkinlikler = etkinlikler.Where(e => e.MekanId == mekanId.Value).ToList();

        var mekanlar = await _db.Mekanlar
            .Where(m => m.IsApproved)
            .Select(m => new { m.Id, m.Ad })
            .OrderBy(m => m.Ad)
            .ToListAsync();

        var etkinlikJson = etkinlikler.Select(e => new {
            id = e.Id,
            title = e.Baslik,
            start = e.Baslangic.ToString("yyyy-MM-ddTHH:mm:ss"),
            end = e.Bitis.ToString("yyyy-MM-ddTHH:mm:ss"),
            mekanAd = e.Mekan.Ad,
            mekanId = e.MekanId,
            aciklama = e.Aciklama,
            fotoUrl = e.FotoUrl,
            detayUrl = Url.Action("Detay", "Mekan", new { id = e.MekanId }) ?? "",
            color = e.Bitis < DateTime.UtcNow ? "#94a3b8" : "#2563eb"
        }).ToList();

        ViewBag.EtkinlikJson = JsonSerializer.Serialize(etkinlikJson, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        ViewBag.Mekanlar = mekanlar;
        ViewBag.SecilenMekanId = mekanId;
        return View();
    }

    private static double HaversineM(double lat1, double lng1, double lat2, double lng2)
    {
        const double R = 6371000;
        var dLat = (lat2 - lat1) * Math.PI / 180;
        var dLng = (lng2 - lng1) * Math.PI / 180;
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(lat1 * Math.PI / 180) * Math.Cos(lat2 * Math.PI / 180) *
                Math.Sin(dLng / 2) * Math.Sin(dLng / 2);
        return R * 2 * Math.Asin(Math.Sqrt(a));
    }
}
