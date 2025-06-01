//using Microsoft.CodeAnalysis;
//using Microsoft.CodeAnalysis.CSharp;
//using Microsoft.CodeAnalysis.CSharp.Syntax;
//using Microsoft.CodeAnalysis.Text;
//using System.Collections.Immutable;
//using System.Diagnostics;
//using System.Text;

//namespace SourceGenerator;


//[Generator]
//public class DispatcherGenerator : IIncrementalGenerator
//{
//    public void Initialize(IncrementalGeneratorInitializationContext context)
//    {
//#if DEBUG
//        // Debugger.Launch();
//#endif



//        var handlerTypes = context.SyntaxProvider
//            .CreateSyntaxProvider(
//                predicate: static (s, _) => s is ClassDeclarationSyntax,
//                transform: static (ctx, _) =>
//                {
//                    var node = (ClassDeclarationSyntax)ctx.Node;
//                    var symbol = ctx.SemanticModel.GetDeclaredSymbol(node) as INamedTypeSymbol;
//                    return symbol;
//                })
//            .Where(symbol => symbol is not null && ImplementsHandlerInterface(symbol))
//            .Collect();



//        context.RegisterSourceOutput(handlerTypes, (ctx, symbols) =>
//        {
//            var sb = new StringBuilder();
//            var generated = new HashSet<string>();
//            var knownEvents = new HashSet<string>(); // <-- zbieramy znane IEvent typy

//            var namespaceForEvent = string.Empty;

//            sb.AppendLine("using System;");
//            sb.AppendLine("using System.Threading.Tasks;");
//            sb.AppendLine("namespace GeneratedDispatchers");
//            sb.AppendLine("{");
//            sb.AppendLine("    public static class Dispatcher");
//            sb.AppendLine("    {");

//            foreach (var handler in symbols.Distinct(SymbolEqualityComparer.Default))
//            {
//                if (handler is not INamedTypeSymbol namedHandler)
//                    continue;

//                foreach (var iface in namedHandler.AllInterfaces)
//                {
//                    if (!iface.Name.StartsWith("ICommandHandler") && !iface.Name.StartsWith("IEventHandler"))
//                        continue;

//                    if (iface.TypeArguments.Length == 0 || iface.TypeArguments.Any(a => a is ITypeParameterSymbol))
//                        continue;

//                    //---------
//                    if (string.IsNullOrEmpty(namespaceForEvent))
//                    {
//                        if (iface.Name.Contains("ICommandHandler"))
//                        {
//                            int index = iface.ToString().IndexOf("ICommandHandler");
//                            namespaceForEvent = iface.OriginalDefinition.ToString().Substring(0, index - 1);
//                        }
//                    }
//                    //-------------
//                    var isEvent = iface.Name.StartsWith("IEventHandler");
//                    var isCommandWithResult = iface.TypeArguments.Length == 2;
//                    var commandType = iface.TypeArguments[0].ToDisplayString();
//                    var methodName = isEvent ? "Publish" : "Send";

//                    var sigKey = $"{methodName}_{commandType}";
//                    if (!generated.Add(sigKey))
//                        continue;

//                    if (isEvent)
//                        knownEvents.Add(commandType); // <- Zbieramy tylko IEvent

//                    if (isCommandWithResult)
//                    {
//                        var resultType = iface.TypeArguments[1].ToDisplayString();
//                        sb.AppendLine($$"""
//    public static async Task<{{resultType}}> {{methodName}}({{commandType}} command, IServiceProvider provider)
//    {
//        var handler = ({{iface.ToDisplayString()}})provider.GetService(typeof({{iface.ToDisplayString()}}));
//        return await handler.Handle(command);
//    }
//""");
//                    }
//                    else
//                    {
//                        sb.AppendLine($$"""
//    public static async Task {{methodName}}({{commandType}} command, IServiceProvider provider)
//    {
//        var handler = ({{iface.ToDisplayString()}})provider.GetService(typeof({{iface.ToDisplayString()}}));
//        await handler.Handle(command);
//    }
//""");
//                    }
//                }
//            }

//            // === GENERUJEMY METODĘ Publish(IEvent ev, IServiceProvider provider) ===

//            sb.AppendLine("        public static async Task Publish(Contracts.IEvent ev, IServiceProvider provider)");
//            sb.AppendLine("        {");
//            sb.AppendLine("            switch (ev)");
//            sb.AppendLine("            {");

//            foreach (var eventType in knownEvents.OrderBy(e => e))
//            {
//                sb.AppendLine($$"""
//                case {{eventType}} e:
//                    await Publish(e, provider);
//                    break;
//""");
//            }

//            sb.AppendLine("                default:");
//            sb.AppendLine("                    throw new NotSupportedException($\"Unknown event type: {ev.GetType()}\");");
//            sb.AppendLine("            }");
//            sb.AppendLine("        }");

//            sb.AppendLine("    }");
//            sb.AppendLine("}");

//            ctx.AddSource("Dispatcher.g.cs", SourceText.From(sb.ToString(), Encoding.UTF8));
//        });
//    }

//    private static bool ImplementsHandlerInterface(INamedTypeSymbol? symbol)
//    {
//        if (symbol == null || symbol.IsAbstract || symbol.IsGenericType)
//            return false;

//        return symbol.AllInterfaces.Any(i =>
//            (i.Name == "ICommandHandler" || i.Name == "IEventHandler") &&
//            i.TypeArguments.All(arg => arg is not ITypeParameterSymbol));
//    }
//}