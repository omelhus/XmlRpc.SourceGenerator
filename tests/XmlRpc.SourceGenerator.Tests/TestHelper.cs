using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace XmlRpc.SourceGenerator.Tests
{
    public static class TestHelper
    {
        public static IEnumerable<PortableExecutableReference> CreateNetCoreReferences()
        {
            foreach (var dllFile in Directory.GetFiles(Path.GetDirectoryName(typeof(System.Attribute).Assembly.Location), "*.dll"))
            {
                yield return MetadataReference.CreateFromFile(dllFile);
            }
        }

        public static GeneratorDriver Compile(string source)
        {
            // Parse the provided string into a C# syntax tree
            SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(source);

            var references = CreateNetCoreReferences().ToList();

            references.Add(MetadataReference.CreateFromFile(typeof(XmlRpcHelper).Assembly.Location));

            CSharpCompilation compilation = CSharpCompilation.Create(
                "Tests",
                new[] { syntaxTree },
                references,
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary,
                  optimizationLevel: OptimizationLevel.Debug
                )
                );

            // Create an instance of our EnumGenerator incremental source generator
            var generator = new XmlRpcClientGenerator();

            // The GeneratorDriver is used to run our generator against a compilation
            GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);

            // Run the source generator!
            driver = driver.RunGenerators(compilation);

            // Use verify to snapshot test the source generator output!
            return driver;
        }
    }
}