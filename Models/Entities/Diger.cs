using System.ComponentModel.DataAnnotations;

namespace AnlikMekanCore.Models.Entities;

public class MekanFoto
{
    public int Id { get; set; }
    public int MekanId { get; set; }
    public Mekan Mekan { get; set; } = null!;
    public string FotoUrl { get; set; } = "";
    public int Siralama { get; set; } = 0;
    public DateTime Yuklenme { get; set; } = DateTime.UtcNow;
}

public class Etkinlik
{
    public int Id { get; set; }
    public int MekanId { get; set; }
    public Mekan Mekan { get; set; } = null!;

    [Required] [MaxLength(200)]
    public string Baslik { get; set; } = "";
    public string Aciklama { get; set; } = "";
    public DateTime Baslangic { get; set; }
    public DateTime Bitis { get; set; }
    public string? FotoUrl { get; set; }
    public DateTime Olusturulma { get; set; } = DateTime.UtcNow;

    public bool AktifMi => Bitis >= DateTime.UtcNow;
}

public class Kampanya
{
    public int Id { get; set; }
    public int MekanId { get; set; }
    public Mekan Mekan { get; set; } = null!;

    [Required] [MaxLength(200)]
    public string Baslik { get; set; } = "";

    [Required]
    public string Aciklama { get; set; } = "";

    public DateTime Baslangic { get; set; }
    public DateTime Bitis { get; set; }
    public string? FotoUrl { get; set; }
    public DateTime Olusturuldu { get; set; } = DateTime.UtcNow;

    public bool AktifMi => Baslangic <= DateTime.UtcNow && DateTime.UtcNow <= Bitis;
}

public class Rezervasyon
{
    public int Id { get; set; }
    public int MekanId { get; set; }
    public Mekan Mekan { get; set; } = null!;
    public string KullaniciId { get; set; } = "";
    public AppUser Kullanici { get; set; } = null!;

    public DateOnly Tarih { get; set; }
    public TimeOnly Saat { get; set; }
    public int KisiSayisi { get; set; } = 1;
    public string NotMesaj { get; set; } = "";

    // BEKLIYOR | ONAYLANDI | REDDEDILDI | IPTAL
    public string Durum { get; set; } = "BEKLIYOR";
    public DateTime Olusturuldu { get; set; } = DateTime.UtcNow;
}

public class CalismaGunu
{
    public int Id { get; set; }
    public int MekanId { get; set; }
    public Mekan Mekan { get; set; } = null!;

    public int Gun { get; set; } // 0=Pzt, 6=Paz (Django convention)
    public bool Acik { get; set; } = true;
    public TimeOnly? Acilis { get; set; }
    public TimeOnly? Kapanis { get; set; }

    public static readonly string[] GunAdlari =
        { "Pazartesi", "Salı", "Çarşamba", "Perşembe", "Cuma", "Cumartesi", "Pazar" };
    public string GunAdi => Gun >= 0 && Gun < 7 ? GunAdlari[Gun] : "";
}

public class Bildirim
{
    public int Id { get; set; }
    public string AliciId { get; set; } = "";
    public AppUser Alici { get; set; } = null!;
    public string? GonderenId { get; set; }
    public AppUser? Gonderen { get; set; }

    // YORUM | BEGENI | TAKIP | ONAY | YANIT | KAMPANYA | REZERVASYON | DUYURU
    public string Tip { get; set; } = "YORUM";

    [MaxLength(255)]
    public string Mesaj { get; set; } = "";

    [MaxLength(255)]
    public string Link { get; set; } = "";

    public bool Okundu { get; set; } = false;
    public DateTime Olusturuldu { get; set; } = DateTime.UtcNow;
}

public class Takip
{
    public int Id { get; set; }
    public string TakipciId { get; set; } = "";
    public AppUser Takipci { get; set; } = null!;
    public string TakipEdilenId { get; set; } = "";
    public AppUser TakipEdilen { get; set; } = null!;
    public DateTime Tarih { get; set; } = DateTime.UtcNow;
}

public class MekanListesi
{
    public int Id { get; set; }
    public string OlusturanId { get; set; } = "";
    public AppUser Olusturan { get; set; } = null!;

    [Required] [MaxLength(100)]
    public string Ad { get; set; } = "";
    public string Aciklama { get; set; } = "";
    public bool HerkesAcik { get; set; } = true;
    public DateTime Olusturuldu { get; set; } = DateTime.UtcNow;

    public ICollection<Mekan> Mekanlar { get; set; } = new List<Mekan>();
}

public class SifreSifirlamaKodu
{
    public int Id { get; set; }
    public string UserId { get; set; } = "";
    public AppUser User { get; set; } = null!;

    [MaxLength(6)]
    public string Kod { get; set; } = "";

    public DateTime Olusturuldu { get; set; } = DateTime.UtcNow;
    public bool Kullanildi { get; set; } = false;

    public bool GecerliMi => !Kullanildi && (DateTime.UtcNow - Olusturuldu).TotalMinutes < 15;
}
