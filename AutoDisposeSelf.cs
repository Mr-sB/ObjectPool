using System.Collections;
using UnityEngine;

namespace GameUtil
{
    [DisallowMultipleComponent]
    public class AutoDisposeSelf : MonoBehaviour, ISpawnHandler, IDisposeHandler
    {
        public float Delay = 1;
        public bool IgnoreTimeScale;
        private Coroutine mCoroutine;

        private void Start()
        {
            DelayDisposeSelf();
        }

        private void OnDestroy()
        {
            StopDelayDisposeSelf();
        }

        public void OnSpawn()
        {
            DelayDisposeSelf();
        }

        public void OnDispose()
        {
            StopDelayDisposeSelf();
            //这里还需要再次销毁一下
            //因为可能是嵌套的外部对象Dispose了，导致自己响应到OnDispose
            //所以还需要再销毁一次确保自身被Dispose
            //重复调用DisposeGameObject不会有错误行为
            ObjectPool.Instance.DisposeGameObject(gameObject);
        }

        private void StopDelayDisposeSelf()
        {
            //使用ObjectPool.Instance开启/关闭协程，避免因为自身隐藏导致协程停止
            if (mCoroutine != null)
                ObjectPool.SafeStopCoroutine(mCoroutine);
            mCoroutine = null;
        }

        private void DelayDisposeSelf()
        {
            if (mCoroutine != null) return;
            if (Delay <= 0)
            {
                ObjectPool.Instance.DisposeGameObject(gameObject);
                return;
            }

            //使用ObjectPool.Instance开启/关闭协程，避免因为自身隐藏导致协程停止
            mCoroutine = ObjectPool.SafeStartCoroutine(DelayDisposeSelfInternal());
        }

        IEnumerator DelayDisposeSelfInternal()
        {
            if (IgnoreTimeScale)
                yield return new WaitForSecondsRealtime(Delay);
            else
                yield return new WaitForSeconds(Delay);
            mCoroutine = null;
            ObjectPool.Instance.DisposeGameObject(gameObject);
        }
    }
}