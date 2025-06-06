using Contracts.CRTP;
using Microsoft.AspNetCore.Http.HttpResults;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using Wrappers;
using OwnMediatR.Lib.Extensions;


var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddScoped<Dispatcher>();

builder.Services.AddCommandAndQueries();

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
    for (int i = 0; i < 1000000; i++)

        await dispatcher.Send(command);
    Console.WriteLine(sw.ElapsedMilliseconds);
    sw.Stop();
    var res2 = await dispatcher.Send(command2);

    return "OK";
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

        //var commandHandler =  _serviceProvider.GetRequiredService<ICommandHandler<ICommand<TResult>, TResult>>();

        //var handlerType = typeof(ICommandHandler<,>).MakeGenericType(command.GetType(), typeof(TResult));

        using var scope = _serviceProvider.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<ICommandHandler<TCommand, TResult>>();

        var result = handler.Handle((TCommand)command);

        // var result = await commandHandler.Handle(command);

        return result;
    }

    public async Task Send<TCommand>(IEnumerable<TCommand> commands) where TCommand : ICommand<TCommand>
    {

        //var commandHandler =  _serviceProvider.GetRequiredService<ICommandHandler<ICommand<TResult>, TResult>>();

        foreach (var command in commands)
        {
            //var handlerType = typeof(ICommandHandler<>).MakeGenericType(command.GetType());

            var handlers = _serviceProvider.GetServices<ICommandHandler<TCommand>>();
            // var handlers = (IEnumerable<ICommandHandler<TCommand>>)_serviceProvider.GetServices(handlerType);
            //  var handlers = _serviceProvider.GetServices<ICommandHandler<TCommand>>();

            foreach (var handler in handlers)
            {
                var hand = handler; // nie działą bo rzutowanie cowariantne, a parametr generyczny kontrawariantny
                await hand.Handle(command);
            }
        }

        // var result = await commandHandler.Handle(command);

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
