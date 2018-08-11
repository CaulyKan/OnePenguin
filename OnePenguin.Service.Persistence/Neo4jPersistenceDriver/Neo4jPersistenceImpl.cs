﻿using Neo4j.Driver.V1;
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

        public static List<BasePenguin> InsertPenguin(ITransaction transaction, List<BasePenguin> penguinsToInsert)
        {
            var result = new List<BasePenguin>();
            penguinsToInsert.ForEach(i => result.Add(null));

            foreach (var groupedPenguins in penguinsToInsert.GroupBy(i => i.TypeName))
            {
                var typeName = groupedPenguins.Key;

                var penguins = new List<BasePenguin>();
                var relations = new List<object>();

                foreach (var penguin in groupedPenguins)
                {
                    penguins.Add(penguin);
                }

                var insertResult = transaction.Run($"UNWIND $props AS properties CREATE (obj:{typeName}) SET obj = properties RETURN obj", new { props = penguins.ConvertAll(i => i.DirtyDatastore.Attributes) });
                var insertedPenguins = insertResult.Select(i => i["obj"].As<INode>()).ToList();

                for (int n = 0; n < penguins.Count; n++)
                {
                    penguins[n].DirtyDatastore.Relations.UnionAllValues().ForEach(i =>
                    {
                        if (i.Direction == PenguinRelationshipDirection.IN)
                            relations.Add(new { name = i.RelationName, from = i.Target.ID.Value, to = insertedPenguins[n].Id });
                        else
                            relations.Add(new { name = i.RelationName, from = insertedPenguins[n].Id, to = i.Target.ID.Value });
                    });
                }

                if (relations.Count > 0)
                    transaction.Run("UNWIND $relations as relation MATCH (a) WHERE id(a)=relation.from WITH a, relation MATCH (b) WHERE id(b)=relation.to CALL apoc.create.relationship(a, relation.name, NULL, b) YIELD rel RETURN rel", new { relations });

                for (var i = 0; i < penguins.Count; i++)
                {
                    result[penguinsToInsert.IndexOf(penguins[i])] = new BasePenguin(insertedPenguins[i].Id, penguins[i].DirtyDatastore);
                }
            }

            return result;
        }
    }
}
