using Neo4j.Driver.V1;
using OnePenguin.Essentials;
using OnePenguin.Essentials.Utilities;
using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace OnePenguin.Service.Persistence.Neo4jPersistenceDriver
{
    public class Neo4jPersistenceImpl
    {
        public static List<BasePenguin> GetPenguinById(ISession session, List<long> ids)
        {
            var objResult = session.Run("MATCH (obj) WHERE ID(obj) IN $ids RETURN obj", new { ids });
            var relationsResult = session.Run(
                "MATCH (relatedObj)-[relation]->(obj) WHERE id(obj) IN $ids RETURN id(obj) as id, 'IN' AS direction, relatedObj, relation " +
                "UNION MATCH (obj)-[relation]->(relatedObj) WHERE id(obj) IN $ids RETURN id(obj) as id, 'OUT' AS direction, relatedObj, relation", new { ids });

            var objs = objResult.Select(i => i["obj"].As<INode>()).ToList();

            if (objs.Count != ids.Count) throw new PersistenceException("Key not Exist:" + string.Join(",", ids.Except(objs.ConvertAll(i => i.Id))));

            var datastoreDic = new Dictionary<long, Datastore>();
            objs.ForEach(i =>
            {
                var ds = new Datastore(i.Labels.First()) { Attributes = i.Properties.ToDictionary() };
                datastoreDic.Add(i.Id, ds);
            });

            foreach (var relationRecord in relationsResult)
            {
                long id = relationRecord["id"].As<long>();
                var relationInfo = relationRecord["relation"].As<IRelationship>();

                var relationDatastore = new RelationDatastore(relationInfo.Type) { Attributes = relationInfo.Properties.ToDictionary() };
                var direction = relationRecord["direction"].As<string>() == "IN" ? PenguinRelationshipDirection.IN : PenguinRelationshipDirection.OUT;
                var relation = new BasePenguinRelationship(direction, relationDatastore, direction == PenguinRelationshipDirection.IN ? relationInfo.StartNodeId : relationInfo.EndNodeId);
                datastoreDic[id].Relations.CreateOrAddToList(relationInfo.Type, relation);
            }

            var resultPenguins = datastoreDic.ToList().Select(i => new BasePenguin(i.Key, i.Value)).ToList();

            return resultPenguins;
        }

        public static List<BasePenguin> InsertPenguin(ITransaction transaction, List<BasePenguin> penguins)
        {
            var penguinsToInsert = penguins.Clone();
            var props = penguinsToInsert.ConvertAll(i => new { name = i.TypeName, attributes = i.DirtyDatastore.Attributes });
            var insertResult = transaction.Run($"UNWIND $props AS properties CALL apoc.create.node([properties.name], properties.attributes) yield node RETURN id(node) as id", new { props });
            var insertedPenguins = insertResult.Select(i => i["id"].As<long>()).ToList();

            List<object> relations = new List<object>();
            for (int n = 0; n < penguinsToInsert.Count; n++)
            {
                penguinsToInsert[n].ID = insertedPenguins[n];
                penguinsToInsert[n].DirtyDatastore.Relations.UnionAllValues().ForEach(i =>
                {
                    relations.Add(ConvertToNeo4jRelationObj(insertedPenguins[n], i));
                });
                penguinsToInsert[n].Datastore = penguinsToInsert[n].DirtyDatastore;
                penguinsToInsert[n].DirtyDatastore = new Datastore(penguinsToInsert[n].TypeName);
            }

            if (relations.Count > 0)
                transaction.Run("UNWIND $relations as relation MATCH (a),(b) WHERE id(a)=relation.from AND id(b) = relation.to CALL apoc.create.relationship(a, relation.name, relation.param, b) YIELD rel RETURN rel", new { relations });

            return penguinsToInsert;
        }

        private static object ConvertToNeo4jRelationObj(long penguinId, BasePenguinRelationship relation)
        {
            if (relation.Direction == PenguinRelationshipDirection.OUT)
                return new { name = relation.RelationName, from = penguinId, to = relation.Target.ID.Value, param = RelationDatastore.Combine(relation.Datastore, relation.DirtyDatastore).Attributes };
            else
                return new { name = relation.RelationName, from = relation.Target.ID.Value, to = penguinId, param = RelationDatastore.Combine(relation.Datastore, relation.DirtyDatastore).Attributes };
        }

        public static List<BasePenguin> UpdatePenguin(ITransaction transaction, List<BasePenguin> penguinsToUpdate)
        {
            var newRelations = new Dictionary<string, List<object>>();
            var removeRelations = new Dictionary<string, List<object>>();

            if (penguinsToUpdate.Any(i => !i.ID.HasValue)) throw new InvalidOperationException("Penguin to update must have an ID.");

            foreach (var penguin in penguinsToUpdate)
            {
                penguin.DirtyDatastore.Relations.UnionAllValues().Except(penguin.Datastore.Relations.UnionAllValues()).ForEach(i =>
                {
                    newRelations.CreateOrAddToList(i.RelationName, ConvertToNeo4jRelationObj(penguin.ID.Value, i));
                });
                penguin.Datastore.Relations.UnionAllValues().Except(penguin.DirtyDatastore.Relations.UnionAllValues()).ForEach(i =>
                {
                    removeRelations.CreateOrAddToList(i.RelationName, ConvertToNeo4jRelationObj(penguin.ID.Value, i));
                });
            }

            foreach (var kvp in removeRelations)
            {
                transaction.Run($"UNWIND $relations as relation MATCH (a)-[r:{kvp.Key}]->(b) WHERE id(a) = relation.from AND id(b) = relation.to DELETE r", new { relations = kvp.Value });
            }

            transaction.Run("UNWIND $relations as relation MATCH (a),(b) WHERE id(a)=relation.from AND id(b) = relation.to CALL apoc.create.relationship(a, relation.name, relation.param, b) YIELD rel RETURN rel", new { relations = newRelations.UnionAllValues() });

            var result = penguinsToUpdate.ConvertAll(i => new BasePenguin(i.ID.Value, Datastore.Combine(i.Datastore, i.DirtyDatastore)));
            var props = result.Select(i => new { id = i.ID.Value, attributes = i.Datastore.Attributes });
            transaction.Run("UNWIND $props as prop MATCH (obj) WHERE id(obj) = prop.id SET obj = prop.attributes RETURN obj", new { props });

            return result;
        }
    }
}
