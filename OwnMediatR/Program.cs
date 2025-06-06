using FluentValidation;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq.Expressions;
using Wrappers;
using OwnMediatR.Lib.Extensions;
using Contracts;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
//builder.Services.AddOpenApi();
//builder.Services.AddScoped<ICommandHandler<GetAlaCommand, int>, GetAlaCommandHandler>();
builder.Services.AddScoped<Dispatcher>();

builder.Services.AddCommandAndQueries();

builder.Services.AddValidatorsFromAssembly(typeof(Program).Assembly, includeInternalTypes:true);
//builder.Services.TryDecorateOpenGeneric(typeof(ICommandHandler<,>), typeof(ValidationCommandHandlerDecorator<,>));
//builder.Services.TryDecorateOpenGeneric(typeof(ICommandHandler<,>), typeof(LoggingCommandHandlerDecorator<,>));

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

   


    //ICommand[] tab = [new AlaArrivedCommand(), new AlaArrivedCommand2()];
    //AlaArrivedCommand[] tab1 = [new AlaArrivedCommand(), new AlaArrivedCommand()];

    //await  dispatcher.Send2(tab1);
    //return Results.Ok(res + " " + res2.Data);
});



app.Run();







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

                    Console.WriteLine($"[VALIDATION]  {string.Join(',', results.Errors.Select(a=>a.ErrorMessage))}");

                    return default;

                }

            }

        }
            return await _inner.Handle(command);
    }
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

