using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using AnlikMekanCore.Models;
using AnlikMekanCore.Models.Entities;
using AnlikMekanCore.Models.ViewModels;
using AnlikMekanCore.Services;
using System.Text.Json.Serialization;

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
        var kullaniciListeleri = new List<MekanListesi>();

        if (user != null)
        {
            kullaniciBegendigi = (await _db.YorumBegeniler
                .Where(yb => yb.KullaniciId == user.Id && yb.Yorum.MekanId == id)
                .Select(yb => yb.YorumId).ToListAsync()).ToHashSet();

            if (mekan.SahibiId != null)
                takipEdiyor = await _db.Takipler.AnyAsync(t =>
                    t.TakipciId == user.Id && t.TakipEdilenId == mekan.SahibiId);

            kullaniciListeleri = await _db.MekanListeleri
                .Include(l => l.Mekanlar)
                .Where(l => l.OlusturanId == user.Id)
                .OrderByDescending(l => l.Olusturuldu)
                .ToListAsync();
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
            BugunNo = ((int)DateTime.Now.DayOfWeek + 6) % 7,
            KullaniciListeleri = kullaniciListeleri
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

        // Mekan sahibine bildirim
        var mekanSahibi = await _db.Mekanlar
            .Where(m => m.Id == id && m.SahibiId != null)
            .Select(m => m.SahibiId!)
            .FirstOrDefaultAsync();
        if (mekanSahibi != null)
            await BildirimHelper.OlusturAsync(_db, mekanSahibi, "YORUM",
                $"{user.UserName} mekanınıza yorum yazdı.",
                $"/Mekan/Detay/{id}", user.Id);

        return RedirectToAction("Detay", new { id });
    }

    // ── Takip ────────────────────────────────────────────────────────────────

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> TakipEt(int mekanId)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Json(new { hata = "Giriş gerekli" });

        var mekan = await _db.Mekanlar.FirstOrDefaultAsync(m => m.Id == mekanId && m.SahibiId != null);
        if (mekan == null) return Json(new { hata = "Mekan bulunamadı" });
        if (mekan.SahibiId == user.Id) return Json(new { hata = "Kendi mekanınızı takip edemezsiniz" });

        var mevcut = await _db.Takipler.FirstOrDefaultAsync(t =>
            t.TakipciId == user.Id && t.TakipEdilenId == mekan.SahibiId);

        bool takipEdiyor;
        if (mevcut != null)
        {
            _db.Takipler.Remove(mevcut);
            await _db.SaveChangesAsync();
            takipEdiyor = false;

            await BildirimHelper.OlusturAsync(_db, mekan.SahibiId!, "TAKIP",
                $"{user.UserName} mekanınızı takip etmeyi bıraktı.",
                $"/Mekan/Detay/{mekanId}", user.Id);
        }
        else
        {
            _db.Takipler.Add(new Takip { TakipciId = user.Id, TakipEdilenId = mekan.SahibiId! });
            await _db.SaveChangesAsync();
            takipEdiyor = true;

            await BildirimHelper.OlusturAsync(_db, mekan.SahibiId!, "TAKIP",
                $"{user.UserName} mekanınızı takip etmeye başladı.",
                $"/Mekan/Detay/{mekanId}", user.Id);
        }

        var takipciSayisi = await _db.Takipler.CountAsync(t => t.TakipEdilenId == mekan.SahibiId);
        return Json(new { takipEdiyor, takipciSayisi });
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

        // Konuma göre sıralama — GoruntulmeSayisi'ni bozmadan ayrı mesafe hesapla
        double? kullaniciLat = null, kullaniciLng = null;
        if (double.TryParse(lat, System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out var ulat) &&
            double.TryParse(lng, System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out var ulng))
        {
            kullaniciLat = ulat;
            kullaniciLng = ulng;
            mekanlar = mekanlar
                .OrderBy(m => m.Latitude.HasValue
                    ? HaversineM(ulat, ulng, (double)m.Latitude, (double)m.Longitude!.Value)
                    : double.MaxValue)
                .ToList();
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
                SigaraIcinUygun = m.SigaraIcinUygun,
                MesafeM = kullaniciLat.HasValue
                    ? HaversineM(kullaniciLat.Value, kullaniciLng!.Value,
                        (double)m.Latitude!, (double)m.Longitude!)
                    : null
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
    public async Task<IActionResult> Takvim(string filtre = "tumu")
    {
        var user = await _userManager.GetUserAsync(User);
        var now = DateTime.UtcNow;

        // Tüm etkinlikler ve kampanyalar (bitmemişler)
        var etkinliklerQ = _db.Etkinlikler
            .Include(e => e.Mekan)
            .Where(e => e.Bitis >= now)
            .AsQueryable();

        var kampanyalarQ = _db.Kampanyalar
            .Include(k => k.Mekan)
            .AsQueryable();

        if (filtre == "takip" && user != null)
        {
            // Takip edilen kullanıcıların mekanları
            var takipEdilenIds = await _db.Takipler
                .Where(t => t.TakipciId == user.Id)
                .Select(t => t.TakipEdilenId)
                .ToListAsync();

            etkinliklerQ = etkinliklerQ.Where(e => e.Mekan.SahibiId != null && takipEdilenIds.Contains(e.Mekan.SahibiId!));
            kampanyalarQ = kampanyalarQ.Where(k => k.Mekan.SahibiId != null && takipEdilenIds.Contains(k.Mekan.SahibiId!));
        }
        else if (filtre == "favori" && user != null)
        {
            var favoriIds = await _db.Users
                .Where(u => u.Id == user.Id)
                .SelectMany(u => u.FavoriMekanlar.Select(m => m.Id))
                .ToListAsync();

            etkinliklerQ = etkinliklerQ.Where(e => favoriIds.Contains(e.MekanId));
            kampanyalarQ = kampanyalarQ.Where(k => favoriIds.Contains(k.MekanId));
        }

        var etkinlikler = await etkinliklerQ.OrderBy(e => e.Baslangic).ToListAsync();
        var kampanyalar = await kampanyalarQ.OrderByDescending(k => k.Baslangic).ToListAsync();

        // Filtre rengine göre etkinlik rengi
        var etkinlikRenk = filtre switch { "takip" => "#7c3aed", "favori" => "#dc2626", _ => "#2563eb" };

        var events = new List<object>();

        foreach (var e in etkinlikler)
        {
            events.Add(new {
                id = $"e_{e.Id}",
                title = e.Baslik + " — " + e.Mekan.Ad,
                start = e.Baslangic.ToString("yyyy-MM-ddTHH:mm:ss"),
                end = e.Bitis.ToString("yyyy-MM-ddTHH:mm:ss"),
                color = etkinlikRenk,
                extendedProps = new {
                    tip = "etkinlik",
                    mekanAd = e.Mekan.Ad,
                    aciklama = e.Aciklama,
                    fotoUrl = e.FotoUrl,
                    detayUrl = Url.Action("Detay", "Mekan", new { id = e.MekanId }) ?? ""
                }
            });
        }

        foreach (var k in kampanyalar)
        {
            var durum = now < k.Baslangic ? "Yakında"
                      : now <= k.Bitis ? "Aktif"
                      : "Sona Erdi";
            var renk = durum == "Yakında" ? "#22c55e"
                     : durum == "Aktif" ? "#f59e0b"
                     : "#94a3b8";
            events.Add(new {
                id = $"k_{k.Id}",
                title = k.Baslik + " — " + k.Mekan.Ad,
                start = k.Baslangic.ToString("yyyy-MM-dd"),
                end = k.Bitis.AddDays(1).ToString("yyyy-MM-dd"),
                color = renk,
                allDay = true,
                extendedProps = new {
                    tip = "kampanya",
                    durum,
                    mekanAd = k.Mekan.Ad,
                    aciklama = k.Aciklama,
                    detayUrl = Url.Action("Detay", "Mekan", new { id = k.MekanId }) ?? ""
                }
            });
        }

        ViewBag.EventsJson = JsonSerializer.Serialize(events,
            new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        ViewBag.AktifFiltre = filtre;
        ViewBag.EtkinlikSayisi = etkinlikler.Count;
        ViewBag.KampanyaSayisi = kampanyalar.Count;
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
