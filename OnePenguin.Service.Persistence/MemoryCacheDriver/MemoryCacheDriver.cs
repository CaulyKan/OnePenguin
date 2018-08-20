
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using Microsoft.Extensions.Configuration;
using OnePenguin.Essentials;

namespace OnePenguin.Service.Persistence.MemoryCacheDriver
{
    public class MemoryCacheManager : IPenguinCacheManager
    {
        private IConfiguration config;
        private IPenguinPersistenceDriver driver;
        private GlobalCache cache;
        private const int defaultCacheCount = 10000;

        public MemoryCacheManager(IPenguinPersistenceDriver driver)
        {
            this.driver = driver;
            this.cache = new GlobalCache(defaultCacheCount);
        }

        public MemoryCacheManager(IConfiguration config, IPenguinPersistenceDriver driver)
        {
            this.config = config;
            this.driver = driver;

            int cacheCount;
            if (int.TryParse(config["memory_cache_count"], out cacheCount))
            {
                this.cache = new GlobalCache(cacheCount);
            }
            else
            {
                throw new PersistenceException("Invalid memory_cache_count configuration, must be a number.");
            }
        }

        public IPenguinCacheDriver StartSession()
        {
            return new MemoryCacheDriver(config, driver, cache);
        }
    }

    public class MemoryCacheDriver : BasePenguinPersistenceDriver, IPenguinCacheDriver
    {
        private IConfiguration config;
        private IPenguinPersistenceDriver driver;
        private GlobalCache globalCache;
        private GlobalCache cache;

        private Dictionary<long, CacheItem> dirtyCache = new Dictionary<long, CacheItem>();

        public void Commit()
        {
            this.Flush();
        }

        public void Rollback()
        {
            this.dirtyCache.Clear();
        }

        public MemoryCacheDriver(IConfiguration config, IPenguinPersistenceDriver driver, GlobalCache globalCache)
        {
            this.config = config;
            this.driver = driver;
            this.globalCache = globalCache;
        }

        public override void Delete(List<BasePenguin> penguins)
        {
            foreach (var penguin in penguins)
            {
                if (!penguin.ID.HasValue) throw new PersistenceException("Delete penguin: must have an ID");
                this.dirtyCache.Add(penguin.ID.Value, new CacheItem(null));
            }
        }

        public BasePenguin GetById(long id)
        {
            return this.GetById(new List<long> { id }).First();
        }

        public List<BasePenguin> GetById(List<long> id)
        {
            var result = new List<BasePenguin>();
            foreach (var i in id)
            {
                if (this.dirtyCache.ContainsKey(i))
                {
                    if (this.dirtyCache[i].Penguin != null)
                        result.Add(this.dirtyCache[i].Penguin.Clone() as BasePenguin);
                    else throw new PersistenceException($"GetById({i}): Already deleted.");
                }
                else if (this.globalCache.CacheExists(i))
                {
                    result.Add(this.globalCache.Get(i));
                }
                else
                {
                    result.Add(this.driver.GetById(i));
                }
            }
            return result;
        }

        public TPenguin GetById<TPenguin>(long id) where TPenguin : BasePenguin
        {
            return this.GetById(id).As<TPenguin>();
        }

        public List<TPenguin> GetById<TPenguin>(List<long> id) where TPenguin : BasePenguin
        {
            return this.GetById(id).ConvertAll(i => i.As<TPenguin>());
        }

        public override List<BasePenguin> Insert(List<BasePenguin> penguins)
        {
            List<BasePenguin> result = null;

            this.driver.RunTransaction(context =>
            {
                result = context.Insert(penguins);
                result.ForEach(i => globalCache.Add(i));
            });
            return result;
        }

        public List<BasePenguin> Query(string query)
        {
            throw new NotImplementedException();
        }

        public List<TPenguin> Query<TPenguin>(string query) where TPenguin : BasePenguin
        {
            throw new NotImplementedException();
        }

        public override List<BasePenguin> Update(List<BasePenguin> penguins)
        {
            var result = new List<BasePenguin>();
            foreach (var penguin in penguins)
            {
                if (!penguin.ID.HasValue) throw new PersistenceException("Update penguin: must have an ID");
                result.Add(penguin.Clone() as BasePenguin);
                this.dirtyCache.Add(penguin.ID.Value, new CacheItem(penguin));
            }
            return result;
        }

        private void Flush()
        {
            var updateList = new List<BasePenguin>();
            var deleteList = new List<BasePenguin>();

            foreach (var kvp in dirtyCache)
            {
                if (kvp.Value.Penguin == null) deleteList.Add(new BasePenguin(kvp.Key, null));
                else if (kvp.Value.Penguin.ID.HasValue) updateList.Add(kvp.Value.Penguin);
                else throw new PersistenceException("Found a cache with no id.");
            }

            this.driver.RunTransaction(context =>
            {
                if (deleteList.Count > 0) context.Delete(deleteList);
                if (updateList.Count > 0) context.Update(updateList);
            });
        }
    }

    public class GlobalCache
    {
        private Dictionary<long, LinkedListNode<CacheItem>> cache = new Dictionary<long, LinkedListNode<CacheItem>>();
        private LinkedList<CacheItem> lru = new LinkedList<CacheItem>();

