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
            Assert.True(insertedPenguin.Datastore.Relations["rel_out"][0].Target.ID.Value == relatedPenguins[2].ID.Value);

            var getPenguin = driver.GetById(insertedPenguin.ID.Value);
            Assert.True(getPenguin.Datastore.Relations.ContainsKey("rel_in") && getPenguin.Datastore.Relations["rel_in"].Count == 2);
            Assert.True(getPenguin.Datastore.Relations.ContainsKey("rel_out") && getPenguin.Datastore.Relations["rel_out"].Count == 1);
            Assert.Equal(new[] { relatedPenguins[0], relatedPenguins[1] }.Select(i => i.ID.Value).OrderBy(i => i).ToList(), getPenguin.Datastore.Relations["rel_in"].Select(i => i.Target.ID.Value).OrderBy(i => i).ToList());
            Assert.True(getPenguin.Datastore.Relations["rel_out"][0].Target.ID.Value == relatedPenguins[2].ID.Value);

            relatedPenguins = driver.GetById(relatedPenguins.Select(i => i.ID.Value).ToList());
            Assert.True(relatedPenguins[0].Datastore.Relations.ContainsKey("rel_in") && relatedPenguins[0].Datastore.Relations["rel_in"][0].Target.ID.Value == insertedPenguin.ID.Value);
            Assert.Equal(PenguinRelationshipDirection.OUT, relatedPenguins[0].Datastore.Relations["rel_in"][0].Direction);
            Assert.True(relatedPenguins[1].Datastore.Relations.ContainsKey("rel_in") && relatedPenguins[1].Datastore.Relations["rel_in"][0].Target.ID.Value == insertedPenguin.ID.Value);
            Assert.Equal(PenguinRelationshipDirection.OUT, relatedPenguins[1].Datastore.Relations["rel_in"][0].Direction);
            Assert.True(relatedPenguins[2].Datastore.Relations.ContainsKey("rel_out") && relatedPenguins[2].Datastore.Relations["rel_out"][0].Target.ID.Value == insertedPenguin.ID.Value);
            Assert.Equal(PenguinRelationshipDirection.IN, relatedPenguins[2].Datastore.Relations["rel_out"][0].Direction);
        }
    }
}
