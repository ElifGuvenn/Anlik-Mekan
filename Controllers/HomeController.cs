using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AnlikMekanCore.Models;
using AnlikMekanCore.Models.Entities;
using AnlikMekanCore.Models.ViewModels;

namespace AnlikMekanCore.Controllers;

public class HomeController : Controller
{
    private readonly AppDbContext _db;
    private readonly UserManager<AppUser> _userManager;
    private const int SayfaBoyutu = 12;

    public HomeController(AppDbContext db, UserManager<AppUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    public IActionResult Landing()
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToAction("Dashboard");
        return View();
    }

    [Authorize]
    public async Task<IActionResult> Dashboard(DashboardFiltre? filtre, int sayfa = 1)
    {
        filtre ??= new DashboardFiltre();

        var q = _db.Mekanlar
            .Include(m => m.Yorumlar)
            .Include(m => m.CalismaGunleri)
            .Where(m => m.IsApproved);

        if (!string.IsNullOrEmpty(filtre.Sehir)) q = q.Where(m => m.Sehir == filtre.Sehir);
        if (!string.IsNullOrEmpty(filtre.Kategori)) q = q.Where(m => m.Kategori == filtre.Kategori);
        if (!string.IsNullOrEmpty(filtre.Wifi)) q = q.Where(m => m.WifiVar);
        if (!string.IsNullOrEmpty(filtre.Priz)) q = q.Where(m => m.PrizVar);
        if (!string.IsNullOrEmpty(filtre.Bahce)) q = q.Where(m => m.BahceVar);
        if (!string.IsNullOrEmpty(filtre.Pet)) q = q.Where(m => m.EvcilHayvanIzinli);
        if (!string.IsNullOrEmpty(filtre.Engelli)) q = q.Where(m => m.EngelliErizimiVar);
        if (!string.IsNullOrEmpty(filtre.Muzik)) q = q.Where(m => m.CanliMuzikVar);
        if (!string.IsNullOrEmpty(filtre.Cocuk)) q = q.Where(m => m.CocukOyunAlaniVar);

        var mekanlar = await q.ToListAsync();

        if (!string.IsNullOrEmpty(filtre.Acik))
            mekanlar = mekanlar.Where(m => m.AcikMi).ToList();

        // Mesafe hesaplama → Dictionary<mekanId, km>
        var distanceDict = new Dictionary<int, double>();
        if (!string.IsNullOrEmpty(filtre.Lat) && !string.IsNullOrEmpty(filtre.Lng) &&
            double.TryParse(filtre.Lat, System.Globalization.CultureInfo.InvariantCulture, out var ulat) &&
            double.TryParse(filtre.Lng, System.Globalization.CultureInfo.InvariantCulture, out var ulng))
        {
            foreach (var m in mekanlar)
                if (m.Latitude.HasValue && m.Longitude.HasValue)
                    distanceDict[m.Id] = HaversineKm(ulat, ulng, (double)m.Latitude, (double)m.Longitude);
        }

        mekanlar = filtre.Siralama switch
        {
            "puan"       => mekanlar.OrderByDescending(m => m.OrtalamaYorum ?? 0).ToList(),
            "doluluk_az" => mekanlar.OrderBy(m => m.DolulukOrani ?? 0).ToList(),
            "doluluk_cok"=> mekanlar.OrderByDescending(m => m.DolulukOrani ?? 0).ToList(),
            "isim_az"    => mekanlar.OrderBy(m => m.Ad).ToList(),
            "isim_za"    => mekanlar.OrderByDescending(m => m.Ad).ToList(),
            "mesafe"     => mekanlar.OrderBy(m => distanceDict.ContainsKey(m.Id) ? distanceDict[m.Id] : double.MaxValue).ToList(),
            _            => mekanlar
        };

        var user = await _userManager.GetUserAsync(User);
        var favoriIds = user != null
            ? (await _db.Mekanlar
                .Where(m => m.Favorileyenler.Any(u => u.Id == user.Id))
                .Select(m => m.Id).ToListAsync()).ToHashSet()
            : new HashSet<int>();

        var toplam = mekanlar.Count;
        var sayfaMekanlar = mekanlar
            .Skip((sayfa - 1) * SayfaBoyutu)
            .Take(SayfaBoyutu)
            .Select(m =>
            {
                var dist = distanceDict.ContainsKey(m.Id) ? distanceDict[m.Id] : (double?)null;
                return new MekanKartModel
                {
                    Id = m.Id, Ad = m.Ad, Kategori = m.Kategori,
                    KategoriAdi = m.KategoriAdi, Sehir = m.Sehir, Adres = m.Adres,
                    ImgUrl = m.ImgUrl, DolulukOrani = m.DolulukOrani, AcikMi = m.AcikMi,
                    OrtPuan = m.Yorumlar.Any(y => y.Puan.HasValue)
                        ? m.Yorumlar.Where(y => y.Puan.HasValue).Average(y => (double)y.Puan!.Value) : null,
                    YorumSayisi = m.Yorumlar.Count,
                    WifiVar = m.WifiVar, PrizVar = m.PrizVar, BahceVar = m.BahceVar,
                    EvcilHayvanIzinli = m.EvcilHayvanIzinli, EngelliErizimiVar = m.EngelliErizimiVar,
                    CanliMuzikVar = m.CanliMuzikVar, SigaraIcinUygun = m.SigaraIcinUygun,
                    CocukOyunAlaniVar = m.CocukOyunAlaniVar, CalismaAlaniVar = m.CalismaAlaniVar,
                    OtoparkVar = m.OtoparkVar,
                    Mesafe = dist,
                    MesafeYazi = dist.HasValue ? (dist < 1 ? $"{(int)(dist * 1000)}m" : $"{dist:F1}km") : null,
                };
            }).ToList();

        return View(new DashboardViewModel
        {
            Mekanlar = sayfaMekanlar,
            ToplamSonuc = toplam,
            SayfaSayisi = (int)Math.Ceiling((double)toplam / SayfaBoyutu),
            MevcutSayfa = sayfa,
            Filtre = filtre,
            FavoriMekanIds = favoriIds,
        });
    }

