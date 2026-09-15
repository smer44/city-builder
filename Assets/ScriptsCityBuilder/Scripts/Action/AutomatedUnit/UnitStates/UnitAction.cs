using UnityEngine;

public abstract class UnitAction : ScriptableObject
{
    [System.NonSerialized] private bool runtimeInstance;

    public virtual string ActionName => string.IsNullOrWhiteSpace(name) ? GetType().Name : name;
    public bool IsRuntimeInstance => runtimeInstance;

    public virtual UnitAction CreateRuntimeInstance()
    {
        UnitAction runtimeAction = Instantiate(this);
        runtimeAction.MarkAsRuntimeInstance();
        return runtimeAction;
    }

    public virtual void Begin(AbstractFieldPlacer field, UnitOnField unit)
    {
    }

    public abstract bool Tick(AbstractFieldPlacer field, UnitOnField unit, float delta);

    public virtual void Finish(AbstractFieldPlacer field, UnitOnField unit)
    {
    }

    public virtual void Cancel(AbstractFieldPlacer field, UnitOnField unit)
    {
    }

    protected void MarkAsRuntimeInstance()
    {
        runtimeInstance = true;
    }

    public void DisposeRuntimeInstance()
    {
        if (runtimeInstance)
            Destroy(this);
    }
}
