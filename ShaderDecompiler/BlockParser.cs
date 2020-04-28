using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShaderDecompiler
{
    class BlockParser : Parser
    {
        protected List<IWriteable> content = new List<IWriteable>();
        protected int _indent = 0;

        public string Run()
        {
            return BlockRoutine();
        }

        public override string toString()
        {
            string ret = string.Empty;
            foreach (var VARIABLE in content)
            {
                ret += VARIABLE.toString();
            }

            return ret;
        }

        protected string BlockRoutine()
        {
            GetLine(_input, out string line);
            while (line.Contains('{') || !line.Contains('}'))
            {
                content.Add(new Line(line));
                if (line.Contains('{') && !line.Contains('}'))
                {
                    string lastline = BlockRoutine();
                    content.Add(new Line(lastline));
                }
                GetLine(_input, out line);
            }
            return line;
        }



        public BlockParser(StreamReader _in, int _indent = 0): base(_in)
        {
            this._indent = _indent;
        }

        ~BlockParser()
        {

        }
    }
}
