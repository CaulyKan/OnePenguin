using System.Collections.Generic;
using Neo4j.Driver.V1;
using OnePenguin.Essentials;
using Microsoft.Extensions.Configuration;
using System;
using Microsoft.Extensions.Logging.Debug;
using Microsoft.Extensions.Logging;

namespace OnePenguin.Service.Persistence.Neo4jPersistenceDriver
{
    public class Neo4jPersistenceDriver : IPenguinPersistenceDriver
    {
        private readonly Microsoft.Extensions.Logging.ILogger logger;
        private IDriver driver;

        public Neo4jPersistenceDriver(IConfiguration config) : this(config["neo4j_uri"], config["neo4j_user"], config["neo4j_passwd"]) { }

        public Neo4jPersistenceDriver(string uri, string user, string passwd)
        {
            var loggerFactory = new Microsoft.Extensions.Logging.LoggerFactory();
            loggerFactory.AddProvider(new DebugLoggerProvider((text, logLevel) => logLevel >= Microsoft.Extensions.Logging.LogLevel.Debug));
            logger = loggerFactory.CreateLogger(nameof(Neo4jPersistenceDriver));

            driver = GraphDatabase.Driver(uri, AuthTokens.Basic(user, passwd),
                                          new Config { Logger = new Neo4jPersistenceDriverLogger(Neo4j.Driver.V1.LogLevel.Debug, logger) });
        }

        public BasePenguin GetById(long id)
        {
            return this.GetById(new List<long> { id })[0];
        }

        public List<BasePenguin> GetById(List<long> id)
        {
            try
            {
                if (this.transactionInfo.Key != null)
                    return Neo4jPersistenceImpl.GetPenguinById(this.transactionInfo.Key, id);
                else using (var session = driver.Session())
                        return Neo4jPersistenceImpl.GetPenguinById(session, id);
            }
            catch (Exception e)
            {
                logger.LogError(e, $"{nameof(Neo4jPersistenceDriver)}: GetById({id}) failed: {e.Message}");
                throw new PersistenceException($"{nameof(Neo4jPersistenceDriver)}: GetById({id}) failed: {e.Message}", e);
            }
        }

        public TPenguin GetById<TPenguin>(long id) where TPenguin : BasePenguin
        {
            return this.GetById(id) as TPenguin;
        }

        public List<TPenguin> GetById<TPenguin>(List<long> id) where TPenguin : BasePenguin
        {
            return this.GetById(id).ConvertAll(i => i.As<TPenguin>());
        }

        public List<BasePenguin> Query(string query)
        {
            throw new System.NotImplementedException();
        }

        public List<TPenguin> Query<TPenguin>(string query) where TPenguin : BasePenguin
        {
            throw new System.NotImplementedException();
        }

        public void RunTransaction(Action<IPenguinPersistenceContext> contextAction)
        {
            using (var session = driver.Session())
            {
                session.WriteTransaction(tx =>
                {
                    var context = new Neo4jPersistenceContext(tx);
                    contextAction(context);
                });
            }
        }

        private KeyValuePair<ISession, ITransaction> transactionInfo;
        public IPenguinPersistenceContext StartTransaction()
        {
            var session = driver.Session();
            var transaction = session.BeginTransaction();
            transactionInfo = new KeyValuePair<ISession, ITransaction>(session, transaction);
            var context = new Neo4jPersistenceContext(transaction);
            return context;
        }

        public void Commit()
        {
            if (transactionInfo.Value != null)
            {
                this.transactionInfo.Value.CommitAsync().Wait();
                this.transactionInfo.Value.Dispose();
                this.transactionInfo.Key.Dispose();
            }
            transactionInfo = new KeyValuePair<ISession, ITransaction>(null, null);
        }

        public void Rollback()
        {
            if (transactionInfo.Value != null)
            {
                this.transactionInfo.Value.RollbackAsync().Wait();
                this.transactionInfo.Value.Dispose();
                this.transactionInfo.Key.Dispose();
            }
            transactionInfo = new KeyValuePair<ISession, ITransaction>(null, null);
        }
    }

    public class Neo4jPersistenceContext : IPenguinPersistenceContext
    {
        private ITransaction transaction;

        public Neo4jPersistenceContext(ITransaction transaction)
        {
            this.transaction = transaction;
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
            this.transaction.Dispose();
        }

        public BasePenguin Insert(BasePenguin penguin)
        {
            return this.Insert(new List<BasePenguin> { penguin })[0];
        }

        public List<BasePenguin> Insert(List<BasePenguin> penguins)
        {
            try
            {
                return Neo4jPersistenceImpl.InsertPenguin(this.transaction, penguins);
            }
            catch (Exception e)
            {
                throw new PersistenceException($"{nameof(Neo4jPersistenceDriver)}: InsertPenguin() failed: {e.Message}", e);
            }
        }

        public TPenguin Insert<TPenguin>(TPenguin penguin) where TPenguin : BasePenguin
        {
            return this.Insert(penguin) as TPenguin;
        }

        public List<TPenguin> Insert<TPenguin>(List<TPenguin> penguin) where TPenguin : BasePenguin
        {
            return this.Insert(penguin).ConvertAll(i => i.As<TPenguin>());
        }

        public BasePenguin Update(BasePenguin penguin)
        {
            return this.Update(new List<BasePenguin> { penguin })[0];
        }

        public List<BasePenguin> Update(List<BasePenguin> penguin)
        {
            try
            {
                return Neo4jPersistenceImpl.UpdatePenguin(this.transaction, penguin);
            }
            catch (Exception e)
            {
                throw new PersistenceException($"{nameof(Neo4jPersistenceDriver)}: UpdatePenguin() failed: {e.Message}", e);
            }
        }

        public TPenguin Update<TPenguin>(TPenguin penguin) where TPenguin : BasePenguin
        {
            return this.Update(penguin) as TPenguin;
        }

        public List<TPenguin> Update<TPenguin>(List<TPenguin> penguin) where TPenguin : BasePenguin
        {
            return this.Update(penguin).ConvertAll(i => i.As<TPenguin>());
        }
    }

    public class Neo4jPersistenceDriverLogger : Neo4j.Driver.V1.ILogger
    {
        public Neo4jPersistenceDriverLogger(Neo4j.Driver.V1.LogLevel level, Microsoft.Extensions.Logging.ILogger logger)
        {
            this.Level = level;
            this.logger = logger;
        }

        public Neo4j.Driver.V1.LogLevel Level { get; set; }

        private readonly Microsoft.Extensions.Logging.ILogger logger;

        public string BuildMessage(string message, object[] restOfMessage)
        {
            var result = message;
            foreach (var s in restOfMessage) message += "," + s.ToString();
            return result;
        }

        public void Debug(string message, params object[] restOfMessage)
        {
            logger.LogDebug(BuildMessage(message, restOfMessage), restOfMessage);
        }

        public void Error(string message, Exception cause = null, params object[] restOfMessage)
        {
            logger.LogError(BuildMessage(message, restOfMessage), cause, restOfMessage);
        }

        public void Info(string message, params object[] restOfMessage)
        {
            logger.LogInformation(BuildMessage(message, restOfMessage), restOfMessage);
        }

        public void Trace(string message, params object[] restOfMessage)
        {
            logger.LogTrace(BuildMessage(message, restOfMessage), restOfMessage);
        }
    }
}