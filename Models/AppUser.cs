using Microsoft.AspNetCore.Identity;
using AnlikMekanCore.Models.Entities;

namespace AnlikMekanCore.Models;

public class AppUser : IdentityUser
{
    public string Rol { get; set; } = "USER"; // USER | OWNER
    public string? FotoUrl { get; set; }
    public bool EmailDogrulandi { get; set; } = false;
    public string EmailDogrulamaToken { get; set; } = Guid.NewGuid().ToString();
    public string? TotpSecretKey { get; set; }
    public bool TwoFaDogrulandi { get; set; } = false;

    // Navigation
    public ICollection<Mekan> Mekanlar { get; set; } = new List<Mekan>();
    public ICollection<Yorum> Yorumlar { get; set; } = new List<Yorum>();
    public ICollection<Mekan> FavoriMekanlar { get; set; } = new List<Mekan>();
    public ICollection<Bildirim> Bildirimler { get; set; } = new List<Bildirim>();
    public ICollection<Takip> TakipEttikleri { get; set; } = new List<Takip>();
    public ICollection<Takip> Takipcileri { get; set; } = new List<Takip>();
    public ICollection<MekanListesi> MekanListeleri { get; set; } = new List<MekanListesi>();
    public ICollection<Rezervasyon> Rezervasyonlar { get; set; } = new List<Rezervasyon>();
}
