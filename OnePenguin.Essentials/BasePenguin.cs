using System;
using System.Collections.Generic;
using OnePenguin.Essentials.Utilities;

namespace OnePenguin.Essentials
{
    public class BasePenguin : PenguinReference
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

        public void AddRelation(PenguinRelationshipDirection direction, string relationName, long id)
        {
            this.AddRelation(direction, relationName, new List<long> { id });
        }

        public void AddRelation(PenguinRelationshipDirection direction, string relationName, List<long> id)
        {
            id.ForEach(i =>
            {
                var basePenguinRelationship = new BasePenguinRelationship(direction, new RelationDatastore(relationName), i);
                this.DirtyDatastore.Relations.CreateOrAddToList(relationName, basePenguinRelationship);
            });
        }
    }

    public class BasePenguinRelationship
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

    public class Datastore
    {
        public Datastore(string typeName)
        {
            this.TypeName = typeName;
        }

        public string TypeName { get; set; }

        public Dictionary<string, object> Attributes = new Dictionary<string, object>();

        public Dictionary<string, List<BasePenguinRelationship>> Relations = new Dictionary<string, List<BasePenguinRelationship>>();
    }

    public class RelationDatastore
    {

        public RelationDatastore(string relationName)
        {
            this.RelationName = relationName;
        }

        public string RelationName { get; set; }

        public Dictionary<string, object> Attributes = new Dictionary<string, object>();
    }
}