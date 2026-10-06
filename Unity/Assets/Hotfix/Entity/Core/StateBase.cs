namespace Hotfix.Gameplay.Core
{
    public abstract class StateBase<TEntity> : IState where TEntity : EntityBase
    {
        protected StateBase(TEntity owner)
        {
            Owner = owner;
        }

        protected TEntity Owner { get; }
        public virtual string Name => GetType().Name;

        public virtual void Enter()
        {
        }

        public virtual void Update(float deltaTime)
        {
        }

        public virtual void Exit()
        {
        }
    }
}
