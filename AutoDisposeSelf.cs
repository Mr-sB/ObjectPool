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
            OnDispose();
        }

        public void OnSpawn()
        {
            DelayDisposeSelf();
        }

        public void OnDispose()
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