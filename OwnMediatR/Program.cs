using FluentValidation;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using OwnMediatR.Ver2;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq.Expressions;
using Wrappers;
var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
//builder.Services.AddOpenApi();
//builder.Services.AddScoped<ICommandHandler<GetAlaCommand, int>, GetAlaCommandHandler>();
builder.Services.AddScoped<Dispatcher>();
builder.Services.AddScoped<Dispatcherv2>();

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


app.MapGet("/weatherforecast", async (Dispatcher dispatcher, OwnMediatR.Ver2.Dispatcherv2 dispatcherv2 ) =>
{
    var commandv2 = new GetAlaCommandV2(20);


    var resV2 = await dispatcherv2.Send(commandv2);

    //-------------------------------------------------

    var command = new GetAlaCommand(20);
    var command2 = new GetAlaWrapperCommand(22);
    Stopwatch sw = Stopwatch.StartNew();
    for (int i = 0; i < 1000000; i++)

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





public record GetAlaCommand(int Age):ICommand<GetAlaCommand, int>;
public record GetAlaWrapperCommand(int Age):ICommandWrapper<GetAlaWrapperCommand, int>;

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


public interface ICommand { }
public interface ICommand<TCommand, TResult> where TCommand : ICommand<TCommand, TResult> { }

public interface ICommandHandler<in TCommand> where TCommand: ICommand
{ 

    Task Handle(TCommand command);

}
public interface ICommandHandler<in TCommand, TResult> where TCommand : ICommand<TCommand, TResult>
{

    Task<TResult> Handle(TCommand command);
}

public class Dispatcher
{
    private readonly IServiceProvider _serviceProvider;

    public Dispatcher(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public  async Task<TResult> Send<TCommand, TResult>(ICommand<TCommand, TResult> command)  where TCommand : ICommand<TCommand, TResult>
    {

        //var commandHandler =  _serviceProvider.GetRequiredService<ICommandHandler<ICommand<TResult>, TResult>>();

        //var handlerType = typeof(ICommandHandler<,>).MakeGenericType(command.GetType(), typeof(TResult));

        var handler =  _serviceProvider.GetRequiredService<ICommandHandler<TCommand, TResult>>();
       
        var result =   await handler.Handle(( TCommand)command);
        
        // var result = await commandHandler.Handle(command);

        return result;
    }

    public async Task Send<TCommand>(IEnumerable<TCommand> commands) where TCommand : ICommand
    {

        //var commandHandler =  _serviceProvider.GetRequiredService<ICommandHandler<ICommand<TResult>, TResult>>();

        foreach (var command in commands)
        {
            var handlerType = typeof(ICommandHandler<>).MakeGenericType(command.GetType());

             var handlers = _serviceProvider.GetServices(handlerType);
            // var handlers = (IEnumerable<ICommandHandler<TCommand>>)_serviceProvider.GetServices(handlerType);
            //  var handlers = _serviceProvider.GetServices<ICommandHandler<TCommand>>();

            foreach (var handler in handlers)
            {
                var hand = (ICommandHandler<ICommand>)handler; // nie działą bo rzutowanie cowariantne, a parametr generyczny kontrawariantny
                await hand.Handle(command);
            }
        }

        // var result = await commandHandler.Handle(command);

    }

    public async Task Send2<TCommand>(IEnumerable<TCommand> commands) where TCommand : ICommand
    {


        foreach (var command in commands)
        {
            await Dispatch(command);
        }


    }
    private static readonly ConcurrentDictionary<Type, Func<object, object, Task>> _dispatchers = new();

    public async Task Dispatch(ICommand command)
    {
        var commandType = command.GetType();

        var dispatcher = _dispatchers.GetOrAdd(commandType, cmdType =>
        {
            var handlerType = typeof(ICommandHandler<>).MakeGenericType(cmdType);
            var method = handlerType.GetMethod("Handle");

            var handlerParam = Expression.Parameter(typeof(object));
            var commandParam = Expression.Parameter(typeof(object));

            var castHandler = Expression.Convert(handlerParam, handlerType);
            var castCommand = Expression.Convert(commandParam, cmdType);

            var call = Expression.Call(castHandler, method!, castCommand);
            var lambda = Expression.Lambda<Func<object, object, Task>>(call, handlerParam, commandParam);

            return lambda.Compile();
        });


        var handlers = _serviceProvider.GetServices(typeof(ICommandHandler<>).MakeGenericType(command.GetType()));

        foreach (var handler in handlers)
        {
            if (handler is null) continue;
            await dispatcher(handler, command);
        }
    }

    public async Task Send3<TCommand>(IEnumerable<TCommand> commands) where TCommand : ICommand
    {


        foreach (var command in commands)
        {
            var commandType = command.GetType(); 
            var handlerType = typeof(ICommandHandler<>).MakeGenericType(commandType);

            var handlers = _serviceProvider.GetServices(handlerType);

            foreach (var handler in handlers)
            {

                var typeForActivatorn = typeof(CommandHandlerAdapter<>).MakeGenericType(commandType);//.MakeGenericType
                var adapter =  (ICommandHandler<ICommand>)Activator.CreateInstance(typeForActivatorn, handler);

                await adapter.Handle(command);
            }

        }


    }

}



static class CommandAndQueriesExtension
{
    public static void AddCommandAndQueries(this IServiceCollection service)
    {

        Type[] typesToRegister = [typeof(ICommandHandler<,>), typeof(ICommandHandler<>), typeof(OwnMediatR.Ver2.ICommandHandler<,>)];//, typeof(IHandlerWrapper<,>)];

        foreach (Type typeToRegister in typesToRegister)
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            var handlerTypes = assemblies
                           .SelectMany(x => x.GetTypes())
                           .Where(x =>  x.IsClass && !x.IsGenericTypeDefinition && x.GetInterfaces()
                                .Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeToRegister))
                           ;

            var handlerTypes2 = handlerTypes.ToList();
                      
            var listArgument = handlerTypes2.Select(a=>a.GetGenericArguments().Count()).ToList();

            handlerTypes.Select(handlerType => new
            {
                ClassType = handlerType,
                InterfaceType = handlerType.GetInterfaces().Single(a => a.GetGenericTypeDefinition() == typeToRegister),

            }).ToList().ForEach(a => service.AddScoped(a.InterfaceType, a.ClassType));

        }


    }
}

public static class ServiceCollectionExtensions
{
  
    public static void TryDecorateOpenGeneric(
    this IServiceCollection services,
    Type openGenericInterface,
    Type openGenericDecorator)
    {
        // 1. znajdź wszystkie matching-descriptory
        var descriptors = services
            .Where(sd => sd.ServiceType.IsGenericType &&
                         sd.ServiceType.GetGenericTypeDefinition() == openGenericInterface)
            .ToList();

        foreach (var descriptor in descriptors)
        {
            var serviceType = descriptor.ServiceType;               // ICommandHandler<,> zamknięty
            var genericArgs = serviceType.GetGenericArguments();
            var closedDecorator = openGenericDecorator.MakeGenericType(genericArgs);

            // 2. usuń oryginał
            services.Remove(descriptor);

            // 3. dodaj nowy – różne ścieżki w zależności od sposobu rejestracji
            //if (descriptor.ImplementationType is { } implType)
            if (descriptor.ImplementationType is Type implType)
                {
                // 3a. klasyczny AddScoped<TInterface, TImpl>()
                services.Add(new ServiceDescriptor(
                    serviceType,
                    provider =>
                    {
                        var original = ActivatorUtilities.CreateInstance(provider, implType);
                        return ActivatorUtilities.CreateInstance(provider, closedDecorator, original);
                    },
                    descriptor.Lifetime));
            }
            //else if (descriptor.ImplementationFactory is { } factory)
            else if (descriptor.ImplementationFactory is Func<IServiceProvider, object> factory)
            {
                // 3b. rejestracja z fabryką
                services.Add(new ServiceDescriptor(
                    serviceType,
                    provider =>
                    {
                        var original = factory(provider);
                        return ActivatorUtilities.CreateInstance(provider, closedDecorator, original);
                    },
                    descriptor.Lifetime));
            }
            //else if (descriptor.ImplementationInstance is { } instance)
            else if (descriptor.ImplementationInstance is object instance)
            {
                // 3c. singleton-instancja
                services.Add(new ServiceDescriptor(
                    serviceType,
                    provider =>
                        ActivatorUtilities.CreateInstance(provider, closedDecorator, instance),
                    descriptor.Lifetime));
            }
            else
            {
                throw new InvalidOperationException("Nieobsługiwany sposób rejestracji.");
            }
        }
    }

}
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

