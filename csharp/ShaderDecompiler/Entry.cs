using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Mime;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ShaderDecompiler
{
    class Entry
    {
        public static void Main(string[] args)
        {
            if (args.Length != 2)
            {
                Console.WriteLine("shaderReconstructor inputFile outputFile");
                return;
            }

            string input = args[0];
            string output = args[1];

            ShaderProcess(input,output);
        }

        private static void ShaderProcess(string input, string output)
        {
            File shaderFile = new File(input);

            var outFile = System.IO.File.Create(output);

            using (StreamWriter stream = new StreamWriter(outFile))
            {
                shaderFile.Write(stream);
            }

            outFile.Close();
        }
    }
}
