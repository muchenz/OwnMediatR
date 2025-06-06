using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using Contracts;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OwnMediatR.Lib.Extensions;
using Dynamic= OwnMediatR.Lib.Dispatchers.Dynamic;
using Wrapped= OwnMediatR.Lib.Dispatchers.Wrapperv1;


BenchmarkRunner.Run<MyBenchmark>();


[ShortRunJob]
[MemoryDiagnoser]
public class MyBenchmark
{
    private Dynamic.Dispatcher _dynamicDispatcher;
    private Wrapped.Dispatcher _wrappedDispatcher;

    private IEvent[] _events = [new AliceArrivedEvent(), new KellyArrivedEvent()];

    [GlobalSetup]
    public void Setup()
    {
        var host = Host.CreateDefaultBuilder()
            .ConfigureServices(services =>
            {
                services.AddCommandAndQueries();
                services.AddScoped<Dynamic.Dispatcher>();
                services.AddScoped<Wrapped.Dispatcher>();

            })
            .Build();

        _dynamicDispatcher = host.Services.GetRequiredService<Dynamic.Dispatcher>();
        _wrappedDispatcher = host.Services.GetRequiredService<Wrapped.Dispatcher>();
    }

    [Benchmark]
    public async Task  Dynamic_Dispatcher()
    {
        
        await _dynamicDispatcher.Send(_events);
    }

    [Benchmark]
    public async Task Wrapped_DispatcherBenchmark()
    {

        await _wrappedDispatcher.Send(_events);
    }
}


public record  AliceArrivedEvent : IEvent;
public record KellyArrivedEvent : IEvent;



public class AliceArrivedEventHandler : IEventHandler<AliceArrivedEvent>
{
    public Task Handle(AliceArrivedEvent @event)
    {

        return Task.CompletedTask;
    }
}


public class KellyArrivedEventHandler : IEventHandler<KellyArrivedEvent>
{
    public Task Handle(KellyArrivedEvent @event)
    {

        return Task.CompletedTask;
    }
}
