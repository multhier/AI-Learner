using Microsoft.AspNetCore.Identity;

namespace Kivilcim.Services;

/// <summary>ASP.NET Core Identity hata mesajlarının Türkçesi.</summary>
public class TurkishIdentityErrorDescriber : IdentityErrorDescriber
{
    public override IdentityError DefaultError() => New(nameof(DefaultError), "Bilinmeyen bir hata oluştu.");
    public override IdentityError ConcurrencyFailure() => New(nameof(ConcurrencyFailure), "Kayıt başka biri tarafından değiştirildi, tekrar deneyin.");
    public override IdentityError PasswordMismatch() => New(nameof(PasswordMismatch), "Şifre yanlış.");
    public override IdentityError InvalidToken() => New(nameof(InvalidToken), "Geçersiz bağlantı ya da kod.");
    public override IdentityError LoginAlreadyAssociated() => New(nameof(LoginAlreadyAssociated), "Bu giriş yöntemi başka bir hesaba bağlı.");
    public override IdentityError InvalidUserName(string? userName) => New(nameof(InvalidUserName), $"'{userName}' geçersiz bir kullanıcı adı. Sadece harf, rakam ve - . _ @ + kullanılabilir.");
    public override IdentityError InvalidEmail(string? email) => New(nameof(InvalidEmail), $"'{email}' geçerli bir e-posta adresi değil.");
    public override IdentityError DuplicateUserName(string userName) => New(nameof(DuplicateUserName), $"'{userName}' zaten kullanılıyor.");
    public override IdentityError DuplicateEmail(string email) => New(nameof(DuplicateEmail), $"'{email}' adresiyle zaten bir hesap var.");
    public override IdentityError InvalidRoleName(string? role) => New(nameof(InvalidRoleName), $"'{role}' geçersiz bir rol adı.");
    public override IdentityError DuplicateRoleName(string role) => New(nameof(DuplicateRoleName), $"'{role}' rolü zaten var.");
    public override IdentityError UserAlreadyHasPassword() => New(nameof(UserAlreadyHasPassword), "Bu hesabın zaten bir şifresi var.");
    public override IdentityError UserLockoutNotEnabled() => New(nameof(UserLockoutNotEnabled), "Bu hesap için kilitleme açık değil.");
    public override IdentityError UserAlreadyInRole(string role) => New(nameof(UserAlreadyInRole), $"Kullanıcı zaten '{role}' rolünde.");
    public override IdentityError UserNotInRole(string role) => New(nameof(UserNotInRole), $"Kullanıcı '{role}' rolünde değil.");
    public override IdentityError PasswordTooShort(int length) => New(nameof(PasswordTooShort), $"Şifre en az {length} karakter olmalı.");
    public override IdentityError PasswordRequiresUniqueChars(int uniqueChars) => New(nameof(PasswordRequiresUniqueChars), $"Şifrede en az {uniqueChars} farklı karakter olmalı.");
    public override IdentityError PasswordRequiresNonAlphanumeric() => New(nameof(PasswordRequiresNonAlphanumeric), "Şifrede en az bir sembol (!, ?, * gibi) olmalı.");
    public override IdentityError PasswordRequiresDigit() => New(nameof(PasswordRequiresDigit), "Şifrede en az bir rakam (0-9) olmalı.");
    public override IdentityError PasswordRequiresLower() => New(nameof(PasswordRequiresLower), "Şifrede en az bir küçük harf (a-z) olmalı.");
    public override IdentityError PasswordRequiresUpper() => New(nameof(PasswordRequiresUpper), "Şifrede en az bir büyük harf (A-Z) olmalı.");
    public override IdentityError RecoveryCodeRedemptionFailed() => New(nameof(RecoveryCodeRedemptionFailed), "Kurtarma kodu kullanılamadı.");

    private static IdentityError New(string code, string description) => new() { Code = code, Description = description };
}
