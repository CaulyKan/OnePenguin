using System;
using System.Collections.Generic;

namespace OnePenguin.Essentials {
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

        public TPenguin As<TPenguin>() where TPenguin: BasePenguin
        {
            return Activator.CreateInstance(typeof(TPenguin), new { this.ID, this.Datastore }) as TPenguin;
        }

        public Datastore Datastore {get;set;}

        public Datastore DirtyDatastore {get;set;}

        public bool IsDirty 
        {
            get 
            {
                return !(DirtyDatastore.Attributes.Count == 0 && DirtyDatastore.RelationsIn.Count == 0 && DirtyDatastore.RelationsOut.Count == 0);
            }
        }
    }

    public class BasePenguinRelationship 
    {
        public string RelationName { get; set; }


    }

    public enum PenguinRelationshipDirection
    {
        IN,
        OUT
    }

    public class PenguinReference : IPenguinReference
    {
        public long? ID {get;set;} = null;
    }

    public interface IPenguinReference
    {
        long? ID {get;set;}
    }

    public class Datastore
    {
        public Datastore(string typeName) 
        {
            this.TypeName = typeName;
        }

        public string TypeName {get;set;}

        public Dictionary<string, object> Attributes = new Dictionary<string, object>();

        public Dictionary<string, List<long>> RelationsIn = new Dictionary<string, List<long>>();

        public Dictionary<string, List<long>> RelationsOut = new Dictionary<string, List<long>>();
    }
}