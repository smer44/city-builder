using UnityEngine;

public class UnitDwelling : MonoBehaviour
{
    [SerializeField] protected UnitOnField[] units;

    public UnitOnField[] Units => units;

    protected void SetUnits(UnitOnField[] newUnits)
    {
        units = newUnits;
    }
}
