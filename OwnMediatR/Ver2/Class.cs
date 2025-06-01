using System.Collections.Concurrent;
using System.Linq.Expressions;
using Wrappers;

namespace OwnMediatR.Ver2;



public interface ICommand { }
public interface ICommand<out TResult>  { }

public interface ICommandHandler<in ICommand> { }
public interface ICommandHandler<in TCommand, TResult> where TCommand : ICommand<TResult>
{

    Task<TResult> Handle(TCommand command);
}

public record AlaArrivedCommandV2 : ICommand;
public record AlaArrivedCommandV2_2 : ICommand;

public record GetAlaCommandV2(int Age) : ICommand<int>;
//public record GetAlaWrapperCommand(int Age) : ICommandWrapper<int>;

public class Dispatcherv2
{
    private readonly IServiceProvider _serviceProvider;

    public Dispatcherv2(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }
    public async Task<TResult> Send<TResult>(ICommand<TResult> command)
    {
        var handlerType = typeof(ICommandHandler<,>).MakeGenericType(command.GetType(), typeof(TResult));

        dynamic handler = _serviceProvider.GetRequiredService(handlerType);

        var result = await handler.Handle((dynamic)command);
        return result;
    }

    public async Task<TResult> Send2<TResult>(ICommand<TResult> command)
    {
        var handler = _serviceProvider.GetService(typeof(ICommandHandler<>).MakeGenericType(command.GetType()));
        if (handler is null) throw new NullReferenceException("Handler is null");
        var result = await InvokeHandleAsync2(handler, command);

        return (TResult)result;
    }
    public async Task Send(ICommand command)
    {
        var handlerType = typeof(ICommandHandler<>).MakeGenericType(command.GetType());

        dynamic handler = _serviceProvider.GetRequiredService(handlerType);

         await handler.Handle((dynamic)command);
    }

    //public async Task<TResult> SendRef<TResult>(ICommand<TResult> command)
    //{
    //    var handlerType = typeof(ICommandHandler<,>).MakeGenericType(command.GetType(), typeof(TResult));

    //    var handler = _serviceProvider.GetRequiredService(handlerType);

    //    var method = handlerType.GetMethod("Handle");

    //    if (method == null)
    //        throw new InvalidOperationException("Metoda 'Handle' nie została znaleziona.");

    //    var task = (Task<TResult>)method.Invoke(handler, new object[] { command });

    //    return await task;
    //}


    private static readonly ConcurrentDictionary<(Type handlerType, Type commandType), Func<object, object, Task<object>>> _handleCache
    = new();

    public static Task<object> InvokeHandleAsync(object handler, object command)
    {
        var key = (handler.GetType(), command.GetType());

        var del = _handleCache.GetOrAdd(key, static key =>
        {
            var (handlerType, commandType) = key;

            // handler: (object h, object c) => ((ICommandHandler<T>)h).Handle((TCommand)c)
            var handlerParam = Expression.Parameter(typeof(object), "handler");
            var commandParam = Expression.Parameter(typeof(object), "command");

            var castedHandler = Expression.Convert(handlerParam, handlerType);
            var castedCommand = Expression.Convert(commandParam, commandType);

            var handleMethod = handlerType.GetMethod("Handle", new[] { commandType });
            if (handleMethod == null)
                throw new InvalidOperationException($"No Handle({commandType.Name}) on {handlerType.Name}");

            var call = Expression.Call(castedHandler, handleMethod, castedCommand);
            var lambda = Expression.Lambda<Func<object, object, Task<object>>>(call, handlerParam, commandParam);

            return lambda.Compile();
        });

        return del(handler, command);
    }

    private static readonly ConcurrentDictionary<(Type handlerType, Type commandType), Func<object, object, Task<object>>> _cache = new();

    public Task<object> InvokeHandleAsync2(object handler, object command)
    {
        var key = (handler.GetType(), command.GetType());

        var del = _cache.GetOrAdd(key, static key =>
        {
            var method = key.Item1.GetMethod("Handle", new[] { key.Item2 });

            return (Func<object, object, Task<object>>)Delegate.CreateDelegate(
                typeof(Func<object, object, Task<object>>),
                null, method
            );
        });

        return del(handler, command); // ⚡ ~50–100 ns
    }
}

public class GetAlaCommandHandlerV2 : ICommandHandler<GetAlaCommandV2, int>
{

    public Task<int> Handle(GetAlaCommandV2 command)
    {
        Console.WriteLine(nameof (GetAlaCommandHandlerV2));
        return Task.FromResult(command.Age);
    }
}