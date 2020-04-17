using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Object = UnityEngine.Object;

namespace GameUtil
{
    public class ObjectPool : MonoBehaviour
    {
        #region DeleteTime
        public const float DEFAULT_POOL_ITEM_DELETE_TIME = 30;
        public const float DEFAULT_ASSET_DELETE_TIME = 30;

        public class DeleteTime
        {
            public float PoolItemDeleteTime;
            public float AssetDeleteTime;

            public DeleteTime(float poolItemDeleteTime, float assetDeleteTime)
            {
                PoolItemDeleteTime = poolItemDeleteTime;
                AssetDeleteTime = assetDeleteTime;
            }
        }
        #endregion

        #region PoolKey
        //实现IEquatable<T>接口，避免在比较时装箱拆箱，产生GC
        private struct PoolKey : IEquatable<PoolKey>
        {
            public readonly Type PoolType;
            public readonly string AssetPath;
            public readonly LoadMode LoadMode;

            public PoolKey(Type poolType, string assetPath, LoadMode loadMode)
            {
                PoolType = poolType;
                AssetPath = assetPath;
                LoadMode = loadMode;
            }
            
            public bool Equals(PoolKey other)
            {
                return PoolType == other.PoolType && AssetPath == other.AssetPath;
            }
            
            public static bool operator ==(PoolKey lhs, PoolKey rhs)
            {
                return lhs.Equals(rhs);
            }
            
            public static bool operator !=(PoolKey lhs, PoolKey rhs)
            {
                return !(lhs == rhs);
            }
            
            public override bool Equals(object other)
            {
                return other is PoolKey other1 && Equals(other1);
            }
            
            public override int GetHashCode()
            {
                return PoolType.GetHashCode() ^ AssetPath.GetHashCode() << 2 ^ LoadMode.GetHashCode() >> 2;
            }
        }
        #endregion
        
        public enum LoadMode
        {
            Resource,
            // TODO: Add yourself load modes
            // CommonAssetBundle,
            // SceneAssetBundle,
        }

        //对象池。type + path + load mode => PoolItem
        private readonly Dictionary<PoolKey, PoolItemBase> mPoolItems = new Dictionary<PoolKey, PoolItemBase>();
        //HashSet方便删除
        private readonly HashSet<PoolKey> mPoolKeys = new HashSet<PoolKey>();
        private readonly List<PoolKey> mToDeletePoolKeys = new List<PoolKey>();
        private readonly Dictionary<PoolKey, DeleteTime> mDeleteTimes = new Dictionary<PoolKey, DeleteTime>();
        
        public const string DEFAULT_PREFAB_ROOT_PATH = "Prefabs/View/";
        public const string DEFAULT_GAME_PATH = "Prefabs/Game/";
        public const string DEFAULT_PARTICLE_PATH = "Prefabs/ParticleSystem/";
        public const string DEFAULT_MATERIAL_PATH = "Materials/";

        #region Instance
#if UNITY_EDITOR
        private static bool _onApplicationQuit;
#endif

        private static ObjectPool instance;
        public static ObjectPool Instance
        {
            get
            {
                //Aviod calling Instance in OnDestroy method to cause error when application quit
#if UNITY_EDITOR
                if (_onApplicationQuit)
                {
                    return new ObjectPool();
                }
#endif
                if (instance == null)
                {
                    //Find
                    instance = FindObjectOfType<ObjectPool>();
                    //Create
                    if (instance == null)
                    {
                        var go = new GameObject(nameof(ObjectPool));
                        instance = go.AddComponent<ObjectPool>();
                        DontDestroyOnLoad(go);
                    }
                }
                return instance;
            }
        }

        private void Awake()
        {
            if (instance == null)
                instance = this;
        }

#if UNITY_EDITOR
        private void OnApplicationQuit()
        {
            _onApplicationQuit = true;
        }
#endif
        #endregion

        #region GetPath
        public static string GetViewPath(string name, string path = null, bool needDefaultPath = true)
        {
            return GetFullPath(name, path, needDefaultPath ? DEFAULT_PREFAB_ROOT_PATH : null);
        }

