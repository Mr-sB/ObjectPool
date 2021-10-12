using UnityEngine;

namespace GameUtil
{
    public class ObjectPoolBase<T> : MonoBehaviour where T : ObjectPoolBase<T>, new()
    {
        #region Instance
#if UNITY_EDITOR
        protected static bool _onApplicationQuit;
#endif

        private static T instance;
        public static T Instance
        {
            get
            {
                //Avoid calling Instance in OnDestroy method to cause error when application quit
#if UNITY_EDITOR
                if (_onApplicationQuit)
                {
                    // ReSharper disable once Unity.IncorrectMonoBehaviourInstantiation
                    return new T();
                }
#endif
                if (instance == null)
                {
                    //Find
                    instance = FindObjectOfType<T>();
                    //Create
                    if (instance == null)
                    {
                        var go = new GameObject(typeof(T).Name);
                        instance = go.AddComponent<T>();
                        DontDestroyOnLoad(go);
                    }
                }
                return instance;
            }
        }

        private void Awake()
        {
            if (instance == null)
                instance = this as T;
        }

#if UNITY_EDITOR
        private void OnApplicationQuit()
        {
            _onApplicationQuit = true;
        }
#endif
        #endregion
    }
}