    [Authorize]
    public async Task<IActionResult> Arama(string? q, string? filtre, int sayfa = 1)
    {
        var mekanlar = _db.Mekanlar
            .Include(m => m.Yorumlar)
            .Include(m => m.CalismaGunleri)
            .Where(m => m.IsApproved);

        if (!string.IsNullOrEmpty(q))
            mekanlar = mekanlar.Where(m =>
                m.Ad.Contains(q) || m.Adres.Contains(q) || (m.Sehir != null && m.Sehir.Contains(q)));

        var user = await _userManager.GetUserAsync(User);
        var liste = await mekanlar.ToListAsync();

        if (filtre == "calisma")
            liste = liste.Where(m => m.Kategori == "KUTUPHANE" || m.CalismaAlaniVar).ToList();
        else if (filtre == "eczane")
            liste = liste.Where(m => m.Kategori == "ECZANE").ToList();
        else if (filtre == "acik")
            liste = liste.Where(m => m.AcikMi).ToList();
        else if (filtre == "favori" && user != null)
        {
            var favoriIds = await _db.Mekanlar
                .Where(m => m.Favorileyenler.Any(u => u.Id == user.Id))
                .Select(m => m.Id).ToListAsync();
            liste = liste.Where(m => favoriIds.Contains(m.Id)).ToList();
        }

        var baslik = (q, filtre) switch
        {
            ({ Length: > 0 }, { Length: > 0 }) => $"\"{q}\" — {FiltreBaslik(filtre!)} içinde",
            ({ Length: > 0 }, _) => $"\"{q}\" için sonuçlar",
            (_, { Length: > 0 }) => FiltreBaslik(filtre!),
            _ => "Tüm Mekanlar"
        };

        ViewBag.Baslik = baslik;
        ViewBag.Q = q;
        ViewBag.Filtre = filtre;

        var toplam = liste.Count;
        var sayfaMekanlar = liste.Skip((sayfa - 1) * SayfaBoyutu).Take(SayfaBoyutu)
            .Select(m => new MekanKartModel
            {
                Id = m.Id, Ad = m.Ad, Kategori = m.Kategori, KategoriAdi = m.KategoriAdi,
                Sehir = m.Sehir, Adres = m.Adres, ImgUrl = m.ImgUrl,
                DolulukOrani = m.DolulukOrani, AcikMi = m.AcikMi,
                OrtPuan = m.Yorumlar.Any(y => y.Puan.HasValue)
                    ? m.Yorumlar.Where(y => y.Puan.HasValue).Average(y => (double)y.Puan!.Value) : null,
                YorumSayisi = m.Yorumlar.Count
            }).ToList();

        ViewBag.ToplamSonuc = toplam;
        ViewBag.SayfaSayisi = (int)Math.Ceiling((double)toplam / SayfaBoyutu);
        ViewBag.MevcutSayfa = sayfa;
        return View("Liste", sayfaMekanlar);
    }

