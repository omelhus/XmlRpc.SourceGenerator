﻿//HintName: TestXmlRpcClient.g.cs
using System.Threading.Tasks;
namespace TestSourceGenNamespace;

public class TestXmlRpcClientOptions
{
    public string Url { get; set; }
}

public static class TestXmlRpcClientServiceExtensions
{
    public static IServiceCollection AddTestXmlRpcClient(this IServiceCollection serviceCollection, Action<TestXmlRpcClientOptions> configureTestXmlRpcClient = null)
    {
        serviceCollection.AddHttpClient();
        serviceCollection.AddTransient<ITestXmlRpcClient, TestXmlRpcClient>();
        if (configureTestXmlRpcClient != null)
            serviceCollection.Configure<TestXmlRpcClientOptions>(configureTestXmlRpcClient);
        return serviceCollection;
    }
}

public partial class TestXmlRpcClient : ITestXmlRpcClient
{

    private readonly System.Net.Http.HttpClient httpClient;
    public string Url { get; set; }
    public TestXmlRpcClient(System.Net.Http.HttpClient httpClient, IOptions<TestXmlRpcClientOptions> options)
    {
        this.httpClient = httpClient;
        Url = options?.Value?.Url;
    }
    public virtual async Task<string> TestConnect()
    {
        return await httpClient.SendXmlRpcRequest<string>(Url, "IntegrationManager.testConnect").ConfigureAwait(false);
    }
    public virtual async Task<string> GetWorkshops(string sysnme, string syspwd)
    {
        return await httpClient.SendXmlRpcRequest<string>(Url, "IntegrationManager.getWorkshops", sysnme, syspwd).ConfigureAwait(false);
    }
}
