using System.Text.RegularExpressions;

namespace Nocco
{
    /// <summary>
    /// Defines the definition and helpers for a particular commenting style
    /// </summary>
    public class CommentDefinition
    {
        /// <summary>
        /// The starting token for this type of comment
        /// </summary>
        public string StartsWith { get; set; }

        /// <summary>
        /// The ending token for this type of comment, if necessary
        /// </summary>
        public string EndsWith { get; set; }

        /// <summary>
        /// If true, repeating characters which are the same as the last in <see cref="StartWith"/>
        /// or the first in <see cref="EndsWith"/> are consider part of the syntax markup
        /// </summary>
        public bool IgnoreRepeatingChars { get; set; }

        /// <summary>
        /// A collection of characters which should be trimmed during a call to <see cref="CleanComment"/>
        /// </summary>
        public char[] TrimFromStart { get; set; }

        /// <summary>
        /// Keeps track of Regex object for the cleaner so we don't have to keep building it
        /// </summary>
        private Regex _Cleaner;


        /// <summary>
        /// Removes comment markup and the whitespace which surrounded it
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public string CleanComment(string input)
        {
            //TODO: this is sloppy

            if (this._Cleaner == null)
            {

                string RegexEndsWith = null;

                if (IgnoreRepeatingChars && !string.IsNullOrWhiteSpace(this.EndsWith))
                {
                    RegexEndsWith = Regex.Escape(this.EndsWith[0].ToString())
                        + "+"
                        + (this.EndsWith.Length > 1 ? Regex.Escape(this.EndsWith.Substring(1)) : string.Empty);
                }


                string pattern = string.Format("^\\s*({0}{1})?(?<comment>.*?)({2})?\\s*$",
                         Regex.Escape(this.StartsWith),
                         this.IgnoreRepeatingChars ? "+" : null,
                         RegexEndsWith);


                this._Cleaner = new Regex(pattern, RegexOptions.Singleline);

            }

            string ret = this._Cleaner.Match(input).Groups["comment"].Value;


            //trim start
            if (this.TrimFromStart != null && this.TrimFromStart.Length > 0)
            {
                ret = ret.Trim(this.TrimFromStart);  
            }
                        

            //TODO: tactical solution for multi-line comments become empty if they don't have the right start/end

            if (string.IsNullOrWhiteSpace(ret))
            {
                if ((!input.Trim().StartsWith(this.StartsWith) //first line, it's okay to be blank
                    && !input.Trim().EndsWith(this.EndsWith))) //last line, perhaps
                {
                    ret = input; //just return the string as-is
                }
            }



            return ret;
        }
    }

}
