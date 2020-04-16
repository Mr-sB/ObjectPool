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
        
        public void OnSpawn()
        {
            DelayDisposeSelf();
        }

        public void OnDispose()
        {
            if(mCoroutine != null)
                StopCoroutine(mCoroutine);
            mCoroutine = null;
        }

        private void DelayDisposeSelf()
        {
            if(mCoroutine != null) return;
            if (Delay <= 0)
            {
                ObjectPool.Instance.DisposeGameObject(gameObject);
                return;
            }

            mCoroutine = StartCoroutine(DelayDisposeSelfInternal());
        }

        IEnumerator DelayDisposeSelfInternal()
        {
            if (IgnoreTimeScale)
                yield return new WaitForSecondsRealtime(Delay);
            else
                yield return new WaitForSeconds(Delay);
            ObjectPool.Instance.DisposeGameObject(gameObject);
            mCoroutine = null;
        }
    }
}