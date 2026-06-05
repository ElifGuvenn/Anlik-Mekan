using System.ComponentModel.DataAnnotations;

namespace AnlikMekanCore.Models.ViewModels;

public class LoginViewModel
{
    [Required(ErrorMessage = "Kullanıcı adı zorunlu")]
    public string KullaniciAdi { get; set; } = "";

    [Required(ErrorMessage = "Şifre zorunlu")]
    [DataType(DataType.Password)]
    public string Sifre { get; set; } = "";
}

public class RegisterViewModel
{
    [Required(ErrorMessage = "Kullanıcı adı zorunlu")]
    [MaxLength(50)]
    public string KullaniciAdi { get; set; } = "";

    [Required(ErrorMessage = "E-posta zorunlu")]
    [EmailAddress(ErrorMessage = "Geçerli bir e-posta girin")]
    public string Email { get; set; } = "";

    [Required(ErrorMessage = "Şifre zorunlu")]
    [MinLength(8, ErrorMessage = "Şifre en az 8 karakter olmalı")]
    [DataType(DataType.Password)]
    public string Sifre { get; set; } = "";

    [Required(ErrorMessage = "Şifre tekrar zorunlu")]
    [Compare("Sifre", ErrorMessage = "Şifreler eşleşmiyor")]
    [DataType(DataType.Password)]
    public string SifreTekrar { get; set; } = "";

    public string Rol { get; set; } = "USER"; // USER | OWNER
}

public class SifreSifirlaViewModel
{
    [Required] [EmailAddress]
    public string Email { get; set; } = "";
}

public class SifreKodViewModel
{
    [Required] [MaxLength(6)]
    public string Kod { get; set; } = "";

    [Required] [MinLength(8)]
    [DataType(DataType.Password)]
    public string Sifre1 { get; set; } = "";

    [Required] [Compare("Sifre1")]
    [DataType(DataType.Password)]
    public string Sifre2 { get; set; } = "";
}

public class TotpDogrulaViewModel
{
    [Required] [MaxLength(6)]
    public string TotpCode { get; set; } = "";
}
