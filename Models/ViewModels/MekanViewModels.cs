using System.ComponentModel.DataAnnotations;
using AnlikMekanCore.Models.Entities;

namespace AnlikMekanCore.Models.ViewModels;

public class DashboardViewModel
{
    public List<MekanKartModel> Mekanlar { get; set; } = new();
    public int ToplamSonuc { get; set; }
    public int SayfaSayisi { get; set; }
    public int MevcutSayfa { get; set; }
    public DashboardFiltre Filtre { get; set; } = new();
    public HashSet<int> FavoriMekanIds { get; set; } = new();
}

public class DashboardFiltre
{
    public string Sehir { get; set; } = "";
    public string Kategori { get; set; } = "";
    public string Acik { get; set; } = "";
    public string Wifi { get; set; } = "";
    public string Priz { get; set; } = "";
    public string Bahce { get; set; } = "";
    public string Pet { get; set; } = "";
    public string Engelli { get; set; } = "";
    public string Muzik { get; set; } = "";
    public string Cocuk { get; set; } = "";
    public string Siralama { get; set; } = "";
    public string Lat { get; set; } = "";
    public string Lng { get; set; } = "";
}

public class MekanKartModel
{
    public int Id { get; set; }
    public string Ad { get; set; } = "";
    public string Kategori { get; set; } = "";
    public string KategoriAdi { get; set; } = "";
    public string? Sehir { get; set; }
    public string Adres { get; set; } = "";
    public string? ImgUrl { get; set; }
    public int? DolulukOrani { get; set; }
    public bool AcikMi { get; set; }
    public double? OrtPuan { get; set; }
    public int YorumSayisi { get; set; }
    public double? Mesafe { get; set; }
    public string? MesafeYazi { get; set; }
    public bool WifiVar { get; set; }
    public bool PrizVar { get; set; }
    public bool BahceVar { get; set; }
    public bool EvcilHayvanIzinli { get; set; }
    public bool EngelliErizimiVar { get; set; }
    public bool CanliMuzikVar { get; set; }
    public bool SigaraIcinUygun { get; set; }
    public bool CocukOyunAlaniVar { get; set; }
    public bool CalismaAlaniVar { get; set; }
    public bool OtoparkVar { get; set; }
}

public class MekanDetayViewModel
{
    public Mekan Mekan { get; set; } = null!;
    public List<Yorum> Yorumlar { get; set; } = new();
    public List<Etkinlik> Etkinlikler { get; set; } = new();
    public List<MekanFoto> Fotolar { get; set; } = new();
    public YorumFormModel YorumForm { get; set; } = new();
    public HashSet<int> KullaniciBegendigi { get; set; } = new();
    public bool TakipEdiyor { get; set; }
    public List<Kampanya> AktifKampanyalar { get; set; } = new();
    public int BugunNo { get; set; }
    public List<MekanListesi> KullaniciListeleri { get; set; } = new();
}

public class YorumFormModel
{
    [Required(ErrorMessage = "Yorum içeriği zorunlu")]
    public string Icerik { get; set; } = "";
    public int? Puan { get; set; }
}

public class MekanFormModel
{
    [Required(ErrorMessage = "Mekan adı zorunlu")]
    [MaxLength(100)]
    public string Ad { get; set; } = "";

    [Required(ErrorMessage = "Kategori seçin")]
    public string Kategori { get; set; } = "KAFE";

    public string? Sehir { get; set; }

    [Required(ErrorMessage = "Adres zorunlu")]
    public string Adres { get; set; } = "";

    public string? Telefon { get; set; }
    public string? Website { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }

    public bool WifiVar { get; set; }
    public bool PrizVar { get; set; }
    public bool CalismaAlaniVar { get; set; }
    public bool OtoparkVar { get; set; }
    public bool SigaraIcinUygun { get; set; }
    public bool BahceVar { get; set; }
    public bool EngelliErizimiVar { get; set; }
    public bool CanliMuzikVar { get; set; }
    public bool EvcilHayvanIzinli { get; set; }
    public bool CocukOyunAlaniVar { get; set; }
    public bool RezervasyonAktif { get; set; }
    public bool SuAnAcik { get; set; } = true;
    public int? DolulukOrani { get; set; }
    public string? AnlikDuyuru { get; set; }
    public string? AcilisSaati { get; set; }
    public string? KapanisSaati { get; set; }

    public IFormFile? Img { get; set; }
    public IFormFile? RuhsatBelgesi { get; set; }
}

public class HaritaMekanJson
{
    public int Id { get; set; }
    public string Ad { get; set; } = "";
    public string Kategori { get; set; } = "";
    public string KategoriDisplay { get; set; } = "";
    public string Adres { get; set; } = "";
    public double Lat { get; set; }
    public double Lng { get; set; }
    public bool SuAnAcik { get; set; }
    public int? DolulukOrani { get; set; }
    public double? OrtPuan { get; set; }
    public int YorumSayisi { get; set; }
    public string DetayUrl { get; set; } = "";
    public string? ImgUrl { get; set; }
    public bool WifiVar { get; set; }
    public bool PrizVar { get; set; }
    public bool BahceVar { get; set; }
    public bool EvcilHayvanIzinli { get; set; }
    public bool EngelliErizimiVar { get; set; }
    public bool CanliMuzikVar { get; set; }
    public bool SigaraIcinUygun { get; set; }
    public double? MesafeM { get; set; }
}
