using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Nocco.Tests
{
    [TestClass]
    public class CommentCleaner
    {
        [TestMethod]
        public void SingleLineCleanerTest()
        {
            var DoubleSlash = new CommentDefinition
            {
                StartsWith = "//",
                EndsWith = null,
                IgnoreRepeatingChars = true
            };


            Dictionary<string, string> InputAndExpected = new Dictionary<string, string>() { 
                {"/// <summary>", " <summary>"},
                {"    //////// comment", " comment"}
            };



            foreach (var item in InputAndExpected)
            {
                var input = item.Key;
                var expected = item.Value;
                var actual = DoubleSlash.CleanComment(input);

                Assert.AreEqual(expected, actual);


            }
        }


        [TestMethod]
        public void MultiLineCleanerTest()
        {
            var SlashStar = new CommentDefinition
            {
                StartsWith = "/*",
                EndsWith = "*/",
                TrimFromStart = new [] { '*' },
                IgnoreRepeatingChars = true
            };


            Dictionary<string, string> InputAndExpected = new Dictionary<string, string>() { 
                {"/*", ""},
                {"*/", ""},
                {" /* comment */ ", " comment "},
                {"* comment", " comment"},
                {" * comment", " comment"},
            };



            foreach (var item in InputAndExpected)
            {
                var input = item.Key;
                var expected = item.Value;
                var actual = SlashStar.CleanComment(input);

                Assert.AreEqual(expected, actual);


            }


        }

    }
}
