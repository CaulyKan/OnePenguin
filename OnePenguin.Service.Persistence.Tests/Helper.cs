using System.Collections.Generic;
using OnePenguin.Essentials;
using OnePenguin.Essentials.Utilities;

namespace OnePenguin.Service.Persistence.Tests
{
    public static class Helper
    {

        public static void AddRelation(this BasePenguin self, PenguinRelationshipDirection direction, string relationName, long id)
        {
            self.AddRelation(direction, relationName, new List<long> { id });
        }

        public static void AddRelation(this BasePenguin self, PenguinRelationshipDirection direction, string relationName, List<long> id)
        {
            id.ForEach(i =>
            {
                var basePenguinRelationship = new BasePenguinRelationship(direction, new RelationDatastore(relationName), i);
                self.DirtyDatastore.Relations.CreateOrAddToList(relationName, basePenguinRelationship);
            });
        }

    }
}