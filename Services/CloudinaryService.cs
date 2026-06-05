using CloudinaryDotNet;
using CloudinaryDotNet.Actions;

namespace AnlikMekanCore.Services;

public class CloudinaryService
{
    private readonly Cloudinary? _cloudinary;

    public CloudinaryService(IConfiguration config)
    {
        var cloudName = config["Cloudinary:CloudName"];
        var apiKey = config["Cloudinary:ApiKey"];
        var apiSecret = config["Cloudinary:ApiSecret"];

        if (!string.IsNullOrEmpty(cloudName))
        {
            var account = new Account(cloudName, apiKey, apiSecret);
            _cloudinary = new Cloudinary(account);
        }
    }

    public async Task<string?> YukleAsync(IFormFile dosya)
    {
        if (dosya == null || dosya.Length == 0) return null;

        // Cloudinary yapılandırılmamışsa dosyayı geçici olarak sakla
        if (_cloudinary == null)
        {
            var dosyaAdi = $"{Guid.NewGuid()}{Path.GetExtension(dosya.FileName)}";
            var kayitYolu = Path.Combine("wwwroot", "uploads", dosyaAdi);
            Directory.CreateDirectory(Path.Combine("wwwroot", "uploads"));
            await using var localStream = File.Create(kayitYolu);
            await dosya.CopyToAsync(localStream);
            return $"/uploads/{dosyaAdi}";
        }

        using var stream = dosya.OpenReadStream();
        var params_ = new ImageUploadParams
        {
            File = new FileDescription(dosya.FileName, stream),
            Folder = "anlikmekan"
        };
        var result = await _cloudinary.UploadAsync(params_);
        return result.SecureUrl?.ToString();
    }

    public async Task SilAsync(string publicId)
    {
        if (_cloudinary == null) return;
        await _cloudinary.DestroyAsync(new DeletionParams(publicId));
    }
}
