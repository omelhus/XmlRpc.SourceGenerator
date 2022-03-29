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
        public Task VerifyByteArrayIsSupported()
        {
            var source = @"
using XmlRpc.SourceGenerator;

namespace TestSourceGenNamespace;

[XmlRpcClient(""IntegrationManager"")]
public interface ITestXmlRpcClient
{
    Task<string> createMethodWithByteArray(byte[] byteArray);
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
        public Task GenerateSourceWithSummaryComments()
        {
            var source = @"
using XmlRpc.SourceGenerator;

namespace TestSourceGenNamespace;

[XmlRpcClient(""IntegrationManager"")]
public interface ITestXmlRpcClient
{
    /// <summary>
    /// Function for populating suppliers product registry with data.
    /// Note1! This function will not update the registry, but overwrite it.It will clear all previous products and then insert all products that are submitted in the file.
    /// Note2! The function will only trigger if the submitted productfile is 100% correct.If one or more errors are detected, the import is aborted and previous registry content is left intact.
    /// Note3! The csv file format used is the same as when uploading products through the web interface.
    /// </summary>
    /// <param name=""accesskey"">Access key for remote system validation</param>
    /// <param name=""supnme"">Suppliers username</param>
    /// <param name=""suppwd"">Suppliers password</param>
    /// <param name=""file"">Csv productfile filecontent as byte array</param>
    /// <returns>Xml message describing the result of the import</returns>
    Task<string> uploadProducts(String accesskey, String supnme, String suppwd, byte[] file);
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