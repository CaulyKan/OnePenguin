using System;
using OnePenguin.Service.Persistence.Neo4jPersistenceDriver;
using Xunit;

namespace OnePenguin.Service.Persistence.Tests
{
    public class Neo4jPersistenceDriverTest
    {
        private readonly Neo4jPersistenceDriver.Neo4jPersistenceDriver driver;

        public Neo4jPersistenceDriverTest()
        {
            this.driver = new Neo4jPersistenceDriver.Neo4jPersistenceDriver("bolt://localhost:7687", "test", "test");
        }

        [Fact]
        public void Test1()
        {
            var penguin = driver.GetById(40);

            Assert.Equal(40, penguin.ID);
        }
    }
}
