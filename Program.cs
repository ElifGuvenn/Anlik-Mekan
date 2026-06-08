using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using AnlikMekanCore.Models;
using AnlikMekanCore.Services;

// Npgsql 6+: DateTime.Kind=Unspecified değerlerini timestamptz sütununa yazmayı etkinleştir
AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

var builder = WebApplication.CreateBuilder(args);

// Render (ve benzeri) PORT env değişkenini okuyarak dinleme portunu ayarla
var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
builder.WebHost.UseUrls($"http://+:{port}");

// Veritabanı: DATABASE_URL varsa PostgreSQL (Railway), yoksa SQLite (lokal)
var databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL");
if (!string.IsNullOrEmpty(databaseUrl))
{
    // Railway: postgresql://user:pass@host:port/db → Npgsql key-value formatına çevir
    var uri = new Uri(databaseUrl);
    var userInfo = uri.UserInfo.Split(':', 2);
    var connStr = $"Host={uri.Host};Port={uri.Port};Database={uri.AbsolutePath.TrimStart('/')};Username={userInfo[0]};Password={userInfo[1]};SSL Mode=Require;Trust Server Certificate=true";

    builder.Services.AddDbContext<AppDbContext>(opt =>
        opt.UseNpgsql(connStr, npgsql =>
            npgsql.EnableRetryOnFailure(maxRetryCount: 3)));
}
else
{
    builder.Services.AddDbContext<AppDbContext>(opt =>
        opt.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));
}

// Identity
builder.Services.AddIdentity<AppUser, IdentityRole>(opt =>
{
    opt.Password.RequireDigit = false;
    opt.Password.RequireNonAlphanumeric = false;
    opt.Password.RequireUppercase = false;
    opt.Password.RequiredLength = 8;
    opt.User.RequireUniqueEmail = false;
})
.AddEntityFrameworkStores<AppDbContext>()
.AddDefaultTokenProviders();

// Cookie ayarları
builder.Services.ConfigureApplicationCookie(opt =>
{
    opt.LoginPath = "/Auth/Login";
    opt.LogoutPath = "/Auth/Logout";
    opt.AccessDeniedPath = "/Auth/Login";
    opt.ExpireTimeSpan = TimeSpan.FromDays(30);
    opt.SlidingExpiration = true;
});

// Session (şifre sıfırlama için)
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(opt => opt.IdleTimeout = TimeSpan.FromMinutes(30));

// MVC
builder.Services.AddControllersWithViews();

// Rol claim'ini cookie'ye ekle
builder.Services.AddScoped<IUserClaimsPrincipalFactory<AppUser>, AppUserClaimsPrincipalFactory>();

// Data Protection: container yeniden başlayınca oturumlar kapanmasın diye anahtarları DB'de sakla
builder.Services.AddDataProtection()
    .SetApplicationName("AnlikMekan")
    .PersistKeysToDbContext<AppDbContext>();

// Servisler
builder.Services.AddScoped<CloudinaryService>();
builder.Services.AddScoped<TotpService>();
builder.Services.AddScoped<EmailService>();
builder.Services.AddHttpContextAccessor();

var app = builder.Build();

// DB oluştur + fixture seed
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();

    // Mevcut PostgreSQL DB'de DataProtectionKeys tablosu yoksa oluştur
    if (db.Database.ProviderName?.Contains("Npgsql") == true)
    {
        db.Database.ExecuteSqlRaw(@"
            CREATE TABLE IF NOT EXISTS ""DataProtectionKeys"" (
                ""Id"" SERIAL PRIMARY KEY,
                ""FriendlyName"" TEXT,
                ""Xml"" TEXT
            )
        ");
    }

    if (!db.Mekanlar.Any())
    {
        var jsonPath = Path.Combine(app.Environment.ContentRootPath, "Data", "initial_data.json");
        if (File.Exists(jsonPath))
        {
            using var doc = System.Text.Json.JsonDocument.Parse(File.ReadAllText(jsonPath));
            foreach (var item in doc.RootElement.EnumerateArray())
            {
                if (!item.TryGetProperty("model", out var m) || m.GetString() != "venues.mekan") continue;
                if (!item.TryGetProperty("fields", out var f)) continue;
                string? Str(string k) { if (!f.TryGetProperty(k, out var e)) return null; var s = e.GetString(); return string.IsNullOrWhiteSpace(s) ? null : s; }
                bool Bool(string k) => f.TryGetProperty(k, out var e) && e.ValueKind == System.Text.Json.JsonValueKind.True;
                decimal? Num(string k) {
                    if (!f.TryGetProperty(k, out var e)) return null;
                    if (e.ValueKind == System.Text.Json.JsonValueKind.Number) return (decimal)e.GetDouble();
                    if (e.ValueKind == System.Text.Json.JsonValueKind.String && decimal.TryParse(e.GetString(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var d)) return d;
                    return null;
                }
                int? Int(string k) {
                    if (!f.TryGetProperty(k, out var e)) return null;
                    if (e.ValueKind == System.Text.Json.JsonValueKind.Number) return e.GetInt32();
                    if (e.ValueKind == System.Text.Json.JsonValueKind.String && int.TryParse(e.GetString(), out var i)) return i;
                    return null;
                }

                db.Mekanlar.Add(new AnlikMekanCore.Models.Entities.Mekan
                {
                    Ad = Str("ad") ?? "İsimsiz", Kategori = Str("kategori") ?? "KAFE",
                    Sehir = Str("sehir") ?? "", Adres = Str("adres") ?? "",
                    Telefon = Str("telefon"), Website = Str("website"),
                    SuAnAcik = Bool("su_an_acik"), DolulukOrani = Int("doluluk_orani"),
                    Latitude = Num("latitude"), Longitude = Num("longitude"),
                    IsApproved = true,
                    WifiVar = Bool("wifi_var"), PrizVar = Bool("priz_var"),
                    OtoparkVar = Bool("otopark_var"), SigaraIcinUygun = Bool("sigara_icin_uygun"),
                    BahceVar = Bool("bahce_var"), EngelliErizimiVar = Bool("engelli_erisimi_var"),
                    CanliMuzikVar = Bool("canli_muzik_var"), EvcilHayvanIzinli = Bool("evcil_hayvan_izinli"),
                    CocukOyunAlaniVar = Bool("cocuk_oyun_alani_var"), RezervasyonAktif = Bool("rezervasyon_aktif"),
                    CalismaAlaniVar = Bool("calisma_alani_var"),
                });
            }
            db.SaveChanges();
        }
    }
}

app.UseStaticFiles();
app.UseSession();

// datetime-local ve sayısal değerlerin parse edilebilmesi için InvariantCulture kullan
app.UseRequestLocalization(new RequestLocalizationOptions()
    .SetDefaultCulture("en-US")
    .AddSupportedCultures("en-US", "tr-TR")
    .AddSupportedUICultures("tr-TR"));

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Landing}/{id?}");

app.Run();
