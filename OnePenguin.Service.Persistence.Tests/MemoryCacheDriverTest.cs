using System;
using System.Collections.Generic;
using System.Linq;
using OnePenguin.Essentials;
using OnePenguin.Service.Persistence.MemoryCacheDriver;
using Xunit;
using OnePenguin.Essentials.Utilities;
using OnePenguin.Service.Persistence.Neo4jPersistenceDriver;

namespace OnePenguin.Service.Persistence.Tests
{
    public class MemoryCacheDriverTest
    {
        private Neo4jPersistenceDriver.Neo4jPersistenceDriver driver;
        private MemoryCacheManager cache;

        public MemoryCacheDriverTest()
        {
            this.driver = new Neo4jPersistenceDriver.Neo4jPersistenceDriver("bolt://localhost:7687", "neo4j", "neo4j");
            this.cache = new MemoryCacheManager(driver);
        }

        [Fact]
        public void InsertTest1()
        {
            var session = cache.StartSession();

            var penguin = new BasePenguin("test");
            penguin.DirtyDatastore.Attributes.Add("StringProp", "StringValue");
            penguin.DirtyDatastore.Attributes.Add("IntProp", 123L);
            penguin.DirtyDatastore.Attributes.Add("BoolProp", false);

            BasePenguin insertedPenguin = session.Insert(penguin);

            Assert.True(insertedPenguin != null);
            Assert.True(insertedPenguin.ID.HasValue);

            var getPenguin = session.GetById(insertedPenguin.ID.Value);
            Assert.Equal("StringValue", insertedPenguin.Datastore.Attributes["StringProp"]);
            Assert.Equal("StringValue", getPenguin.Datastore.Attributes["StringProp"]);
            Assert.Equal(123L, insertedPenguin.Datastore.Attributes["IntProp"]);
            Assert.Equal(123L, (long)getPenguin.Datastore.Attributes["IntProp"]);
            Assert.Equal(false, insertedPenguin.Datastore.Attributes["BoolProp"]);
            Assert.Equal(false, getPenguin.Datastore.Attributes["BoolProp"]);
            Assert.False(insertedPenguin.IsDirty);
            Assert.False(getPenguin.IsDirty);

            session.Commit();

            var getPenguin2 = this.driver.GetById(insertedPenguin.ID.Value);
            Assert.Equal(getPenguin, getPenguin2);
        }