        public static string GetGamePath(string name, string path = null, bool needDefaultPath = true)
        {
            return GetFullPath(name, path, needDefaultPath ? DEFAULT_GAME_PATH : null);
        }
        
        public static string GetParticlePath(string particleName, string path = null, bool needDefaultPath = true)
        {
            return GetFullPath(particleName, path, needDefaultPath ? DEFAULT_PARTICLE_PATH : null);
        }
        
        public static string GetMaterialPath(string materialName, string path = null, bool needDefaultPath = true)
        {
            return GetFullPath(materialName, path, needDefaultPath ? DEFAULT_MATERIAL_PATH : null);
        }
        
        public static string GetFullPath(string name, string path, string root)
        {
            string fullpath = null;
            if (root != null)
            {
                if (root.StartsWith("/"))
                    root = root.Remove(0, 1);
                fullpath = root;
            }
            if (path != null)
            {
                if (path.StartsWith("/"))
                    path = path.Remove(0, 1);
                fullpath = fullpath == null ? path : Path.Combine(fullpath, path);
            }
            if (name != null)
            {
                if (name.StartsWith("/"))
                    name = name.Remove(0, 1);
                fullpath = fullpath == null ? name : Path.Combine(fullpath, name);
            }
            return fullpath;
        }
        #endregion

        #region SetDeleteTime
        public void SetDeleteTime<T>(string assetPath, LoadMode loadMode, float poolItemDeleteTime, float assetDeleteTime) where T : Object
        {
            PoolKey poolKey = new PoolKey(typeof(T), assetPath, loadMode);
            if (mDeleteTimes.TryGetValue(poolKey, out var deleteTime))
            {
                deleteTime.PoolItemDeleteTime = poolItemDeleteTime;
                deleteTime.AssetDeleteTime = assetDeleteTime;
            }
            else
                mDeleteTimes.Add(poolKey, new DeleteTime(poolItemDeleteTime, assetDeleteTime));
        }
        
        public void SetPoolItemDeleteTime<T>(string assetPath, LoadMode loadMode, float poolItemDeleteTime) where T : Object
        {
            PoolKey poolKey = new PoolKey(typeof(T), assetPath, loadMode);
            if (mDeleteTimes.TryGetValue(poolKey, out var deleteTime))
                deleteTime.PoolItemDeleteTime = poolItemDeleteTime;
            else
                mDeleteTimes.Add(poolKey, new DeleteTime(poolItemDeleteTime, DEFAULT_ASSET_DELETE_TIME));
        }

        public void SetAssetDeleteTime<T>(string assetPath, LoadMode loadMode, float assetDeleteTime) where T : Object
        {
            PoolKey poolKey = new PoolKey(typeof(T), assetPath, loadMode);
            if (mDeleteTimes.TryGetValue(poolKey, out var deleteTime))
                deleteTime.AssetDeleteTime = assetDeleteTime;
            else
                mDeleteTimes.Add(poolKey, new DeleteTime(DEFAULT_POOL_ITEM_DELETE_TIME, assetDeleteTime));
        }

        public bool TryGetDeleteTime<T>(string assetPath, LoadMode loadMode, out DeleteTime deleteTime) where T : Object
        {
            PoolKey poolKey = new PoolKey(typeof(T), assetPath, loadMode);
            return mDeleteTimes.TryGetValue(poolKey, out deleteTime);
        }
        #endregion

        #region GetResorcesMode
        /// <summary>
        /// 获取Prefab实例对象 目录:Prefabs/View/
        /// </summary>
        public GameObject GetView(string viewName, string path = null, bool needDefaultPath = true)
        {
            return GetResources<GameObject>(GetViewPath(viewName, path, needDefaultPath));
        }

        /// <summary>
        /// 获取Prefab实例对象 目录:Prefabs/Game/
        /// </summary>
        public GameObject GetGame(string objName, string path = null, bool needDefaultPath = true)
        {
            return GetResources<GameObject>(GetGamePath(objName, path, needDefaultPath));
        }
        
