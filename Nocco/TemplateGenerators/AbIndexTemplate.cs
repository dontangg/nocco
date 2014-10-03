using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Nocco
{
    public abstract class AbIndexTemplate : AbTemplateGenerator
    {
        // Properties available from within the template
        public string Title { get; set; }
        public string DocsRelative { get; set; }
        public DocumentSummary[] GeneratedDocuments { get; set; }
    }
}
