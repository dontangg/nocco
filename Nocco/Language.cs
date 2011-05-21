// A smart class used for generating nice HTML based on the language of your
// choice.
using System.Text.RegularExpressions;

namespace Nocco
{
	class Language
	{
		public string Name;
		public string Symbol;
		public Regex CommentMatcher { get { return new Regex(@"^\s*" + Symbol + @"\s?"); } }
		public Regex CommentFilter { get { return new Regex(@"(^#![/]|^\s*#\{)"); } }
		public string DividerText { get { return "\n" + Symbol + "DIVIDER\n"; } }
		public Regex DividerHTML { get { return new Regex(@"\n*<span class=""c1?"">" + Symbol + @"DIVIDER<\/span>\n*"); } }
	}
}
