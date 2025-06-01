using FluentValidation;

namespace OwnMediatR.Validators;

public class GetAlaCommandValidator : AbstractValidator<GetAlaCommand>
{
    public GetAlaCommandValidator()
    {
        RuleFor(a => a.Age).GreaterThan(30);
    }
}
