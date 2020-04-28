using System;
using System.IO;
using System.Text.RegularExpressions;

namespace ShaderDecompiler
{
    internal class ShaderParser : Parser
    {
        Line firstLine;
        ShaderBlock shaderBlock;
        Line lastLine;

        public ShaderParser(StreamReader instream) : base(instream)
        {
            shaderBlock = new ShaderBlock(instream);
        }

        internal string Run()
        {
            string labelstr;
            Regex label;
            Match match;

            string tmp = "";
            GetLine(base._input, out tmp);

            labelstr = "^Shader \"([^\"]+)\" \\{$";
            label = new Regex(labelstr);
            LineMatch(tmp, out match, label, labelstr);
            firstLine = new Line(tmp);

            string lastline = shaderBlock.Run();

            labelstr = "^}$";
            label = new Regex(labelstr);
            LineMatch(lastline, out match, label, labelstr);
            lastLine = new Line(lastline);

            return "";
        }

        public override string toString()
        {
            string ret = "";
            ret += firstLine.toString();
            ret += shaderBlock.toString();
            ret += lastLine.toString();
            return ret;
        }
    }
}