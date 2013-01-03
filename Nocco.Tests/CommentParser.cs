using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Nocco.Tests
{
    [TestClass]
    public class CommentParser
    {


        [TestMethod]
        public void CSharpStyleParse()
        {
            List<CommentDefinition> definitions = new List<CommentDefinition>();

            var DoubleSlash = new CommentDefinition
            {
                StartsWith = "//",
                EndsWith = null,
                IgnoreRepeatingChars = true
            };

            var SlashStar = new CommentDefinition
            {
                StartsWith = "/*",
                EndsWith = "*/",
                IgnoreRepeatingChars = true
            };


            definitions.Add(DoubleSlash);
            definitions.Add(SlashStar);


            string[] lines = new[] { 
                "",
                "/// <summary>",
                "/// XML comment for method",
                "/// </summary>",
                "private static void method()",
                "{",
                "//TODO: another comment",
                "foreach (var entry in File.ReadAllText(LanguageFile).FromJson<LanguageConfig[]>()) //comment",
                "{",
                "   /* comment */ Languages.Add(entry.FileExtension.TrimStart(new[] { '.' }), entry);",
                "}",
                "/*  ",
                "*   Comment block inside of the method",
                "*/ var x = \"with some code on the same line\";",
                "}"
            };



            Queue<LineResult> ExpectedQueue = new Queue<LineResult>();

            Action<int, string, ResultType, SourceComposition, CommentDefinition> QuickEnq = (a, b, c, d, e) =>
            {
                ExpectedQueue.Enqueue(new LineResult { SourceLineNumber = a, Result = b, ResultType = c, SourceLineComposition = d, MatchingDefinition = e });
            };


            QuickEnq(1, lines[0], ResultType.Unknown, SourceComposition.Unknown, null);

            //first comment block
            QuickEnq(2, lines[1], ResultType.Comment, SourceComposition.Comment, DoubleSlash);
            QuickEnq(3, lines[2], ResultType.Comment, SourceComposition.Comment, DoubleSlash);
            QuickEnq(4, lines[3], ResultType.Comment, SourceComposition.Comment, DoubleSlash);

            //code block
            QuickEnq(5, lines[4], ResultType.Code, SourceComposition.Code, null);
            QuickEnq(6, lines[5], ResultType.Code, SourceComposition.Code, null);

            //in function comment
            QuickEnq(7, lines[6], ResultType.Comment, SourceComposition.Comment, DoubleSlash);

            //line of code with a comment at the end
            QuickEnq(8, "foreach (var entry in File.ReadAllText(LanguageFile).FromJson<LanguageConfig[]>()) ", ResultType.Code, SourceComposition.Mixed, null);
            QuickEnq(8, "//comment", ResultType.Comment, SourceComposition.Mixed, DoubleSlash);

            QuickEnq(9, lines[8], ResultType.Code, SourceComposition.Code, null);

            //line with comment in front, line of code at end
            QuickEnq(10, "   /* comment */", ResultType.Comment, SourceComposition.Mixed, SlashStar);
            QuickEnq(10, " Languages.Add(entry.FileExtension.TrimStart(new[] { '.' }), entry);", ResultType.Code, SourceComposition.Mixed, null);

            QuickEnq(11, lines[10], ResultType.Code, SourceComposition.Code, null);

            //comment code block
            QuickEnq(12, lines[11], ResultType.Comment, SourceComposition.Comment, SlashStar);
            QuickEnq(13, lines[12], ResultType.Comment, SourceComposition.Comment, SlashStar);

            //code block end with code at end
            QuickEnq(14, "*/", ResultType.Comment, SourceComposition.Mixed, SlashStar);
            QuickEnq(14, " var x = \"with some code on the same line\";", ResultType.Code, SourceComposition.Mixed, null);

            //end of method
            QuickEnq(15, lines[10], ResultType.Code, SourceComposition.Code, null);


            //action
            using (var reader = new StringReader(string.Join(Environment.NewLine, lines)))
            {
                foreach (var actual in Parser.Process(reader, definitions))
                {
                    var expected = ExpectedQueue.Dequeue();

                    Assert.AreEqual(expected.Result, actual.Result);
                    Assert.AreEqual(expected.ResultType, actual.ResultType);
                    Assert.AreEqual(expected.SourceLineComposition, actual.SourceLineComposition);
                    Assert.AreEqual(expected.SourceLineNumber, actual.SourceLineNumber);
                    Assert.AreEqual(expected.MatchingDefinition, actual.MatchingDefinition);

                }

            }
        }


        [TestMethod]
        public void JSDocsStyleParse()
        {
            List<CommentDefinition> definitions = new List<CommentDefinition>();

            var SlashStar = new CommentDefinition
            {
                StartsWith = "/*",
                EndsWith = "*/",
                IgnoreRepeatingChars = true
            };


            definitions.Add(SlashStar);


            string[] lines = new[] { 
                //from: http://en.wikipedia.org/wiki/JSDoc
" /**",
" * Creates an instance of Circle.",
" *",
" * @constructor",
" * @this {Circle}",
" * @param {number} r The desired radius of the circle.",
" */",
"function Circle(r) {",
"    /** @private */ this.radius = r;",
"    /** @private */ this.circumference = 2 * Math.PI * r;",
"}"
            };



            Queue<LineResult> ExpectedQueue = new Queue<LineResult>();

            Action<int, string, ResultType, SourceComposition, CommentDefinition> QuickEnq = (a, b, c, d, e) =>
            {
                ExpectedQueue.Enqueue(new LineResult { SourceLineNumber = a, Result = b, ResultType = c, SourceLineComposition = d, MatchingDefinition = e });
            };

            //comment block
            QuickEnq(1, lines[0], ResultType.Comment, SourceComposition.Comment, SlashStar);
            QuickEnq(2, lines[1], ResultType.Comment, SourceComposition.Comment, SlashStar);
            QuickEnq(3, lines[2], ResultType.Comment, SourceComposition.Comment, SlashStar);
            QuickEnq(4, lines[3], ResultType.Comment, SourceComposition.Comment, SlashStar);
            QuickEnq(5, lines[4], ResultType.Comment, SourceComposition.Comment, SlashStar);
            QuickEnq(6, lines[5], ResultType.Comment, SourceComposition.Comment, SlashStar);
            QuickEnq(7, lines[6], ResultType.Comment, SourceComposition.Comment, SlashStar);

            //code only
            QuickEnq(8, lines[7], ResultType.Code, SourceComposition.Code, null);

            //mix 1
            QuickEnq(9, "    /** @private */", ResultType.Comment, SourceComposition.Mixed, SlashStar);
            QuickEnq(9, " this.radius = r;", ResultType.Code, SourceComposition.Mixed, null);

            //mix 2
            QuickEnq(10, "    /** @private */", ResultType.Comment, SourceComposition.Mixed, SlashStar);
            QuickEnq(10, " this.circumference = 2 * Math.PI * r;", ResultType.Code, SourceComposition.Mixed, null);

            //code only
            QuickEnq(11, lines[10], ResultType.Code, SourceComposition.Code, null);

            //action
            using (var reader = new StringReader(string.Join(Environment.NewLine, lines)))
            {
                foreach (var actual in Parser.Process(reader, definitions))
                {
                    var expected = ExpectedQueue.Dequeue();

                    Assert.AreEqual(expected.Result, actual.Result);
                    Assert.AreEqual(expected.ResultType, actual.ResultType);
                    Assert.AreEqual(expected.SourceLineComposition, actual.SourceLineComposition);
                    Assert.AreEqual(expected.SourceLineNumber, actual.SourceLineNumber);
                    Assert.AreEqual(expected.MatchingDefinition, actual.MatchingDefinition);

                }

            }
        }

    }
}
