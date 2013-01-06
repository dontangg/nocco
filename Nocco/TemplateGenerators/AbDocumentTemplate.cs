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
        
        /// <summary>
        /// Other documents in the same job which produced this document. Contents should be in a relative path form.
        /// </summary>
        public string[] OtherDocumentsInJob { get; set; }
    }
}
