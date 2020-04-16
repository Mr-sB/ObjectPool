using System.Collections.Generic;
using UnityEngine;

namespace GameUtil
{
    public abstract class PoolItemBase
    {
        protected readonly ObjectPool.DeleteTime mDeleteTime;
        protected readonly string mAssetPath;
        protected readonly ObjectPool.LoadMode mLoadMode;
        protected readonly HashSet<int> mItemIDs;
        protected float mNullTime;

        public PoolItemBase(string assetPath, ObjectPool.DeleteTime deleteTime, ObjectPool.LoadMode loadMode)
        {
            mAssetPath = assetPath;
            mDeleteTime = deleteTime;
            mLoadMode = loadMode;
            mItemIDs = new HashSet<int>();
        }
        
        public abstract bool Update();
        public abstract void Clear();
    }
    
    public class PoolItem<T> : PoolItemBase where T : Object
    {
        public struct Item
        {
            public readonly T Object;
            public readonly int InstanceID;
            public readonly float Time;

            public Item(T obj, float time)
            {
                Object = obj;
                InstanceID = obj.GetInstanceID();
                Time = time;
            }
            
            public Item(T obj, int instanceID, float time)
            {
                Object = obj;
                InstanceID = instanceID;
                Time = time;
            }
        }

        private readonly T m_ObjRes;//原始资源
        private readonly bool mIsGameObject;
        //链表，方便增删
        private readonly LinkedList<Item> mItems;
        private readonly List<ISpawnHandler> mSpawnHandlers;
        private readonly List<IDisposeHandler> mDisposeHandlers;

        public PoolItem(string assetPath, ObjectPool.DeleteTime deleteTime, ObjectPool.LoadMode loadMode) : base(assetPath, deleteTime, loadMode)
        {
            mIsGameObject = typeof(T) == typeof(GameObject);
            mItems = new LinkedList<Item>();
            switch (mLoadMode)
            {
                case ObjectPool.LoadMode.Resource:
                    m_ObjRes = Resources.Load<T>(assetPath);
                    break;
                // TODO: Add yourself load methods
                // case ObjectPool.LoadMode.CommonAssetBundle:
                //     m_ObjRes = AssetBundleManager.Instance.GetCommonAsset<T>(assetPath);
                //     break;
                // case ObjectPool.LoadMode.SceneAssetBundle:
                //     m_ObjRes = AssetBundleManager.Instance.GetSceneAsset<T>(assetPath);
                //     break;
            }

            if (mIsGameObject)
            {
                mSpawnHandlers = new List<ISpawnHandler>();
                mDisposeHandlers = new List<IDisposeHandler>();
            }
        }

        public override void Clear()
        {
            if (mIsGameObject)
            {
                mSpawnHandlers.Clear();
                mDisposeHandlers.Clear();
            }

            foreach (var item in mItems)
                if(item.Object)
                    Object.Destroy(item.Object);
            mItems.Clear();
            mItemIDs.Clear();
        }
        
        public T Get()
        {
            mNullTime = Time.realtimeSinceStartup;
            T obj = null;
            if (mItems.Count <= 0)
                return Spawn();
            //倒序遍历，手动维护一个栈(Stack)，先进后出，保证前面的对象尽量不被使用，方便删除这些资源
            var itemNode = mItems.Last;
            while (!obj && itemNode != null)
            {
                var item = itemNode.Value;
                obj = item.Object;
                mItemIDs.Remove(item.InstanceID);
                var toRemove = itemNode;
                itemNode = itemNode.Previous;
                mItems.Remove(toRemove);
            }
            if (obj == null)
                return Spawn();

            if (!mIsGameObject || !(obj is GameObject go)) return obj;
            //GameObject类型需要多做一些处理
            go.SetActive(true);
            go.transform.SetParent(null);
            OnGameObjectSpawn(go);
            return obj;
        }

        public void Dispose(T obj)
        {
            if(!obj) return;
            int id = obj.GetInstanceID();
            //防止重复放入对象池
            if(mItemIDs.Contains(id)) return;
            mItems.AddLast(new Item(obj, id, Time.realtimeSinceStartup));
            mItemIDs.Add(id);
            if (!mIsGameObject || !(obj is GameObject go)) return;
            
            //GameObject类型需要多做一些处理
            go.SetActive(false);
            OnGameObjectDispose(go);
        }

        public override bool Update()
        {
            //需要自动删除资源
            if (mDeleteTime.AssetDeleteTime >= 0)
            {
                if (mItems.Count > 0)
                {
                    //顺序遍历
                    var itemNode = mItems.First;
                    while (itemNode != null)
                    {
                        var item = itemNode.Value;
                        //没到自动删除的时间，后面的也无需遍历了
                        if(Time.realtimeSinceStartup - item.Time < mDeleteTime.AssetDeleteTime)
                            break;
                        //需要被删除的资源
                        if(item.Object)
                            Object.Destroy(item.Object);
                        mItemIDs.Remove(item.InstanceID);
                        var toRemove = itemNode;
                        itemNode = itemNode.Next;
                        mItems.Remove(toRemove);
                    }
                    
                    if (mItems.Count <= 0)
                        mNullTime = Time.realtimeSinceStartup;
                }
            }
            //需要自动删除PoolItem
            if (mDeleteTime.PoolItemDeleteTime >= 0 && mItems.Count <= 0)
                return Time.realtimeSinceStartup - mNullTime < mDeleteTime.PoolItemDeleteTime;
            return true;
        }

        private T Spawn()
        {
            if (!mIsGameObject) return Object.Instantiate(m_ObjRes);
            
            //GameObject类型需要多做一些处理
            var obj = Object.Instantiate(m_ObjRes, ObjectPool.Instance.transform);
            //转换失败
            if (!(obj is GameObject go)) return obj;
            var dispose = go.GetComponent<DisposeSelf>();
            if(!dispose)
                dispose = go.AddComponent<DisposeSelf>();
            dispose.Init(mAssetPath, mLoadMode);
            OnGameObjectSpawn(go);
            return obj;
        }

        private void OnGameObjectSpawn(GameObject go)
        {
            go.GetComponentsInChildren(mSpawnHandlers);
            if (mSpawnHandlers != null && mSpawnHandlers.Count > 0)
            {
                for (int i = 0, count = mSpawnHandlers.Count; i < count; i++)
                    mSpawnHandlers[i].OnSpawn();
                mSpawnHandlers.Clear();
            }
        }

        private void OnGameObjectDispose(GameObject go)
        {
            go.GetComponentsInChildren(mDisposeHandlers);
            if (mDisposeHandlers != null && mDisposeHandlers.Count > 0)
            {
                for (int i = 0, count = mDisposeHandlers.Count; i < count; i++)
                    mDisposeHandlers[i].OnDispose();
                mDisposeHandlers.Clear();
            }
        }
    }
}