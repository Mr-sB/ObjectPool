namespace GameUtil
{
    public abstract class PoolItemBase
    {
        protected DeleteTime mDeleteTime;
        protected float mNullTime;

        protected PoolItemBase(DeleteTime deleteTime)
        {
            mDeleteTime = deleteTime;
        }

        public void SetDeleteTime(DeleteTime deleteTime)
        {
            mDeleteTime = deleteTime;
        }
        
        public abstract int ItemCount { get; }
        public abstract object Get();
        public abstract bool Update();
        public abstract void Resize(int size, UnityEngine.Transform parent);
        public abstract void Clear();
    }
}