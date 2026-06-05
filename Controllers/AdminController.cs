using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using AnlikMekanCore.Models;
using AnlikMekanCore.Models.Entities;

namespace AnlikMekanCore.Controllers;

public class AdminController : Controller
{
    private readonly AppDbContext _db;
    private readonly IWebHostEnvironment _env;

    public AdminController(AppDbContext db, IWebHostEnvironment env)
    {
        _db = db;
        _env = env;
    }

    [HttpGet]
    public IActionResult Login() => View();

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Login(string kullaniciAdi, string sifre)
    {
        if (kullaniciAdi != "Admin" || sifre != "Admin1234!")
        {
            TempData["Hata"] = "Kullanıcı adı veya şifre hatalı.";
            return View();
        }

        HttpContext.Session.SetString("admin_id", "hardcoded_admin");
        return RedirectToAction("Panel");
    }

    private bool AdminMi() => !string.IsNullOrEmpty(HttpContext.Session.GetString("admin_id"));

    public async Task<IActionResult> Panel()
    {
        if (!AdminMi()) return RedirectToAction("Login");
        var bekleyenler = await _db.Mekanlar.Where(m => !m.IsApproved).ToListAsync();
        return View(bekleyenler);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Onayla(int mekanId)
    {
        if (!AdminMi()) return RedirectToAction("Login");
        await _db.Mekanlar.Where(m => m.Id == mekanId)
            .ExecuteUpdateAsync(s => s.SetProperty(m => m.IsApproved, true));
        TempData["Mesaj"] = "Mekan onaylandı.";
        return RedirectToAction("Panel");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reddet(int mekanId)
    {
        if (!AdminMi()) return RedirectToAction("Login");
        await _db.Mekanlar.Where(m => m.Id == mekanId)
            .ExecuteUpdateAsync(s => s.SetProperty(m => m.IsApproved, false));
        TempData["Mesaj"] = "Mekan reddedildi.";
        return RedirectToAction("Panel");
    }

    // ── Fixture JSON Veri İçe Aktarma ────────────────────────────────────────

    [HttpGet]
    public IActionResult Import()
    {
        if (!AdminMi()) return RedirectToAction("Login");
        return View();
    }

    [HttpPost]
    [ActionName("Import")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ImportPost()
    {
        if (!AdminMi()) return RedirectToAction("Login");

        var jsonPath = Path.Combine(_env.ContentRootPath, "Data", "initial_data.json");
        if (!System.IO.File.Exists(jsonPath))
        {
            TempData["Hata"] = "Fixture dosyası bulunamadı: Data/initial_data.json";
            return View();
        }

        var json = await System.IO.File.ReadAllTextAsync(jsonPath);
        using var doc = JsonDocument.Parse(json);

        int eklendi = 0, atlandi = 0;

        foreach (var item in doc.RootElement.EnumerateArray())
        {
            if (!item.TryGetProperty("model", out var modelEl) || modelEl.GetString() != "venues.mekan")
                continue;
            if (!item.TryGetProperty("fields", out var f)) continue;

            var ad    = FixStr(f, "ad") ?? "İsimsiz Mekan";
            var sehir = FixStr(f, "sehir") ?? "";

            if (await _db.Mekanlar.AnyAsync(m => m.Ad == ad && m.Sehir == sehir))
            { atlandi++; continue; }

            decimal? lat = null, lon = null;
            if (f.TryGetProperty("latitude", out var latEl) && latEl.ValueKind == JsonValueKind.Number)
                lat = (decimal)latEl.GetDouble();
            if (f.TryGetProperty("longitude", out var lonEl) && lonEl.ValueKind == JsonValueKind.Number)
                lon = (decimal)lonEl.GetDouble();

            int? doluluk = null;
            if (f.TryGetProperty("doluluk_orani", out var dolEl) && dolEl.ValueKind == JsonValueKind.Number)
                doluluk = dolEl.GetInt32();

            _db.Mekanlar.Add(new Mekan
            {
                Ad              = ad,
                Kategori        = FixStr(f, "kategori") ?? "KAFE",
                Sehir           = sehir,
                Adres           = FixStr(f, "adres") ?? "",
                Telefon         = FixStr(f, "telefon"),
                Website         = FixStr(f, "website"),
                SuAnAcik        = FixBool(f, "su_an_acik"),
                DolulukOrani    = doluluk,
                Latitude        = lat,
                Longitude       = lon,
                IsApproved      = true, // başlangıç verisi, direkt onaylı
                WifiVar         = FixBool(f, "wifi_var"),
                PrizVar         = FixBool(f, "priz_var"),
                OtoparkVar      = FixBool(f, "otopark_var"),
                SigaraIcinUygun = FixBool(f, "sigara_icin_uygun"),
                BahceVar        = FixBool(f, "bahce_var"),
                EngelliErizimiVar = FixBool(f, "engelli_erisimi_var"),
                CanliMuzikVar   = FixBool(f, "canli_muzik_var"),
                EvcilHayvanIzinli = FixBool(f, "evcil_hayvan_izinli"),
                CocukOyunAlaniVar = FixBool(f, "cocuk_oyun_alani_var"),
                RezervasyonAktif  = FixBool(f, "rezervasyon_aktif"),
            });
            eklendi++;
        }

        await _db.SaveChangesAsync();
        TempData["Mesaj"] = $"{eklendi} mekan eklendi, {atlandi} mekan atlandı (zaten mevcut).";
        return RedirectToAction("Import");
    }

    private static string? FixStr(JsonElement f, string key)
    {
        if (!f.TryGetProperty(key, out var el)) return null;
        var s = el.GetString();
        return string.IsNullOrWhiteSpace(s) ? null : s;
    }

    private static bool FixBool(JsonElement f, string key)
        => f.TryGetProperty(key, out var el) && el.ValueKind == JsonValueKind.True;
}
