using System.ComponentModel.DataAnnotations;

namespace AnlikMekanCore.Models.Entities;

public class Mekan
{
    public int Id { get; set; }

    [Required] [MaxLength(100)]
    public string Ad { get; set; } = "";

    [Required]
    public string Kategori { get; set; } = "KAFE"; // KAFE | KUTUPHANE | ECZANE | RESTORAN | PUB

    public string? Sehir { get; set; }

    [Required]
    public string Adres { get; set; } = "";

    public string? ImgUrl { get; set; }
    public string? Telefon { get; set; }
    public string? Website { get; set; }

    public bool SuAnAcik { get; set; } = true;
    public int? DolulukOrani { get; set; }
    public TimeOnly? AcilisSaati { get; set; }
    public TimeOnly? KapanisSaati { get; set; }

    public string? SahibiId { get; set; }
    public AppUser? Sahibi { get; set; }

    public bool DogrulanmisMi { get; set; } = false;
    public string? AnlikDuyuru { get; set; }
    public bool IsApproved { get; set; } = false;
    public int GoruntulmeSayisi { get; set; } = 0;

    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }

    // Özellikler
    public bool WifiVar { get; set; } = false;
    public bool PrizVar { get; set; } = false;
    public bool CalismaAlaniVar { get; set; } = false;
    public bool OtoparkVar { get; set; } = false;
    public bool SigaraIcinUygun { get; set; } = false;
    public bool BahceVar { get; set; } = false;
    public bool EngelliErizimiVar { get; set; } = false;
    public bool CanliMuzikVar { get; set; } = false;
    public bool EvcilHayvanIzinli { get; set; } = false;
    public bool CocukOyunAlaniVar { get; set; } = false;
    public bool RezervasyonAktif { get; set; } = false;

    public string? MenuPdfUrl { get; set; }
    public string? MenuFotoUrl { get; set; }

    // Navigation
    public ICollection<Yorum> Yorumlar { get; set; } = new List<Yorum>();
    public ICollection<MekanFoto> Fotolar { get; set; } = new List<MekanFoto>();
    public ICollection<Etkinlik> Etkinlikler { get; set; } = new List<Etkinlik>();
    public ICollection<Kampanya> Kampanyalar { get; set; } = new List<Kampanya>();
    public ICollection<Rezervasyon> Rezervasyonlar { get; set; } = new List<Rezervasyon>();
    public ICollection<CalismaGunu> CalismaGunleri { get; set; } = new List<CalismaGunu>();
    public ICollection<AppUser> Favorileyenler { get; set; } = new List<AppUser>();

    // Computed
    public double? OrtalamaYorum => Yorumlar.Any(y => y.Puan.HasValue)
        ? Yorumlar.Where(y => y.Puan.HasValue).Average(y => (double)y.Puan!.Value)
        : null;

    public bool AcikMi
    {
        get
        {
            var now = DateTime.Now;
            var bugun = (int)now.DayOfWeek; // 0=Pazar, 1=Pazartesi...
            // .NET: Sunday=0, Django: Monday=0 → dönüştür
            var djangoGun = bugun == 0 ? 6 : bugun - 1;

            var gun = CalismaGunleri.FirstOrDefault(g => g.Gun == djangoGun);
            if (gun != null)
            {
                if (!gun.Acik) return false;
                if (gun.Acilis.HasValue && gun.Kapanis.HasValue)
                {
                    var t = TimeOnly.FromDateTime(now);
                    return t >= gun.Acilis && t <= gun.Kapanis;
                }
                return true;
            }
            if (AcilisSaati.HasValue && KapanisSaati.HasValue)
            {
                var t = TimeOnly.FromDateTime(now);
                return t >= AcilisSaati && t <= KapanisSaati;
            }
            return SuAnAcik;
        }
    }

    public static readonly Dictionary<string, string> KategoriAdlari = new()
    {
        { "KAFE", "Kafe" },
        { "KUTUPHANE", "Kütüphane" },
        { "ECZANE", "Eczane" },
        { "RESTORAN", "Restoran" },
        { "PUB", "Pub" },
    };

    public string KategoriAdi => KategoriAdlari.TryGetValue(Kategori, out var ad) ? ad : Kategori;
}
