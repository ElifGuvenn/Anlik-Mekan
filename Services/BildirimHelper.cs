using AnlikMekanCore.Models;
using AnlikMekanCore.Models.Entities;

namespace AnlikMekanCore.Services;

public static class BildirimHelper
{
    public static async Task OlusturAsync(AppDbContext db,
        string aliciId, string tip, string mesaj,
        string link = "", string? gonderenId = null)
    {
        if (aliciId == gonderenId) return; // kendine bildirim gönderme
        db.Bildirimler.Add(new Bildirim
        {
            AliciId = aliciId,
            GonderenId = gonderenId,
            Tip = tip,
            Mesaj = mesaj,
            Link = link
        });
        await db.SaveChangesAsync();
    }

    public static async Task TakipcilareBildirimAsync(AppDbContext db,
        string sahibiId, string tip, string mesaj, string link = "")
    {
        var takipciler = db.Takipler
            .Where(t => t.TakipEdilenId == sahibiId)
            .Select(t => t.TakipciId)
            .ToList();

        foreach (var tid in takipciler)
        {
            db.Bildirimler.Add(new Bildirim
            {
                AliciId = tid,
                GonderenId = null,
                Tip = tip,
                Mesaj = mesaj,
                Link = link
            });
        }
        if (takipciler.Any())
            await db.SaveChangesAsync();
    }
}
