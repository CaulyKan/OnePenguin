using System.IO;
using OnePenguin.Essentials;
using OnePenguin.Essentials.Metadata;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System;
using System.Reflection;
using OnePenguin.Essentials.Interfaces;
using Newtonsoft.Json;
using System.Linq;
using System.ComponentModel;

namespace OnePenguin.Essentials.CodeGenerator
{
    public class CodeGenerator
    {
        public Options Options { get; }
        public MetaModel Model { get; }
        public List<Type> Interfaces
        { get; }

        public static Dictionary<MetaDataType, Type> ValueTypeMap = new Dictionary<MetaDataType, Type>
        {
            {MetaDataType.INT, typeof(long)},
            {MetaDataType.BINARY, typeof(byte[])},
            {MetaDataType.DOUBLE, typeof(double)},
            {MetaDataType.DATETIME, typeof(DateTime)},
            {MetaDataType.STRING, typeof(string)},
        };
        private CodeDomProvider provider;

        public CodeGenerator(Options options)
        {
            this.Options = options;
            this.Model = GetModel();
            this.Interfaces = GetPenguinInterfaces();
        }

        public void Generate()
        {
            var writer = new StreamWriter(this.Options.OutputFile, false);
            this.provider = CodeDomProvider.CreateProvider("CSharp");

            var compileUnit = new CodeCompileUnit();
            var codeNamespace = new CodeNamespace(this.Options.Namespace);

            GenerateModel(codeNamespace);

            compileUnit.Namespaces.Add(codeNamespace);
            provider.GenerateCodeFromCompileUnit(compileUnit, writer, new CodeGeneratorOptions());
            writer.Close();
        }

        public void Compile()
        {
            var cp = new CompilerParameters(this.GetAllAssemblies().Select(i => i.FullName).ToArray(), this.Options.OutputDllFile, true);

            cp.GenerateExecutable = false;

            var cr = provider.CompileAssemblyFromFile(cp, this.Options.OutputFile);
        }

        private void GenerateModel(CodeNamespace codeNamespace)
        {
            foreach (var type in this.Model.Types)
            {
                codeNamespace.Types.Add(GenerateClass(type));
            }
        }

        private CodeTypeDeclaration GenerateClass(MetaType metaType)
        {
            var resultClass = new CodeTypeDeclaration(metaType.ClassName);
            resultClass.IsClass = true;
            resultClass.TypeAttributes = TypeAttributes.Class | TypeAttributes.Public;
            resultClass.IsPartial = true;

            resultClass.Members.AddRange(GenerateClassConstructors(metaType));

            if (string.IsNullOrEmpty(metaType.ParentTypeName))
            {
                resultClass.BaseTypes.Add(new CodeTypeReference(typeof(BasePenguin)));
            }
            else
            {
                resultClass.BaseTypes.Add(new CodeTypeReference(this.Options.Namespace + "." + metaType.ParentTypeName + "Penguin"));
            }

            foreach (var t in GetObjectInterfaces(metaType))
            {
                resultClass.BaseTypes.Add(new CodeTypeReference(t));
            }
            resultClass.Members.AddRange(CreateMembers(metaType).ToArray());

            resultClass.CustomAttributes.Add(new CodeAttributeDeclaration(typeof(DisplayNameAttribute).FullName,
                 new CodeAttributeArgument(new CodePrimitiveExpression(metaType.DisplayName))));

            return resultClass;
        }

