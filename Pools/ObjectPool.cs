using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Object = UnityEngine.Object;

namespace GameUtil
{
    public class ObjectPool : ObjectPoolBase<ObjectPool>
    {
        #region PoolKey
        //实现IEquatable<T>接口，避免在比较时装箱拆箱，产生GC
        private struct PoolKey : IEquatable<PoolKey>
        {
            public readonly Type PoolType;
            public readonly LoadMode LoadMode;
            public readonly string BundleName;
            public readonly string AssetName;
            
            public PoolKey(Type poolType, LoadMode loadMode, string bundleName, string assetName)
            {
                PoolType = poolType;
                LoadMode = loadMode;
                BundleName = LoadMode == LoadMode.AssetBundle ? bundleName : null;
                AssetName = assetName;
            }

            public bool Equals(PoolKey other)
            {
                return PoolType == other.PoolType && LoadMode == other.LoadMode && BundleName == other.BundleName && AssetName == other.AssetName;
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
                unchecked
                {
                    var hashCode = (PoolType != null ? PoolType.GetHashCode() : 1);
                    hashCode = (hashCode * 397) ^ (int) LoadMode;
                    hashCode = (hashCode * 397) ^ (BundleName != null ? BundleName.GetHashCode() : 1);
                    hashCode = (hashCode * 397) ^ (AssetName != null ? AssetName.GetHashCode() : 1);
                    return hashCode;
                }
            }
        }
        #endregion
        
        public enum LoadMode
        {
            Resource,
            Custom,
            AssetBundle,
            // MARK: Add yourself load modes
        }

        //对象池。type + path + load mode => PoolItem
        private readonly Dictionary<PoolKey, ObjectPoolItem> mPoolItems = new Dictionary<PoolKey, ObjectPoolItem>();
        private readonly List<PoolKey> mToDeletePoolKeys = new List<PoolKey>();
        //DeleteTime
        private readonly Dictionary<PoolKey, DeleteTime> mDeleteTimes = new Dictionary<PoolKey, DeleteTime>();
        private readonly DeleteTime mDefaultDeleteTime = new DeleteTime(DEFAULT_POOL_ITEM_DELETE_TIME, DEFAULT_ASSET_DELETE_TIME);
        public static float DEFAULT_POOL_ITEM_DELETE_TIME = 120;
        public static float DEFAULT_ASSET_DELETE_TIME = 120;
        
        public const string DEFAULT_PREFAB_ROOT_PATH = "Prefabs/View/";
        public const string DEFAULT_GAME_PATH = "Prefabs/Game/";
        public const string DEFAULT_PARTICLE_PATH = "Prefabs/ParticleSystem/";
        public const string DEFAULT_MATERIAL_PATH = "Materials/";

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

        #region TypeValidate
        public static bool TypeValidate(Type assetType)
        {
            //IsSubclassOf会循环查找BaseType
            return assetType.IsSubclassOf(typeof(Object));
        }
        
        public static bool TypeValidateWithLog(Type assetType)
        {
            var success = TypeValidate(assetType);
            if (!success)
                Debug.LogErrorFormat("ObjectPool TypeValidate failed. Type: {0}", assetType);
            return success;
        }
        #endregion
        
        #region SetDeleteTime
        public void SetDeleteTime<T>(LoadMode loadMode, string bundleName, string assetName, float poolItemDeleteTime, float assetDeleteTime) where T : Object
        {
            SetDeleteTime(typeof(T), loadMode, bundleName, assetName, poolItemDeleteTime, assetDeleteTime);
        }
        
        public void SetPoolItemDeleteTime<T>(LoadMode loadMode, string bundleName, string assetName, float poolItemDeleteTime) where T : Object
        {
            SetPoolItemDeleteTime(typeof(T), loadMode, bundleName, assetName, poolItemDeleteTime);
        }

        public void SetAssetDeleteTime<T>(LoadMode loadMode, string bundleName, string assetName, float assetDeleteTime) where T : Object
        {
            SetAssetDeleteTime(typeof(T), loadMode, bundleName, assetName, assetDeleteTime);
        }

        public bool TryGetDeleteTime<T>(LoadMode loadMode, string bundleName, string assetName, out DeleteTime deleteTime) where T : Object
        {
            return TryGetDeleteTime(typeof(T), loadMode, bundleName, assetName, out deleteTime);
        }

        public void SetDeleteTime(Type assetType, LoadMode loadMode, string bundleName, string assetName, float poolItemDeleteTime, float assetDeleteTime)
        {
            if (!TypeValidateWithLog(assetType)) return;
            PoolKey poolKey = new PoolKey(assetType, loadMode, bundleName, assetName);
            if (mDeleteTimes.TryGetValue(poolKey, out var deleteTime))
            {
                deleteTime.PoolItemDeleteTime = poolItemDeleteTime;
                deleteTime.AssetDeleteTime = assetDeleteTime;
            }
            else
                AddDeleteTime(poolKey, new DeleteTime(poolItemDeleteTime, assetDeleteTime));
        }
        
