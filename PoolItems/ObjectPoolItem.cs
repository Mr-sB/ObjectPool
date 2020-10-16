using System.Collections.Generic;
using UnityEngine;

namespace GameUtil
{
    public class ObjectPoolItem<T> : PoolItemBase where T : Object
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
        private readonly ObjectPool.LoadMode mLoadMode;
        private readonly string mBundleName;
        private readonly string mAssetName;
        private readonly T m_ObjRes;//原始资源
        private readonly bool mIsGameObject;
        private readonly HashSet<int> mItemIDs;
        //链表，方便增删
        private readonly LinkedList<Item> mItems;
        private readonly List<ISpawnHandler> mSpawnHandlers;
        private readonly List<IDisposeHandler> mDisposeHandlers;
        public override int ItemCount => mItems.Count;

        public ObjectPoolItem(ObjectPool.LoadMode loadMode, string bundleName, string assetName, DeleteTime deleteTime) : base(deleteTime)
        {
            mLoadMode = loadMode;
            mBundleName = bundleName;
            mAssetName = assetName;
            mIsGameObject = typeof(T) == typeof(GameObject);
            mItemIDs = new HashSet<int>();
            mItems = new LinkedList<Item>();
            switch (mLoadMode)
            {
                case ObjectPool.LoadMode.Resource:
                    m_ObjRes = Resources.Load<T>(assetName);
                    break;
                // TODO: Add yourself AssetBundle load method
                // case ObjectPool.LoadMode.AssetBundle:
                //     m_ObjRes = AssetBundleManager.GetAsset<T>(bundleName, assetName);
                //     break;
                // MARK: Add yourself load methods
            }
            if (mIsGameObject)
            {
                mSpawnHandlers = new List<ISpawnHandler>();
                mDisposeHandlers = new List<IDisposeHandler>();
#if !UNITY_EDITOR
                //直接添加在预制体上
                if (m_ObjRes && m_ObjRes is GameObject go)
                {
                    var itemKey = go.GetComponent<ObjectPoolItemKey>();
                    if(!itemKey)
                        itemKey = go.AddComponent<ObjectPoolItemKey>();
                    itemKey.Init(mLoadMode, mBundleName, mAssetName);
                }
#endif
            }
            if (!m_ObjRes)
                Debug.LogError("ObjectItem load asset is null! Type: " + typeof(T) + ", LoadMode: " + loadMode + ", BundleName: " + bundleName + ", AssetName: " + assetName);
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

        public void Resize(int size, Transform parent)
        {
            if (size <= 0)
            {
                Clear();
                return;
            }
            int difference = size - mItems.Count;
            if(difference == 0) return;
            if (difference > 0)
            {
                //Add
                for (int i = 0; i < difference; i++)
                {
                    var obj = Spawn(parent, false);
                    //GameObject类型需要多做一些处理
                    if (mIsGameObject && obj is GameObject go)
                    {
                        go.SetActive(false);
                    }

                    int id = obj.GetInstanceID();
                    mItems.AddLast(new Item(obj, id, Time.realtimeSinceStartup));
                    mItemIDs.Add(id);
                }
            }
            else
            {
                //Reduce 最前面的为最先删除的
                difference = -difference;
                for (int i = 0; i < difference; i++)
                {
                    var obj = mItems.First.Value.Object;
                    if (obj)
                        mItemIDs.Remove(obj.GetInstanceID());
                    mItems.RemoveFirst();
                }
            }
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
            //先设置parent再显示
            go.transform.SetParent(null);
            go.SetActive(true);
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

        private T Spawn(Transform parent = null, bool callInterface = true)
        {
            if (!m_ObjRes) return null;
            T obj;
            if(mIsGameObject && parent)
                obj = Object.Instantiate(m_ObjRes, parent);
            else
                obj = Object.Instantiate(m_ObjRes);
            if (!mIsGameObject || !(obj is GameObject go)) return obj;
            
            //GameObject类型需要多做一些处理
#if UNITY_EDITOR
            //添加在实例对象上
            var itemKey = go.GetComponent<ObjectPoolItemKey>();
            if(!itemKey)
                itemKey = go.AddComponent<ObjectPoolItemKey>();
            itemKey.Init(mLoadMode, mBundleName, mAssetName);
#endif
            if(callInterface)
                OnGameObjectSpawn(go);
            return obj;
        }

        private void OnGameObjectSpawn(GameObject go)
        {
            List<ISpawnHandler> spawnHandlers;
            bool needDispose;
            if (mSpawnHandlers != null && mSpawnHandlers.Count == 0)
            {
                spawnHandlers = mSpawnHandlers;
                needDispose = false;
            }
            else
            {
                spawnHandlers = CommonPool.Instance.Get<List<ISpawnHandler>>();
                spawnHandlers.Clear();
                needDispose = true;
            }
            go.GetComponentsInChildren(spawnHandlers);
            if (spawnHandlers.Count > 0)
            {
                //Count - 1
                for (int i = 0, count = spawnHandlers.Count - 1; i < count; i++)
                    spawnHandlers[i].OnSpawn();
                //单独触发最后一个接口，提前Clear Handlers
                var lastHandler = spawnHandlers[spawnHandlers.Count - 1];
                spawnHandlers.Clear();
                if (needDispose)
                    CommonPool.Instance.Dispose(spawnHandlers);
                lastHandler.OnSpawn();
            }
            else if (needDispose)
                CommonPool.Instance.Dispose(spawnHandlers);
        }

        private void OnGameObjectDispose(GameObject go)
        {
            List<IDisposeHandler> disposeHandlers;
            bool needDispose;
            if (mDisposeHandlers != null && mDisposeHandlers.Count == 0)
            {
                disposeHandlers = mDisposeHandlers;
                needDispose = false;
            }
            else
            {
                disposeHandlers = CommonPool.Instance.Get<List<IDisposeHandler>>();
                disposeHandlers.Clear();
                needDispose = true;
            }
            go.GetComponentsInChildren(disposeHandlers);
            if (disposeHandlers.Count > 0)
            {
                //Count - 1
                for (int i = 0, count = disposeHandlers.Count - 1; i < count; i++)
                    disposeHandlers[i].OnDispose();
                //单独触发最后一个接口，提前Clear Handlers
                var lastHandler = disposeHandlers[disposeHandlers.Count - 1];
                disposeHandlers.Clear();
                if (needDispose)
                    CommonPool.Instance.Dispose(disposeHandlers);
                lastHandler.OnDispose();
            }
            else if (needDispose)
                CommonPool.Instance.Dispose(disposeHandlers);
        }
    }
}