        private CodeTypeMember[] GenerateClassConstructors(MetaType metaType)
        {
            List<CodeTypeMember> resultConstructors = new List<CodeTypeMember>();

            CodeConstructor resultMethod1 = new CodeConstructor();
            resultMethod1.Attributes = MemberAttributes.Public;
            resultMethod1.BaseConstructorArgs.Add(new CodePrimitiveExpression(metaType.Name));
            resultConstructors.Add(resultMethod1);

            CodeConstructor resultMethod2 = new CodeConstructor();
            resultMethod2.Attributes = MemberAttributes.Public;
            resultMethod2.Parameters.Add(new CodeParameterDeclarationExpression(new CodeTypeReference(typeof(long)), "id"));
            resultMethod2.Parameters.Add(new CodeParameterDeclarationExpression(new CodeTypeReference(typeof(Datastore).FullName), "dataStore"));
            resultMethod2.BaseConstructorArgs.Add(new CodeArgumentReferenceExpression("id"));
            resultMethod2.BaseConstructorArgs.Add(new CodeArgumentReferenceExpression("dataStore"));
            resultMethod2.Statements.Add(
                new CodeConditionStatement(
                    new CodeBinaryOperatorExpression(
                        new CodePropertyReferenceExpression(
                            new CodeArgumentReferenceExpression("dataStore"), "TypeName"), CodeBinaryOperatorType.IdentityInequality, new CodePrimitiveExpression(metaType.Name)),
                    new CodeThrowExceptionStatement(
                        new CodeObjectCreateExpression(
                            new CodeTypeReference("System.InvalidCastException"), new CodeBinaryOperatorExpression(
                                new CodePrimitiveExpression($"该Entity的TypeId为{metaType.Name}, 而给定Datastore的TypeId为"), CodeBinaryOperatorType.Add, new CodePropertyReferenceExpression(
                                    new CodeArgumentReferenceExpression("dataStore"), "TypeName"))))));
            resultConstructors.Add(resultMethod2);

            return resultConstructors.ToArray();
        }

        private IEnumerable<BaseMetaMember> GetMembers(MetaType metaType)
        {
            if (metaType == null) return new List<BaseMetaMember>();

            var result = new List<BaseMetaMember>();
            result.AddRange(metaType.Attributes);
            result.AddRange(metaType.Relations);
            result.AddRange(metaType.ReferenceAttributes);

            foreach (var t in GetObjectInterfaces(metaType))
            {
                result.AddRange(GetMembers(t));
            }

            return result;
        }

        private IEnumerable<BaseMetaMember> GetMembers(Type interfaceType)
        {
            var result = new List<BaseMetaMember>();
            foreach (var member in interfaceType.GetMembers())
            {
                result.AddRange(member.GetCustomAttributes(true).ToList()
                    .FindAll(i => i is BaseMetaMember).Cast<BaseMetaMember>()
                    .Select(i => { i.Name = member.Name; return i; }));
            }
            return result;
        }

        private IEnumerable<CodeTypeMember> CreateMembers(MetaType metaType)
        {
            foreach (var member in GetMembers(metaType))
            {
                if (member is BaseMetaAttribute attr)
                {
                    yield return CreateAttribute(metaType, attr);
                }
                else if (member is MetaRelation relation)
                {
                    yield return CreateRelation(metaType, relation);
                }
            }
        }

        private CodeTypeMember CreateRelation(MetaType metaType, MetaRelation relation)
        {
            var resultRelation = new CodeMemberProperty();

            resultRelation.Name = relation.Name;
            if (relation.RelationType == MetaRelationType.ONE)
                resultRelation.Type = new CodeTypeReference(typeof(PenguinReference).FullName);
            else
                resultRelation.Type = new CodeTypeReference(typeof(List<>).FullName.Replace("`1", $"<{typeof(PenguinReference).FullName}>"));
            resultRelation.Attributes = MemberAttributes.Public;

            if (IsInherited(metaType, relation.Name))
                resultRelation.Attributes |= MemberAttributes.Override;

            resultRelation.CustomAttributes.Add(new CodeAttributeDeclaration(
                new CodeTypeReference(typeof(BrowsableAttribute).FullName),
                new CodeAttributeArgument(new CodePrimitiveExpression(false))));

            var getMethod = relation.RelationType == MetaRelationType.ONE ? "GetRelation" : "GetRelations";
            var setMethod = relation.RelationType == MetaRelationType.ONE ? "SetRelation" : "SetRelations";

            resultRelation.GetStatements.Add(new CodeMethodReturnStatement(
                new CodeMethodInvokeExpression(
                    new CodeThisReferenceExpression(), getMethod, new CodePrimitiveExpression(relation.Name))
                )
            );

            resultRelation.SetStatements.Add(new CodeMethodInvokeExpression(
                new CodeThisReferenceExpression(), setMethod, new CodePrimitiveExpression(relation.Name), new CodePropertySetValueReferenceExpression()
            ));

            resultRelation.SetStatements.Add(new CodeMethodInvokeExpression(
                new CodeThisReferenceExpression(), "OnPropertyChanged", new CodePrimitiveExpression(relation.Name)
            ));

            return resultRelation;
        }

