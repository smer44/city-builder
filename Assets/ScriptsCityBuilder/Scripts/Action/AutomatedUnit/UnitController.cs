using System.Collections.Generic;
using UnityEngine;

public class UnitController : MonoBehaviour
{
    [SerializeField] private Transform field;

    private readonly List<UnitOnField> units = new();

    public Transform Field => field;
    public IReadOnlyList<UnitOnField> Units => units;

    public int AddUnitsFromDwelling(UnitDwelling dwelling)
    {
        if (dwelling == null || dwelling.Units == null)
            return 0;

        int addedCount = 0;
        foreach (UnitOnField unit in dwelling.Units)
        {
            if (unit == null || units.Contains(unit))
                continue;

            units.Add(unit);
            addedCount++;
        }

        return addedCount;
    }

    public UnitOnField ClosestFreeUnit(AbstractFieldPlacer fieldPlacer, Transform target)
    {
        if (fieldPlacer == null || target == null)
            return null;

        units.RemoveAll(unit => unit == null);

        UnitOnField closest = null;
        float closestSqrDistance = float.PositiveInfinity;
        Vector3 targetPosition = fieldPlacer.GetPosition(target);

        foreach (UnitOnField unit in units)
        {
            if (unit == null || !unit.IsFreeForJob || unit.Field != fieldPlacer)
                continue;

            float sqrDistance = (fieldPlacer.GetPosition(unit.transform) - targetPosition).sqrMagnitude;
            if (sqrDistance >= closestSqrDistance)
                continue;

            closest = unit;
            closestSqrDistance = sqrDistance;
        }

        return closest;
    }
}