        [Fact]
        public void InsertTest2()
        {
            var session = cache.StartSession();

            var relatedPenguins = new List<BasePenguin> { new BasePenguin("in"), new BasePenguin("in"), new BasePenguin("out") };

            relatedPenguins = session.Insert(relatedPenguins);

            Assert.True(relatedPenguins.All(i => i.ID.HasValue));

            var penguin = new BasePenguin("obj");
            penguin.AddRelation(PenguinRelationshipDirection.IN, "rel_in", relatedPenguins[0].ID.Value);
            penguin.AddRelation(PenguinRelationshipDirection.IN, "rel_in", relatedPenguins[1].ID.Value);
            penguin.AddRelation(PenguinRelationshipDirection.OUT, "rel_out", relatedPenguins[2].ID.Value);
            BasePenguin insertedPenguin = null;

            insertedPenguin = session.Insert(penguin);

            Assert.True(insertedPenguin.ID.HasValue);
            Assert.True(insertedPenguin.Datastore.Relations.ContainsKey("rel_in") && insertedPenguin.Datastore.Relations["rel_in"].Count == 2);
            Assert.True(insertedPenguin.Datastore.Relations.ContainsKey("rel_out") && insertedPenguin.Datastore.Relations["rel_out"].Count == 1);
            Assert.Equal(new[] { relatedPenguins[0], relatedPenguins[1] }.Select(i => i.ID.Value).OrderBy(i => i).ToList(), insertedPenguin.Datastore.Relations["rel_in"].Select(i => i.Target.ID.Value).OrderBy(i => i).ToList());
            Assert.True(insertedPenguin.Datastore.Relations["rel_out"].First().Target.ID.Value == relatedPenguins[2].ID.Value);

            var getPenguin = session.GetById(insertedPenguin.ID.Value);
            Assert.True(getPenguin.Datastore.Relations.ContainsKey("rel_in") && getPenguin.Datastore.Relations["rel_in"].Count == 2);
            Assert.True(getPenguin.Datastore.Relations.ContainsKey("rel_out") && getPenguin.Datastore.Relations["rel_out"].Count == 1);
            Assert.Equal(new[] { relatedPenguins[0], relatedPenguins[1] }.Select(i => i.ID.Value).OrderBy(i => i).ToList(), getPenguin.Datastore.Relations["rel_in"].Select(i => i.Target.ID.Value).OrderBy(i => i).ToList());
            Assert.True(getPenguin.Datastore.Relations["rel_out"].First().Target.ID.Value == relatedPenguins[2].ID.Value);

            relatedPenguins = session.GetById(relatedPenguins.Select(i => i.ID.Value).ToList());
            Assert.True(relatedPenguins[0].Datastore.Relations.ContainsKey("rel_in") && relatedPenguins[0].Datastore.Relations["rel_in"].First().Target.ID.Value == insertedPenguin.ID.Value);
            Assert.Equal(PenguinRelationshipDirection.OUT, relatedPenguins[0].Datastore.Relations["rel_in"].First().Direction);
            Assert.True(relatedPenguins[1].Datastore.Relations.ContainsKey("rel_in") && relatedPenguins[1].Datastore.Relations["rel_in"].First().Target.ID.Value == insertedPenguin.ID.Value);
            Assert.Equal(PenguinRelationshipDirection.OUT, relatedPenguins[1].Datastore.Relations["rel_in"].First().Direction);
            Assert.True(relatedPenguins[2].Datastore.Relations.ContainsKey("rel_out") && relatedPenguins[2].Datastore.Relations["rel_out"].First().Target.ID.Value == insertedPenguin.ID.Value);
            Assert.Equal(PenguinRelationshipDirection.IN, relatedPenguins[2].Datastore.Relations["rel_out"].First().Direction);

            session.Commit();

            var getPenguin2 = this.driver.GetById(insertedPenguin.ID.Value);
            Assert.Equal(getPenguin, getPenguin2);
        }

        [Fact]
        public void UpdateTest1()
        {
            var session = this.cache.StartSession();
            var penguin1 = new BasePenguin("UpdateTest1");

            penguin1.DirtyDatastore.Attributes.Add("StringProp", "StringValue");
            penguin1.DirtyDatastore.Attributes.Add("IntProp", 123L);
            penguin1.DirtyDatastore.Attributes.Add("BoolProp", false);

            var penguin2 = new BasePenguin("UpdateTest1");

            penguin2.DirtyDatastore.Attributes.Add("StringProp", "StringValue");
            penguin2.DirtyDatastore.Attributes.Add("IntProp", 123L);
            penguin2.DirtyDatastore.Attributes.Add("BoolProp", false);

            List<BasePenguin> insertedPenguin = null;

            insertedPenguin = session.Insert(new List<BasePenguin> { penguin1, penguin2 });

            Assert.False(insertedPenguin[0].IsDirty && insertedPenguin[1].IsDirty);

            insertedPenguin[0].DirtyDatastore.Attributes.Add("StringProp", "ModifiedStringValue");
            insertedPenguin[1].DirtyDatastore.Attributes.Add("IntProp", 456L);
            insertedPenguin[1].DirtyDatastore.Attributes.Add("NewStringProp", "StringValue");

            List<BasePenguin> updatedPenguins = null;
            updatedPenguins = session.Update(insertedPenguin);

            Assert.Equal("ModifiedStringValue", updatedPenguins[0].Datastore.Attributes["StringProp"]);
            Assert.Equal(123L, updatedPenguins[0].Datastore.Attributes["IntProp"]);
            Assert.Equal(false, updatedPenguins[0].Datastore.Attributes["BoolProp"]);
            Assert.Equal("StringValue", updatedPenguins[1].Datastore.Attributes["StringProp"]);
            Assert.Equal(456L, updatedPenguins[1].Datastore.Attributes["IntProp"]);
            Assert.Equal(false, updatedPenguins[1].Datastore.Attributes["BoolProp"]);
            Assert.Equal("StringValue", updatedPenguins[1].Datastore.Attributes["NewStringProp"]);

            var getPenguins = session.GetById(new List<long> { updatedPenguins[0].ID.Value, updatedPenguins[1].ID.Value });
            Assert.Equal(new ComparableHashSet<BasePenguin>(getPenguins), new ComparableHashSet<BasePenguin>(updatedPenguins));

            session.Commit();

            var getPenguins2 = this.driver.GetById(new List<long> { updatedPenguins[0].ID.Value, updatedPenguins[1].ID.Value });
            Assert.Equal(getPenguins, getPenguins2);
        }

