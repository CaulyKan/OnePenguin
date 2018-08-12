using System;
using System.Collections.Generic;
using System.Linq;
using OnePenguin.Essentials;
using OnePenguin.Service.Persistence.Neo4jPersistenceDriver;
using Xunit;
using OnePenguin.Essentials.Utilities;

namespace OnePenguin.Service.Persistence.Tests
{
    public class Neo4jPersistenceDriverTest
    {
        private readonly Neo4jPersistenceDriver.Neo4jPersistenceDriver driver;

        public Neo4jPersistenceDriverTest()
        {
            this.driver = new Neo4jPersistenceDriver.Neo4jPersistenceDriver("bolt://localhost:7687", "neo4j", "neo4j");
        }

        [Fact]
        public void InsertTest1()
        {
            var penguin = new BasePenguin("test");
            penguin.DirtyDatastore.Attributes.Add("StringProp", "StringValue");
            penguin.DirtyDatastore.Attributes.Add("IntProp", 123);
            penguin.DirtyDatastore.Attributes.Add("BoolProp", false);

            BasePenguin insertedPenguin = null;

            this.driver.RunTransaction(context =>
            {
                insertedPenguin = context.Insert(penguin);
            });

            Assert.True(insertedPenguin != null);
            Assert.True(insertedPenguin.ID.HasValue);

            var getPenguin = this.driver.GetById(insertedPenguin.ID.Value);
            Assert.Equal("StringValue", insertedPenguin.Datastore.Attributes["StringProp"]);
            Assert.Equal("StringValue", getPenguin.Datastore.Attributes["StringProp"]);
            Assert.Equal(123, insertedPenguin.Datastore.Attributes["IntProp"]);
            Assert.Equal(123, (long)getPenguin.Datastore.Attributes["IntProp"]);
            Assert.Equal(false, insertedPenguin.Datastore.Attributes["BoolProp"]);
            Assert.Equal(false, getPenguin.Datastore.Attributes["BoolProp"]);
            Assert.False(insertedPenguin.IsDirty);
            Assert.False(getPenguin.IsDirty);
        }

        [Fact]
        public void InsertTest2()
        {
            var relatedPenguins = new List<BasePenguin> { new BasePenguin("in"), new BasePenguin("in"), new BasePenguin("out") };

            this.driver.RunTransaction(context =>
            {
                relatedPenguins = context.Insert(relatedPenguins);
            });

            Assert.True(relatedPenguins.All(i => i.ID.HasValue));

            var penguin = new BasePenguin("obj");
            penguin.AddRelation(PenguinRelationshipDirection.IN, "rel_in", relatedPenguins[0].ID.Value);
            penguin.AddRelation(PenguinRelationshipDirection.IN, "rel_in", relatedPenguins[1].ID.Value);
            penguin.AddRelation(PenguinRelationshipDirection.OUT, "rel_out", relatedPenguins[2].ID.Value);
            BasePenguin insertedPenguin = null;

            this.driver.RunTransaction(context =>
            {
                insertedPenguin = context.Insert(penguin);
            });

            Assert.True(insertedPenguin.ID.HasValue);
            Assert.True(insertedPenguin.Datastore.Relations.ContainsKey("rel_in") && insertedPenguin.Datastore.Relations["rel_in"].Count == 2);
            Assert.True(insertedPenguin.Datastore.Relations.ContainsKey("rel_out") && insertedPenguin.Datastore.Relations["rel_out"].Count == 1);
            Assert.Equal(new[] { relatedPenguins[0], relatedPenguins[1] }.Select(i => i.ID.Value).OrderBy(i => i).ToList(), insertedPenguin.Datastore.Relations["rel_in"].Select(i => i.Target.ID.Value).OrderBy(i => i).ToList());
            Assert.True(insertedPenguin.Datastore.Relations["rel_out"].First().Target.ID.Value == relatedPenguins[2].ID.Value);

            var getPenguin = driver.GetById(insertedPenguin.ID.Value);
            Assert.True(getPenguin.Datastore.Relations.ContainsKey("rel_in") && getPenguin.Datastore.Relations["rel_in"].Count == 2);
            Assert.True(getPenguin.Datastore.Relations.ContainsKey("rel_out") && getPenguin.Datastore.Relations["rel_out"].Count == 1);
            Assert.Equal(new[] { relatedPenguins[0], relatedPenguins[1] }.Select(i => i.ID.Value).OrderBy(i => i).ToList(), getPenguin.Datastore.Relations["rel_in"].Select(i => i.Target.ID.Value).OrderBy(i => i).ToList());
            Assert.True(getPenguin.Datastore.Relations["rel_out"].First().Target.ID.Value == relatedPenguins[2].ID.Value);

            relatedPenguins = driver.GetById(relatedPenguins.Select(i => i.ID.Value).ToList());
            Assert.True(relatedPenguins[0].Datastore.Relations.ContainsKey("rel_in") && relatedPenguins[0].Datastore.Relations["rel_in"].First().Target.ID.Value == insertedPenguin.ID.Value);
            Assert.Equal(PenguinRelationshipDirection.OUT, relatedPenguins[0].Datastore.Relations["rel_in"].First().Direction);
            Assert.True(relatedPenguins[1].Datastore.Relations.ContainsKey("rel_in") && relatedPenguins[1].Datastore.Relations["rel_in"].First().Target.ID.Value == insertedPenguin.ID.Value);
            Assert.Equal(PenguinRelationshipDirection.OUT, relatedPenguins[1].Datastore.Relations["rel_in"].First().Direction);
            Assert.True(relatedPenguins[2].Datastore.Relations.ContainsKey("rel_out") && relatedPenguins[2].Datastore.Relations["rel_out"].First().Target.ID.Value == insertedPenguin.ID.Value);
            Assert.Equal(PenguinRelationshipDirection.IN, relatedPenguins[2].Datastore.Relations["rel_out"].First().Direction);
        }

