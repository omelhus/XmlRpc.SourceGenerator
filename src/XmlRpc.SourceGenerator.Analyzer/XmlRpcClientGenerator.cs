using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace XmlRpc.SourceGenerator
{
    [Generator]
    public class XmlRpcClientGenerator : IIncrementalGenerator
    {
        const string XmlRpcClientAttributeName = "XmlRpc.SourceGenerator.XmlRpcClientAttribute";
        const string XmlRpcMethodAttributeName = "XmlRpc.SourceGenerator.XmlRpcMethodAttribute";

        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            try
            {
                IncrementalValuesProvider<InterfaceDeclarationSyntax> interfaceDeclarations = context.SyntaxProvider
                  .CreateSyntaxProvider(
                      predicate: static (s, _) => IsSyntaxTargetForGeneration(s),
                      transform: static (ctx, _) => GetSemanticTargetForGeneration(ctx)) // sect the enum with the [EnumExtensions] attribute
                  .Where(static m => m is not null)!; // filter out attributed enums that we don't care about

                IncrementalValueProvider<(Compilation, ImmutableArray<InterfaceDeclarationSyntax>)> compilationAndEnums
                    = context.CompilationProvider.Combine(interfaceDeclarations.Collect());

                context.RegisterSourceOutput(compilationAndEnums,
                    static (spc, source) => Execute(source.Item1, source.Item2, spc));
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
            }
        }

        static void Execute(Compilation compilation, ImmutableArray<InterfaceDeclarationSyntax> interfaces, SourceProductionContext context)
        {
            try
            {
                if (interfaces.IsDefaultOrEmpty)
                {
                    Debug.WriteLine("There are no interfaces to implement.");
                    return;
                }

                foreach (var declaration in interfaces.Distinct())
                {
                    var isValid = true;
                    var interfaceName = declaration.Identifier.Text;
                    var className = interfaceName.Substring(1);
                    Debug.WriteLine($"Implementing interface {interfaceName} as {className}");
                    var model = compilation.GetSemanticModel(declaration.SyntaxTree);
                    var sourceBuilder = new StringBuilder();

                    var methods = declaration.DescendantNodes().OfType<MethodDeclarationSyntax>();
                    AddNamespaceAndUsings(declaration, sourceBuilder);
                    sourceBuilder.AppendLine();
                    AddOptionsClass(interfaceName, className, sourceBuilder);
                    sourceBuilder.AppendLine();
                    sourceBuilder.AppendLine($"public partial class {className} : {interfaceName} {{");
                    CreateConstructor(className, sourceBuilder);
                    var clientAttribute = GetAttributeWithName(declaration.AttributeLists, XmlRpcClientAttributeName, model);
                    var prefix = "";
                    if (clientAttribute?.ArgumentList?.Arguments.Count > 0)
                    {
                        var root = model.GetConstantValue(clientAttribute.ArgumentList.Arguments[0].Expression).ToString();
                        if (!string.IsNullOrEmpty(root))
                            prefix = $"{root}.";
                    }
                    foreach (var method in methods)
                    {
                        var methodName = method.Identifier.Text;
                        string returnType = "string";
                        string methodReturnType = "string";
                        if (method.ReturnType is not GenericNameSyntax genericNameSyntax ||
                            !genericNameSyntax.Identifier.Text.Contains("Task"))
                        {
                            context.ReportDiagnostic(CreateReturnTypeDiagnosticError(className, method));
                            isValid = false;
                            break;
                        }
                        else
                        {
                            returnType = genericNameSyntax.TypeArgumentList.Arguments[0].ToString().Trim();
                            methodReturnType = genericNameSyntax.ToString().Trim();
                        }

                        var methodAttribute = GetAttributeWithName(method.AttributeLists, XmlRpcMethodAttributeName, model);
                        string rpcMethodName = method.Identifier.Text;
                        if (methodAttribute != null && methodAttribute.ArgumentList.Arguments.Count > 0)
                        {
                            var arg = methodAttribute.ArgumentList.Arguments[0];
                            var val = model.GetConstantValue(arg.Expression).ToString();
                            if (!string.IsNullOrEmpty(val))
                            {
                                rpcMethodName = val;
                            }
                        }
                        sourceBuilder.Append($@"    public virtual async {methodReturnType} {methodName}(");
                        var parameterIdentifiers = AppendParameters(sourceBuilder, method);
                        sourceBuilder.Append(")\n");
                        sourceBuilder.Append("\t{\n");
                        sourceBuilder.AppendLine($"\t\treturn await httpClient.SendXmlRpcRequest<{returnType}>(Url, \"{prefix}{rpcMethodName}\"{AppendParameters(parameterIdentifiers)}).ConfigureAwait(false);");
                        sourceBuilder.Append("\t}\n");
                    }
                    var steamContent =
                    sourceBuilder.AppendLine("}");
                    if (isValid)
                        context.AddSource($"{className}.g.cs", SourceText.From(sourceBuilder.ToString(), Encoding.UTF8));
                }

            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
            }
        }

        private static void AddOptionsClass(string interfaceName, string className, StringBuilder sourceBuilder)
        {
            sourceBuilder.AppendLine($"public class {className}Options {{");
            sourceBuilder.AppendLine("  public string Url { get; set; }");
            sourceBuilder.AppendLine("}");
            sourceBuilder.AppendLine();
            sourceBuilder.AppendLine($"public static class {className}ServiceExtensions {{");
            sourceBuilder.AppendLine($@"
    public static IServiceCollection Add{className}(this IServiceCollection serviceCollection, Action<{className}Options> configure{className} = null){{
       serviceCollection.AddHttpClient();
       serviceCollection.AddTransient<{interfaceName}, {className}>();
       if(configure{className} != null)
            serviceCollection.Configure<{className}Options>(configure{className});
       return serviceCollection;
    }}
".Trim());
            sourceBuilder.AppendLine("}");
        }

        private static void AddNamespaceAndUsings(InterfaceDeclarationSyntax declaration, StringBuilder sourceBuilder)
        {
            var ns = GetNamespace(declaration);
            sourceBuilder.AppendLine("using System.Threading.Tasks;");
            sourceBuilder.AppendLine("using System.Net.Http;");
            sourceBuilder.AppendLine("using Microsoft.Extensions.Options;");
            sourceBuilder.AppendLine("using Microsoft.Extensions.DependencyInjection;");
            sourceBuilder.AppendLine("using XmlRpc.SourceGenerator;");

            if (!string.IsNullOrEmpty(ns))
                sourceBuilder.AppendLine($"namespace {ns};");
        }

        private static Diagnostic CreateReturnTypeDiagnosticError(string className, MethodDeclarationSyntax method)
        {
            return Diagnostic.Create(new DiagnosticDescriptor("XR1001",
                                                                     "Return type must be Task<T>",
                                                                     "Return type must be Task<T>",
                                                                     "Usage",
                                                                     DiagnosticSeverity.Error,
                                                                     true,
                                                                     description: $"All methods on {className} must be async, and thus must return Task<T>"
                                                                     ),
                                            Location.Create(method.SyntaxTree, method.ReturnType.FullSpan));
        }

        private static List<string> AppendParameters(StringBuilder sourceBuilder, MethodDeclarationSyntax method)
        {
            var parameterIdentifiers = new List<string>();
            for (var i = 0; i < method.ParameterList.Parameters.Count; i++)
            {
                var parameter = method.ParameterList.Parameters[i];
                var name = parameter.Identifier.ValueText;
                sourceBuilder.Append(parameter.ToFullString());
                parameterIdentifiers.Add(name);
                if (i != method.ParameterList.Parameters.Count - 1)
                {
                    sourceBuilder.Append(", ");
                }
            }

            return parameterIdentifiers;
        }

        private static string AppendParameters(List<string> parameterIdentifiers)
        {
            return (parameterIdentifiers.Any() ? ", " + string.Join(", ", parameterIdentifiers) : "").Trim();
        }

        private static void CreateConstructor(string className, StringBuilder sourceBuilder)
        {
            sourceBuilder.AppendLine($@"
    private readonly System.Net.Http.HttpClient httpClient;
    public string Url {{ get; set; }}
    public {className}(System.Net.Http.HttpClient httpClient, IOptions<{className}Options> options){{
        this.httpClient = httpClient;
        Url = options?.Value?.Url;
    }}");
        }

        static string GetNamespace(BaseTypeDeclarationSyntax syntax)
        {
            string nameSpace = string.Empty;
            SyntaxNode potentialNamespaceParent = syntax.Parent;
            while (potentialNamespaceParent != null &&
                    potentialNamespaceParent is not NamespaceDeclarationSyntax
                    && potentialNamespaceParent is not FileScopedNamespaceDeclarationSyntax)
            {
                potentialNamespaceParent = potentialNamespaceParent.Parent;
            }
            if (potentialNamespaceParent is BaseNamespaceDeclarationSyntax namespaceParent)
            {
                nameSpace = namespaceParent.Name.ToString();

                while (true)
                {
                    if (namespaceParent.Parent is not NamespaceDeclarationSyntax parent)
                    {
                        break;
                    }

                    nameSpace = $"{namespaceParent.Name}.{nameSpace}";
                    namespaceParent = parent;
                }
            }

            return nameSpace;
        }

        static bool IsSyntaxTargetForGeneration(SyntaxNode node)
        {
            var rsp = node is InterfaceDeclarationSyntax m && m.AttributeLists.Count > 0;
            return rsp;
        }

        static AttributeSyntax GetAttributeWithName(SyntaxList<AttributeListSyntax> attributeLists, string name, SemanticModel model)
        {
            foreach (AttributeListSyntax attributeListSyntax in attributeLists)
            {
                foreach (AttributeSyntax attributeSyntax in attributeListSyntax.Attributes)
                {
                    if (model.GetSymbolInfo(attributeSyntax).Symbol is not IMethodSymbol attributeSymbol)
                    {
                        continue;
                    }

                    INamedTypeSymbol attributeContainingTypeSymbol = attributeSymbol.ContainingType;
                    string fullName = attributeContainingTypeSymbol.ToDisplayString();

                    if (fullName == name)
                    {
                        return attributeSyntax;
                    }
                    else
                    {
                        Debug.WriteLine(fullName);
                    }
                }
            }
            return null;
        }

        static InterfaceDeclarationSyntax GetSemanticTargetForGeneration(GeneratorSyntaxContext context)
        {
            var interfaceDeclaration = (InterfaceDeclarationSyntax)context.Node;
            foreach (AttributeListSyntax attributeListSyntax in interfaceDeclaration.AttributeLists)
            {
                foreach (AttributeSyntax attributeSyntax in attributeListSyntax.Attributes)
                {
                    if (context.SemanticModel.GetSymbolInfo(attributeSyntax).Symbol is not IMethodSymbol attributeSymbol)
                    {
                        continue;
                    }

                    INamedTypeSymbol attributeContainingTypeSymbol = attributeSymbol.ContainingType;
                    string fullName = attributeContainingTypeSymbol.ToDisplayString();

                    if (fullName == XmlRpcClientAttributeName)
                    {
                        return interfaceDeclaration;
                    }
                }
            }

            return null;
        }
    }
}
