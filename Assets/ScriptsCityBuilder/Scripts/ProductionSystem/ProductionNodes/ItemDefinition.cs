using UnityEngine;

[CreateAssetMenu(fileName = "Item_", menuName = "Production/Item Definition")]
public class ItemDefinition : ScriptableObject
{
    [Header("Identity")]
    [SerializeField] public string DisplayName;
    [SerializeField] public ItemType ItemType;
    [SerializeField] public ItemSuperType ItemSuperType;

    [Header("Logistics")]
    [SerializeField] public float UnitMass = 1f;

}