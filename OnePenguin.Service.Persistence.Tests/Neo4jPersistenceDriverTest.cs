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
            
            this.driver.RunTransaction(context => {
                insertedPenguin = context.Insert(penguin);
            });

            Assert.True(insertedPenguin != null);
            Assert.True(insertedPenguin.ID.HasValue);
            
            var getPenguin = this.driver.GetById(insertedPenguin.ID.Value);
            Assert.Equal("StringValue", insertedPenguin.Datastore.Attributes["StringProp"]);
            Assert.Equal("StringValue", getPenguin.Datastore.Attributes["StringProp"]);
            Assert.Equal(123, insertedPenguin.Datastore.Attributes["IntProp"]);
            Assert.Equal(123, (long) getPenguin.Datastore.Attributes["IntProp"]);
            Assert.Equal(false, insertedPenguin.Datastore.Attributes["BoolProp"]);
            Assert.Equal(false, getPenguin.Datastore.Attributes["BoolProp"]);
            Assert.False(insertedPenguin.IsDirty);
            Assert.False(getPenguin.IsDirty);
        }

        [Fact]
        public void InsertTest2()
        {
            var relatedPenguins = new List<BasePenguin> { new BasePenguin("in"), new BasePenguin("in"), new BasePenguin("out") };

            this.driver.RunTransaction(context => {
                relatedPenguins = context.Insert(relatedPenguins);
            });

            Assert.True(relatedPenguins.All(i => i.ID.HasValue));

            var penguin = new BasePenguin("obj");
            penguin.DirtyDatastore.RelationsIn.CreateOrAddToList("rel_in", relatedPenguins[0].ID.Value);
            penguin.DirtyDatastore.RelationsIn.CreateOrAddToList("rel_in", relatedPenguins[1].ID.Value);
            penguin.DirtyDatastore.RelationsOut.CreateOrAddToList("rel_out", relatedPenguins[2].ID.Value);

            BasePenguin insertedPenguin = null;

            this.driver.RunTransaction(context => {
                insertedPenguin = context.Insert(penguin);
            });

            Assert.True(insertedPenguin.ID.HasValue);
            Assert.True(insertedPenguin.Datastore.RelationsIn.ContainsKey("rel_in") && insertedPenguin.Datastore.RelationsIn["rel_in"].Count == 2);
            Assert.True(insertedPenguin.Datastore.RelationsOut.ContainsKey("rel_out") && insertedPenguin.Datastore.RelationsOut["rel_out"].Count == 1);
            Assert.True(insertedPenguin.Datastore.RelationsIn["rel_in"][0] == relatedPenguins[0].ID.Value && insertedPenguin.Datastore.RelationsIn["rel_in"][1] == relatedPenguins[1].ID.Value);
            Assert.True(insertedPenguin.Datastore.RelationsOut["rel_out"][0] == relatedPenguins[2].ID.Value);

            var getPenguin = driver.GetById(insertedPenguin.ID.Value);
            Assert.True(getPenguin.Datastore.RelationsIn.ContainsKey("rel_in") && getPenguin.Datastore.RelationsIn["rel_in"].Count == 2);
            Assert.True(getPenguin.Datastore.RelationsOut.ContainsKey("rel_out") && getPenguin.Datastore.RelationsOut["rel_out"].Count == 1);
            Assert.True(getPenguin.Datastore.RelationsIn["rel_in"][0] == relatedPenguins[0].ID.Value && getPenguin.Datastore.RelationsIn["rel_in"][1] == relatedPenguins[1].ID.Value);
            Assert.True(getPenguin.Datastore.RelationsOut["rel_out"][0] == relatedPenguins[2].ID.Value);

            relatedPenguins = driver.GetById(relatedPenguins.Select(i => i.ID.Value).ToList());
            Assert.True(relatedPenguins[0].Datastore.RelationsOut.ContainsKey("rel_in") && relatedPenguins[0].Datastore.RelationsOut["rel_in"][0] == insertedPenguin.ID.Value);
            Assert.True(relatedPenguins[1].Datastore.RelationsOut.ContainsKey("rel_in") && relatedPenguins[1].Datastore.RelationsOut["rel_in"][0] == insertedPenguin.ID.Value);
            Assert.True(relatedPenguins[2].Datastore.RelationsIn.ContainsKey("rel_out") && relatedPenguins[2].Datastore.RelationsIn["rel_out"][0] == insertedPenguin.ID.Value);
        }
    }
}