    [Authorize]
    public async Task<IActionResult> AramaApi(string? q, string? filtre)
    {
        if (string.IsNullOrWhiteSpace(q))
            return Json(new { sonuclar = Array.Empty<object>() });

        var user = await _userManager.GetUserAsync(User);
        var pattern = $"%{q}%";
        var isPostgres = _db.Database.ProviderName?.Contains("Npgsql") == true;
        IQueryable<Mekan> qs;
        if (isPostgres)
        {
            qs = _db.Mekanlar.Where(m => m.IsApproved &&
                (EF.Functions.ILike(m.Ad, pattern) ||
                 EF.Functions.ILike(m.Adres, pattern) ||
                 (m.Sehir != null && EF.Functions.ILike(m.Sehir, pattern))));
        }
        else
        {
            var qLower = q.ToLower();
            qs = _db.Mekanlar.Where(m => m.IsApproved &&
                (m.Ad.ToLower().Contains(qLower) ||
                 m.Adres.ToLower().Contains(qLower) ||
                 (m.Sehir != null && m.Sehir.ToLower().Contains(qLower))));
        }

        if (filtre == "calisma")
            qs = qs.Where(m => m.Kategori == "KUTUPHANE" || m.CalismaAlaniVar);
        else if (filtre == "eczane")
            qs = qs.Where(m => m.Kategori == "ECZANE");
        else if (filtre == "acik")
            qs = qs.Where(m => m.SuAnAcik);
        else if (filtre == "favori" && user != null)
            qs = qs.Where(m => m.Favorileyenler.Any(u => u.Id == user.Id));

        var sonuclar = await qs.Take(8)
            .Select(m => new { m.Id, m.Ad, m.Kategori, m.Sehir })
            .ToListAsync();

        return Json(new { sonuclar });
    }

    [Authorize]
    public async Task<IActionResult> SuAnAcik(int sayfa = 1)
    {
        var mekanlar = await _db.Mekanlar.Include(m => m.Yorumlar)
            .Include(m => m.CalismaGunleri).Where(m => m.IsApproved).ToListAsync();
        var aciklar = mekanlar.Where(m => m.AcikMi).ToList();
        return ListeView(aciklar, $"Şu An Açık Olan Mekanlar ({aciklar.Count})", "acik", sayfa);
    }

    [Authorize]
    public async Task<IActionResult> CalismaAlanlari(int sayfa = 1)
    {
        var mekanlar = await _db.Mekanlar.Include(m => m.Yorumlar)
            .Where(m => m.IsApproved && (m.Kategori == "KUTUPHANE" || m.CalismaAlaniVar)).ToListAsync();
        return ListeView(mekanlar, "Çalışma Alanları", "calisma", sayfa);
    }

    [Authorize]
    public async Task<IActionResult> AcilIhtiyaclar(int sayfa = 1)
    {
        var mekanlar = await _db.Mekanlar.Include(m => m.Yorumlar)
            .Where(m => m.IsApproved && m.Kategori == "ECZANE").ToListAsync();
        return ListeView(mekanlar, "Nöbetçi/Açık Eczaneler", "eczane", sayfa);
    }

