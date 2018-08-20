using System;
using System.Collections.Generic;
using OnePenguin.Essentials;

namespace OnePenguin.Service.Persistence
{
    public interface IPenguinPersistenceDriver
    {
        BasePenguin GetById(long id);

        List<BasePenguin> GetById(List<long> id);

        TPenguin GetById<TPenguin>(long id) where TPenguin : BasePenguin;

        List<TPenguin> GetById<TPenguin>(List<long> id) where TPenguin : BasePenguin;

        List<BasePenguin> Query(string query);

        List<TPenguin> Query<TPenguin>(string query) where TPenguin : BasePenguin;

        void RunTransaction(Action<IPenguinPersistenceContext> contextAction);

        IPenguinPersistenceContext StartTransaction();

        void Commit();

        void Rollback();
    }

    public interface IPenguinPersistenceContext : IDisposable
    {
        BasePenguin Insert(BasePenguin penguin);

        List<BasePenguin> Insert(List<BasePenguin> penguins);

        TPenguin Insert<TPenguin>(TPenguin penguin) where TPenguin : BasePenguin;

        List<TPenguin> Insert<TPenguin>(List<TPenguin> penguins) where TPenguin : BasePenguin;

        BasePenguin Update(BasePenguin penguin);

        List<BasePenguin> Update(List<BasePenguin> penguins);

        TPenguin Update<TPenguin>(TPenguin penguin) where TPenguin : BasePenguin;

        List<TPenguin> Update<TPenguin>(List<TPenguin> penguins) where TPenguin : BasePenguin;

        void Delete(BasePenguin penguin);

        void Delete(List<BasePenguin> penguins);

        void Delete<TPenguin>(TPenguin penguin) where TPenguin : BasePenguin;

        void Delete<TPenguin>(List<TPenguin> penguins) where TPenguin : BasePenguin;
    }

    public abstract class BasePenguinPersistenceDriver
    {

        public virtual void Delete(BasePenguin penguin)
        {
            this.Delete(new List<BasePenguin> { penguin });
        }

        public virtual void Delete(List<BasePenguin> penguins)
        {
            throw new NotImplementedException();
        }

        public virtual void Delete<TPenguin>(TPenguin penguin) where TPenguin : BasePenguin
        {
            this.Delete(penguin as BasePenguin);
        }

        public virtual void Delete<TPenguin>(List<TPenguin> penguins) where TPenguin : BasePenguin
        {
            this.Delete(penguins.ConvertAll(i => i as BasePenguin));
        }

        public virtual BasePenguin Insert(BasePenguin penguin)
        {
            return this.Insert(new List<BasePenguin> { penguin })[0];
        }

        public virtual List<BasePenguin> Insert(List<BasePenguin> penguins)
        {
            throw new NotImplementedException();
        }

        public virtual TPenguin Insert<TPenguin>(TPenguin penguin) where TPenguin : BasePenguin
        {
            return this.Insert(penguin as BasePenguin) as TPenguin;
        }

        public virtual List<TPenguin> Insert<TPenguin>(List<TPenguin> penguin) where TPenguin : BasePenguin
        {
            return this.Insert(penguin.ConvertAll(i => i as BasePenguin)).ConvertAll(i => i.As<TPenguin>());
        }

        public virtual BasePenguin Update(BasePenguin penguin)
        {
            return this.Update(new List<BasePenguin> { penguin })[0];
        }

        public virtual List<BasePenguin> Update(List<BasePenguin> penguin)
        {
            throw new NotImplementedException();
        }

        public virtual TPenguin Update<TPenguin>(TPenguin penguin) where TPenguin : BasePenguin
        {
            return this.Update(penguin as BasePenguin) as TPenguin;
        }

        public virtual List<TPenguin> Update<TPenguin>(List<TPenguin> penguin) where TPenguin : BasePenguin
        {
            return this.Update(penguin.ConvertAll(i => i as BasePenguin)).ConvertAll(i => i.As<TPenguin>());
        }
    }
}