using System.IO;
using OnePenguin.Essentials;
using OnePenguin.Essentials.Metadata;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Collections.Generic;

namespace OnePenguin.Essentials.CodeGenerator
{
    public class CodeGenerator
    {
        public CodeGenerator(MetaModel model, string outputFile, string fileNameSpace, List<string> importNamesSpaces)
        {
            var writer = new StreamWriter(outputFile, false);
            var provider = CodeDomProvider.CreateProvider("CSharp");

            var compileUnit = new CodeCompileUnit();
            var codeNamespace = new CodeNamespace(fileNameSpace);

            codeNamespace.Imports.Add(new CodeNamespaceImport("System.Linq"));
            codeNamespace.Imports.Add(new CodeNamespaceImport("OnePenguin.Essentials"));
            importNamesSpaces.ForEach(i => codeNamespace.Imports.Add(new CodeNamespaceImport(i)));

            provider.GenerateCodeFromCompileUnit(compileUnit, writer, new CodeGeneratorOptions());
        }
    }
}