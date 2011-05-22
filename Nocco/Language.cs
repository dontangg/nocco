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
	}
}
