using System;
using System.Collections.Generic;
using OnePenguin.Essentials;

namespace OnePenguin.Service.Persistence
{
    public interface IPenguinPersistenceDriver
    {
        BasePenguin GetById(long id);
        
        List<BasePenguin> GetById(List<long> id);

        TPenguin GetById<TPenguin>(long id) where TPenguin: BasePenguin;

        List<TPenguin> GetById<TPenguin>(List<long> id) where TPenguin: BasePenguin;

        List<BasePenguin> Query(string query);

        List<TPenguin> Query<TPenguin>(string query) where TPenguin: BasePenguin;

        void RunContext(Action<IPenguinPersistenceContext> contextAction);
    }

    public interface IPenguinPersistenceContext : IDisposable
    {
        BasePenguin Insert(BasePenguin penguin);

        List<BasePenguin> Insert(List<BasePenguin> penguins);

        TPenguin Insert<TPenguin>(TPenguin penguin) where TPenguin: BasePenguin;

        List<TPenguin> Insert<TPenguin>(List<TPenguin> penguins) where TPenguin: BasePenguin;

        BasePenguin Update(BasePenguin penguin);

        List<BasePenguin> Update(List<BasePenguin> penguins);

        TPenguin Update<TPenguin>(TPenguin penguin) where TPenguin: BasePenguin;

        List<TPenguin> Update<TPenguin>(List<TPenguin> penguins) where TPenguin: BasePenguin;

        void Delete(BasePenguin penguin);

        void Delete(List<BasePenguin> penguins);

        void Delete<TPenguin>(TPenguin penguin) where TPenguin: BasePenguin;

        void Delete<TPenguin>(List<TPenguin> penguins) where TPenguin: BasePenguin;

        void Commit();

        void Rollback();
    }
}