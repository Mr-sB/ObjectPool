using System;
using System.Collections.Generic;
using UnityEngine;

namespace GameUtil
{
    public class CommonPoolItem<T> : PoolItemBase where T : class, new()
    {
        public struct Item
        {
            public readonly T Object;
            public readonly float Time;

            public Item(T obj, float time)
            {
                Object = obj;
                Time = time;
            }
        }
        
        private static Action<T> mSpawnHandler;
        private static Action<T> mDisposeHandler;
        
        //链表，方便增删
        private readonly LinkedList<Item> mItems;
        public override int ItemCount => mItems.Count;

        public CommonPoolItem(DeleteTime deleteTime) : base(deleteTime)
        {
            mItems = new LinkedList<Item>();
        }
        
        public static void SetHandlers(Action<T> spawnHandler, Action<T> disposeHandler)
        {
            SetSpawnHandler(spawnHandler);
            SetDisposeHandler(disposeHandler);
        }
        
        public static void SetSpawnHandler(Action<T> handler)
        {
            mSpawnHandler = handler;
        }
        
        public static void SetDisposeHandler(Action<T> handler)
        {
            mDisposeHandler = handler;
        }
        
        public override void Clear()
        {
            mItems.Clear();
        }

        public override void Resize(int size, Transform parent)
        {
            Resize(size);
        }
        
        public void Resize(int size)
        {
            if (size <= 0)
            {
                Clear();
                return;
            }
            int difference = size - mItems.Count;
            if (difference == 0) return;
            if (difference > 0)
            {
                //Add
                for (int i = 0; i < difference; i++)
                    Dispose(Spawn());
            }
            else
            {
                //Reduce 最前面的为最先删除的
                difference = -difference;
                for (int i = 0; i < difference; i++)
                    mItems.RemoveFirst();
            }
        }

        public override object Get()
        {
            return GetT();
        }
        
        public T GetT()
        {
            mNullTime = Time.realtimeSinceStartup;
            T obj = null;
            if (mItems.Count <= 0)
                return Spawn();
            //倒序遍历，手动维护一个栈(Stack)，先进后出，保证前面的对象尽量不被使用，方便删除这些资源
            var itemNode = mItems.Last;
            while (obj == null && itemNode != null)
            {
                var item = itemNode.Value;
                obj = item.Object;
                var toRemove = itemNode;
                itemNode = itemNode.Previous;
                mItems.Remove(toRemove);
            }
            return obj ?? Spawn();
        }

        public void Dispose(T obj)
        {
            if (obj == null) return;
            try
            {
                mDisposeHandler?.Invoke(obj);
            }
            catch (Exception e)
            {
                Debug.LogError(e);
            }
            mItems.AddLast(new Item(obj, Time.realtimeSinceStartup));
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
                        if (Time.realtimeSinceStartup - item.Time < mDeleteTime.AssetDeleteTime)
                            break;
                        //需要被删除的资源
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

        private static T Spawn()
        {
            var obj = new T();
            try
            {
                mSpawnHandler?.Invoke(obj);
                return obj;
            }
            catch (Exception e)
            {
                Debug.LogError(e);
            }
            return obj;
        }
    }
}