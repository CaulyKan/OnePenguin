using System.Collections.Generic;
using Neo4j.Driver.V1;
using OnePenguin.Essentials;
using Microsoft.Extensions.Configuration;
using System;

namespace OnePenguin.Service.Persistence.Neo4jPersistenceDriver
{
    public class Neo4jPersistenceDriver : IPenguinPersistenceDriver
    {
        private IDriver driver;

        public Neo4jPersistenceDriver(IConfiguration config)
        {
            driver = GraphDatabase.Driver(config["neo4j_uri"], AuthTokens.Basic(config["neo4j_user"], config["neo4j_passwd"]));
        }

        public Neo4jPersistenceDriver(string uri, string user, string passwd)
        {
            driver = GraphDatabase.Driver(uri, AuthTokens.Basic(user, passwd));
        }

        public BasePenguin GetById(long id)
        {
            try
            {
                using (var session = driver.Session())
                {
                    var result = session.Run("MATCH ()-[relations_in]->(obj)-[relations_out]->() WHERE ID(obj)=$id RETURN obj, relations_in, relations_out", new { id });

                    return Neo4jConverter.ConvertToPenguin(result);
                }
            }
            catch (Exception e)
            {
                throw new PersistenceException($"{nameof(Neo4jPersistenceDriver)}: GetById({id}) failed: {e.Message}", e);
            }
        }

        public List<BasePenguin> GetById(List<long> id)
        {
            throw new System.NotImplementedException();
        }

        public TPenguin GetById<TPenguin>(long id) where TPenguin : BasePenguin
        {
            throw new System.NotImplementedException();
        }

        public List<TPenguin> GetById<TPenguin>(List<long> id) where TPenguin : BasePenguin
        {
            throw new System.NotImplementedException();
        }

        public List<BasePenguin> Query(string query)
        {
            throw new System.NotImplementedException();
        }

        public List<TPenguin> Query<TPenguin>(string query) where TPenguin : BasePenguin
        {
            throw new System.NotImplementedException();
        }

        public void RunContext(Action<IPenguinPersistenceContext> contextAction)
        {
            using (var session = driver.Session())
            {
                session.WriteTransaction(tx => {
                    var context = new Neo4jPersistenceContext(tx);
                    contextAction(context);
                });
            }
        }
    }

    public class Neo4jPersistenceContext : IPenguinPersistenceContext
    {
        private ITransaction transaction;

        public Neo4jPersistenceContext(ITransaction transaction)
        {
            this.transaction = transaction;
        }

        public void Commit()
        {
            transaction.CommitAsync().Wait();
        }

        public void Delete(BasePenguin penguin)
        {
            throw new System.NotImplementedException();
        }

        public void Delete(List<BasePenguin> penguins)
        {
            throw new System.NotImplementedException();
        }

        public void Delete<TPenguin>(TPenguin penguin) where TPenguin : BasePenguin
        {
            throw new System.NotImplementedException();
        }

        public void Delete<TPenguin>(List<TPenguin> penguins) where TPenguin : BasePenguin
        {
            throw new System.NotImplementedException();
        }

        public void Dispose()
        {
            
        }

        public BasePenguin Insert(BasePenguin penguin)
        {
            throw new System.NotImplementedException();
        }

        public List<BasePenguin> Insert(List<BasePenguin> penguins)
        {
            throw new System.NotImplementedException();
        }

        public TPenguin Insert<TPenguin>(TPenguin penguin) where TPenguin : BasePenguin
        {
            throw new System.NotImplementedException();
        }

        public List<TPenguin> Insert<TPenguin>(List<TPenguin> penguins) where TPenguin : BasePenguin
        {
            throw new System.NotImplementedException();
        }

        public void Rollback()
        {
            this.transaction.RollbackAsync().Wait();
        }

        public BasePenguin Update(BasePenguin penguin)
        {
            throw new System.NotImplementedException();
        }

        public List<BasePenguin> Update(List<BasePenguin> penguins)
        {
            throw new System.NotImplementedException();
        }

        public TPenguin Update<TPenguin>(TPenguin penguin) where TPenguin : BasePenguin
        {
            throw new System.NotImplementedException();
        }

        public List<TPenguin> Update<TPenguin>(List<TPenguin> penguins) where TPenguin : BasePenguin
        {
            throw new System.NotImplementedException();
        }
    }
}