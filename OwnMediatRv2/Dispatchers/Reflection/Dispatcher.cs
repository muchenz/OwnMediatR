using Contracts;
using System.Collections.Concurrent;
using System.Reflection;

namespace Examplev2.Dispatchers.Reflection;

public class Dispatcher
{
    private readonly IServiceProvider _serviceProvider;

    public Dispatcher(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task Send(IEnumerable<IEvent> evets)
    {
        foreach (var evet in evets)
        {
            await Send(evet);
        }
    }

    public async Task Send(IEvent command)
    {
        var handlerType = typeof(IEventHandler<>).MakeGenericType(command.GetType());

        using var scope = _serviceProvider.CreateScope();

        var handlers = scope.ServiceProvider.GetServices(handlerType);
        //var method = handlerType.GetMethod("Handle");

        var method = _methondCache.GetOrAdd(handlerType, handlerType => handlerType.GetMethod("Handle"));

        if (method == null)
            throw new InvalidOperationException("Method  'Handle' not found.");

        foreach (var handler in handlers)
        {
            await (Task)method.Invoke(handler, new object[] { command });
        }

    }




    private static readonly ConcurrentDictionary<Type, MethodInfo> _methondCache
    = new(); // do nothing
    public async Task<TResult> Send<TResult>(ICommand<TResult> command)
    {
        var handlerType = typeof(ICommandHandler<,>).MakeGenericType(command.GetType(), typeof(TResult));

        using var scope = _serviceProvider.CreateScope();

        var handler = scope.ServiceProvider.GetRequiredService(handlerType);

        //var method = handlerType.GetMethod("Handle");
        if (!_methondCache.TryGetValue(handlerType, out var method))
        {
            method = handlerType.GetMethod("Handle");
            _methondCache[handlerType] = method;
        }
        if (method == null)
            throw new InvalidOperationException("Method  'Handle' not found.");

        var task = (Task<TResult>)method.Invoke(handler, new object[] { command });

        return await task;
    }
}
