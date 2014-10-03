using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Nocco
{

    /// <summary>
    /// Result type
    /// </summary>
    public enum ResultType
    {

        /// <summary>
        /// Result type is not known
        /// </summary>
        Unknown,

        /// <summary>
        /// Result type is code
        /// </summary>
        Code,

        /// <summary>
        /// Result type is commenting
        /// </summary>
        Comment
    }

    /// <summary>
    /// The composition of a source line
    /// </summary>
    [Flags]
    public enum SourceComposition
    {

        /// <summary>
        /// Composition is not known
        /// </summary>
        Unknown = 0,

        /// <summary>
        /// Source line contained code
        /// </summary>
        Code = 1,

        /// <summary>
        /// Source line contained commenting
        /// </summary>
        Comment =2,

        /// <summary>
        /// Source line contained a mixture of code and commenting
        /// </summary>
        Mixed = Code | Comment
    }

    public sealed class LineResult
    {
        /// <summary>
        /// The type of result this payload represents
        /// </summary>
        public ResultType ResultType;

        /// <summary>
        /// The composition of the source line read
        /// </summary>
        public SourceComposition SourceLineComposition;

        /// <summary>
        /// Approximate line number in the source this result came from
        /// </summary>
        public int SourceLineNumber;

        /// <summary>
        /// The result of the processed line which matches the <see cref="ResultType"/>
        /// </summary>
        public string Result;

        /// <summary>
        /// If <see cref="ResultType"/> is <see cref="ResultType.Comment"/>, this definition is what 
        /// was used to create this result
        /// </summary>
        public CommentDefinition MatchingDefinition;

    }

}
