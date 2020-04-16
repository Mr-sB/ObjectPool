# ObjectPool
The Unity object pooling can load and cache all objects inherited from UnityEngine.Object, and can easily expand the loading method, such as loading objects from AssetBundle

# Feature
* Load and cache all objects inherited from `UnityEngine.Object`.
* Unused assets and `PoolItem` will auto destroy by `DeleteTime`. If `DeleteTime` is -1, they will not auto destroy.
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
* The `ObjectPool` basically does not consume extra memory and GC.
