using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using OnePenguin.Essentials.Utilities;

namespace OnePenguin.Essentials
{
    public partial class BasePenguin
    {
        protected void SetRelation(string relation, PenguinReference penguin)
        {
            if (!penguin.ID.HasValue) throw new InvalidOperationException($"Set relation must have an ID.");
            this.DirtyDatastore.Relations.CreateOrSet(relation, new ComparableHashSet<BasePenguinRelationship> { new BasePenguinRelationship(relation, PenguinRelationshipDirection.OUT, penguin.ID.Value) });
        }

        protected void SetRelations(string relation, List<PenguinReference> penguins)
        {
            if (penguins.Any(i => !i.ID.HasValue)) throw new InvalidOperationException($"Set relation must have an ID.");
            this.DirtyDatastore.Relations.CreateOrSet(relation, new ComparableHashSet<BasePenguinRelationship>(penguins.ConvertAll(i => new BasePenguinRelationship(relation, PenguinRelationshipDirection.OUT, i.ID.Value))));
        }

        protected PenguinReference GetRelation(string relation)
        {
            if (this.DirtyDatastore.Relations.ContainsKey(relation))
            {
                var t = this.DirtyDatastore.Relations[relation];
                return t == null || t.Count == 0 ? null : t.First()?.Target;
            }
            else
            {
                if (this.Datastore.Relations.ContainsKey(relation))
                {
                    var t = this.Datastore.Relations[relation];
                    return t == null || t.Count == 0 ? null : t.First()?.Target;
                }
                else return null;
            }
        }

        protected List<PenguinReference> GetRelations(string relation)
        {
            if (this.DirtyDatastore.Relations.ContainsKey(relation))
            {
                var t = this.DirtyDatastore.Relations[relation];
                return t == null || t.Count == 0 ? new List<PenguinReference>() : t.Select(i => i.Target).ToList();
            }
            else
            {
                if (this.Datastore.Relations.ContainsKey(relation))
                {
                    var t = this.Datastore.Relations[relation];
                    return t == null || t.Count == 0 ? new List<PenguinReference>() : t.Select(i => i.Target).ToList();
                }
                else return null;
            }
        }

        protected object GetAttribute(string attribute)
        {
            if (this.DirtyDatastore.Attributes.ContainsKey(attribute))
            {
                return this.DirtyDatastore.Attributes[attribute];
            }
            else
            {
                return this.Datastore.Attributes[attribute];
            }
        }

        protected object GetReferenceAttribute(string attribute)
        {
            return this.Datastore.Attributes[attribute];
        }

        protected void SetAttribute(string attribute, object value)
        {
            this.DirtyDatastore.Attributes.CreateOrSet(attribute, value);
        }
    }
}