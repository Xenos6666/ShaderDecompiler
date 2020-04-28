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
            Array.Resize(ref args, 2);
            //args[0] = "hgwaterfall.shader";
            //args[1] = "hgwaterfallNEW.shader";
            //if (args.Length != 2)
            //{
            //    Console.WriteLine("shaderReconstructor inputFile outputFile");
            //    return;
            //}

            //string input = args[0];
            //string output = args[1];

            var inputs = Directory.GetFiles(Path.GetDirectoryName(Assembly.GetAssembly(typeof(Entry)).Location), "*.shader", SearchOption.AllDirectories);

            foreach (var input in inputs)
            {
                Console.WriteLine(input);
            }

            Console.ReadLine();

            foreach (var file in inputs)
            {
                Console.Title = Path.GetFileNameWithoutExtension(file);
                var output = Path.GetFileNameWithoutExtension(file) + "_new" + ".shader";
                try
                {
                    ShaderProcess(file, output);
                }
                catch (Exception e)
                {
                    Console.WriteLine("exception on shader " + file + ":\n" + e);
                    if (System.IO.File.Exists(output))
                    {
                        System.IO.File.Delete(output);
                    }
                }
            }
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
