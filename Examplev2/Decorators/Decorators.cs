using Contracts;
using FluentValidation;

namespace Examplev2.Decorators;

class LoggingCommandHandlerDecorator<TCommand, TResult> : ICommandHandler<TCommand, TResult>
    where TCommand : ICommand<TResult>
{
    private readonly ICommandHandler<TCommand, TResult> _inner;

    public LoggingCommandHandlerDecorator(ICommandHandler<TCommand, TResult> inner)
    {
        _inner = inner;
    }

    public async Task<TResult> Handle(TCommand command)
    {
        Console.WriteLine($"[LOG] Handling {typeof(TCommand).Name}");
        return await _inner.Handle(command);
    }
}

class ValidationCommandHandlerDecorator<TCommand, TResult> : ICommandHandler<TCommand, TResult>
    where TCommand : ICommand<TResult>
{
    private readonly ICommandHandler<TCommand, TResult> _inner;
    private readonly IEnumerable<IValidator<TCommand>> _validators;

    public ValidationCommandHandlerDecorator(ICommandHandler<TCommand, TResult> inner, IEnumerable<IValidator<TCommand>> validators)
    {
        _inner = inner;
        _validators = validators;
    }

    public async Task<TResult> Handle(TCommand command)
    {

        if (_validators.Any())
        {
            foreach (var validator in _validators)
            {

                var results = await validator.ValidateAsync(command);

                if (!results.IsValid)
                {

                    Console.WriteLine($"[VALIDATION]  {string.Join(',', results.Errors.Select(a => a.ErrorMessage))}");

                    return default;

                }

            }

        }
        return await _inner.Handle(command);
    }
}