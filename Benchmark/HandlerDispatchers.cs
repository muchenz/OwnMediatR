using Contracts;
using Microsoft.Extensions.DependencyInjection;

namespace Benchmark;

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

        var res = GeneratedDispatchers.Dispatcher.Send(command, scope.ServiceProvider);

        return res;
    }



    public async Task Send(IEnumerable<IEvent> evets)
    {

        foreach (var evet in evets)
        {

            using var scope = _serviceProvider.CreateScope();

            await GeneratedDispatchers.Dispatcher.Publish(evet, scope.ServiceProvider);
        }
    }

}