        public void SetPoolItemDeleteTime(Type assetType, LoadMode loadMode, string bundleName, string assetName, float poolItemDeleteTime)
        {
            if (!TypeValidateWithLog(assetType)) return;
            PoolKey poolKey = new PoolKey(assetType, loadMode, bundleName, assetName);
            if (mDeleteTimes.TryGetValue(poolKey, out var deleteTime))
                deleteTime.PoolItemDeleteTime = poolItemDeleteTime;
            else
                AddDeleteTime(poolKey, new DeleteTime(poolItemDeleteTime, DEFAULT_ASSET_DELETE_TIME));
        }

        public void SetAssetDeleteTime(Type assetType, LoadMode loadMode, string bundleName, string assetName, float assetDeleteTime)
        {
            if (!TypeValidateWithLog(assetType)) return;
            PoolKey poolKey = new PoolKey(assetType, loadMode, bundleName, assetName);
            if (mDeleteTimes.TryGetValue(poolKey, out var deleteTime))
                deleteTime.AssetDeleteTime = assetDeleteTime;
            else
                AddDeleteTime(poolKey, new DeleteTime(DEFAULT_POOL_ITEM_DELETE_TIME, assetDeleteTime));
        }

        public bool TryGetDeleteTime(Type assetType, LoadMode loadMode, string bundleName, string assetName, out DeleteTime deleteTime)
        {
            if (!TypeValidateWithLog(assetType))
            {
                deleteTime = null;
                return false;
            }
            return mDeleteTimes.TryGetValue(new PoolKey(assetType, loadMode, bundleName, assetName), out deleteTime);
        }
        
        private void AddDeleteTime(PoolKey poolKey, DeleteTime deleteTime)
        {
            mDeleteTimes.Add(poolKey, deleteTime);
            if (mPoolItems.TryGetValue(poolKey, out var poolItem))
                poolItem.SetDeleteTime(deleteTime);
        }
        #endregion

        #region CustomRegister
        public void RegisterCustomPoolItem<T>(string assetPath, T originAsset) where T : Object
        {
            RegisterCustomPoolItem(typeof(T), assetPath, originAsset);
        }
        
        public void RegisterCustomPoolItem(Type assetType, string assetPath, Object originAsset)
        {
            if (!originAsset) return;
            var poolKey = new PoolKey(assetType, LoadMode.Custom, null, assetPath);
            bool createNewPoolItem = false;
            if (mPoolItems.TryGetValue(poolKey, out var poolItem))
            {
                if (poolItem.OriginAsset != originAsset)
                {
                    poolItem.Clear();
                    poolItem.SetOriginAsset(originAsset);
                }
            }
            else
                createNewPoolItem = true;

            if (createNewPoolItem)
            {
                poolItem = CreatePoolItem(assetType, LoadMode.Custom, null, assetPath);
                poolItem.SetOriginAsset(originAsset);
            }
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
            return GetPoolItem<T>(LoadMode.Resource, null, assetPath)?.GetObj() as T;
        }

        public T GetCustom<T>(string assetPath) where T : Object
        {
            return GetPoolItem<T>(LoadMode.Custom, null, assetPath)?.GetObj() as T;
        }
        
        public T GetAssetBundle<T>(string bundleName, string assetName) where T : Object
        {
            return GetPoolItem<T>(LoadMode.AssetBundle, bundleName, assetName)?.GetObj() as T;
        }
        // MARK: Add yourself load methods

        public T Get<T>(LoadMode loadMode, string bundleName, string assetName) where T : Object
        {
            return GetPoolItem<T>(loadMode, bundleName, assetName)?.GetObj() as T;
        }
        
        public Object GetResources(Type assetType, string assetPath)
        {
            if (!TypeValidateWithLog(assetType)) return null;
            return GetPoolItem(assetType, LoadMode.Resource, null, assetPath)?.GetObj();
        }
        
        public Object GetCustom(Type assetType, string assetPath)
        {
            if (!TypeValidateWithLog(assetType)) return null;
            return GetPoolItem(assetType, LoadMode.Custom, null, assetPath)?.GetObj();
        }
        
        public Object GetAssetBundle(Type assetType, string bundleName, string assetName)
        {
            if (!TypeValidateWithLog(assetType)) return null;
            return GetPoolItem(assetType, LoadMode.AssetBundle, bundleName, assetName)?.GetObj();
        }
        // MARK: Add yourself load methods

        public Object Get(Type assetType, LoadMode loadMode, string bundleName, string assetName)
        {
            if (!TypeValidateWithLog(assetType)) return null;
            return GetPoolItem(assetType, loadMode, bundleName, assetName)?.GetObj();
        }
        #endregion

        #region Dispose
        public void Dispose<T>(T obj, LoadMode loadMode, string bundleName, string assetName) where T : Object
        {
#if UNITY_EDITOR
            if (_onApplicationQuit) return;
#endif
            if (!obj) return;
            //如果是GameObject，做特殊处理
            if (obj is GameObject go)
                DisposeGameObject(go);
            else
            {
                var poolItem = GetPoolItem<T>(loadMode, bundleName, assetName);
                if (poolItem != null)
                    poolItem.Dispose(obj);
                else
                    Destroy(obj);
            }
        }
        
