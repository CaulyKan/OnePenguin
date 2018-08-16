using System;
using System.Reflection;
using Xunit;
using System.IO;
using OnePenguin.Essentials.CodeGenerator;
using Xunit.Abstractions;
using OnePenguin.Essentials.Interfaces;
using OnePenguin.Essentials.Metadata;
using System.Collections.Generic;

namespace OnePenguin.Essentials.CodeGenerator.Tests
{
    public class CodeGeneratorTest
    {
        [Fact]
        public void Test1()
        {
            var generator = new CodeGenerator(new Options
            {
                InputFile = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "test.penguin-model"),
                OutputFile = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "test.cs"),
                Namespace = "OnePenguin.Essentials.CodeGenerator.Tests.Test",
                Assemblies = new List<string> { Assembly.GetExecutingAssembly().Location },
            });
            generator.Generate();
            string result = File.ReadAllText(generator.Options.OutputFile);
            Console.WriteLine(result);
        }
    }

    public interface ITest : IMetaInterface
    {
        [MetaAttribute(MetaDataType.INT)]
        int TestInt { get; set; }

        [MetaAttribute(MetaDataType.STRING)]
        string TestString { get; set; }

        [MetaAttribute(MetaDataType.DATETIME)]
        DateTime TestDateTime { get; set; }

        [MetaRelation(nameof(ITest), MetaRelationType.ONE)]
        PenguinReference TestRelation { get; set; }

        [MetaReference(nameof(TestRelation), nameof(TestInt))]
        int ReferenceTestInt { get; }
    }
}
