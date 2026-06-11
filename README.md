# Anlık Mekan

Gerçek zamanlı mekan keşif ve yönetim platformu. Kullanıcılar yakınlarındaki kafeler, kütüphaneler, restoranlar ve eczaneleri keşfedebilir; işletme sahipleri mekanlarını yönetebilir.

---

## İçindekiler

- [Özellikler](#özellikler)
- [Teknoloji Yığını](#teknoloji-yığını)
- [Kurulum](#kurulum)

---

## Özellikler

### Kullanıcı Özellikleri
- **Mekan Keşfi** — Şehir, kategori ve özellik filtrelerine (Wi-Fi, priz, bahçe, otopark vb.) göre arama
- **Konum Bazlı Sıralama** — Haversine formülü ile mesafe hesabı, yakın mekanları önce listele
- **Harita Görünümü** — Tüm mekanlara haritadan bakma ve filtre uygulama
- **Favori & Listeler** — Mekanlara favori ekle, özel koleksiyonlar oluştur ve paylaş
- **Yorum & Puanlama** — 1–5 yıldız puanlama, fotoğraflı yorum, yoruma beğeni
- **Rezervasyon** — Tarih, saat ve kişi sayısı ile masa rezervasyonu
- **Etkinlik Takvimi** — Takip edilen ve favori mekanlara ait etkinlik/kampanyaları takvimde gör
- **Bildirimler** — Yorum, beğeni, takip, onay, kampanya ve rezervasyon bildirimleri
- **2FA Güvenliği** — TOTP tabanlı iki faktörlü kimlik doğrulama

### İşletme Sahibi Özellikleri
- **Mekan Yönetimi** — Mekan ekle/düzenle, çalışma saatlerini gün gün tanımla
- **Anlık Durum** — Açık/kapalı durumu, doluluk oranı (%), duyuru metni güncelle
- **Kampanya & Etkinlik** — Tarih aralıklı kampanya ve etkinlik oluştur, fotoğraf ekle
- **Galeri & Menü** — Birden fazla fotoğraf yükle, menü (fotoğraf + PDF) yönet
- **Rezervasyon Yönetimi** — Gelen rezervasyonları onayla veya reddet
- **Takipçi Bildirimi** — Yeni kampanya oluşturulduğunda takipçilere otomatik bildirim

### Admin Özellikleri
- **Mekan Onaylama** — Yeni eklenen mekanları incele, onayla veya reddet


---

## Teknoloji Yığını

| Katman | Teknoloji |
|--------|-----------|
| Framework | ASP.NET Core 10 MVC |
| ORM | Entity Framework Core 9 |
| Veritabanı (yerel) | SQLite |
| Veritabanı (prod) | PostgreSQL |
| Kimlik Doğrulama | ASP.NET Core Identity |
| Resim Depolama | Cloudinary |
| E-posta | MailKit (Gmail SMTP) |
| 2FA / TOTP | Otp.NET + QRCoder |
| Dağıtım | Render (render.yaml + Dockerfile) |

---

## Kurulum

### Gereksinimler

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- (İsteğe bağlı) Cloudinary hesabı — resim yükleme için
- (İsteğe bağlı) Gmail hesabı — e-posta ile şifre sıfırlama için

### Yerel Ortamda Çalıştırma

```bash
git clone https://github.com/ElifGuvenn/Anl-k-Mekan.git
cd Anl-k-Mekan
dotnet run
```

Tarayıcıda `http://localhost:5000` adresini aç.

> İlk çalıştırmada uygulama SQLite veritabanını otomatik oluşturur ve `Data/initial_data.json` dosyasından örnek mekanları yükler.

