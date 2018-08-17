using System;
using System.Reflection;
using Xunit;
using System.IO;
using OnePenguin.Essentials.CodeGenerator;
using Xunit.Abstractions;
using OnePenguin.Essentials.Interfaces;
using OnePenguin.Essentials.Metadata;
using System.Linq;
using System.Collections.Generic;

namespace OnePenguin.Essentials.CodeGenerator.Tests
{
    public class CodeGeneratorTest
    {
        [Fact]
        public void Test1()
        {
            var outputPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var generatedFile = Path.Combine(new DirectoryInfo(outputPath).Parent.Parent.Parent.FullName, "_test.cs");

            var generator = new CodeGenerator(new Options
            {
                InputFile = Path.Combine(outputPath, "test.penguin-model"),
                OutputFile = Path.Combine(outputPath, "_test.cs"),
                Namespace = "OnePenguin.Essentials.CodeGenerator.Tests.Test",
                Assemblies = new List<string> { Assembly.GetExecutingAssembly().Location },
                OutputDllFile = Path.Combine(outputPath, "_test.dll"),
            });

            generator.Generate();

            string result = File.ReadAllText(generator.Options.OutputFile);
            Console.WriteLine(result);
            //generator.Compile();

            if (!File.Exists(generatedFile) || !CompareFiles(Path.Combine(outputPath, "_test.cs"), generatedFile))
            {
                File.Copy(Path.Combine(outputPath, "_test.cs"), generatedFile);
                Assert.False(true, "Please re-compile the project and run the test.");
            }
            else
            {
                File.Delete(generatedFile);
            }
        }

        private bool CompareFiles(string path1, string path2)
        {
            return new FileInfo(path1).Length == new FileInfo(path2).Length &&
                File.ReadAllBytes(path1).SequenceEqual(File.ReadAllBytes(path2));
        }
    }

    public interface ITest : IMetaInterface
    {
        [MetaAttribute(MetaDataType.INT)]
        long TestInt { get; set; }

        [MetaAttribute(MetaDataType.STRING)]
        string TestString { get; set; }

        [MetaAttribute(MetaDataType.DATETIME)]
        DateTime TestDateTime { get; set; }

        [MetaRelation(nameof(ITest), MetaRelationType.ONE)]
        PenguinReference TestRelation { get; set; }

        [MetaReference(nameof(TestRelation), nameof(TestInt))]
        long ReferenceTestInt { get; }
    }
}