        private CodeMemberProperty CreateAttribute(MetaType metaType, BaseMetaAttribute attr)
        {
            var resultAttribute = new CodeMemberProperty();

            var targetAttr = attr as MetaAttribute;
            if (attr is MetaReferenceAttribute refAttr)
            {
                var relation = GetMembers(metaType).ToList().Find(i => i is MetaRelation r && r.Name == refAttr.RelationName) as MetaRelation;
                if (relation == null) throw new CodeGeneratorException($"Can't find relation for reference attribute {attr.Name}");

                var targetType = this.Model.GetMetaType(relation.TargetTypeName);
                if (targetType == null)
                {
                    if (this.Interfaces.Exists(i => i.Name == relation.TargetTypeName))
                    {
                        targetAttr = GetMembers(this.Interfaces.Find(i => i.Name == relation.TargetTypeName))
                            .ToList().Find(i => i is MetaAttribute a && a.Name == refAttr.TargetAttributeName) as MetaAttribute;
                        if (targetAttr == null) throw new CodeGeneratorException($"Can't find target attribute {refAttr.TargetAttributeName} for {refAttr.Name}");
                    }
                    else
                    {
                        throw new CodeGeneratorException($"Can't find type {relation.TargetTypeName} for relation {relation.Name}");
                    }
                }
                else
                {
                    targetAttr = GetMembers(targetType).ToList().Find(i => i is MetaAttribute a && a.Name == refAttr.TargetAttributeName) as MetaAttribute;
                    if (targetAttr == null) throw new CodeGeneratorException($"Can't find target attribute {refAttr.TargetAttributeName} for {refAttr.Name}");
                }
            }

            resultAttribute.Name = attr.Name;
            resultAttribute.Type = new CodeTypeReference(ValueTypeMap[targetAttr.DataType]);
            resultAttribute.Attributes = MemberAttributes.Public;

            if (IsInherited(metaType, attr.Name))
                resultAttribute.Attributes |= MemberAttributes.Override;

            resultAttribute.CustomAttributes.Add(new CodeAttributeDeclaration(
                    new CodeTypeReference(typeof(DisplayNameAttribute).FullName),
                    new CodeAttributeArgument(new CodePrimitiveExpression(string.IsNullOrEmpty(attr.DisplayName) ? attr.Name : attr.DisplayName))));
            resultAttribute.CustomAttributes.Add(new CodeAttributeDeclaration(
                new CodeTypeReference(typeof(DescriptionAttribute).FullName),
                new CodeAttributeArgument(new CodePrimitiveExpression(attr.Description ?? ""))));
            resultAttribute.CustomAttributes.Add(new CodeAttributeDeclaration(
                new CodeTypeReference(typeof(BrowsableAttribute).FullName),
                new CodeAttributeArgument(new CodePrimitiveExpression(attr.Browsable))));
            resultAttribute.CustomAttributes.Add(new CodeAttributeDeclaration(
                new CodeTypeReference(typeof(CategoryAttribute).FullName),
                new CodeAttributeArgument(new CodePrimitiveExpression(attr.Category))));
            resultAttribute.CustomAttributes.Add(new CodeAttributeDeclaration(
                new CodeTypeReference(typeof(ReadOnlyAttribute).FullName),
                new CodeAttributeArgument(new CodePrimitiveExpression(attr.Readonly))));

            resultAttribute.GetStatements.Add(new CodeMethodReturnStatement(
                new CodeCastExpression(ValueTypeMap[targetAttr.DataType], new CodeMethodInvokeExpression(
                    new CodeThisReferenceExpression(), attr is MetaAttribute ? "GetAttribute" : "GetReferenceAttribute", new CodePrimitiveExpression(attr.Name))
                ))
            );

            if (attr is MetaAttribute)
            {
                resultAttribute.SetStatements.Add(new CodeMethodInvokeExpression(
                    new CodeThisReferenceExpression(), "SetAttribute", new CodePrimitiveExpression(attr.Name), new CodePropertySetValueReferenceExpression()
                ));

                resultAttribute.SetStatements.Add(new CodeMethodInvokeExpression(
                    new CodeThisReferenceExpression(), "OnPropertyChanged", new CodePrimitiveExpression(attr.Name)
                ));
            }

            return resultAttribute;
        }

