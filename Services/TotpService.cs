using OtpNet;
using QRCoder;
using System.Drawing;

namespace AnlikMekanCore.Services;

public class TotpService
{
    public string YeniSecretOlustur() =>
        Base32Encoding.ToString(KeyGeneration.GenerateRandomKey(20));

    public string GetProvisioningUri(string secret, string hesap, string issuer = "AnlikMekan")
    {
        var totp = new Totp(Base32Encoding.ToBytes(secret));
        return $"otpauth://totp/{Uri.EscapeDataString(issuer)}:{Uri.EscapeDataString(hesap)}" +
               $"?secret={secret}&issuer={Uri.EscapeDataString(issuer)}";
    }

    public bool Dogrula(string secret, string kod)
    {
        if (string.IsNullOrEmpty(secret) || string.IsNullOrEmpty(kod)) return false;
        try
        {
            var totp = new Totp(Base32Encoding.ToBytes(secret));
            return totp.VerifyTotp(kod, out _, VerificationWindow.RfcSpecifiedNetworkDelay);
        }
        catch { return false; }
    }

    public string QrKodBase64(string uri)
    {
        using var qrGenerator = new QRCodeGenerator();
        var qrData = qrGenerator.CreateQrCode(uri, QRCodeGenerator.ECCLevel.L);
        using var qrCode = new PngByteQRCode(qrData);
        var bytes = qrCode.GetGraphic(10);
        return Convert.ToBase64String(bytes);
    }
}
