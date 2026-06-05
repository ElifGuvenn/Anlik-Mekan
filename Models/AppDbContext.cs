using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using AnlikMekanCore.Models.Entities;

namespace AnlikMekanCore.Models;

public class AppDbContext : IdentityDbContext<AppUser>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Mekan> Mekanlar => Set<Mekan>();
    public DbSet<MekanFoto> MekanFotolar => Set<MekanFoto>();
    public DbSet<Yorum> Yorumlar => Set<Yorum>();
    public DbSet<YorumFoto> YorumFotolar => Set<YorumFoto>();
    public DbSet<YorumBegeni> YorumBegeniler => Set<YorumBegeni>();
    public DbSet<YorumYanit> YorumYanitlar => Set<YorumYanit>();
    public DbSet<Etkinlik> Etkinlikler => Set<Etkinlik>();
    public DbSet<Kampanya> Kampanyalar => Set<Kampanya>();
    public DbSet<Rezervasyon> Rezervasyonlar => Set<Rezervasyon>();
    public DbSet<CalismaGunu> CalismaGunleri => Set<CalismaGunu>();
    public DbSet<Bildirim> Bildirimler => Set<Bildirim>();
    public DbSet<Takip> Takipler => Set<Takip>();
    public DbSet<MekanListesi> MekanListeleri => Set<MekanListesi>();
    public DbSet<SifreSifirlamaKodu> SifreSifirlamaKodlari => Set<SifreSifirlamaKodu>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Mekan — Sahibi (User)
        builder.Entity<Mekan>()
            .HasOne(m => m.Sahibi)
            .WithMany(u => u.Mekanlar)
            .HasForeignKey(m => m.SahibiId)
            .OnDelete(DeleteBehavior.SetNull);

        // Mekan — Favoriler (M2M)
        builder.Entity<Mekan>()
            .HasMany(m => m.Favorileyenler)
            .WithMany(u => u.FavoriMekanlar)
            .UsingEntity(j => j.ToTable("MekanFavoriler"));

        // Yorum — Mekan
        builder.Entity<Yorum>()
            .HasOne(y => y.Mekan)
            .WithMany(m => m.Yorumlar)
            .HasForeignKey(y => y.MekanId)
            .OnDelete(DeleteBehavior.Cascade);

        // Yorum — Yazar
        builder.Entity<Yorum>()
            .HasOne(y => y.Yazar)
            .WithMany(u => u.Yorumlar)
            .HasForeignKey(y => y.YazarId)
            .OnDelete(DeleteBehavior.Cascade);

        // YorumBegeni — unique constraint
        builder.Entity<YorumBegeni>()
            .HasIndex(yb => new { yb.YorumId, yb.KullaniciId })
            .IsUnique();

        // YorumYanit — bire bir
        builder.Entity<YorumYanit>()
            .HasOne(yy => yy.Yorum)
            .WithOne(y => y.Yanit)
            .HasForeignKey<YorumYanit>(yy => yy.YorumId);

        // Takip — unique constraint
        builder.Entity<Takip>()
            .HasIndex(t => new { t.TakipciId, t.TakipEdilenId })
            .IsUnique();

        builder.Entity<Takip>()
            .HasOne(t => t.Takipci)
            .WithMany(u => u.TakipEttikleri)
            .HasForeignKey(t => t.TakipciId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Takip>()
            .HasOne(t => t.TakipEdilen)
            .WithMany(u => u.Takipcileri)
            .HasForeignKey(t => t.TakipEdilenId)
            .OnDelete(DeleteBehavior.Restrict);

        // Bildirim
        builder.Entity<Bildirim>()
            .HasOne(b => b.Alici)
            .WithMany(u => u.Bildirimler)
            .HasForeignKey(b => b.AliciId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<Bildirim>()
            .HasOne(b => b.Gonderen)
            .WithMany()
            .HasForeignKey(b => b.GonderenId)
            .OnDelete(DeleteBehavior.SetNull);

        // CalismaGunu — unique per mekan+gun
        builder.Entity<CalismaGunu>()
            .HasIndex(c => new { c.MekanId, c.Gun })
            .IsUnique();

        // MekanListesi — Mekanlar (M2M)
        builder.Entity<MekanListesi>()
            .HasMany(ml => ml.Mekanlar)
            .WithMany()
            .UsingEntity(j => j.ToTable("MekanListesiMekanlar"));

        // MekanListesi — Olusturan
        builder.Entity<MekanListesi>()
            .HasOne(ml => ml.Olusturan)
            .WithMany(u => u.MekanListeleri)
            .HasForeignKey(ml => ml.OlusturanId)
            .OnDelete(DeleteBehavior.Cascade);

        // Rezervasyon
        builder.Entity<Rezervasyon>()
            .HasOne(r => r.Kullanici)
            .WithMany(u => u.Rezervasyonlar)
            .HasForeignKey(r => r.KullaniciId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
