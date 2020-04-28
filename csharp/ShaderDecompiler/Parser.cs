using System;
using System.IO;
using System.Text.RegularExpressions;

namespace ShaderDecompiler
{
    internal class Parser : IWriteable
    {
        protected StreamReader _input;
        public Parser(StreamReader input)
        {
            _input = input;
        }

        public void GetLine(StreamReader input, out string output)
        {
            Regex empty = new Regex("^\\s*$");
            output = "";
            while (empty.IsMatch(output))
            {
                //if (input.EndOfStream)
                //{
                //    throw new EndOfStreamException();
                //}
                output = input.ReadLine();
            }
        }

        public void LastLine(StreamReader input)
        {
            Regex empty = new Regex("^\\s*$");
            string output = "";
            while (empty.IsMatch(output))
            {
                if (input.EndOfStream)
                {
                    throw new EndOfStreamException();
                }
                output = input.ReadLine();
            }
        }

        public void LineMatch(string input, out Match match, Regex regex, string expectedstr)
        {
            match = regex.Match(input);
            if (!match.Success)
            {
                throw new System.Exception($"line does not match expected format \nLine \"{input}\"\nExpected: \"{expectedstr}\"");
            }
        }

        public void GetLineMatch(StreamReader input, out Match match, Regex regex, string expectedstr)
        {
            GetLine(input, out string line);
            LineMatch(line, out match, regex, expectedstr);
        }

        public virtual string toString()
        {
            throw new System.NotImplementedException();
        }
    }
}