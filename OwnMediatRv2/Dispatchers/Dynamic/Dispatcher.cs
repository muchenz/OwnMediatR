using Contracts;

namespace OwnMediatRv2.Dispatchers.Dynamic;

public class Dispatcher
{
    private readonly IServiceProvider _serviceProvider;

    public Dispatcher(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task Send(IEvent command)
    {
        var handlerType = typeof(IEventHandler<>).MakeGenericType(command.GetType());

        IEnumerable<dynamic> handlers = _serviceProvider.GetServices(handlerType);

        foreach (dynamic handler in handlers)
        {
            await handler.Handle((dynamic)command);
        }
    }

    public async Task Send(IEnumerable<IEvent> evets)
    {
        foreach (var evet in evets)
        {
            await Send(evet);
        }
    }

    public async Task<TResult> Send<TResult>(ICommand<TResult> command)
    {
        var handlerType = typeof(ICommandHandler<,>).MakeGenericType(command.GetType(), typeof(TResult));

        dynamic handler = _serviceProvider.GetRequiredService(handlerType);


        return await handler.Handle((dynamic)command);
    }


}