        [Fact]
        public void UpdateTest2()
        {
            var session = cache.StartSession();
            var relatedPenguins = new List<BasePenguin> { new BasePenguin("in"), new BasePenguin("in"), new BasePenguin("out"), new BasePenguin("out") };

            relatedPenguins = session.Insert(relatedPenguins);

            Assert.True(relatedPenguins.All(i => i.ID.HasValue));

            var penguin = new BasePenguin("obj");
            penguin.AddRelation(PenguinRelationshipDirection.IN, "rel_in", relatedPenguins[0].ID.Value);
            penguin.AddRelation(PenguinRelationshipDirection.IN, "rel_in", relatedPenguins[1].ID.Value);
            penguin.AddRelation(PenguinRelationshipDirection.OUT, "rel_out", relatedPenguins[2].ID.Value);
            BasePenguin insertedPenguin = null;

            insertedPenguin = session.Insert(penguin);

            insertedPenguin.DirtyDatastore.Relations.CreateOrAddToList("rel_out", insertedPenguin.Datastore.Relations["rel_out"].First());
            insertedPenguin.DirtyDatastore.Relations.CreateOrAddToList("rel_out", new BasePenguinRelationship("rel_out", PenguinRelationshipDirection.OUT, relatedPenguins[3].ID.Value));
            insertedPenguin.DirtyDatastore.Relations.CreateOrAddToList("rel_in", insertedPenguin.Datastore.Relations["rel_in"].ToList().Find(i => i.Target.ID == relatedPenguins[0].ID.Value));

            BasePenguin updatedPenguin = null;
            updatedPenguin = session.Update(insertedPenguin);

            Assert.Equal(1, updatedPenguin.Datastore.Relations["rel_in"].Count);
            Assert.Equal(insertedPenguin.DirtyDatastore.Relations["rel_in"].First(), updatedPenguin.Datastore.Relations["rel_in"].First());
            Assert.Equal(2, updatedPenguin.Datastore.Relations["rel_out"].Count);
            Assert.Equal(insertedPenguin.DirtyDatastore.Relations["rel_out"].Select(i => i.Target.ID.Value).OrderBy(i => i).ToList(), new List<long> { relatedPenguins[2].ID.Value, relatedPenguins[3].ID.Value });

            var getPenguin = session.GetById(updatedPenguin.ID.Value);
            Assert.Equal(updatedPenguin.Datastore, getPenguin.Datastore);
            Assert.Equal(updatedPenguin, getPenguin);

            session.Commit();

            var getPenguin2 = this.driver.GetById(insertedPenguin.ID.Value);
            Assert.Equal(getPenguin, getPenguin2);
        }

        [Fact]
        public void DeleteTest()
        {
            var session = cache.StartSession();
            var penguin1 = new BasePenguin("DeleteTest1");
            penguin1 = session.Insert(penguin1);

            var penguin2 = new BasePenguin("DeleteTest1");
            penguin2.AddRelation(PenguinRelationshipDirection.OUT, "test", penguin1.ID.Value);
            penguin2 = session.Insert(penguin2);

            Assert.Equal(1, penguin2.Datastore.Relations["test"].Count);

            session.Delete(penguin1);

            Assert.Throws(typeof(PersistenceException), () => session.GetById(penguin1.ID.Value));
            var newPenguin2 = session.GetById(penguin2.ID.Value);
            Assert.Equal(0, newPenguin2.Datastore.Relations.Count);

            session.Commit();
            Assert.Throws(typeof(PersistenceException), () => driver.GetById(penguin1.ID.Value));
            var newPenguin3 = driver.GetById(penguin2.ID.Value);
            Assert.Equal(0, newPenguin3.Datastore.Relations.Count);
        }
    }
}