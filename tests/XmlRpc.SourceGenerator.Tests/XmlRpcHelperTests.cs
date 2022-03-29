using Microsoft.VisualStudio.TestTools.UnitTesting;
using RichardSzalay.MockHttp;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using VerifyMSTest;

namespace XmlRpc.SourceGenerator.Tests
{
    [TestClass]
    public class XmlRpcHelperTests : VerifyBase
    {

        [TestMethod]
        public Task TestCreateRpcCommandWithParameters()
        {
            var param1 = "abc123";
            var byteArray1 = Encoding.UTF8.GetBytes("param1");
            var stream = XmlRpcHelper.CreateCommand("testCommand", param1, byteArray1);
            return Verify(TestHelper.StreamToText(stream))
                .UseDirectory("Snapshots");
        }
        [TestMethod]
        public Task TestCreateRpcCommandWithoutParameters()
        {
            var stream = XmlRpcHelper.CreateCommand("testCommand");
            return Verify(TestHelper.StreamToText(stream))
                .UseDirectory("Snapshots");
        }

        [TestMethod]
        public async Task TestSendXmlRpcRequestHttpClientMethod()
        {
            var mockHttp = new MockHttpMessageHandler();

            mockHttp.When("https://localost/xml-rpc")
                    .Respond("text/xml", @"<?xml version=""1.0"" encoding=""ISO-8859-1""?><methodResponse><params><param><value>&lt;?xml version=""1.0"" encoding=""ISO-8859-1""?&gt;
&lt;hello&gt;TEST CONNECT SUCCESFUL&lt;/hello&gt;

</value></param></params></methodResponse>");
            var client = new HttpClient(mockHttp);
            var response = await client.SendXmlRpcRequest<string>("https://localost/xml-rpc", "testCommand", Encoding.UTF8.GetBytes("abc"));
            await Verify(response).UseDirectory("Snapshots");
        }
    }
}