using System;

namespace ShaderDecompiler
{
    internal class Line : IWriteable
    {
        string _str;

        public Line()
        {

        }

        public Line(string st)
        {
            _str = st;
        }

        ~Line()
        {

        }

        string Run()
        {
            throw new NotImplementedException();
        }

        public static Line operator *(Line line,string text) 
        { 
            line._str = text; 
            return line; 
        }

        public string toString()
        {
            return _str + "\n";
        }
    }
}