    [Authorize]
    public async Task<IActionResult> Favorilerim(int sayfa = 1)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return RedirectToAction("Login", "Auth");
        var mekanlar = await _db.Mekanlar.Include(m => m.Yorumlar)
            .Where(m => m.IsApproved && m.Favorileyenler.Any(u => u.Id == user.Id)).ToListAsync();
        return ListeView(mekanlar, "Favori Mekanlarım", "favori", sayfa);
    }

    [Authorize]
    public async Task<IActionResult> PopulerMekanlar()
    {
        var mekanlar = await _db.Mekanlar.Include(m => m.Yorumlar)
            .Include(m => m.Favorileyenler).Where(m => m.IsApproved).ToListAsync();

        var sirali = mekanlar.OrderByDescending(m =>
            (m.GoruntulmeSayisi * 0.1) +
            (m.Yorumlar.Count * 30) +
            ((m.OrtalamaYorum ?? 0) * 20) +
            (m.Favorileyenler.Count * 40) +
            ((m.DolulukOrani ?? 0) * 0.1)
        ).Take(20).ToList();

        return ListeView(sirali, "Şu An Popüler", "", 1);
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> FavoriIslem(int mekanId)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return RedirectToAction("Login", "Auth");

        var mekan = await _db.Mekanlar.Include(m => m.Favorileyenler)
            .FirstOrDefaultAsync(m => m.Id == mekanId);
        if (mekan == null) return NotFound();

        var favori = mekan.Favorileyenler.FirstOrDefault(u => u.Id == user.Id);
        if (favori != null) mekan.Favorileyenler.Remove(favori);
        else mekan.Favorileyenler.Add(user);

        await _db.SaveChangesAsync();
        return Redirect(Request.Headers["Referer"].ToString() ?? "/dashboard");
    }

    // ── Yardımcı ─────────────────────────────────────────────────────────────

    private IActionResult ListeView(List<Mekan> mekanlar, string baslik, string filtre, int sayfa)
    {
        var toplam = mekanlar.Count;
        var kart = mekanlar.Skip((sayfa - 1) * SayfaBoyutu).Take(SayfaBoyutu)
            .Select(m => new MekanKartModel
            {
                Id = m.Id, Ad = m.Ad, Kategori = m.Kategori, KategoriAdi = m.KategoriAdi,
                Sehir = m.Sehir, Adres = m.Adres, ImgUrl = m.ImgUrl,
                DolulukOrani = m.DolulukOrani, AcikMi = m.AcikMi,
                OrtPuan = m.Yorumlar.Any(y => y.Puan.HasValue)
                    ? m.Yorumlar.Where(y => y.Puan.HasValue).Average(y => (double)y.Puan!.Value) : null,
                YorumSayisi = m.Yorumlar.Count
            }).ToList();

        ViewBag.Baslik = baslik;
        ViewBag.Filtre = filtre;
        ViewBag.ToplamSonuc = toplam;
        ViewBag.SayfaSayisi = (int)Math.Ceiling((double)toplam / SayfaBoyutu);
        ViewBag.MevcutSayfa = sayfa;
        return View("Liste", kart);
    }

    private static string FiltreBaslik(string filtre) => filtre switch
    {
        "calisma" => "Çalışma Alanları",
        "acik" => "Şu An Açık",
        "eczane" => "Eczaneler",
        "favori" => "Favorilerim",
        _ => ""
    };

    private static double HaversineKm(double lat1, double lng1, double lat2, double lng2)
    {
        const double R = 6371;
        var dLat = (lat2 - lat1) * Math.PI / 180;
        var dLng = (lng2 - lng1) * Math.PI / 180;
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(lat1 * Math.PI / 180) * Math.Cos(lat2 * Math.PI / 180) *
                Math.Sin(dLng / 2) * Math.Sin(dLng / 2);
        return R * 2 * Math.Asin(Math.Sqrt(a));
    }
}
