using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace XmlRpc.SourceGenerator
{
    public static class XmlRpcHelper
    {
        private static Stream CreateCommand(string methodName, params object[] parameters)
        {
            var serializer = new XmlRpcRequestSerializer();
            var request = new XmlRpcRequest
            {
                method = methodName,
                args = parameters
            };
            var stream = new MemoryStream();
            serializer.SerializeRequest(stream, request);
            stream.Seek(0, SeekOrigin.Begin);
            return stream;
        }

        public static async Task<T> SendXmlRpcRequest<T>(this HttpClient httpClient, string url, string methodName, params object[] parameters) where T : class
        {
            using var request = CreateCommand(methodName, parameters);
            var rsp = await httpClient
                .PostAsync(url, new StreamContent(request) { Headers = { { "Content-Type", "text/xml" } } })
                .ConfigureAwait(false);
            var deserializer = new XmlRpcResponseDeserializer();
            var obj = deserializer.DeserializeResponse(await rsp.Content.ReadAsStreamAsync().ConfigureAwait(false), typeof(T));
            return obj?.retVal as T;
        }
    }
}