
//Curiously Recurring Template Pattern

using Example.CRTP.Contracts.CRTP;
using Example.CRTP.Extensions;
using FluentValidation;
using System.Diagnostics;
using Wrappers;


var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddScoped<Dispatcher>();

builder.Services.AddCommandAndQueries();


builder.Services.AddValidatorsFromAssembly(typeof(Program).Assembly, includeInternalTypes: true);
//builder.Services.TryDecorateOpenGeneric(typeof(ICommandHandler<,>), typeof(ValidationCommandHandlerDecorator<,>));
builder.Services.TryDecorateOpenGeneric(typeof(ICommandHandler<,>), typeof(LoggingCommandHandlerDecorator<,>));


var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();


app.MapGet("/weatherforecast", async (Dispatcher dispatcher) =>
{
    var command = new GetAlaCommand(20);
    var command2 = new GetAlaWrapperCommand(22);
    Stopwatch sw = Stopwatch.StartNew();
    //for (int i = 0; i < 1000000; i++)

    await dispatcher.Send(command);
    Console.WriteLine(sw.ElapsedMilliseconds);
    sw.Stop();
    var res2 = await dispatcher.Send(command2);

    return res2.Status + " " + res2.Data;
});


app.Run();


public class Dispatcher
{
    private readonly IServiceProvider _serviceProvider;

    public Dispatcher(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public Task<TResult> Send<TCommand, TResult>(ICommand<TCommand, TResult> command) where TCommand : ICommand<TCommand, TResult>
    {
        using var scope = _serviceProvider.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<ICommandHandler<TCommand, TResult>>();

        var result = handler.Handle((TCommand)command);

        return result;
    }

    public async Task Send<TEvent>(IEnumerable<TEvent> events) where TEvent : IEvent<TEvent>
    {
        foreach (var command in events)
        {
            var handlers = _serviceProvider.GetServices<IEventHandler<TEvent>>();

            foreach (var handler in handlers)
            {
                var hand = handler;
                await hand.Handle(command);
            }
        }
    }

}



public record GetAlaCommand(int Age) : ICommand<GetAlaCommand, int>;
public record GetAlaWrapperCommand(int Age) : ICommandWrapper<GetAlaWrapperCommand, int>;

public class GetAlaCommandHandler : ICommandHandler<GetAlaCommand, int>
{

    public Task<int> Handle(GetAlaCommand command)
    {
        return Task.FromResult(command.Age);
    }
}

public class GetAlaCommandHandler2 : IHandlerWrapper<GetAlaWrapperCommand, int>
{

    public Task<MessageAndStatusAndData<int>> Handle(GetAlaWrapperCommand command)
    {
        return Task.FromResult(MessageAndStatusAndData<int>.Ok(command.Age));
    }
}





namespace Wrappers //for test :)
{
    public interface ICommandWrapper<TWrapper, TResult> : ICommand<TWrapper, MessageAndStatusAndData<TResult>>
        where TWrapper : ICommandWrapper<TWrapper, TResult>
    { }
    public interface IHandlerWrapper<in TWrapper, TResult> : ICommandHandler<TWrapper, MessageAndStatusAndData<TResult>>
        where TWrapper : ICommandWrapper<TWrapper, TResult>
    {

    }
}

public class MessageAndStatus
{
    public bool IsError => Status == MessageSatus.Error;
    public string Status { get; set; }
    public string Message { get; set; }

}

public class MessageSatus
{
    public const string OK = "OK";
    public const string Error = "ERROR";

}

public class MessageAndStatusAndData<T> : MessageAndStatus
{
    private MessageAndStatusAndData(T data, string msg, string status)
    {
        Data = data;
        Message = msg;
        Status = status;
    }

    public T Data { get; set; }

    public static MessageAndStatusAndData<T> Ok(T data) =>
        new MessageAndStatusAndData<T>(data, string.Empty, MessageSatus.OK);

    public static MessageAndStatusAndData<T> Fail(string msg) =>
       new MessageAndStatusAndData<T>(default, msg, MessageSatus.Error);
}
//---------------------------------------------





class LoggingCommandHandlerDecorator<TCommand, TResult> : ICommandHandler<TCommand, TResult>
    where TCommand : ICommand<TCommand, TResult>
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
    where TCommand : ICommand<TCommand, TResult>
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