        private bool IsInherited(MetaType metaType, string member)
        {
            var result = false;
            var parent = this.Model.GetMetaType(metaType.ParentTypeName);
            while (parent != null)
            {
                if (GetMembers(parent).Any(i => i.Name == member))
                    return true;
                parent = this.Model.GetMetaType(metaType.ParentTypeName);
            }
            return result;
        }


        private IEnumerable<Type> GetObjectInterfaces(MetaType metaType)
        {
            var interfaceStrings = new List<string>();

            var current = metaType;
            while (current != null)
            {
                interfaceStrings.AddRange(current.Interfaces);
                current = this.Model.GetMetaType(current.ParentTypeName);
            }

            foreach (var metaInterface in interfaceStrings.Distinct())
            {
                var t = this.Interfaces.Find(i => i.Name == metaInterface);
                if (t == null) throw new CodeGeneratorException($"Can't find interface {metaInterface} in given assemblies.");

                yield return t;
            }
        }

        private MetaModel GetModel()
        {
            var content = File.ReadAllText(this.Options.InputFile);
            return JsonConvert.DeserializeObject<MetaModel>(content);
        }

        private List<Assembly> GetAllAssemblies()
        {
            var result = new List<Assembly>() { Assembly.GetAssembly(typeof(BasePenguin)) };

            if (!string.IsNullOrEmpty(this.Options.AssembliesDirectory))
            {
                var folder = new DirectoryInfo(this.Options.AssembliesDirectory);
                var dlls = folder.GetFiles("*.dll");
                foreach (var dll in dlls)
                {
                    try { result.Add(Assembly.LoadFile(dll.FullName)); } catch { };
                }
            }
            if (this.Options.Assemblies != null)
            {
                foreach (var i in this.Options.Assemblies)
                {
                    try { result.Add(Assembly.LoadFile(i)); } catch { };
                }
            }
            return result;
        }

        private List<Type> GetPenguinInterfaces()
        {
            var result = new List<Type>();
            foreach (var assembly in this.GetAllAssemblies())
            {
                var allTypes = assembly.GetTypes();
                foreach (var type in allTypes)
                {
                    if (type.IsInterface && typeof(IMetaInterface).IsAssignableFrom(type))
                    {
                        result.Add(type);
                    }
                }
            }

            return result;
        }
    }

    [System.Serializable]
    public class CodeGeneratorException : System.Exception
    {
        public CodeGeneratorException() { }
        public CodeGeneratorException(string message) : base(message) { }
        public CodeGeneratorException(string message, System.Exception inner) : base(message, inner) { }
        protected CodeGeneratorException(
            System.Runtime.Serialization.SerializationInfo info,
            System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
}