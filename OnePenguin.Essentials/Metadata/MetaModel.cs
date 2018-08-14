using OnePenguin.Essentials;
using System.Collections.Generic;

namespace OnePenguin.Essentials.Metadata
{
    public class MetaModel
    {
        public virtual List<MetaType> Types { get; set; }
    }

    public class MetaType
    {
        public virtual string Name { get; set; }

        public virtual string DisplayName { get; set; }

        public virtual string ParentTypeName { get; set; }

        public virtual List<string> Interfaces { get; set; }

        public virtual List<MetaAttribute> Attributes { get; set; } = new List<MetaAttribute>();

        public virtual List<MetaRelation> Relations { get; set; } = new List<MetaRelation>();

        public virtual MetaType GetParentType(MetaModel model)
        {
            return model.Types.Find(i => i.Name == this.ParentTypeName);
        }
    }

    public class BaseMetaAttribute : System.Attribute
    {
        public virtual string Name { get; set; }

        public virtual string DisplayName { get; set; }

        public virtual string Category { get; set; }

        public virtual bool Browsable { get; set; } = true;
    }

    public class MetaAttribute : BaseMetaAttribute
    {

        public virtual MetaDataType DataType { get; set; }
    }

    public class MetaReferenceAttribute : BaseMetaAttribute
    {
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

    public class MetaRelation : System.Attribute
    {
        public virtual string Name { get; set; }

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
        LONG,
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