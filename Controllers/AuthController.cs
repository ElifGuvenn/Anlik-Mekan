using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AnlikMekanCore.Models;
using AnlikMekanCore.Models.ViewModels;
using AnlikMekanCore.Services;

namespace AnlikMekanCore.Controllers;

public class AuthController : Controller
{
    private readonly UserManager<AppUser> _userManager;
    private readonly SignInManager<AppUser> _signInManager;
    private readonly AppDbContext _db;
    private readonly TotpService _totp;
    private readonly EmailService _email;

    public AuthController(UserManager<AppUser> userManager, SignInManager<AppUser> signInManager,
        AppDbContext db, TotpService totp, EmailService email)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _db = db;
        _totp = totp;
        _email = email;
    }

    [HttpGet]
    public IActionResult Login() =>
        User.Identity?.IsAuthenticated == true ? RedirectToDashboard() : View();

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var user = await _userManager.FindByNameAsync(model.KullaniciAdi);
        if (user == null)
        {
            ModelState.AddModelError("", "Kullanıcı adı veya şifre hatalı!");
            return View(model);
        }

        var result = await _signInManager.PasswordSignInAsync(user, model.Sifre, true, false);
        if (!result.Succeeded)
        {
            ModelState.AddModelError("", "Kullanıcı adı veya şifre hatalı!");
            return View(model);
        }

        return RedirectToDashboard(user);
    }

    [HttpGet]
    public IActionResult Register(string? rol)
    {
        if (User.Identity?.IsAuthenticated == true) return RedirectToDashboard();
        return View(new RegisterViewModel { Rol = rol ?? "USER" });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var user = new AppUser
        {
            UserName = model.KullaniciAdi,
            Email = model.Email,
            Rol = model.Rol
        };

        var result = await _userManager.CreateAsync(user, model.Sifre);
        if (!result.Succeeded)
        {
            foreach (var err in result.Errors)
                ModelState.AddModelError("", err.Description);
            return View(model);
        }

        TempData["Mesaj"] = $"Hesabınız oluşturuldu! Giriş yapabilirsiniz.";
        return RedirectToAction("Login");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        return RedirectToAction("Landing", "Home");
    }

    // ── 2FA ──────────────────────────────────────────────────────────────────

    [HttpGet]
    public async Task<IActionResult> QrKodOlustur()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return RedirectToAction("Login");

        if (string.IsNullOrEmpty(user.TotpSecretKey))
        {
            user.TotpSecretKey = _totp.YeniSecretOlustur();
            await _userManager.UpdateAsync(user);
        }

        var uri = _totp.GetProvisioningUri(user.TotpSecretKey, user.Email ?? user.UserName ?? "");
        var qrBase64 = _totp.QrKodBase64(uri);

        ViewBag.QrBase64 = qrBase64;
        ViewBag.SecretKey = user.TotpSecretKey;
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> QrKodDogrula(TotpDogrulaViewModel model)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return RedirectToAction("Login");

        if (string.IsNullOrEmpty(user.TotpSecretKey) || !_totp.Dogrula(user.TotpSecretKey, model.TotpCode))
        {
            TempData["Hata"] = "Doğrulama kodu yanlış. Lütfen tekrar deneyin.";
            return RedirectToAction("QrKodOlustur");
        }

        user.TwoFaDogrulandi = true;
        await _userManager.UpdateAsync(user);

        TempData["Mesaj"] = "2FA başarıyla doğrulandı!";
        return user.Rol == "OWNER"
            ? RedirectToAction("Dashboard", "Owner")
            : RedirectToAction("Dashboard", "Home");
    }

    // ── Şifre Sıfırlama ──────────────────────────────────────────────────────

    [HttpGet]
    public IActionResult SifreSifirla() => View();

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SifreSifirla(SifreSifirlaViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var user = await _userManager.FindByEmailAsync(model.Email);
        if (user != null)
        {
            var eskiKodlar = _db.SifreSifirlamaKodlari
                .Where(k => k.UserId == user.Id && !k.Kullanildi);
            foreach (var k in eskiKodlar) k.Kullanildi = true;

            var kod = new string(Enumerable.Range(0, 6)
                .Select(_ => (char)('0' + Random.Shared.Next(10))).ToArray());

            _db.SifreSifirlamaKodlari.Add(new Models.Entities.SifreSifirlamaKodu
            {
                UserId = user.Id,
                Kod = kod
            });
            await _db.SaveChangesAsync();

            await _email.GonderAsync(
                user.Email!,
                "Anlık Mekan — Şifre Sıfırlama Kodu",
                $"Merhaba {user.UserName},\n\nŞifrenizi sıfırlamak için kodunuz:\n\n  {kod}\n\nBu kod 15 dakika geçerlidir."
            );
        }

        TempData["Mesaj"] = "Girilen e-posta kayıtlıysa kod gönderildi.";
        HttpContext.Session.SetString("sifre_sifirla_email", model.Email);
        return RedirectToAction("SifreKod");
    }

    [HttpGet]
    public IActionResult SifreKod() => View();

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SifreKod(SifreKodViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var email = HttpContext.Session.GetString("sifre_sifirla_email");
        if (string.IsNullOrEmpty(email))
        {
            TempData["Hata"] = "Oturum süresi doldu.";
            return RedirectToAction("SifreSifirla");
        }

        var user = await _userManager.FindByEmailAsync(email);
        if (user == null) { TempData["Hata"] = "Kullanıcı bulunamadı."; return RedirectToAction("SifreSifirla"); }

        var kayit = await _db.SifreSifirlamaKodlari
            .Where(k => k.UserId == user.Id && !k.Kullanildi)
            .OrderByDescending(k => k.Olusturuldu)
            .FirstOrDefaultAsync();

        if (kayit == null || !kayit.GecerliMi)
        {
            TempData["Hata"] = "Kodun süresi dolmuş. Yeni kod isteyin.";
            return RedirectToAction("SifreSifirla");
        }

        if (kayit.Kod != model.Kod)
        {
            TempData["Hata"] = "Doğrulama kodu hatalı.";
            return View(model);
        }

        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        var resetResult = await _userManager.ResetPasswordAsync(user, token, model.Sifre1);
        if (!resetResult.Succeeded)
        {
            foreach (var err in resetResult.Errors) ModelState.AddModelError("", err.Description);
            return View(model);
        }

        kayit.Kullanildi = true;
        await _db.SaveChangesAsync();
        HttpContext.Session.Remove("sifre_sifirla_email");

        TempData["Mesaj"] = "Şifreniz başarıyla sıfırlandı.";
        return RedirectToAction("Login");
    }

    // ── Yardımcı ─────────────────────────────────────────────────────────────

    private IActionResult RedirectToDashboard(AppUser? user = null)
    {
        user ??= _userManager.GetUserAsync(User).Result;
        return user?.Rol == "OWNER"
            ? RedirectToAction("Dashboard", "Owner")
            : RedirectToAction("Dashboard", "Home");
    }
}
