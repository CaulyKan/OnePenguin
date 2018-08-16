using System;
using System.CodeDom;
using CommandLine;
using System.Linq;
using System.Collections.Generic;

namespace OnePenguin.Essentials.CodeGenerator
{
    class Program
    {
        static void Main(string[] args)
        {
            Parser.Default.ParseArguments<Options>(args).WithParsed<Options>(options =>
            {
                var generator = new CodeGenerator(options);
                generator.Generate();
            });
        }
    }

    public class Options
    {
        [Option('i', "input", Required = true, HelpText = "The metadata file.")]
        public string InputFile { get; set; }

        [Option('o', "output", Required = true, HelpText = "The output cs file.")]
        public string OutputFile { get; set; }

        [Option('a', "assemblies", Required = false, HelpText = "Assemblies to load.")]
        public IEnumerable<string> Assemblies { get; set; }

        [Option('A', "assemblies-folder", Required = false, HelpText = "All assemblies in this folder will be loaded.")]
        public string AssembliesDirectory { get; set; }

        [Option('n', "namespace", Required = true, HelpText = "Target namespace.")]
        public string Namespace { get; set; }
    }
}
