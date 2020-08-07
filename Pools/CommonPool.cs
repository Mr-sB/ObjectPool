using System;
using System.Collections.Generic;

namespace GameUtil
{
    public class CommonPool : ObjectPoolBase<CommonPool>
    {
        //对象池。type => PoolItem
        private readonly Dictionary<Type, PoolItemBase> mPoolItems = new Dictionary<Type, PoolItemBase>();
        //HashSet方便删除
        private readonly HashSet<Type> mPoolKeys = new HashSet<Type>();
        private readonly List<Type> mToDeletePoolKeys = new List<Type>();
        //DeleteTime
        private readonly Dictionary<Type, DeleteTime> mDeleteTimes = new Dictionary<Type, DeleteTime>();
        private readonly DeleteTime mDefaultDeleteTime = new DeleteTime(DEFAULT_POOL_ITEM_DELETE_TIME, DEFAULT_ASSET_DELETE_TIME);
        public const float DEFAULT_POOL_ITEM_DELETE_TIME = 30;
        public const float DEFAULT_ASSET_DELETE_TIME = 30;
        
        #region SetDeleteTime
        public void SetDeleteTime<T>(float poolItemDeleteTime, float assetDeleteTime) where T : class, new()
        {
            Type poolKey = typeof(T);
            if (mDeleteTimes.TryGetValue(poolKey, out var deleteTime))
            {
                deleteTime.PoolItemDeleteTime = poolItemDeleteTime;
                deleteTime.AssetDeleteTime = assetDeleteTime;
            }
            else
                AddDeleteTime(poolKey, new DeleteTime(poolItemDeleteTime, assetDeleteTime));
        }
        
        public void SetPoolItemDeleteTime<T>(float poolItemDeleteTime) where T : class, new()
        {
            Type poolKey = typeof(T);
            if (mDeleteTimes.TryGetValue(poolKey, out var deleteTime))
                deleteTime.PoolItemDeleteTime = poolItemDeleteTime;
            else
                AddDeleteTime(poolKey, new DeleteTime(poolItemDeleteTime, DEFAULT_ASSET_DELETE_TIME));
        }

        public void SetAssetDeleteTime<T>(float assetDeleteTime) where T : class, new()
        {
            Type poolKey = typeof(T);
            if (mDeleteTimes.TryGetValue(poolKey, out var deleteTime))
                deleteTime.AssetDeleteTime = assetDeleteTime;
            else
                AddDeleteTime(poolKey, new DeleteTime(DEFAULT_POOL_ITEM_DELETE_TIME, assetDeleteTime));
        }

        public bool TryGetDeleteTime<T>(out DeleteTime deleteTime) where T : class, new()
        {
            return mDeleteTimes.TryGetValue(typeof(T), out deleteTime);
        }

        private void AddDeleteTime(Type poolKey, DeleteTime deleteTime)
        {
            mDeleteTimes.Add(poolKey, deleteTime);
            if (mPoolItems.TryGetValue(poolKey, out var poolItemBase))
                poolItemBase.SetDeleteTime(deleteTime);
        }
        #endregion
        
        public T Get<T>() where T : class, new()
        {
            return GetPoolItem<T>().Get();
        }
        
        public void Dispose<T>(T obj) where T : class, new()
        {
            if(obj == null) return;
            GetPoolItem<T>().Dispose(obj);
        }
        
        #region Clear
        public void Clear<T>() where T : class, new()
        {
            ClearInternal(typeof(T));
        }
        
        public void Clear(Type type)
        {
            if(!type.IsClass) return;
            ClearInternal(type);
        }

        private void ClearInternal(Type type)
        {
            if (mPoolItems.TryGetValue(type, out var poolItem))
            {
                mPoolKeys.Remove(type);
                poolItem.Clear();
                mPoolItems.Remove(type);
            }
        }
        
        public void ClearAll()
        {
            mPoolKeys.Clear();
            mToDeletePoolKeys.Clear();
            foreach (var poolItem in mPoolItems.Values)
                poolItem.Clear();
            mPoolItems.Clear();
        }
        #endregion
        
        public int GetItemCount<T>() where T : class, new()
        {
            return mPoolItems.TryGetValue(typeof(T), out var poolItemBase) ? poolItemBase.ItemCount : 0;
        }
        
        public void Resize<T>(int size) where T : class, new()
        {
            GetPoolItem<T>().Resize(size);
        }
        
        private void Update()
        {
            if (mPoolKeys.Count > 0)
            {
                foreach (var key in mPoolKeys)
                {
                    if (!mPoolItems[key].Update())
                    {
                        mPoolItems.Remove(key);
                        mToDeletePoolKeys.Add(key);
                    }
                }
            }

            if (mToDeletePoolKeys.Count > 0)
            {
                foreach (var key in mToDeletePoolKeys)
                    mPoolKeys.Remove(key);
                mToDeletePoolKeys.Clear();
            }
        }

        private CommonPoolItem<T> GetPoolItem<T>() where T : class, new()
        {
            CommonPoolItem<T> poolItem;
            if (mPoolItems.TryGetValue(typeof(T), out var poolItemBase))
                poolItem = poolItemBase as CommonPoolItem<T>;
            else
                poolItem = CreatePoolItem<T>();
            return poolItem;
        }
        
        private CommonPoolItem<T> CreatePoolItem<T>() where T : class, new()
        {
            var poolKey = typeof(T);
            if (!mDeleteTimes.TryGetValue(poolKey, out var deleteTime))
                deleteTime = mDefaultDeleteTime;
            var poolItem = new CommonPoolItem<T>(deleteTime);
            mPoolKeys.Add(poolKey);
            mPoolItems.Add(poolKey, poolItem);
            return poolItem;
        }
    }
}