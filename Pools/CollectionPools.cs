using System.Collections.Generic;

namespace GameUtil
{
    public static class ListPool<T>
    {
        static ListPool()
        {
            CommonPoolItem<List<T>>.SetDisposeHandler(list => list.Clear());
        }
        
        public static List<T> Get()
        {
            return CommonPool.Instance.Get<List<T>>();
        }
        
        public static void Dispose(List<T> obj)
        {
            CommonPool.Instance.Dispose(obj);
        }
        
        public static void Clear()
        {
            CommonPool.Instance.Clear<List<T>>();
        }
        
        public static int GetItemCount()
        {
            return CommonPool.Instance.GetItemCount<List<T>>();
        }
        
        public static void Resize(int size)
        {
            CommonPool.Instance.Resize<List<T>>(size);
        }
        
        #region SetDeleteTime
        public static void SetDeleteTime(float poolItemDeleteTime, float assetDeleteTime)
        {
            CommonPool.Instance.SetDeleteTime<List<T>>(poolItemDeleteTime, assetDeleteTime);
        }
        
        public static void SetPoolItemDeleteTime(float poolItemDeleteTime)
        {
            CommonPool.Instance.SetPoolItemDeleteTime<List<T>>(poolItemDeleteTime);
        }

        public static void SetAssetDeleteTime(float assetDeleteTime)
        {
            CommonPool.Instance.SetAssetDeleteTime<List<T>>(assetDeleteTime);
        }

        public static bool TryGetDeleteTime(out DeleteTime deleteTime)
        {
            return CommonPool.Instance.TryGetDeleteTime<List<T>>(out deleteTime);
        }
        #endregion
    }
    
    public static class QueuePool<T>
    {
        static QueuePool()
        {
            CommonPoolItem<Queue<T>>.SetDisposeHandler(queue => queue.Clear());
        }
        
        public static Queue<T> Get()
        {
            return CommonPool.Instance.Get<Queue<T>>();
        }
        
        public static void Dispose(Queue<T> obj)
        {
            CommonPool.Instance.Dispose(obj);
        }
        
        public static void Clear()
        {
            CommonPool.Instance.Clear<Queue<T>>();
        }
        
        public static int GetItemCount()
        {
            return CommonPool.Instance.GetItemCount<Queue<T>>();
        }
        
        public static void Resize(int size)
        {
            CommonPool.Instance.Resize<Queue<T>>(size);
        }
        
        #region SetDeleteTime
        public static void SetDeleteTime(float poolItemDeleteTime, float assetDeleteTime)
        {
            CommonPool.Instance.SetDeleteTime<Queue<T>>(poolItemDeleteTime, assetDeleteTime);
        }
        
        public static void SetPoolItemDeleteTime(float poolItemDeleteTime)
        {
            CommonPool.Instance.SetPoolItemDeleteTime<Queue<T>>(poolItemDeleteTime);
        }

        public static void SetAssetDeleteTime(float assetDeleteTime)
        {
            CommonPool.Instance.SetAssetDeleteTime<Queue<T>>(assetDeleteTime);
        }

        public static bool TryGetDeleteTime(out DeleteTime deleteTime)
        {
            return CommonPool.Instance.TryGetDeleteTime<Queue<T>>(out deleteTime);
        }
        #endregion
    }
    
    public static class StackPool<T>
    {
        static StackPool()
        {
            CommonPoolItem<Stack<T>>.SetDisposeHandler(stack => stack.Clear());
        }
        
        public static Stack<T> Get()
        {
            return CommonPool.Instance.Get<Stack<T>>();
        }
        
        public static void Dispose(Stack<T> obj)
        {
            CommonPool.Instance.Dispose(obj);
        }
        
        public static void Clear()
        {
            CommonPool.Instance.Clear<Stack<T>>();
        }
        
        public static int GetItemCount()
        {
            return CommonPool.Instance.GetItemCount<Stack<T>>();
        }
        
        public static void Resize(int size)
        {
            CommonPool.Instance.Resize<Stack<T>>(size);
        }
        
        #region SetDeleteTime
        public static void SetDeleteTime(float poolItemDeleteTime, float assetDeleteTime)
        {
            CommonPool.Instance.SetDeleteTime<Stack<T>>(poolItemDeleteTime, assetDeleteTime);
        }
        
