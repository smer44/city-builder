using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class Storage : MonoBehaviour
{
    [SerializeField] public StorageSize StorageSize;
    [FormerlySerializedAs("StoredItems")]
    [SerializeField] private List<AmountOf<ItemDefinition>> initialStoredItems = new();

    public Dictionary<ItemType, float> StoredItems = new();
    public Dictionary<ItemSuperType, float> StorageCapacities = new();

    private readonly Dictionary<ItemType, ItemSuperType> itemSuperTypes = new();
    public IReadOnlyDictionary<ItemType, ItemSuperType> ItemSuperTypes => itemSuperTypes;

    protected virtual void Awake()
    {
        RefreshStorageCapacities();
        StoredItems = StorageFunctions.ListToDict(initialStoredItems, entry => entry.Item.ItemType, entry => entry.Amount);

        foreach (var entry in initialStoredItems)
            RegisterItemDefinition(entry.Item);
    }

    public void RefreshStorageCapacities()
    {
        StorageCapacities = StorageSize == null
            ? new Dictionary<ItemSuperType, float>()
            : StorageFunctions.ListToDict(StorageSize.Capacities);
    }

    public void RegisterItemDefinition(ItemDefinition item)
    {
        if (item == null)
            throw new ArgumentNullException(nameof(item));

        if (itemSuperTypes.TryGetValue(item.ItemType, out ItemSuperType superType) && superType != item.ItemSuperType)
            throw new InvalidOperationException($"Item type '{item.ItemType}' has conflicting super types: '{superType}' and '{item.ItemSuperType}'.");

        itemSuperTypes[item.ItemType] = item.ItemSuperType;
    }
}
