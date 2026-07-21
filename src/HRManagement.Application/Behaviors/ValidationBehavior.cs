using FluentValidation;
using MediatR;

namespace HRManagement.Application.Behaviors;

/// <summary>
/// MediatR pipeline halkası: her mesaj handler'a ulaşmadan ÖNCE buradan geçer.
/// İlgili mesaj için kayıtlı validator varsa çalıştırır; hata varsa handler hiç
/// çağrılmaz, ValidationException fırlar. Böylece input validation'ı 26 handler'a
/// tek tek yazmak yerine tek yerde topluyoruz.
/// </summary>
public sealed class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
    {
        _validators = validators;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        // Validator'ı olmayan mesajlar (ör. basit query'ler) doğrudan geçer.
        if (!_validators.Any())
            return await next(cancellationToken);

        var context = new ValidationContext<TRequest>(request);

        var results = await Task.WhenAll(
            _validators.Select(validator => validator.ValidateAsync(context, cancellationToken)));

        var failures = results
            .SelectMany(result => result.Errors)
            .Where(failure => failure is not null)
            .ToList();

        if (failures.Count > 0)
            throw new ValidationException(failures);

        return await next(cancellationToken);
    }
}
