using Contracts;
using System.Collections.Concurrent;

namespace OwnMediatRv2.Dispatchers.Wrapperv1;

public class Dispatcher
{

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
    public async Task Send(IEvent e)
    {

        var eventType = e.GetType();

        var wrapper = _wapperEventCache.GetOrAdd(eventType, eventType =>
        {

            // var typedWrapper = typeof(HandlerEventWrapprer<>).MakeGenericType(eventType);

            // var instance = Activator.CreateInstance(typedWrapper, []);

            //  return (HandlerEventWrapprer)instance;

            return HandlerEventWrapprer.Create(eventType);

        });

        using var scope = _serviceProvider.CreateScope();

        await wrapper.Handle(e, scope.ServiceProvider);


    }




    private static readonly ConcurrentDictionary<Type, HandlerEventWrapprer> _wapperEventCache
    = new();
    private readonly IServiceProvider _serviceProvider;

    public abstract class HandlerEventWrapprer
    {
        public abstract Task Handle(IEvent @event, IServiceProvider serviceProvider);

        public static HandlerEventWrapprer Create(Type eventType)
        {
            var typedWrapper = typeof(HandlerEventWrapprer<>).MakeGenericType(eventType);

            var instance = Activator.CreateInstance(typedWrapper, []);

            return (HandlerEventWrapprer)instance;

        }
    }

    public class HandlerEventWrapprer<T> : HandlerEventWrapprer where T : IEvent
    {

        public HandlerEventWrapprer()
        {
        }
        public async Task HandleTyped(T @event, IServiceProvider serviceProvider)
        {
            //var handlerType = typeof(IEventHandler<T>);

            var handlers = serviceProvider.GetServices<IEventHandler<T>>();

            foreach (var handler in handlers)
            {

                await handler.Handle(@event);
            }
        }


        public override Task Handle(IEvent @event, IServiceProvider serviceProvider)
        {

            return HandleTyped((T)@event, serviceProvider);
        }
    }

    // -----------------------

    private static readonly ConcurrentDictionary<Type, HandlerCommandWrapprer> _wapperCommandCache
   = new();

    public abstract class HandlerCommandWrapprer
    {
        public abstract Task Handle(object command, IServiceProvider serviceProvider);

        public static HandlerCommandWrapprer Create(Type command, Type result)
        {
            var typedWrapper = typeof(HandlerCommandWrapprer<,>).MakeGenericType(command, result);

            var instance = Activator.CreateInstance(typedWrapper, []);

            return (HandlerCommandWrapprer)instance;

        }
    }

    public class HandlerCommandWrapprer<TCommand, TResult> : HandlerCommandWrapprer where TCommand : ICommand<TResult>
    {

        public  Task HandleTyped(TCommand command, IServiceProvider serviceProvider) 
        {
            //var handlerType = typeof(IEventHandler<T>);

            var handler = serviceProvider.GetRequiredService<ICommandHandler<TCommand, TResult>>();
            if (handler is null) throw new ArgumentNullException(nameof(handler));

            return handler.Handle(command);
        }


        public override Task Handle(object command, IServiceProvider serviceProvider)
        {
            return HandleTyped((TCommand)command, serviceProvider);
        }
       
    }

    public async Task<TResult> Send<TResult>(ICommand<TResult> command)
    {

        var commandType = command.GetType();

        var wrapper = _wapperCommandCache.GetOrAdd(commandType, commandType =>
        {

            // var typedWrapper = typeof(HandlerEventWrapprer<>).MakeGenericType(eventType);

            // var instance = Activator.CreateInstance(typedWrapper, []);

            //  return (HandlerEventWrapprer)instance;

            return HandlerCommandWrapprer.Create(commandType, typeof(TResult));

        });

        using var scope = _serviceProvider.CreateScope();
      
        return await ((Task<TResult>) wrapper.Handle(command, scope.ServiceProvider));


    }


}