        private static Thread cleanupThread;
        private int cleanUpCount = 0;
        private static object locker = new object();
        private List<long> lockList = new List<long>();

        public int hit = 0;
        public int miss = 0;

        public GlobalCache(int cleanUpCount = 100000)
        {
            this.cleanUpCount = cleanUpCount;

            cleanupThread = new Thread(new ThreadStart(clenupThreadEntry));
            cleanupThread.IsBackground = true;
            cleanupThread.Priority = ThreadPriority.Lowest;
            cleanupThread.Start();
        }

        public void ReleaseLock(long id)
        {
            lock (locker)
            {
                lockList.Remove(id);
            }
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public void AcquireLock(long id)
        {
            while (lockList.Contains(id)) { }

            lock (locker)
            {
                lockList.Add(id);
            }
        }

        public void WaitLock(long id)
        {
            while (lockList.Contains(id)) { }
        }

        private void clenupThreadEntry()
        {
            while (true)
            {
                Thread.Sleep(10000);

                if (cache.Count > this.cleanUpCount)
                {
                    var count = cache.Count;

                    for (var i = 0; i < count / 5; i++)
                    {
                        this.removeLeastUsage();
                    }
                }
            }
        }

        public bool TryGetValue<T>(long id, out T result) where T : BasePenguin
        {
            result = Get<T>(id);
            return result != null;
        }

        public T Get<T>(long id) where T : BasePenguin
        {
            return this.Get(id) as T;
        }

        public BasePenguin Get(long id)
        {
            this.WaitLock(id);
            return this._get(id);
        }

        private BasePenguin _get(long id)
        {
            LinkedListNode<CacheItem> node;
            if (cache.TryGetValue(id, out node))
            {
                var value = node.Value.Penguin;
                node.Value.hit++;
                lru.Remove(node);
                lru.AddLast(node);
                hit++;
                return value.Clone() as BasePenguin;
            }
            miss++;
            return null;
        }

        public bool CacheExists(long id)
        {
            return cache.ContainsKey(id);
        }

        public void Update(BasePenguin entity)
        {
            if (entity.ID.HasValue)
            {
                try
                {
                    AcquireLock(entity.ID.Value);
                    _update(entity);
                }
                finally
                {
                    ReleaseLock(entity.ID.Value);
                }
            }
        }

        private void _update(BasePenguin entity)
        {
            if (entity.ID.HasValue)
            {
                LinkedListNode<CacheItem> node;
                if (cache.TryGetValue(entity.ID.Value, out node))
                {
                    node.Value.Penguin = entity.Clone() as BasePenguin;
                    node.Value.hit++;
                    lru.Remove(node);
                    lru.AddLast(node);
                }
                else
                {
                    Add(entity);
                }
            }
        }

        public void Add(BasePenguin entity)
        {
            if (entity.ID.HasValue)
            {
                var item = new CacheItem(entity.Clone() as BasePenguin);
                var node = new LinkedListNode<CacheItem>(item);

                lru.AddLast(node);
                cache.Add(entity.ID.Value, node);
            }
        }

        private void removeLeastUsage()
        {
            var node = lru.First();
            lru.RemoveFirst();
            cache.Remove(node.Penguin.ID.Value);
        }

        // internal void Update(long key, List<DirtyGlobalCacheItem> items)
        // {
        //     if (CacheExists(key))
        //     {
        //         try
        //         {
        //             AcquireLock(key);
        //             var entity = GetDiryGlobalCacheItem(key, items);

        //             _update(entity);
        //         }
        //         finally
        //         {
        //             ReleaseLock(key);
        //         }
        //     }
        // }

        // internal BasePenguin GetDiryGlobalCacheItem(long id, List<DirtyGlobalCacheItem> items)
        // {
        //     if (CacheExists(id))
        //     {
        //         var entity = _get(id);

        //         foreach (var i in items)
        //         {
        //             if (i.Role == RelationRole.A)
        //             {
        //                 if (i.ItemToAdd != 0) entity.DataStore.TreatedAsDataARelations.AddOrInsertInto(i.Relation, i.ItemToAdd);
        //                 else if (i.ItemToRemove != 0) entity.DataStore.TreatedAsDataARelations.TryRemoveFrom(i.Relation, i.ItemToRemove);
        //             }
        //             else if (i.Role == RelationRole.B)
        //             {
        //                 if (i.ItemToAdd != 0) entity.DataStore.TreatedAsDataBRelations.AddOrInsertInto(i.Relation, i.ItemToAdd);
        //                 else if (i.ItemToRemove != 0) entity.DataStore.TreatedAsDataBRelations.TryRemoveFrom(i.Relation, i.ItemToRemove);
        //             }
        //         }
        //         return entity;
        //     }
        //     return null;
        // }

        internal void Clear()
        {
            this.cache.Clear();
        }
    }

    public class CacheItem
    {
        public CacheItem(BasePenguin penguin)
        {
            this.Penguin = penguin;
            this.timestamp = DateTime.Now.Ticks;
        }

        public long timestamp;
        public BasePenguin Penguin;
        public long hit = 0;
    }
}