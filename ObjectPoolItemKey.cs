using System;
using UnityEngine;

namespace GameUtil
{
    [DisallowMultipleComponent]
    public class ObjectPoolItemKey : MonoBehaviour, ISpawnHandler, IDisposeHandler
    {
        [SerializeField] private ObjectPool.LoadMode mLoadMode;
        [SerializeField] private string mBundleName;
        [SerializeField] private string mAssetName;
        public ObjectPool.LoadMode LoadMode => mLoadMode;
        public string BundleName => mBundleName;
        public string AssetName => mAssetName;

        public event Action<ObjectPoolItemKey> SpawnEvent;
        public event Action<ObjectPoolItemKey> DisposeEvent;
        
        public void Init(ObjectPool.LoadMode loadMode, string bundleName, string assetName)
        {
            mLoadMode = loadMode;
            mBundleName = bundleName;
            mAssetName = assetName;
        }

        public void OnSpawn()
        {
            if (SpawnEvent == null) return;
            try
            {
                SpawnEvent(this);
            }
            catch (Exception e)
            {
                Debug.LogError(e, this);
            }
        }

        public void OnDispose()
        {
            if (DisposeEvent == null) return;
            try
            {
                DisposeEvent(this);
            }
            catch (Exception e)
            {
                Debug.LogError(e, this);
            }
        }
    }
}