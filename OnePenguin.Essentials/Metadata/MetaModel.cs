using OnePenguin.Essentials;
using System;
using System.Collections.Generic;

namespace OnePenguin.Essentials.Metadata
{
    public class MetaModel
    {
        public virtual List<MetaType> Types { get; set; }

        public MetaType GetMetaType(string parentTypeName)
        {
            return this.Types.Find(i => i.Name == parentTypeName);
        }
    }

    public class MetaType
    {
        public MetaType() { }

        public virtual string Name { get; set; }

        public virtual string ClassName { get { return this.Name + "Penguin"; } }

        public virtual string DisplayName { get; set; }

        public virtual string ParentTypeName { get; set; }

        public virtual List<string> Interfaces { get; set; }

        public virtual List<MetaAttribute> Attributes { get; set; } = new List<MetaAttribute>();

        public virtual List<MetaReferenceAttribute> ReferenceAttributes { get; set; } = new List<MetaReferenceAttribute>();

        public virtual List<MetaRelation> Relations { get; set; } = new List<MetaRelation>();

        public virtual MetaType GetParentType(MetaModel model)
        {
            return model.Types.Find(i => i.Name == this.ParentTypeName);
        }
    }

    public class BaseMetaMember : System.Attribute
    {
        public virtual string Name { get; set; }
    }

    public class BaseMetaAttribute : BaseMetaMember
    {
        public BaseMetaAttribute() { }
        public BaseMetaAttribute(string displayName = "", string category = "", bool browsable = true, string description = "", bool readOnly = false)
        {
            this.DisplayName = displayName;
            this.Category = category;
            this.Browsable = browsable;
            this.Description = description;
            this.Readonly = readOnly;
        }

        public virtual string DisplayName { get; set; }

        public virtual string Category { get; set; }

        public virtual bool Browsable { get; set; } = true;

        public virtual string Description { get; set; }

        public virtual bool Readonly { get; set; }
    }

    public class MetaAttribute : BaseMetaAttribute
    {
        public MetaAttribute() { }

        public MetaAttribute(MetaDataType dataType, string displayName = "", string category = "", bool browsable = true, string description = "", bool readOnly = false)
            : base(displayName, category, browsable, description, readOnly)
        {
            this.DataType = dataType;
        }

        public virtual MetaDataType DataType { get; set; }
    }

    public class MetaReferenceAttribute : BaseMetaAttribute
    {
        public MetaReferenceAttribute() { }

        public MetaReferenceAttribute(string RelationName, string targetAttributeName, string displayName = "", string category = "", bool browsable = true, string description = "", bool readOnly = false)
            : base(displayName, category, browsable, description, readOnly)
        {
            this.RelationName = RelationName;
            this.TargetAttributeName = targetAttributeName;
        }

        public virtual string RelationName { get; set; }

        public virtual string TargetAttributeName { get; set; }

        public virtual MetaRelation GetRelation(MetaModel model, MetaType type)
        {
            return type.Relations.Find(i => i.Name == this.RelationName);
        }

        public virtual MetaAttribute GetTargetAttribute(MetaModel model, MetaType type)
        {
            return this.GetRelation(model, type)?.GetTargetType(model)?.Attributes?.Find(i => i.Name == this.TargetAttributeName);
        }
    }

    public class MetaRelation : BaseMetaMember
    {
        public MetaRelation() { }

        public MetaRelation(string targetTypeName, MetaRelationType relationType)
        {
            this.TargetTypeName = targetTypeName;
            this.RelationType = relationType;
        }

        public virtual string TargetTypeName { get; set; }

        public virtual List<MetaAttribute> Attributes { get; set; } = new List<MetaAttribute>();

        public virtual MetaType GetTargetType(MetaModel model)
        {
            return model.Types.Find(i => i.Name == this.TargetTypeName);
        }

        public virtual MetaRelationType RelationType { get; set; }
    }

    public enum MetaDataType
    {
        INT,
        DOUBLE,
        STRING,
        DATETIME,
        BINARY,
    }

    public enum MetaRelationType
    {
        ONE,
        MANY
    }
}