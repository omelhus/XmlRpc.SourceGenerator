namespace XmlRpc.SourceGenerator
{
    [System.AttributeUsage(System.AttributeTargets.Interface)]
    public class XmlRpcClientAttribute : System.Attribute
    {
        public XmlRpcClientAttribute(string root = null)
        {
            Root = root;
        }

        public string Root { get; }
    }
}