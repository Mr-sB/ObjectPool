# ObjectPool
The Unity object pooling can load and cache all objects inherited from UnityEngine.Object, and can easily expand the loading method, such as loading objects from AssetBundle

# Feature
* Load and cache all objects inherited from `UnityEngine.Object`.
* Unused assets and `PoolItem` will auto destroy by `DeleteTime`. If `DeleteTime` is -1, they will not auto destroy.
* `LoadMode.Resource` to cache from Resources folder.
* `LoadMode.Custom` to cache from anywhere by call `ObjectPool.Instance.RegisterCustomPoolItem` function first.
* If you cache `GameObject`, you can implement `ISpawnHandler` and `IDisposeHandler` interfaces to listen spawn and dispose actions.
```c#
public interface ISpawnHandler
{
    void OnSpawn();
}
public interface IDisposeHandler
{
    void OnDispose();
}
```
Or you can add listener from `ObjectPoolItemKey.SpawnEvent` and `ObjectPoolItemKey.DisposeEvent`.
```c#
public event Action<ObjectPoolItemKey> SpawnEvent;
public event Action<ObjectPoolItemKey> DisposeEvent;
```
* The `ObjectPool` basically does not consume extra memory and GC.

# Note
* If you want to cache Object from `AssetBundle`, maybe you can use [AssetBundleManager](https://github.com/Mr-sB/AssetBundleManager)(Open source) to manage your AssetBundles.
```c#
public ObjectPoolItem(ObjectPool.LoadMode loadMode, string bundleName, string assetName, DeleteTime deleteTime) : base(deleteTime)
{
    ...
    switch (mLoadMode)
    {
        case ObjectPool.LoadMode.Resource:
            OriginAsset = Resources.Load<T>(assetName);
            break;
        case ObjectPool.LoadMode.Custom:
            break;
        // MARK: Use AssetBundleManager to load asset.
        case ObjectPool.LoadMode.AssetBundle:
            OriginAsset = AssetBundleManager.GetAsset<T>(bundleName, assetName);
            break;
        // MARK: Add yourself load methods
    }
    ...
}
```
