using System;
using System.Collections.Generic;
using System.Text;

namespace PE.Entities
{
    internal class PeDataDirectory
    {
        public PeField VirtualAddress { get; set; }
        public PeField Size { get; set; }
    }
}
