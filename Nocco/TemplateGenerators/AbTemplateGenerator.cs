using System;
using System.Collections.Generic;
using System.IO;

namespace Nocco
{

    /// <summary>
    /// To be used as the base class for the generated template.
    /// </summary>
    public abstract class AbTemplateGenerator 
    {
        
        private StreamWriter _writer;        
        
        /// <summary>
        /// Creates a new documentation file
        /// </summary>
        /// <param name="destinationFile"></param>
        public void Generate(string destinationFile)
        {
            //uses a stream writer to minimize memory use
            //this.Sections is defered, so we only need to keep a little bit in memory at a time
            try
            {
                if (!Directory.Exists(Path.GetDirectoryName(destinationFile)))
                    Directory.CreateDirectory(Path.GetDirectoryName(destinationFile));
                

                this._writer = new StreamWriter(destinationFile, false);

                this.Execute();
            }
            finally
            {
                this._writer.Flush();
                this._writer.Dispose();
            }
        }

        /// <summary>
        /// This function will be defined in the inheriting template class. 
        /// It generates the HTML by calling <see cref="Write"/> and <see cref="WriteLiteral"/>.
        /// </summary>
        public abstract void Execute();


        public virtual void Write(object value)
        {
            WriteLiteral(value);
        }

        public virtual void WriteLiteral(object value)
        {
            this._writer.Write(value);
        }
    }
}
