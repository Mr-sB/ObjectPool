using UnityEngine;

namespace GameUtil
{
    [DisallowMultipleComponent]
    public class ObjectPoolItemKey : MonoBehaviour
    {
        [SerializeField] private ObjectPool.LoadMode mLoadMode;
        [SerializeField] private string mBundleName;
        [SerializeField] private string mAssetName;
        public ObjectPool.LoadMode LoadMode => mLoadMode;
        public string BundleName => mBundleName;
        public string AssetName => mAssetName;
        
        public void Init(ObjectPool.LoadMode loadMode, string bundleName, string assetName)
        {
            mLoadMode = loadMode;
            mBundleName = bundleName;
            mAssetName = assetName;
        }
    }
}