        public void Dispose(Type assetType, Object obj, LoadMode loadMode, string bundleName, string assetName)
        {
#if UNITY_EDITOR
            if (_onApplicationQuit) return;
#endif
            if (!obj) return;
            if (!TypeValidateWithLog(assetType)) return;
            //如果是GameObject，做特殊处理
            if (obj is GameObject go)
                DisposeGameObject(go);
            else
            {
                var poolItem = GetPoolItem(assetType, loadMode, bundleName, assetName);
                if (poolItem != null)
                    poolItem.Dispose(obj);
                else
                    Destroy(obj);
            }
        }
        
        public void DisposeGameObject(GameObject go)
        {
#if UNITY_EDITOR
            if (_onApplicationQuit) return;
#endif
            if (!go) return;
            var dispose = go.GetComponent<ObjectPoolItemKey>();
            if (!dispose)
            {
                Destroy(go);
                return;
            }
            //先隐藏再设置parent
            go.SetActive(false);
            go.transform.SetParent(transform);
            
            var poolItem = GetPoolItem<GameObject>(dispose.LoadMode, dispose.BundleName, dispose.AssetName);
            if (poolItem != null)
                poolItem.Dispose(go);
            else
                Destroy(go);
        }
        #endregion

        #region Clear
        public void Clear<T>(LoadMode loadMode, string bundleName, string assetName) where T : Object
        {
            Clear(typeof(T), loadMode, bundleName, assetName);
        }
        
        public void Clear(Type assetType, LoadMode loadMode, string bundleName, string assetName)
        {
            if (!TypeValidateWithLog(assetType)) return;
            PoolKey poolKey = new PoolKey(assetType, loadMode, bundleName, assetName);
            if (mPoolItems.TryGetValue(poolKey, out var poolItem))
            {
                poolItem.Clear();
                mPoolItems.Remove(poolKey);
            }
        }
        
        public void ClearAll()
        {
            mToDeletePoolKeys.Clear();
            foreach (var poolItem in mPoolItems.Values)
                poolItem.Clear();
            mPoolItems.Clear();
        }
        #endregion

        public int GetItemCount<T>(LoadMode loadMode, string bundleName, string assetName) where T : Object
        {
            return GetItemCount(typeof(T), loadMode, bundleName, assetName);
        }
        
        public int GetItemCount(Type assetType, LoadMode loadMode, string bundleName, string assetName)
        {
            if (!TypeValidateWithLog(assetType)) return 0;
            return mPoolItems.TryGetValue(new PoolKey(assetType, loadMode, bundleName, assetName), out var poolItem) ? poolItem.ItemCount : 0;
        }

        public void Resize<T>(LoadMode loadMode, string bundleName, string assetName, int size) where T : Object
        {
            GetPoolItem<T>(loadMode, bundleName, assetName)?.Resize(size, transform);
        }
        
        public void Resize(Type assetType, LoadMode loadMode, string bundleName, string assetName, int size)
        {
            if (!TypeValidateWithLog(assetType)) return;
            GetPoolItem(assetType, loadMode, bundleName, assetName)?.Resize(size, transform);
        }

        private void Update()
        {
            if (mPoolItems.Count > 0)
            {
                foreach (var pair in mPoolItems)
                {
                    if (!pair.Value.Update())
                        mToDeletePoolKeys.Add(pair.Key);
                }
            }

            if (mToDeletePoolKeys.Count > 0)
            {
                foreach (var key in mToDeletePoolKeys)
                    mPoolItems.Remove(key);
                mToDeletePoolKeys.Clear();
            }
        }

        private ObjectPoolItem GetPoolItem<T>(LoadMode loadMode, string bundleName, string assetName) where T : Object
        {
            return GetPoolItem(typeof(T), loadMode, bundleName, assetName);
        }
        
        private ObjectPoolItem CreatePoolItem<T>(LoadMode loadMode, string bundleName, string assetName) where T : Object
        {
            return CreatePoolItem(typeof(T), loadMode, bundleName, assetName);
        }
        
        private ObjectPoolItem GetPoolItem(Type assetType, LoadMode loadMode, string bundleName, string assetName)
        {
            if (mPoolItems.TryGetValue(new PoolKey(assetType, loadMode, bundleName, assetName), out var poolItem))
                return poolItem;
            if (loadMode != LoadMode.Custom)
                poolItem = CreatePoolItem(assetType, loadMode, bundleName, assetName);
            else
                Debug.LogError("Cannot create Custom PoolItem automatically.");
            return poolItem;
        }
        
        private ObjectPoolItem CreatePoolItem(Type assetType, LoadMode loadMode, string bundleName, string assetName)
        {
            PoolKey poolKey = new PoolKey(assetType, loadMode, bundleName, assetName);
            if (!mDeleteTimes.TryGetValue(poolKey, out var deleteTime))
                deleteTime = mDefaultDeleteTime;
            var poolItem = new ObjectPoolItem(assetType, loadMode, bundleName, assetName, deleteTime);
            mPoolItems.Add(poolKey, poolItem);
            return poolItem;
        }
    }
}