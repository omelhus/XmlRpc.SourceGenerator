using System;
using System.Collections.Generic;
using System.Text;

namespace XmlRpc.SourceGenerator
{
    public class MappingActions
    {
        public NullMappingAction NullMappingAction { get; set; }
        public EnumMapping EnumMapping { get; set; }
    }
}