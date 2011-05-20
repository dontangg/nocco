using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Nocco {
	public abstract class TemplateBase {

		public string Title { get; set; }
		public string PathToCss { get; set; }
		public Func<string, string> GetSourcePath { get; set; }
		public List<Section> Sections { get; set; }
		public List<string> Sources { get; set; }

		public StringBuilder Buffer { get; set; }

		public TemplateBase() {
			Buffer = new StringBuilder();
		}

		public abstract void Execute();

		public virtual void Write(object value) {
			WriteLiteral(value);
		}

		public virtual void WriteLiteral(object value) {
			Buffer.Append(value);
		}
	}
}