        /// <summary>
        /// 获取Prefab实例对象 目录:Prefabs/ParticleSystem/
        /// </summary>
        public GameObject GetParticle(string particleName, string path = null, bool needDefaultPath = true)
        {
            return GetResources<GameObject>(GetParticlePath(particleName, path, needDefaultPath));
        }
        
        /// <summary>
        /// 获取Material实例对象 目录:Materials/
        /// </summary>
        public Material GetMaterial(string materialName, string path = null, bool needDefaultPath = true)
        {
            return GetResources<Material>(GetMaterialPath(materialName, path, needDefaultPath));
        }
        #endregion

        #region Get
        public T GetResources<T>(string assetPath) where T : Object
        {
            return GetPoolItem<T>(assetPath, LoadMode.Resource).Get();
        }
        
        // TODO: Add yourself load methods
        // public T GetCommonAssetBundle<T>(string assetPath) where T : Object
        // {
        //     return GetPoolItem<T>(assetPath, LoadMode.CommonAssetBundle).Get();
        // }
        //
        // public T GetSceneAssetBundle<T>(string assetPath) where T : Object
        // {
        //     return GetPoolItem<T>(assetPath, LoadMode.SceneAssetBundle).Get();
        // }

        public T Get<T>(string assetPath, LoadMode loadMode) where T : Object
        {
            return GetPoolItem<T>(assetPath, loadMode).Get();
        }
        #endregion

        #region Dispose
        public void Dispose<T>(T obj, string assetPath, LoadMode loadMode) where T : Object
        {
            if(!obj) return;
            //如果是GameObject，做特殊处理
            if (obj is GameObject go)
                DisposeGameObject(go);
            else
                GetPoolItem<T>(assetPath, loadMode).Dispose(obj);
        }
        
        public void DisposeGameObject(GameObject go)
        {
            if(!go) return;
            var dispose = go.GetComponent<DisposeSelf>();
            if (!dispose)
            {
                Destroy(go);
                return;
            }
            go.transform.SetParent(transform);
            
            GetPoolItem<GameObject>(dispose.AssetPath, dispose.LoadMode).Dispose(go);
        }
        #endregion

        #region Clear
        public void Clear<T>(string assetPath, LoadMode loadMode) where T : Object
        {
            ClearInternal(typeof(T), assetPath, loadMode);
        }
        
        public void Clear(Type type, string assetPath, LoadMode loadMode)
        {
            //IsSubclassOf会循环查找BaseType
            if(!type.IsSubclassOf(typeof(Object))) return;
            ClearInternal(type, assetPath, loadMode);
        }

        private void ClearInternal(Type type, string assetPath, LoadMode loadMode)
        {
            PoolKey poolKey = new PoolKey(type, assetPath, loadMode);
            if (mPoolItems.TryGetValue(poolKey, out var poolItem))
            {
                mPoolKeys.Remove(poolKey);
                poolItem.Clear();
                mPoolItems.Remove(poolKey);
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

        public void Update()
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

        private PoolItem<T> GetPoolItem<T>(string assetPath, LoadMode loadMode) where T : Object
        {
            PoolItem<T> poolItem;
            if (mPoolItems.TryGetValue(new PoolKey(typeof(T), assetPath, loadMode), out var poolItemBase))
                poolItem = poolItemBase as PoolItem<T>;
            else
                poolItem = CreatePoolItem<T>(assetPath, loadMode);
            return poolItem;
        }
        
        private PoolItem<T> CreatePoolItem<T>(string assetPath, LoadMode loadMode) where T : Object
        {
            PoolKey poolKey = new PoolKey(typeof(T), assetPath, loadMode);
            if (!mDeleteTimes.TryGetValue(poolKey, out var deleteTime))
            {
                deleteTime = new DeleteTime(DEFAULT_POOL_ITEM_DELETE_TIME, DEFAULT_ASSET_DELETE_TIME);
                mDeleteTimes.Add(poolKey, deleteTime);
            }
            var poolItem = new PoolItem<T>(assetPath, deleteTime, loadMode);
            mPoolKeys.Add(poolKey);
            mPoolItems.Add(poolKey, poolItem);
            return poolItem;
        }
    }
}