using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EmbeddedMVC
{
    class CachedView
    {
        public CachedView(DateTime lastWrite, Type tView)
        {
            LastWrite = lastWrite;
            TView = tView;
        }

        public DateTime LastWrite { get; set; }

        public Type TView { get; set; }
    }
}
