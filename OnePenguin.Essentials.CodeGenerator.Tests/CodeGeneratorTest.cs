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

            //generator.Compile();

            if (!File.Exists(generatedFile) || !CompareFiles(Path.Combine(outputPath, "_test.cs"), generatedFile))
            {
                File.Copy(Path.Combine(outputPath, "_test.cs"), generatedFile);
                Assert.False(true, "Please re-compile the project and run the test.");
            }
            else
            {
                var t = Assembly.GetExecutingAssembly().GetType("OnePenguin.Essentials.CodeGenerator.Tests.Test.Test1Penguin");
                Assert.NotNull(t);

                var props = t.GetProperties().ToList();
                var penguin = Activator.CreateInstance(t) as BasePenguin;

                Assert.True(props.Exists(i => i.Name == "TestInt"));
                props.Find(i => i.Name == "TestInt").SetValue(penguin, 123);
                Assert.Equal(123L, penguin.DirtyDatastore.Attributes["TestInt"]);
                Assert.Equal(123L, props.Find(i => i.Name == "TestInt").GetValue(penguin));

                Assert.True(props.Exists(i => i.Name == "TestInt2"));
                props.Find(i => i.Name == "TestInt2").SetValue(penguin, 123);
                Assert.Equal(123L, penguin.DirtyDatastore.Attributes["TestInt2"]);
                Assert.Equal(123L, props.Find(i => i.Name == "TestInt2").GetValue(penguin));

                var dt = DateTime.Now;
                Assert.True(props.Exists(i => i.Name == "TestDateTime"));
                props.Find(i => i.Name == "TestDateTime").SetValue(penguin, dt);
                Assert.Equal(dt, penguin.DirtyDatastore.Attributes["TestDateTime"]);
                Assert.Equal(dt, props.Find(i => i.Name == "TestDateTime").GetValue(penguin));

                Assert.True(props.Exists(i => i.Name == "TestString"));
                props.Find(i => i.Name == "TestString").SetValue(penguin, "123");
                Assert.Equal("123", penguin.DirtyDatastore.Attributes["TestString"]);
                Assert.Equal("123", props.Find(i => i.Name == "TestString").GetValue(penguin));

                var relatedPenguin = new BasePenguin(1, new Datastore("test"));

                Assert.True(props.Exists(i => i.Name == "TestRelation"));
                props.Find(i => i.Name == "TestRelation").SetValue(penguin, relatedPenguin);
                Assert.Equal(new PenguinReference(relatedPenguin.ID.Value), penguin.DirtyDatastore.Relations["TestRelation"].First().Target);
                Assert.Equal(new PenguinReference(relatedPenguin.ID.Value), props.Find(i => i.Name == "TestRelation").GetValue(penguin));

                Assert.True(props.Exists(i => i.Name == "TestRelation2"));
                props.Find(i => i.Name == "TestRelation2").SetValue(penguin, new List<PenguinReference> { relatedPenguin });
                Assert.Equal(relatedPenguin.ID.Value, penguin.DirtyDatastore.Relations["TestRelation2"].First().Target.ID.Value);
                Assert.Equal(new List<PenguinReference> { relatedPenguin }, props.Find(i => i.Name == "TestRelation2").GetValue(penguin));

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
