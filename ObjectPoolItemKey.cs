using UnityEngine;

namespace GameUtil
{
    [DisallowMultipleComponent]
    public class ObjectPoolItemKey : MonoBehaviour
    {
        [SerializeField] private ObjectPool.LoadMode mLoadMode;
        [SerializeField] private string mAssetPath;
        public ObjectPool.LoadMode LoadMode => mLoadMode;
        public string AssetPath => mAssetPath;
        public void Init(string assetPath, ObjectPool.LoadMode loadMode)
        {
            mAssetPath = assetPath;
            mLoadMode = loadMode;
        }
    }
}