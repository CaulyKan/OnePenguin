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
        private TransactionInfo transactionInfo;

        public Neo4jPersistenceDriver(IConfiguration config) : this(config["neo4j_uri"], config["neo4j_user"], config["neo4j_passwd"]) { }

        public Neo4jPersistenceDriver(string uri, string user, string passwd)
        {
            logger = new LoggerFactory().AddDebug().CreateLogger(nameof(Neo4jPersistenceDriver));

            driver = GraphDatabase.Driver(uri, AuthTokens.Basic(user, passwd));
        }

        public BasePenguin GetById(long id)
        {
            return this.GetById(new List<long> { id })[0];
        }

        public List<BasePenguin> GetById(List<long> id)
        {
            try
            {
                if (this.transactionInfo != null)
                {
                    return Neo4jPersistenceImpl.GetPenguinById(this.transactionInfo.Session, id);
                }
                else
                {
                    using (var session = driver.Session())
                        return Neo4jPersistenceImpl.GetPenguinById(session, id);
                }
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
            if (this.transactionInfo == null)
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
            else
            {
                contextAction(this.transactionInfo.Context);
            }
        }

        public IPenguinPersistenceContext StartTransaction()
        {
            if (transactionInfo != null) throw new PersistenceException("Already in transaction.");
            var session = driver.Session();
            var transaction = session.BeginTransaction();
            var context = new Neo4jPersistenceContext(transaction);
            transactionInfo = new TransactionInfo { Session = session, Transaction = transaction, Context = context };
            return context;
        }

        public void Commit()
        {
            if (transactionInfo != null)
            {
                this.transactionInfo.Transaction.CommitAsync().Wait();
                this.transactionInfo.Transaction.Dispose();
                this.transactionInfo.Session.Dispose();
            }
            transactionInfo = null;
        }

        public void Rollback()
        {
            if (transactionInfo != null)
            {
                this.transactionInfo.Transaction.RollbackAsync().Wait();
                this.transactionInfo.Transaction.Dispose();
                this.transactionInfo.Session.Dispose();
            }
            transactionInfo = null;
        }

        public class TransactionInfo
        {
            public ISession Session { get; set; }
            public ITransaction Transaction { get; set; }
            public Neo4jPersistenceContext Context { get; set; }
        }
    }

    public class Neo4jPersistenceContext : BasePenguinPersistenceDriver, IPenguinPersistenceContext
    {
        private ITransaction transaction;

        public Neo4jPersistenceContext(ITransaction transaction)
        {
            this.transaction = transaction;
        }

        public override void Delete(List<BasePenguin> penguins)
        {
            try
            {
                Neo4jPersistenceImpl.DeletePenguin(this.transaction, penguins);
            }
            catch (Exception e)
            {
                throw new PersistenceException($"{nameof(Neo4jPersistenceDriver)}: DeletePenguin() failed: {e.Message}", e);
            }
        }

        public void Dispose()
        {
            this.transaction.Dispose();
        }

        public override List<BasePenguin> Insert(List<BasePenguin> penguins)
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

        public override List<BasePenguin> Update(List<BasePenguin> penguin)
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
    }
}