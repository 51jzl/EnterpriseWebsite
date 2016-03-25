using PetaPoco;
using PetaPoco.Internal;
using System;
using System.Collections.Generic;
using System.Linq;

namespace WZ.Repositories
{
    /// <summary>
    /// 用于处理Entity持久化操作
    /// </summary>
    /// <typeparam name="TEntity"> 实体类型</typeparam>
    public class Repository<TEntity> : IRepository<TEntity> where TEntity : class, IEntity
    {
        private int cacheablePageCount;
        private Database database;
        private int primaryMaxRecords;
        private int secondaryMaxRecords;

        public Repository()
        {
            this.cacheablePageCount = 30;
            this.primaryMaxRecords = 0xc350;
            this.secondaryMaxRecords = 0x3e8;
        }

        protected virtual Database CreateDAO()
        {
            if (this.database == null)
            {
                this.database = Database.CreateInstance(null);
            }
            return this.database;
        }

        public virtual int Delete(TEntity entity)
        {
            if (entity == null)
            {
                return 0;
            }
            int num = this.CreateDAO().Delete(entity);
            return num;
        }

        public virtual int DeleteByEntityId(object entityId)
        {
            TEntity entity = this.Get(entityId);
            if (entity == null)
            {
                return 0;
            }
            return this.Delete(entity);
        }

        public bool Exists(object entityId)
        {
            return this.CreateDAO().Exists<TEntity>(entityId);
        }

        public virtual TEntity Get(object entityId)
        {
            TEntity local = default(TEntity);

            local = this.CreateDAO().SingleOrDefault<TEntity>(entityId);

            if ((local != null))
            {
                return local;
            }
            return default(TEntity);
        }

        public IEnumerable<TEntity> GetAll()
        {
            return this.GetAll(null);
        }

        public IEnumerable<TEntity> GetAll(string orderBy)
        {
            IEnumerable<object> enumerable = null;

            PocoData data = PocoData.ForType(typeof(TEntity));
            Sql sql = Sql.Builder.Select(new object[] { data.TableInfo.PrimaryKey }).From(new object[] { data.TableInfo.TableName });
            if (!string.IsNullOrEmpty(orderBy))
            {
                sql.OrderBy(new object[] { orderBy });
            }
            enumerable = this.CreateDAO().FetchFirstColumn(sql);

            return this.PopulateEntitiesByEntityIds<object>(enumerable);
        }

        protected virtual PagingDataSet<TEntity> GetPagingEntities(int pageSize, int pageIndex, Sql sql)
        {
            PagingEntityIdCollection ids = this.CreateDAO().FetchPagingPrimaryKeys<TEntity>((long)this.PrimaryMaxRecords, pageSize, pageIndex, sql);
            return new PagingDataSet<TEntity>(this.PopulateEntitiesByEntityIds<object>(ids.GetPagingEntityIds(pageSize, pageIndex))) { PageIndex = pageIndex, PageSize = pageSize, TotalRecords = ids.TotalRecords };
        }

        protected virtual PagingDataSet<TEntity> GetPagingEntities(int pageSize, int pageIndex, Func<Sql> generateSql)
        {
            PagingEntityIdCollection ids = null;
            if ((pageIndex < this.CacheablePageCount) && (pageSize <= this.SecondaryMaxRecords))
            {
                ids = this.CreateDAO().FetchPagingPrimaryKeys<TEntity>((long)this.PrimaryMaxRecords, pageSize * this.CacheablePageCount, 1, generateSql.Invoke());
                ids.IsContainsMultiplePages = true;
            }
            else
            {
                ids = this.CreateDAO().FetchPagingPrimaryKeys<TEntity>((long)this.PrimaryMaxRecords, pageSize, pageIndex, generateSql.Invoke());
            }
            return new PagingDataSet<TEntity>(this.PopulateEntitiesByEntityIds<object>(ids.GetPagingEntityIds(pageSize, pageIndex))) { PageIndex = pageIndex, PageSize = pageSize, TotalRecords = ids.TotalRecords };
        }

        protected virtual IEnumerable<TEntity> GetTopEntities(int topNumber, Func<Sql> generateSql)
        {
            PagingEntityIdCollection ids = null;

            ids = new PagingEntityIdCollection(this.CreateDAO().FetchTopPrimaryKeys<TEntity>(this.SecondaryMaxRecords, generateSql.Invoke()));
            IEnumerable<object> topEntityIds = ids.GetTopEntityIds(topNumber);
            return this.PopulateEntitiesByEntityIds<object>(topEntityIds);
        }

        public virtual object Insert(TEntity entity)
        {
            if (entity is ISerializableProperties)
            {
                ISerializableProperties properties = entity as ISerializableProperties;
                if (properties != null)
                {
                    properties.Serialize();
                }
            }
            object obj2 = this.CreateDAO().Insert(entity);
            return obj2;
        }

        public virtual IEnumerable<TEntity> PopulateEntitiesByEntityIds<T>(IEnumerable<T> entityIds)
        {
            TEntity[] localArray = new TEntity[entityIds.Count<T>()];
            Dictionary<object, int> dictionary = new Dictionary<object, int>();
            for (int i = 0; i < entityIds.Count<T>(); i++)
            {
                TEntity local = null;
                if (local != null)
                {
                    localArray[i] = local;
                }
                else
                {
                    localArray[i] = default(TEntity);
                    dictionary[entityIds.ElementAt<T>(i)] = i;
                }
            }

            if (dictionary.Count > 0)
            {
                foreach (TEntity local2 in this.CreateDAO().FetchByPrimaryKeys<TEntity>(dictionary.Keys))
                {
                    localArray[dictionary[local2.EntityId]] = local2;
                }
            }
            List<TEntity> list = new List<TEntity>();
            foreach (TEntity local3 in localArray)
            {
                if ((local3 != null))
                {
                    list.Add(local3);
                }
            }
            return list;
        }

        public virtual void Update(TEntity entity)
        {
            int num;
            Database database = this.CreateDAO();
            if (entity is ISerializableProperties)
            {
                ISerializableProperties properties = entity as ISerializableProperties;
                if (properties != null)
                {
                    properties.Serialize();
                }
            }
            num = database.Update(entity);
        }

        protected virtual int CacheablePageCount
        {
            get
            {
                return this.cacheablePageCount;
            }
        }

        protected virtual int PrimaryMaxRecords
        {
            get
            {
                return this.primaryMaxRecords;
            }
        }

        protected virtual int SecondaryMaxRecords
        {
            get
            {
                return this.secondaryMaxRecords;
            }
        }
    }
}
