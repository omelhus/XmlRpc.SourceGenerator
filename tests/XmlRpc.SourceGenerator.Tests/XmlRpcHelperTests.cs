using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RichardSzalay.MockHttp;
using System;
using System.IO;
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
        public void TestCreateAttribute()
        {
            var attr = new XmlRpcClientAttribute("IntegrationManager");
            attr.Root.Should().Be("IntegrationManager");
        }

        [TestMethod]
        public Task TestCreateRpcCommandWithParameters()
        {
            var param1 = "abc123";
            var byteArray1 = Encoding.UTF8.GetBytes("param1");
            var stream = XmlRpcHelper.CreateCommand("testCommand", param1, byteArray1);
            return Verify(TestHelper.StreamToText(stream))
                .UseDirectory("Snapshots");
        }

        public class TestSerializerClass
        {
            public class SubType
            {
                [XmlRpcNullMapping(NullMappingAction.Ignore)]
                public string Id { get; set; }
            }
            public enum TestEnum
            {
                A,
                B,
            }
            public string Id { get; set; }
            public DateTime Date { get; set; } = DateTime.MinValue;
            public int Number { get; set; }
            public double Double { get; set; }
            [XmlRpcNullMapping(NullMappingAction.Ignore)]
            public byte[]? SomeData { get; set; }
            [XmlRpcNullMapping(NullMappingAction.Ignore)]
            public string[]? List { get; set; }
            [XmlRpcMember(Member = "someSubType", Description = "Some Sub Type"), XmlRpcNullMapping(NullMappingAction.Ignore)]
            public SubType SomeSubType { get; set; } = new SubType();
            public bool SomeBool { get; set; }
            public long SomeLong { get; set; }
            [XmlRpcEnumMapping(EnumMapping.Number)]
            public TestEnum SomeEnum { get; set; }

            [XmlRpcEnumMapping(EnumMapping.String)]
            public TestEnum SomeEnum2 { get; set; }

            [XmlRpcNullMapping(NullMappingAction.Ignore)]
            [XmlRpcMember]
            public XmlRpcStruct Hashtable { get; set; }

            [XmlRpcNullMapping(NullMappingAction.Ignore)]
            public int[][] SomeMultidim { get; set; }
        }

        [TestMethod]
        public Task TestCreateRpcCommandWithDifferentParameters()
        {
            var stream = XmlRpcHelper.CreateCommand("testCommand",
                "abc123",
                100,
                (long)250,
                80.9,
                false,
                new DateTime(2022, 01, 01),
                new[] { "a", "b" },
                null,
                new TestSerializerClass
                {
                    Id = "abc123"
                }
                );
            return Verify(TestHelper.StreamToText(stream))
                .UseDirectory("Snapshots");
        }

        [DataTestMethod]
        [DataRow("<invalid>", "XmlRpcInvalidXmlRpcException", DisplayName = "Invalid XML")]
        [DataRow("<malFormed", "XmlRpcIllFormedXmlException", DisplayName = "Malformed XML")]
        public void TestThatInvalidXmlThrowsExceptionInDeserializer(string input, string exceptionType)
        {
            var stream = new MemoryStream(Encoding.UTF8.GetBytes(input));
            var r = stream.Invoking(x => XmlRpcHelper.DeserializeResponse<object>(x))
                .Should().Throw<XmlRpcException>();
            TestContext.WriteLine(r.Which.GetType().Name);
            r.Which.GetType().Name.Should().Be(exceptionType);
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
            response.Should().NotBeNullOrWhiteSpace();
            await Verify(response).UseDirectory("Snapshots");
        }

        [TestMethod]
        public async Task TestSendXmlRpcRequestHttpClientMethodReturnObject()
        {
            var mockHttp = new MockHttpMessageHandler();
            var date = new DateTime(2022, 01, 01);
            var source = new TestSerializerClass()
            {
                Id = "key",
                Date = date,
                Number = 123,
                Double = 123.8d,
                SomeData = Encoding.UTF8.GetBytes("test"),
                List = new[] { "a", "b", "c" },
                SomeSubType = new TestSerializerClass.SubType
                {
                    Id = "123"
                },
                SomeBool = false,
                SomeLong = 123,
                SomeEnum = TestSerializerClass.TestEnum.B,
                SomeMultidim = new[] { new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, }, new[] { 10, 11, 12, 13, 14 } },
                Hashtable = new XmlRpcStruct()
            };
            source.Hashtable.Add("key", "value");
            mockHttp.When("https://localost/xml-rpc")
                .Respond((request) =>
                {
                    var content = request.Content.ReadAsStream();
                    using var reader = new StreamReader(content);
                    return new StringContent(reader.ReadToEnd().Replace("<methodCall", "<methodResponse"), Encoding.UTF8, "text/xml");
                });
            var client = new HttpClient(mockHttp);
            var response = await client.SendXmlRpcRequest<TestSerializerClass>("https://localost/xml-rpc", "testCommand", source);
            response.Should().BeEquivalentTo(source);
        }
    }
}