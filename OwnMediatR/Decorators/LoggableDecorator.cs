using OwnMediatR.Lib.Extensions;

namespace OwnMediatR.Decorators;

public static class LoggableDecorator
{
    public static void TryDecorateOnlyLoggable(
    this IServiceCollection services,
    Type openGenericInterface,
    Type openGenericDecorator)
    {
        var descriptorsToDecorate = services
            .Where(sd =>
                sd.ServiceType.IsGenericType &&
                sd.ServiceType.GetGenericTypeDefinition() == openGenericInterface &&
                (
                    (sd.ImplementationType != null &&
                     sd.ImplementationType.GetCustomAttributes(typeof(LoggableAttribute), true).Any())
                    ||
                    (sd.ImplementationInstance != null &&
                     sd.ImplementationInstance.GetType().GetCustomAttributes(typeof(LoggableAttribute), true).Any())
                    ||
                    (sd.ImplementationFactory != null &&
                     // W przypadku factory — użycie Type może być ograniczone, ale próbujemy
                     sd.ImplementationFactory.GetType().GetCustomAttributes(typeof(LoggableAttribute), true).Any())
                )
            )
            .ToList();

        // Tymczasowo wyodrębnione — usuwamy tylko te, które chcemy ponownie dodać
        foreach (var descriptor in descriptorsToDecorate)
        {
            services.Remove(descriptor);
        }

        // Dodaj ponownie tylko loggable — i wtedy dopiero dekoruj
        foreach (var descriptor in descriptorsToDecorate)
        {
            services.Add(descriptor);
        }

        // Dekoruj tylko te, które przeszły filtr
        services.TryDecorateOpenGeneric(openGenericInterface, openGenericDecorator);
    }
}

[AttributeUsage(AttributeTargets.Class)]
public class LoggableAttribute : Attribute
{
}

