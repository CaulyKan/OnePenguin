using System;
using System.Collections;
using System.Collections.Generic;
using OnePenguin.Essentials.Utilities;

namespace OnePenguin.Essentials
{
    public class BasePenguin : PenguinReference, IEquatable<BasePenguin>, ICloneable
    {
        public string TypeName
        {
            get
            {
                return this.Datastore.TypeName;
            }
        }

        public BasePenguin(string typeName)
        {
            this.Datastore = new Datastore(typeName);
            this.DirtyDatastore = new Datastore(typeName);
        }

        public BasePenguin(long id, Datastore datastore) : this(datastore.TypeName)
        {
            this.ID = id;
            this.Datastore = datastore;
        }

        public TPenguin As<TPenguin>() where TPenguin : BasePenguin
        {
            return Activator.CreateInstance(typeof(TPenguin), new { this.ID, this.Datastore }) as TPenguin;
        }

        public Datastore Datastore { get; set; }

        public Datastore DirtyDatastore { get; set; }

        public bool IsDirty
        {
            get
            {
                return !(DirtyDatastore.Attributes.Count == 0 && DirtyDatastore.Relations.Count == 0);
            }
        }

        public bool Equals(BasePenguin other)
        {
            if (other == null) return false;

            return this.TypeName == other.TypeName &&
                this.Datastore.Equals(other.Datastore) &&
                this.DirtyDatastore.Equals(other.DirtyDatastore);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var result = 1;
                result = (result * 13) ^ this.TypeName.GetHashCode();
                if (this.Datastore != null) result = (result * 13) ^ this.Datastore.GetHashCode();
                if (this.DirtyDatastore != null) result = (result * 13) ^ this.DirtyDatastore.GetHashCode();

                return result;
            }
        }

        public object Clone()
        {
            var result = Activator.CreateInstance(this.GetType(), new object[] { this.TypeName }) as BasePenguin;

            result.Datastore = this.Datastore.Clone() as Datastore;
            result.DirtyDatastore = this.DirtyDatastore.Clone() as Datastore;

            return result;
        }
    }

    public class BasePenguinRelationship : IEquatable<BasePenguinRelationship>, ICloneable
    {
        public BasePenguinRelationship(string relationName)
        {
            this.Datastore = new RelationDatastore(relationName);
            this.DirtyDatastore = new RelationDatastore(relationName);
        }

        public BasePenguinRelationship(PenguinRelationshipDirection direction, RelationDatastore datastore) : this(datastore.RelationName)
        {
            this.Direction = direction;
            this.Datastore = datastore;
        }

        public BasePenguinRelationship(PenguinRelationshipDirection direction, RelationDatastore datastore, long target) : this(direction, datastore)
        {
            this.Target = new PenguinReference(target);
        }

        public string RelationName
        {
            get
            {
                return this.Datastore.RelationName;
            }
        }

        public PenguinRelationshipDirection Direction { get; set; }

        public PenguinReference Target { get; set; }

        public RelationDatastore Datastore { get; set; }

        public RelationDatastore DirtyDatastore { get; set; }

        public bool IsDirty
        {
            get
            {
                return DirtyDatastore.Attributes.Count > 0;
            }
        }

        public object Clone()
        {
            var result = Activator.CreateInstance(this.GetType(), new object[] { RelationName }) as BasePenguinRelationship;

            result.Direction = this.Direction;
            result.Target = this.Target;
            result.Datastore = this.Datastore.Clone() as RelationDatastore;
            result.DirtyDatastore = this.DirtyDatastore.Clone() as RelationDatastore;

            return result;
        }

        public bool Equals(BasePenguinRelationship other)
        {
            if (other == null) return false;

            return this.RelationName == other.RelationName &&
                this.Datastore.Equals(other.Datastore) &&
                this.Direction.Equals(other.Direction) &&
                this.DirtyDatastore.Equals(other.DirtyDatastore) &&
                this.Target.Equals(other.Target);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var result = 1;
                result = (result * 13) ^ this.RelationName.GetHashCode();
                result = (result * 13) ^ this.Direction.GetHashCode();
                if (this.Target != null) result = (result * 13) ^ this.Target.GetHashCode();
                if (this.Datastore != null) result = (result * 13) ^ this.Datastore.GetHashCode();
                if (this.DirtyDatastore != null) result = (result * 13) ^ this.DirtyDatastore.GetHashCode();

                return result;
            }
        }
    }

    public enum PenguinRelationshipDirection
    {
        IN,
        OUT
    }

    public class PenguinReference : IPenguinReference
    {
        public PenguinReference() { }

        public PenguinReference(long id)
        {
            this.ID = id;
        }

        public long? ID { get; set; } = null;
    }

    public interface IPenguinReference
    {
        long? ID { get; set; }
    }

    public class Datastore : ICloneable, IEquatable<Datastore>
    {
        public Datastore(string typeName)
        {
            this.TypeName = typeName;
        }

        public string TypeName { get; set; }

        public Dictionary<string, object> Attributes = new Dictionary<string, object>();

        public Dictionary<string, List<BasePenguinRelationship>> Relations = new Dictionary<string, List<BasePenguinRelationship>>();

        public object Clone()
        {
            return new Datastore(TypeName) { Attributes = this.Attributes.Clone(), Relations = this.Relations.Clone() };
        }

        public static Datastore Combine(Datastore a, Datastore b)
        {
            var result = a.Clone() as Datastore;

            foreach (var kvp in b.Attributes) result.Attributes.CreateOrSet(kvp.Key, kvp.Value);

            foreach (var kvp in b.Relations) result.Relations.CreateOrSet(kvp.Key, kvp.Value);

            return result;
        }

        public bool Equals(Datastore other)
        {
            if (other == null) return false;

            return this.TypeName == other.TypeName &&
                this.Attributes.EqualsTo(other.Attributes) &&
                this.Relations.EqualsTo(other.Relations);
        }
    }

    public class RelationDatastore : ICloneable, IEquatable<RelationDatastore>
    {
        public RelationDatastore(string relationName)
        {
            this.RelationName = relationName;
        }

        public string RelationName { get; set; }

        public Dictionary<string, object> Attributes = new Dictionary<string, object>();

        public object Clone()
        {
            return new RelationDatastore(RelationName) { Attributes = this.Attributes.Clone() };
        }

        public static RelationDatastore Combine(RelationDatastore a, RelationDatastore b)
        {
            var result = a.Clone() as RelationDatastore;

            foreach (var kvp in b.Attributes) result.Attributes.CreateOrSet(kvp.Key, kvp.Value);

            return result;
        }

        public bool Equals(RelationDatastore other)
        {
            if (other == null) return false;

            return this.RelationName == other.RelationName &&
            this.Attributes.EqualsTo(other.Attributes);
        }
    }
}