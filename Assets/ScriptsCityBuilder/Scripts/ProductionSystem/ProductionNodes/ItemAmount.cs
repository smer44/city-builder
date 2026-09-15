using UnityEngine;

[CreateAssetMenu(fileName = "ItemAmount", menuName = "Production/Item Amount")]
public class ItemAmount : ScriptableObject
{
    [SerializeField] private AmountOf<ItemDefinition> itemAmount = new();

    public ItemDefinition Item => itemAmount?.Item;
    public float Amount => itemAmount == null ? 0f : itemAmount.Amount;

    public AmountOf<ItemDefinition> CreateRuntimeAmount()
    {
        return new AmountOf<ItemDefinition> { Item = Item, Amount = Amount };
    }
}
