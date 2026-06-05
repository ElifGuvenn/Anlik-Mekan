using System.ComponentModel.DataAnnotations;

namespace AnlikMekanCore.Models.Entities;

public class Yorum
{
    public int Id { get; set; }

    public int MekanId { get; set; }
    public Mekan Mekan { get; set; } = null!;

    public string YazarId { get; set; } = "";
    public AppUser Yazar { get; set; } = null!;

    [Required]
    public string Icerik { get; set; } = "";

    public int? Puan { get; set; }

    public DateTime Tarih { get; set; } = DateTime.UtcNow;

    // Navigation
    public ICollection<YorumFoto> Fotolar { get; set; } = new List<YorumFoto>();
    public ICollection<YorumBegeni> Begeniler { get; set; } = new List<YorumBegeni>();
    public YorumYanit? Yanit { get; set; }
}

public class YorumFoto
{
    public int Id { get; set; }
    public int YorumId { get; set; }
    public Yorum Yorum { get; set; } = null!;
    public string FotoUrl { get; set; } = "";
    public DateTime Yuklenme { get; set; } = DateTime.UtcNow;
}

public class YorumBegeni
{
    public int Id { get; set; }
    public int YorumId { get; set; }
    public Yorum Yorum { get; set; } = null!;
    public string KullaniciId { get; set; } = "";
    public AppUser Kullanici { get; set; } = null!;
    public DateTime Tarih { get; set; } = DateTime.UtcNow;
}

public class YorumYanit
{
    public int Id { get; set; }
    public int YorumId { get; set; }
    public Yorum Yorum { get; set; } = null!;
    public string YazanId { get; set; } = "";
    public AppUser Yazan { get; set; } = null!;

    [Required]
    public string Icerik { get; set; } = "";

    public DateTime Tarih { get; set; } = DateTime.UtcNow;
    public DateTime Guncellendi { get; set; } = DateTime.UtcNow;
}
