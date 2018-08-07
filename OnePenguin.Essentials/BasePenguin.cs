using System.Collections.Generic;

namespace OnePenguin.Essentials {
    public class BasePenguin : PenguinReference
    {
        public string TypeName {get;}

        public BasePenguin(string typeName)
        {
            this.TypeName = typeName;
        }

        protected Datastore Datastore {get;set;} = new Datastore();

        protected Datastore DirtyDatastore {get;set;} = new Datastore();
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
        public Dictionary<string, object> Attributes;

        public Dictionary<string, List<long>> Relations;
    }
}