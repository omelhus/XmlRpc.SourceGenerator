# .Net XmlRpc SourceGenerator
Using .Net 6 source generators to create a XML-RPC client. Serialization based on (now defunct) xml-rpc.net.

All you need to do to create a XML-RPC Client is to define the interface and decorate it with [XmlRpcClient]. The source generator will take it from there and implement the client for you.

## Examples

The following example will create a class named `TestXmlRpcClient` in the namespace `TestSourceGenNamespace`. 
It will also create an extension method for IServiceCollection, so that it can be added to DI using `serviceCollection.AddTestXmlRpcClient()`.

```csharp
using XmlRpc.SourceGenerator;

namespace TestSourceGenNamespace;

[XmlRpcClient]
public interface ITestXmlRpcClient
{
    [XmlRpcMethod("IntegrationManager.testConnect")]
    Task<string> TestConnect();

    [XmlRpcMethod("IntegrationManager.getWorkshops")]
    Task<string> GetWorkshops(string sysnme, string syspwd);
}
```

Then add it to DI using the extension method that's been generated.

```csharp
...
services.AddTestXmlRpcClient(options => options.Url = "https://some-test-xml-rcp-server.example");
...
```