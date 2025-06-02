using Contracts;

namespace OwnMediatRv2.Extensions;

static class CommandAndQueriesExtension
{
    public static void AddCommandAndQueries(this IServiceCollection service)
    {

        Type[] typesToRegister = [typeof(ICommandHandler<,>), typeof(ICommandHandler<>), typeof(IEventHandler<>)];//, typeof(IHandlerWrapper<,>)];

        foreach (Type typeToRegister in typesToRegister)
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            var handlerTypes = assemblies
                           .SelectMany(x => x.GetTypes())
                           .Where(x => x.IsClass && !x.IsGenericTypeDefinition && x.GetInterfaces()
                                .Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeToRegister))
                           ;

            var handlerTypes2 = handlerTypes.ToList();

            var listArgument = handlerTypes2.Select(a => a.GetGenericArguments().Count()).ToList();

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
