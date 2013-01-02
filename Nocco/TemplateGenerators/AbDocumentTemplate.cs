using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Nocco
{
    public abstract class AbDocumentTemplate : AbTemplateGenerator
    {
        // Properties available from within the template
        public string Title { get; set; }
        public IEnumerable<Section> Sections { get; set; }
        public string DocsRelative { get; set; }
        public string IndexFile { get; set; }
    }
}