        [Fact]
        public void UpdateTest1()
        {
            var penguin1 = new BasePenguin("UpdateTest1");

            penguin1.DirtyDatastore.Attributes.Add("StringProp", "StringValue");
            penguin1.DirtyDatastore.Attributes.Add("IntProp", 123L);
            penguin1.DirtyDatastore.Attributes.Add("BoolProp", false);

            var penguin2 = new BasePenguin("UpdateTest1");

            penguin2.DirtyDatastore.Attributes.Add("StringProp", "StringValue");
            penguin2.DirtyDatastore.Attributes.Add("IntProp", 123L);
            penguin2.DirtyDatastore.Attributes.Add("BoolProp", false);

            List<BasePenguin> insertedPenguin = null;

            this.driver.RunTransaction(context =>
            {
                insertedPenguin = context.Insert(new List<BasePenguin> { penguin1, penguin2 });
            });

            Assert.False(insertedPenguin[0].IsDirty && insertedPenguin[1].IsDirty);

            insertedPenguin[0].DirtyDatastore.Attributes.Add("StringProp", "ModifiedStringValue");
            insertedPenguin[1].DirtyDatastore.Attributes.Add("IntProp", 456L);
            insertedPenguin[1].DirtyDatastore.Attributes.Add("NewStringProp", "StringValue");

            List<BasePenguin> updatedPenguins = null;
            this.driver.RunTransaction(context =>
            {
                updatedPenguins = context.Update(insertedPenguin);
            });

            Assert.Equal("ModifiedStringValue", updatedPenguins[0].Datastore.Attributes["StringProp"]);
            Assert.Equal(123L, updatedPenguins[0].Datastore.Attributes["IntProp"]);
            Assert.Equal(false, updatedPenguins[0].Datastore.Attributes["BoolProp"]);
            Assert.Equal("StringValue", updatedPenguins[1].Datastore.Attributes["StringProp"]);
            Assert.Equal(456L, updatedPenguins[1].Datastore.Attributes["IntProp"]);
            Assert.Equal(false, updatedPenguins[1].Datastore.Attributes["BoolProp"]);
            Assert.Equal("StringValue", updatedPenguins[1].Datastore.Attributes["NewStringProp"]);

            var getPenguins = this.driver.GetById(new List<long> { updatedPenguins[0].ID.Value, updatedPenguins[1].ID.Value });
            Assert.Equal(new HashSet<BasePenguin>(getPenguins), new HashSet<BasePenguin>(updatedPenguins));
        }

        [Fact]
        public void UpdateTest2()
        {
            var relatedPenguins = new List<BasePenguin> { new BasePenguin("in"), new BasePenguin("in"), new BasePenguin("out"), new BasePenguin("out") };

            this.driver.RunTransaction(context =>
            {
                relatedPenguins = context.Insert(relatedPenguins);
            });

            Assert.True(relatedPenguins.All(i => i.ID.HasValue));

            var penguin = new BasePenguin("obj");
            penguin.AddRelation(PenguinRelationshipDirection.IN, "rel_in", relatedPenguins[0].ID.Value);
            penguin.AddRelation(PenguinRelationshipDirection.IN, "rel_in", relatedPenguins[1].ID.Value);
            penguin.AddRelation(PenguinRelationshipDirection.OUT, "rel_out", relatedPenguins[2].ID.Value);
            BasePenguin insertedPenguin = null;

            this.driver.RunTransaction(context =>
            {
                insertedPenguin = context.Insert(penguin);
            });

            insertedPenguin.DirtyDatastore.Relations.CreateOrAddToList("rel_out", insertedPenguin.Datastore.Relations["rel_out"].First());
            insertedPenguin.DirtyDatastore.Relations.CreateOrAddToList("rel_out", new BasePenguinRelationship("rel_out", PenguinRelationshipDirection.OUT, relatedPenguins[3].ID.Value));
            insertedPenguin.DirtyDatastore.Relations.CreateOrAddToList("rel_in", insertedPenguin.Datastore.Relations["rel_in"].ToList().Find(i => i.Target.ID == relatedPenguins[0].ID.Value));

            BasePenguin updatedPenguin = null;
            this.driver.RunTransaction(context =>
            {
                updatedPenguin = context.Update(insertedPenguin);
            });

            Assert.Equal(1, updatedPenguin.Datastore.Relations["rel_in"].Count);
            Assert.Equal(insertedPenguin.Datastore.Relations["rel_in"].First(), updatedPenguin.Datastore.Relations["rel_in"].First());
            Assert.Equal(2, updatedPenguin.Datastore.Relations["rel_out"].Count);
            Assert.Equal(insertedPenguin.Datastore.Relations["rel_out"].Select(i => i.Target.ID.Value).OrderBy(i => i).ToList(), new List<long> { relatedPenguins[2].ID.Value, relatedPenguins[3].ID.Value });

            var getPenguin = this.driver.GetById(updatedPenguin.ID.Value);
            Assert.Equal(updatedPenguin, getPenguin);
        }
    }
}
