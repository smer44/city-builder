using UnityEngine;

public class UnitDwellingAutoFill : UnitDwelling, OnSpawn<AbstractFieldPlacer>
{
    [SerializeField] private UnitOnField unitPrefab;
    [SerializeField, Min(0)] private int unitCount = 1;
    [SerializeField] private Transform unitsParent;

    public void OnSpawn(AbstractFieldPlacer field)
    {
        AutoFill(field);
        RegisterUnitsWithController(field);
    }

    public void AutoFill(AbstractFieldPlacer field = null)
    {
        if (field == null)
            field = GetComponentInParent<AbstractFieldPlacer>();

        if (field == null)
        {
            Debug.LogWarning($"{nameof(UnitDwellingAutoFill)}: spawning field is not assigned.", this);
            return;
        }

        int count = Mathf.Max(0, unitCount);
        UnitOnField[] spawnedUnits = new UnitOnField[count];

        if (unitPrefab == null)
        {
            Debug.LogWarning($"{nameof(UnitDwellingAutoFill)}: unit prefab is not assigned.", this);
            SetUnits(new UnitOnField[0]);
            return;
        }

        Transform parent = GetUnitsParent(field);
        Vector3 dwellingPosition = field.GetPosition(transform);

        for (int i = 0; i < count; i++)
        {
            UnitOnField unit = Instantiate(unitPrefab, parent, false);
            UnitOnFieldData data = ScriptableObject.CreateInstance<UnitOnFieldData>();
            data.AutoFill();
            unit.Data = data;

            field.SetUnitPosition(unit, dwellingPosition);
            unit.SetHome(transform);
            unit.SetField(field);

            spawnedUnits[i] = unit;
        }

        SetUnits(spawnedUnits);
    }

    private Transform GetUnitsParent(AbstractFieldPlacer field)
    {
        if (unitsParent != null)
            return unitsParent;

        if (field != null && field.ParentTransform != null)
            return field.ParentTransform;

        return transform;
    }

    private void RegisterUnitsWithController(AbstractFieldPlacer field)
    {
        UnitController controller = field == null ? null : field.UnitController;
        if (controller == null)
        {
            Debug.LogWarning($"{nameof(UnitDwellingAutoFill)}: {nameof(UnitController)} is not assigned on the spawning {nameof(AbstractFieldPlacer)}.", this);
            return;
        }

        int addedCount = controller.AddUnitsFromDwelling(this);
        Debug.Log($"{nameof(UnitDwellingAutoFill)}: registered {addedCount}/{(Units == null ? 0 : Units.Length)} units with {controller.name}.", this);
    }
}
