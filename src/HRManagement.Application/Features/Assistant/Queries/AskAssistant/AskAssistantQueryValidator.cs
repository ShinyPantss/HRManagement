using FluentValidation;

namespace HRManagement.Application.Features.Assistant.Queries.AskAssistant;

/// <summary>
/// Projedeki ilk QUERY validator'ı. Diğer query'lerin girdisi route'tan gelen
/// int ve JWT claim'i olduğu için doğrulanacak bir şey yoktu; burada gerçek bir
/// kullanıcı metni var — uzunluk sınırı hem maliyet hem kötüye kullanım kontrolü.
/// </summary>
public sealed class AskAssistantQueryValidator : AbstractValidator<AskAssistantQuery>
{
    public AskAssistantQueryValidator()
    {
        RuleFor(query => query.ActorUserId)
            .GreaterThan(0).WithMessage("Soruyu soran belirlenemedi.");

        RuleFor(query => query.Question)
            .NotEmpty().WithMessage("Soru boş olamaz.")
            // Uzun metin hem token maliyeti hem prompt injection yüzeyi büyütür.
            .MaximumLength(500).WithMessage("Soru en fazla 500 karakter olabilir.");

        // Sohbet kimliği önbellek anahtarının parçası olacak. Serbest metne izin
        // vermek anahtar alanını kirletir; istemci UUID üretiyor, biçimi zorluyoruz.
        RuleFor(query => query.ConversationId)
            .NotEmpty().WithMessage("Sohbet kimliği boş olamaz.")
            .Matches("^[A-Za-z0-9-]{1,64}$")
            .WithMessage("Sohbet kimliği geçersiz.");
    }
}
