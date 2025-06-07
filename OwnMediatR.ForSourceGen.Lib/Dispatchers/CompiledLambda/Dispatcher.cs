using Contracts;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Concurrent;
using System.Linq.Expressions;

namespace OwnMediatR.ForSourceGen.Lib.Dispatchers.CompiledLambda;

public class Dispatcher
{
    private readonly IServiceProvider _serviceProvider;

    public Dispatcher(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }


    public Task<TResult> Send<TResult>(ICommand<TResult> command)
    {
        using var scope = _serviceProvider.CreateScope();

        var handler = scope.ServiceProvider.GetRequiredService(typeof(ICommandHandler<,>).MakeGenericType(command.GetType(), typeof(TResult)));
        if (handler is null) throw new NullReferenceException("Handler is null");
        var task = (Task<TResult>)InvokeLamdaAsync(handler, command);


        return task;
    }

    public async Task Send(ICommand command)
    {
        var handler = _serviceProvider.GetRequiredService(typeof(ICommandHandler<>).MakeGenericType(command.GetType()));
        if (handler is null) throw new NullReferenceException("Handler is null");
        await InvokeLamdaAsync(handler, command);
    }

    public async Task Send(IEvent evet)
    {
        using var scope = _serviceProvider.CreateScope();

        var handlers = scope.ServiceProvider.GetServices(typeof(IEventHandler<>).MakeGenericType(evet.GetType()));

        foreach (var handler in handlers)
        {
            if (handler is null) continue;
            await InvokeLamdaAsync(handler, evet);
        }
    }
    public async Task Send(IEnumerable<IEvent> evets)
    {
        foreach (var evet in evets)
        {
            using var scope = _serviceProvider.CreateScope();

            var handlers = scope.ServiceProvider.GetServices(typeof(IEventHandler<>).MakeGenericType(evet.GetType()));
            foreach (var handler in handlers)
            {
                if (handler is null) continue;

                await InvokeLamdaAsync(handler, evet);
            }
        }
    }

    private static readonly ConcurrentDictionary<(Type handlerType, Type commandType), Func<object, object, Task<object>>> _handleCache
    = new();

    private static Task<object> InvokeLabdaWithResultAsync(object handler, object command) //slow !! - slower than 'invoke' !!
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

            //------------------ casting Task<T> => Task<object>
            var taskResultType = call.Type.GetGenericArguments()[0]; // TResult
            var convertMethod = typeof(TaskCast)
                .GetMethod(nameof(TaskCast.Cast))?
                .MakeGenericMethod(taskResultType);

            var castToObjectTask = Expression.Call(convertMethod!, call); // Task<object>

            return Expression.Lambda<Func<object, object, Task<object>>>(
                castToObjectTask, handlerParam, commandParam).Compile();
        });

        return del(handler, command);
    }
    private static class TaskCast
    {
        public static async Task<object> Cast<T>(Task<T> task)
        {
            return await task.ConfigureAwait(false);
        }
    }

    private static readonly ConcurrentDictionary<(Type handlerType, Type commandType), Func<object, object, Task>> _cache = new();

    private static Task InvokeLamdaAsync(object handler, object command)
    {
        var key = (handler.GetType(), command.GetType());

        var del = _cache.GetOrAdd(key, static key =>
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
            var lambda = Expression.Lambda<Func<object, object, Task>>(call, handlerParam, commandParam);

            return lambda.Compile();
        });

        return del(handler, command);
    }
}
