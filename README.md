# Anlık Mekan — ASP.NET Core MVC

Gerçek zamanlı mekan keşif uygulaması.

## Kurulum

### Gereksinimler
- [.NET 10 SDK](https://dotnet.microsoft.com/download)

### Çalıştırma

```bash
git clone https://github.com/ElifGuvenn/Anl-k-Mekan.git
cd Anl-k-Mekan
dotnet run
```

Tarayıcıda: `http://localhost:5000`

> Uygulama ilk çalıştığında veritabanını otomatik oluşturur.


## Kullanıcı Rolleri

| Rol | Açıklama |
|-----|----------|
| USER | Mekan keşfeder, yorum yazar, favori ekler, liste oluşturur |
| OWNER | Mekan ekler/düzenler, kampanya oluşturur, rezervasyon yönetir |
| Admin | `http://localhost:5000/Admin/Login` → Admin/Admin1234! |

## Özellikler

- Mekan arama, filtreleme (Wi-Fi, Priz, Bahçe vb.), harita görünümü
- Konum bazlı "Yakınımdakiler" sıralama
- İşletme sahibi paneli (doluluk, çalışma saatleri, kampanya)
- 2FA/TOTP ile güvenli giriş
- Favori mekanlar, liste oluşturma, rezervasyon
