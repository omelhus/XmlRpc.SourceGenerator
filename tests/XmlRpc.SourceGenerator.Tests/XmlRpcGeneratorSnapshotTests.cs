using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;
using VerifyMSTest;
using VerifyTests;

namespace XmlRpc.SourceGenerator.Tests
{
    [TestClass]
    public class XmlRpcGeneratorSnapshotTests : VerifyBase
    {
        public XmlRpcGeneratorSnapshotTests()
        {
            VerifySourceGenerators.Enable();
        }

        [TestMethod]
        public Task GenerateSourceAndVerify()
        {
            var source = @"
using XmlRpc.SourceGenerator;

namespace TestSourceGenNamespace;

[XmlRpcClient]
public interface ITestXmlRpcClient
{
    [XmlRpcMethod(""IntegrationManager.testConnect"")]
    Task<string> TestConnect();

    [XmlRpcMethod(""IntegrationManager.getWorkshops"")]
    Task<string> GetWorkshops(string sysnme, string syspwd);
}";
            return Verify(TestHelper.Compile(source))
                         .UseDirectory("Snapshots");
        }

        [TestMethod]
        public Task GenerateSourceAndVerifyWithRoot()
        {
            var source = @"
using XmlRpc.SourceGenerator;

namespace TestSourceGenNamespace;

[XmlRpcClient(""IntegrationManager"")]
public interface ITestXmlRpcClient
{
    [XmlRpcMethod(""testConnect"")]
    Task<string> TestConnect();

    [XmlRpcMethod(""getWorkshops"")]
    Task<string> GetWorkshops(string sysnme, string syspwd);
}";
            return Verify(TestHelper.Compile(source))
                         .UseDirectory("Snapshots");
        }

        [TestMethod]
        public Task GenerateSourceAndVerifyWithRootAndNoMethodAttributes()
        {
            var source = @"
using XmlRpc.SourceGenerator;

namespace TestSourceGenNamespace;

[XmlRpcClient(""IntegrationManager"")]
public interface ITestXmlRpcClient
{
    Task<string> testConnect();

    Task<string> getWorkshops(string sysnme, string syspwd);
}";
            return Verify(TestHelper.Compile(source))
                         .UseDirectory("Snapshots");
        }

        [TestMethod]
        public Task InvalidReturnTypeInSource()
        {
            var source = @"
using XmlRpc.SourceGenerator;

namespace TestSourceGenNamespace;

[XmlRpcClient]
public interface ITestXmlRpcClient
{
    [XmlRpcMethod(""IntegrationManager.testConnect"")]
    Task<string> TestConnect();

    [XmlRpcMethod(""IntegrationManager.getWorkshops"")]
    string GetWorkshops(string sysnme, string syspwd);
}";
            return Verify(TestHelper.Compile(source))
                         .UseDirectory("Snapshots");
        }
    }
}