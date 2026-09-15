public abstract class AbstractMouseState
{
    public abstract string InfoStr();

    public abstract void OnLeftMouseClick(AbstractMouseController owner);
    public abstract void OnRightMouseClick(AbstractMouseController owner);
    public abstract void OnMiddleMouseClick(AbstractMouseController owner);

    public virtual void OnEnter(AbstractMouseController owner) { }
    public virtual void Update(AbstractMouseController owner) { }
    public virtual void OnExit(AbstractMouseController owner) { }
}
