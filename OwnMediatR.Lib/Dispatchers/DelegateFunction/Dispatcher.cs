using Contracts;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Concurrent;
using System.Reflection;

namespace OwnMediatR.Lib.Dispatchers.DelegateFunction;

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

        var handler = scope.ServiceProvider.GetService(typeof(ICommandHandler<,>).MakeGenericType(command.GetType(), typeof(TResult)));
        if (handler is null) throw new NullReferenceException("Handler is null");
        var result = InvokeCreateDelegateWithResultAsync(handler, command);

        return (Task<TResult>)result;
    }


    public abstract class HandlerWrapper
    {
        public abstract Task InvokeAsync(object handler, object command);
    }
    public class HandlerWrapper<TH, TC, TR> : HandlerWrapper
    {
        private readonly Func<TH, TC, Task> _handle;

        public HandlerWrapper(MethodInfo handleMethod)
        {
            _handle = (Func<TH, TC, Task>)Delegate.CreateDelegate(
                typeof(Func<TH, TC, Task>), handleMethod
            );
        }

        public override Task InvokeAsync(object handler, object command)
        {
            return _handle((TH)handler, (TC)command);
        }
    }


    private static readonly ConcurrentDictionary<(Type handlerType, Type commandType), HandlerWrapper> _cache333 = new();


    public async Task Send(IEnumerable<IEvent> events)
    {
        foreach (var e in events)
        {
            await Send(e);
        }
    }

    public async Task Send(IEvent e)
    {
        using var scope = _serviceProvider.CreateScope();

        var handlers = scope.ServiceProvider.GetServices(typeof(IEventHandler<>).MakeGenericType(e.GetType()));

        foreach (var handler in handlers)
        {
            if (handler is null) continue;

            var handlerType = handler.GetType();
            var commnadType = e.GetType();

            var wr = _cache333.GetOrAdd((handlerType, commnadType), key =>
            {


                var typedWrapper = typeof(HandlerWrapper<,,>).MakeGenericType(handlerType, commnadType, typeof(Task));

                var methodInfo = handler.GetType().GetMethod("Handle", new[] { commnadType });


                var wrapper = (HandlerWrapper)Activator.CreateInstance(typedWrapper, methodInfo);

                return wrapper;
            });

            await wr.InvokeAsync(handler, e);
        }
    }
    private static readonly ConcurrentDictionary<(Type handlerType, Type commandType), Func<object, object, object>> _cacheWithResult = new();

    private object InvokeCreateDelegateWithResultAsync(object handler, object command)  //slow !! - slower than 'invoke' !!
    {
        var key = (handler.GetType(), command.GetType());

        var del = _cacheWithResult.GetOrAdd(key, static key =>
        {
            var (handlerType, commandType) = key;

            var method = handlerType.GetMethod("Handle", new[] { commandType });
            var returnType = method.ReturnType; // Task<TResult>
            var resultType = returnType.GetGenericArguments()[0];

            // typeof(Func<TH, TC, Task<TR>>)
            var delegateType = typeof(Func<,,>).MakeGenericType(handlerType, commandType, returnType);

            var openDelegate = Delegate.CreateDelegate(delegateType, method);


            var wrapperType = typeof(StronglyTypedWrapper<,,>).MakeGenericType(handlerType, commandType, resultType);
            var wrapperMethod = wrapperType.GetMethod("Invoke");

            return (Func<object, object, object>)Delegate.CreateDelegate(
                typeof(Func<object, object, object>),
                openDelegate,
                wrapperMethod!
            );
        });

        return del(handler, command);
    }
    public static class StronglyTypedWrapper<TH, TC, TR>
    {
        public static object Invoke(Func<TH, TC, Task<TR>> del, object h, object c)
        {
            var result = del((TH)h, (TC)c);//.ConfigureAwait(false);
            return result!;
        }
    }
}
