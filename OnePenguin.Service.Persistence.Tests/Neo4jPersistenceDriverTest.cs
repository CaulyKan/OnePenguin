using System;
using OnePenguin.Essentials;
using OnePenguin.Service.Persistence.Neo4jPersistenceDriver;
using Xunit;

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
            Assert.Equal(123, getPenguin.Datastore.Attributes["IntProp"]);
            Assert.Equal(false, insertedPenguin.Datastore.Attributes["BoolProp"]);
            Assert.Equal(false, getPenguin.Datastore.Attributes["BoolProp"]);
        }
    }
}
