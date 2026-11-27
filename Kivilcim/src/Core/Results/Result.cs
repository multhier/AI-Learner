namespace Kivilcim.Core.Results;

/// <summary>Bir doğrulama ya da işlem hatası. Field boşsa genel hatadır.</summary>
public sealed record ValidationError(string Field, string Message);

/// <summary>İşlem sonucu: başarılı ya da hata listesi. İstisna yerine kullanılır.</summary>
public class Result
{
    protected Result(IEnumerable<ValidationError>? errors)
    {
        Errors = errors?.ToList() ?? new List<ValidationError>();
    }

    public IReadOnlyList<ValidationError> Errors { get; }

    public bool Succeeded => Errors.Count == 0;

    public string FirstError => Errors.Count > 0 ? Errors[0].Message : string.Empty;

    public static Result Success() => new(null);

    public static Result Failure(string message) => new(new[] { new ValidationError(string.Empty, message) });

    public static Result Failure(IEnumerable<ValidationError> errors) => new(errors);

    public static Result<T> Success<T>(T value) => new(value, null);

    public static Result<T> Failure<T>(string message) => new(default, new[] { new ValidationError(string.Empty, message) });

    public static Result<T> Failure<T>(IEnumerable<ValidationError> errors) => new(default, errors);
}

/// <summary>Değer taşıyan işlem sonucu.</summary>
public sealed class Result<T> : Result
{
    internal Result(T? value, IEnumerable<ValidationError>? errors) : base(errors)
    {
        Value = value;
    }

    public T? Value { get; }
}
