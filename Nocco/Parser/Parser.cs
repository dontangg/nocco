using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Nocco
{

    public static class Parser
    {

        public static IEnumerable<LineResult> Process(FileInfo source, ICollection<CommentDefinition> definitions)
        {
            using (var reader = new StreamReader(File.Open(source.FullName, FileMode.Open, FileAccess.Read, FileShare.Read)))
            {
                foreach (var ret in Process(reader, definitions))
                    yield return ret;
            }

        }



        private static Dictionary<int, Regex> BackingStore = new Dictionary<int, Regex>();
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="pattern"></param>
        /// <returns></returns>
        private static Regex GetRegex(string pattern)
        {
            int key = pattern.GetHashCode();

            Regex ret = null;

            BackingStore.TryGetValue(key, out ret);

            if (ret == null)
            {
                ret = new Regex(pattern);
                BackingStore.Add(key, ret);
            }

            return ret;
        }



        /// <summary>
        /// Processes a series of text lines to separate code and commenting
        /// </summary>
        /// <param name="source"></param>
        /// <param name="definitions"></param>
        /// <returns></returns>
        public static IEnumerable<LineResult> Process(TextReader source, ICollection<CommentDefinition> definitions)
        {            
            int CurrentLineNumber = 0;

            //if in a multiline comment, we need to keep track of it
            CommentDefinition MultilineComment = null;

            Func<string, LineResult> ProcessMultiLine = (line) =>
            {
                if (MultilineComment == null)
                    throw new InvalidOperationException("MultilineComment cannot be null");

                string ResultText = null;


                //does this line contain the ending we're looking for?
                int IndexOfEnding = line.IndexOf(MultilineComment.EndsWith);

                if (IndexOfEnding > -1)
                    ResultText = line.Substring(0, IndexOfEnding + MultilineComment.EndsWith.Length);
                else
                    ResultText = line;


                //get result
                LineResult ret = new LineResult
                {
                    ResultType = ResultType.Comment,
                    Result = ResultText,
                    MatchingDefinition = MultilineComment
                };


                if (IndexOfEnding > -1)
                {
                    //dont with the multiline matching for this definition
                    MultilineComment = null;
                }


                return ret;
            };


            //read stream
            while (source.Peek() > -1)
            {
                CurrentLineNumber++;

                SourceComposition CurrentLineComposition = SourceComposition.Unknown;

                bool ConsumedEntireLine = false;

                //this string will not be changed during this loop
                string CurrentLine = source.ReadLine();


                if (string.IsNullOrWhiteSpace(CurrentLine))
                {
                    yield return new LineResult
                    {
                        ResultType = ResultType.Unknown,
                        Result = CurrentLine,
                        SourceLineNumber = CurrentLineNumber,
                        SourceLineComposition = SourceComposition.Unknown
                    };

                    continue;
                }


                int FromNextIndex = 0;

                List<LineResult> Results = new List<LineResult>();



                while (FromNextIndex < CurrentLine.Length || ConsumedEntireLine)
                {
                    bool KeepProcessing = false;

                    string ToProcess = CurrentLine.Substring(FromNextIndex);

                    //check to see if we are currently processing a multi-line comment
                    if (MultilineComment != null)
                    {
                        var Result = ProcessMultiLine(ToProcess);

                        FromNextIndex = CurrentLine.IndexOf(Result.Result, FromNextIndex) + Result.Result.Length;

                        CurrentLineComposition |= SourceComposition.Comment;

                        Results.Add(Result);

                        continue;
                    }

                    //go through all definitions
                    foreach (var def in definitions)
                    {
                        bool HasEnding = !string.IsNullOrWhiteSpace(def.EndsWith);

                        Regex StartingMatcher = GetRegex("^\\s*" + Regex.Escape(def.StartsWith) + (def.IgnoreRepeatingChars ? "+" : string.Empty));

                        //does line start with the comment token?
                        if (StartingMatcher.IsMatch(ToProcess))
                        {
                            if (!HasEnding) //single-line comment
                            {
                                //entire line is a comment
                                Results.Add(new LineResult
                                {
                                    Result = ToProcess,
                                    ResultType = ResultType.Comment,
                                    MatchingDefinition = def
                                });

                                CurrentLineComposition |= SourceComposition.Comment;

                                ConsumedEntireLine = true;

                                break;
                            }
                            else //multi
                            {
                                //proces multi-line comment
                                MultilineComment = def;

                                KeepProcessing = true;

                                //var Result = ProcessMultiLine(ToProcess);
                                //LastLineMatchIndex = CurrentLine.IndexOf(Result.Result, LastLineMatchIndex) + Result.Result.Length;
                                //CurrentLineComposition |= SourceComposition.Comment;
                                //Results.Add(Result);

                                break;
                            }
                        }

                        //does the line contain the token but not inside of quotes
                        Regex Contains = GetRegex(@"(?<=^([^""]|""[^""]*"")*)("
                            + Regex.Escape(def.StartsWith)
                            + (def.IgnoreRepeatingChars ? "+" : string.Empty)
                            + ")");

                        var match = Contains.Match(ToProcess);

                        if (match.Success) //contains a mixed line
                        {
                            CurrentLineComposition = SourceComposition.Mixed;

                            int CaptureIndex = match.Captures[0].Index;

                            //code
                            Results.Add(new LineResult
                            {
                                Result = ToProcess.Substring(0, CaptureIndex),
                                ResultType = ResultType.Code
                            });

                            //comment 

                            if (!HasEnding) //single line, can't have additional code or comments after this
                            {
                                Results.Add(new LineResult
                                {
                                    Result = ToProcess.Substring(CaptureIndex),
                                    ResultType = ResultType.Comment,
                                    MatchingDefinition = def
                                });

                                ConsumedEntireLine = true;
                                break;
                            }
                            else //multi-line, need to process this as such
                            {
                                MultilineComment = def;
                                FromNextIndex = CaptureIndex;
                                KeepProcessing = true;
                                break;
                            }
                        }
                    }

                    //leave if we don't need to keep processing
                    if (!KeepProcessing)
                        break;

                }

                //code line left over
                if (!ConsumedEntireLine && FromNextIndex < CurrentLine.Length)
                {
                    Results.Add(new LineResult
                    {
                        Result = CurrentLine.Substring(FromNextIndex),
                        ResultType = ResultType.Code,
                    });

                    CurrentLineComposition |= SourceComposition.Code;

                }

                foreach (var res in Results)
                {
                    res.SourceLineNumber = CurrentLineNumber;
                    res.SourceLineComposition = CurrentLineComposition;
                    yield return res;
                }

                



            }


            yield break;


        }





    }
}
