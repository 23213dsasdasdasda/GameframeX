namespace Hotfix.Gameplay.Core
{
    public sealed class FSM
    {
        public IState CurrentState { get; private set; }
        public IState PreviousState { get; private set; }
        public bool IsTransitioning { get; private set; }

        public void Update(float deltaTime)
        {
            CurrentState?.Update(deltaTime);
        }

        public bool IsCurrent<TState>() where TState : class, IState
        {
            return CurrentState is TState;
        }

        public void ChangeState(IState nextState)
        {
            if (nextState == null || ReferenceEquals(CurrentState, nextState))
            {
                return;
            }

            IsTransitioning = true;
            CurrentState?.Exit();
            PreviousState = CurrentState;
            CurrentState = nextState;
            CurrentState.Enter();
            IsTransitioning = false;
        }

        public void Clear()
        {
            CurrentState?.Exit();
            PreviousState = CurrentState;
            CurrentState = null;
            IsTransitioning = false;
        }
    }
}