        public static void SetPoolItemDeleteTime(float poolItemDeleteTime)
        {
            CommonPool.Instance.SetPoolItemDeleteTime<Stack<T>>(poolItemDeleteTime);
        }

        public static void SetAssetDeleteTime(float assetDeleteTime)
        {
            CommonPool.Instance.SetAssetDeleteTime<Stack<T>>(assetDeleteTime);
        }

        public static bool TryGetDeleteTime(out DeleteTime deleteTime)
        {
            return CommonPool.Instance.TryGetDeleteTime<Stack<T>>(out deleteTime);
        }
        #endregion
    }

    public static class HashSetPool<T>
    {
        static HashSetPool()
        {
            CommonPoolItem<HashSet<T>>.SetDisposeHandler(hashSet => hashSet.Clear());
        }
        
        public static HashSet<T> Get()
        {
            return CommonPool.Instance.Get<HashSet<T>>();
        }
        
        public static void Dispose(HashSet<T> obj)
        {
            CommonPool.Instance.Dispose(obj);
        }
        
        public static void Clear()
        {
            CommonPool.Instance.Clear<HashSet<T>>();
        }
        
        public static int GetItemCount()
        {
            return CommonPool.Instance.GetItemCount<HashSet<T>>();
        }
        
        public static void Resize(int size)
        {
            CommonPool.Instance.Resize<HashSet<T>>(size);
        }
        
        #region SetDeleteTime
        public static void SetDeleteTime(float poolItemDeleteTime, float assetDeleteTime)
        {
            CommonPool.Instance.SetDeleteTime<HashSet<T>>(poolItemDeleteTime, assetDeleteTime);
        }
        
        public static void SetPoolItemDeleteTime(float poolItemDeleteTime)
        {
            CommonPool.Instance.SetPoolItemDeleteTime<HashSet<T>>(poolItemDeleteTime);
        }

        public static void SetAssetDeleteTime(float assetDeleteTime)
        {
            CommonPool.Instance.SetAssetDeleteTime<HashSet<T>>(assetDeleteTime);
        }

        public static bool TryGetDeleteTime(out DeleteTime deleteTime)
        {
            return CommonPool.Instance.TryGetDeleteTime<HashSet<T>>(out deleteTime);
        }
        #endregion
    }

    public static class DictionaryPool<TKey, TValue>
    {
        static DictionaryPool()
        {
            CommonPoolItem<Dictionary<TKey, TValue>>.SetDisposeHandler(dictionary => dictionary.Clear());
        }
        
        public static Dictionary<TKey, TValue> Get()
        {
            return CommonPool.Instance.Get<Dictionary<TKey, TValue>>();
        }
        
        public static void Dispose(Dictionary<TKey, TValue> obj)
        {
            CommonPool.Instance.Dispose(obj);
        }
        
        public static void Clear()
        {
            CommonPool.Instance.Clear<Dictionary<TKey, TValue>>();
        }
        
        public static int GetItemCount()
        {
            return CommonPool.Instance.GetItemCount<Dictionary<TKey, TValue>>();
        }
        
        public static void Resize(int size)
        {
            CommonPool.Instance.Resize<Dictionary<TKey, TValue>>(size);
        }
        
        #region SetDeleteTime
        public static void SetDeleteTime(float poolItemDeleteTime, float assetDeleteTime)
        {
            CommonPool.Instance.SetDeleteTime<Dictionary<TKey, TValue>>(poolItemDeleteTime, assetDeleteTime);
        }
        
        public static void SetPoolItemDeleteTime(float poolItemDeleteTime)
        {
            CommonPool.Instance.SetPoolItemDeleteTime<Dictionary<TKey, TValue>>(poolItemDeleteTime);
        }

        public static void SetAssetDeleteTime(float assetDeleteTime)
        {
            CommonPool.Instance.SetAssetDeleteTime<Dictionary<TKey, TValue>>(assetDeleteTime);
        }

        public static bool TryGetDeleteTime(out DeleteTime deleteTime)
        {
            return CommonPool.Instance.TryGetDeleteTime<Dictionary<TKey, TValue>>(out deleteTime);
        }
        #endregion
    }
}