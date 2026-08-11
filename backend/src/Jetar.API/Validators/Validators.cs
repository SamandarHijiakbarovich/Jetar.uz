using FluentValidation;
using Jetar.Application.Contracts;

namespace Jetar.API.Validators;

public class RegisterRequestValidator : AbstractValidator<RegisterRequest>
{
    // Harflar (lotin va kirill), apostrof, defis va bo'shliq.
    private const string NamePattern = @"^[\p{L}][\p{L}'’\- ]{1,49}$";

    public RegisterRequestValidator()
    {
        RuleFor(x => x.FirstName)
            .NotEmpty().WithMessage("Ismingizni kiriting.")
            .Matches(NamePattern).WithMessage("Ism 2–50 ta harfdan iborat bo'lsin.");

        RuleFor(x => x.LastName)
            .NotEmpty().WithMessage("Familiyangizni kiriting.")
            .Matches(NamePattern).WithMessage("Familiya 2–50 ta harfdan iborat bo'lsin.");

        RuleFor(x => x.Username)
            .NotEmpty().WithMessage("Foydalanuvchi nomini kiriting.")
            .Matches("^@?[A-Za-z0-9_]{3,32}$").WithMessage("3–32 belgi: harf, raqam va pastki chiziq.");

        RuleFor(x => x.Phone)
            .NotEmpty().WithMessage("Telefon raqamini kiriting.")
            .Matches(@"^\+?[\d\s]{9,20}$").WithMessage("Telefon raqami noto'g'ri.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Parolni kiriting.")
            .MinimumLength(6).WithMessage("Parol kamida 6 belgidan iborat bo'lsin.");
    }
}

public class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.Login).NotEmpty().WithMessage("Login yoki telefon raqamini kiriting.");
        RuleFor(x => x.Password).NotEmpty().WithMessage("Parolni kiriting.");
    }
}

public class CreateListingRequestValidator : AbstractValidator<CreateListingRequest>
{
    public CreateListingRequestValidator()
    {
        RuleFor(x => x.Type).IsInEnum().WithMessage("E'lon turini tanlang.");
        RuleFor(x => x.GameType).IsInEnum().WithMessage("O'yinni tanlang.");

        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Sarlavhani kiriting.")
            .MinimumLength(10).WithMessage("Sarlavha kamida 10 belgidan iborat bo'lsin.")
            .MaximumLength(160).WithMessage("Sarlavha 160 belgidan oshmasin.");

        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("Tavsifni kiriting.")
            .MinimumLength(20).WithMessage("Tavsif kamida 20 belgidan iborat bo'lsin.")
            .MaximumLength(4000);

        RuleFor(x => x.Price)
            .GreaterThan(0).WithMessage("Narxni kiriting.");

        RuleFor(x => x.ServerRegion)
            .NotEmpty()
            .Must(r => new[] { "ASIA", "EU", "NA", "CIS" }.Contains(r.ToUpperInvariant()))
            .WithMessage("Server: ASIA, EU, NA yoki CIS.");

        RuleFor(x => x.Images)
            .Must(i => i == null || i.Count <= 8).WithMessage("Maksimal 8 ta rasm.");
    }
}

public class OpenDisputeRequestValidator : AbstractValidator<OpenDisputeRequest>
{
    public OpenDisputeRequestValidator()
    {
        RuleFor(x => x.Reason)
            .NotEmpty().WithMessage("Nizo sababini yozing.")
            .MinimumLength(15).WithMessage("Sababni batafsilroq yozing (kamida 15 belgi).")
            .MaximumLength(2000);
    }
}

public class CreateRatingRequestValidator : AbstractValidator<CreateRatingRequest>
{
    public CreateRatingRequestValidator()
    {
        RuleFor(x => x.Score).InclusiveBetween((short)1, (short)5).WithMessage("Baho 1 dan 5 gacha.");
        RuleFor(x => x.Comment).MaximumLength(1000);
    }
}

public class SendMessageRequestValidator : AbstractValidator<SendMessageRequest>
{
    public SendMessageRequestValidator()
    {
        RuleFor(x => x.TransactionId).NotEmpty();
        RuleFor(x => x.Text)
            .MaximumLength(2000).WithMessage("Xabar 2000 belgidan oshmasin.");
    }
}
