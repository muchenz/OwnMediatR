using Contracts;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq.Expressions;
using Wrappers;
using OwnMediatR.Lib.Dispatchers.Wrapperv1;
using OwnMediatR.Lib.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
//builder.Services.AddOpenApi();
//builder.Services.AddScoped<ICommandHandler<GetAlaCommand, int>, GetAlaCommandHandler>();
builder.Services.AddScoped<Dispatcher>();

builder.Services.AddCommandAndQueries();

builder.Services.AddValidatorsFromAssembly(typeof(Program).Assembly, includeInternalTypes:true);
//builder.Services.TryDecorateOpenGeneric(typeof(ICommandHandler<,>), typeof(ValidationCommandHandlerDecorator<,>));
builder.Services.TryDecorateOpenGeneric(typeof(ICommandHandler<,>), typeof(LoggingCommandHandlerDecorator<,>));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
   // app.MapOpenApi();
}

app.UseHttpsRedirection();


app.MapGet("/weatherforecast", async (Dispatcher dispatcher) =>
{



    //-------------------------------------------------

    var command = new GetAlaCommand(20);
    var command2 = new GetAlaWrapperCommand(22);
    Stopwatch sw = Stopwatch.StartNew();
    //for (int i = 0; i < 1000000; i++)

        await dispatcher.Send(command);
    Console.WriteLine(sw.ElapsedMilliseconds);
    sw.Stop();
    var res2 = await dispatcher.Send(command2);


    //ICommand[] tab = [new AlaArrivedCommand(), new AlaArrivedCommand2()];
    //AlaArrivedCommand[] tab1 = [new AlaArrivedCommand(), new AlaArrivedCommand()];

    //await  dispatcher.Send2(tab1);
    //return Results.Ok(res + " " + res2.Data);
});



app.Run();





public record GetAlaCommand(int Age):ICommand<int>;
public record GetAlaWrapperCommand(int Age):ICommandWrapper<int>;

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

                    Console.WriteLine($"[VALIDATION]  {string.Join(',', results.Errors.Select(a=>a.ErrorMessage))}");

                    return default;

                }

            }

        }
            return await _inner.Handle(command);
    }
}


namespace Wrappers //for test :)
{
    public interface ICommandWrapper<TResult> : ICommand<MessageAndStatusAndData<TResult>>
    { }
    public interface IHandlerWrapper<in TWrapper, TResult> : ICommandHandler<TWrapper, MessageAndStatusAndData<TResult>>
        where TWrapper : ICommandWrapper<TResult>
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

public class TokenAndEmailData
{
    public string Token { get; set; }
    public string Email { get; set; }
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


///////////
///
public record AlaArrivedCommand:ICommand;
public record AlaArrivedCommand2:ICommand;

public class AlaArrivedCommandHandler : ICommandHandler<AlaArrivedCommand>
{
    public Task Handle(AlaArrivedCommand command)
    {

        System.Console.WriteLine("ala arrived - handler 1");
        return Task.CompletedTask;
    }
}

public class AlaArrivedCommandHandler2 : ICommandHandler<AlaArrivedCommand>
{
    public Task Handle(AlaArrivedCommand command)
    {

        System.Console.WriteLine("ala arrived - handler 2");
        return Task.CompletedTask;
    }
}

public class AlaArrivedCommand2Handler : ICommandHandler<AlaArrivedCommand2>
{
    public Task Handle(AlaArrivedCommand2 command)
    {

        System.Console.WriteLine("ala arrived 2 - handler 1");
        return Task.CompletedTask;
    }
}


public class CommandHandlerAdapter<TCommand> : ICommandHandler<ICommand>
    where TCommand : ICommand
{
    private readonly ICommandHandler<TCommand> _inner;

    public CommandHandlerAdapter(ICommandHandler<TCommand> inner)
    {
        _inner = inner;
    }

    public Task Handle(ICommand command)
    {
        return _inner.Handle((TCommand)command); // bezpieczne, bo wiemy że to TCommand
    }
}

