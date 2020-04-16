using UnityEngine;

namespace GameUtil
{
    [DisallowMultipleComponent]
    public class DisposeSelf : MonoBehaviour
    {
        public ObjectPool.LoadMode LoadMode { private set; get; }
        public string AssetPath { private set; get; }
        public void Init(string assetPath, ObjectPool.LoadMode loadMode)
        {
            AssetPath = assetPath;
            LoadMode = loadMode;
        }
    }
}