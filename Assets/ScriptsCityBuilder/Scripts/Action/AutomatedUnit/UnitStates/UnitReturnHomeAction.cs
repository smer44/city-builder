using UnityEngine;

public class UnitReturnHomeAction : UnitAction
{
    public static UnitReturnHomeAction Create()
    {
        UnitReturnHomeAction action = CreateInstance<UnitReturnHomeAction>();
        action.name = "Return Home";
        action.MarkAsRuntimeInstance();
        return action;
    }

    public override bool Tick(AbstractFieldPlacer field, UnitOnField unit, float delta)
    {
        return unit == null || (field != null && field.MoveTowardsHome(unit, delta));
    }
}
