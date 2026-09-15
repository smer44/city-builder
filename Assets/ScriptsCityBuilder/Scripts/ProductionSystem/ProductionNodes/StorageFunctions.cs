using System.Collections.Generic;
using UnityEngine;

public static class StorageFunctions
{   

    /// <summary>
    /// Returns the configured storage capacity for the given broad item supertype category.
    /// If the category is not present in the capacity list, returns 0.
    /// </summary>
    /// <param name="Capacities">List of storage capacities grouped by item super type.</param>
    /// <param name="superType">Broad item category whose capacity should be returned.</param>
    /// <returns>The maximum capacity for the given super type, or 0 if no capacity entry exists.</returns>
    public static float GetCapacity(List<AmountOf<ItemSuperType>> capacities, ItemSuperType superType)
    {

        for (int i = 0; i < capacities.Count; i++)
        {
            var entry = capacities[i];
            if (entry.Item == superType)
                return entry.Amount;
        }

        return 0f;
    }

    /// <summary>
    /// Returns true if the requested exact item amount can be stored
    /// within the capacity of the item's broad super type.
    /// </summary>
    public static bool CanPut(
        List<AmountOf<ItemSuperType>> capacities,
        List<AmountOf<ItemDefinition>> storedItems,
        AmountOf<ItemDefinition> request)
    {
        ItemDefinition item = request.Item;
        float amountToAdd = request.Amount;

        float typeCapacity = GetCapacity(capacities, item.ItemSuperType);
        if (typeCapacity <= 0f)
            return false;

        float currentlyStoredForType = GetStoredAmountBySuperType(storedItems, item.ItemSuperType);
        float freeSpaceForType = typeCapacity - currentlyStoredForType;

        return freeSpaceForType >= amountToAdd;
    }



    /// <summary>
    /// Adds the requested item amount into storage, can overflow.
    /// The method succeeds only if the storage has enough remaining capacity for the item's super type.
    /// </summary>
    /// <param name="capacities">List of storage capacities grouped by item super type.</param>
    /// <param name="storedItems">Current list of stored exact item definitions and their amounts.</param>
    /// <param name="request">Requested exact item and amount to store.</param>
    /// 
    public static bool Put(List<AmountOf<ItemSuperType>> capacities, List<AmountOf<ItemDefinition>> storedItems, AmountOf<ItemDefinition> request)
    {
        ItemDefinition item = request.Item;
        float amountToAdd = request.Amount;

        AmountOf<ItemDefinition> existing = FindStoredEntry(storedItems, item);
        if (existing != null)
        {
            existing.Amount += amountToAdd;
        }
        else
        {
            storedItems.Add(new AmountOf<ItemDefinition> { Item = item, Amount = amountToAdd });
        }


        return true;
    }

    /// <summary>
    /// Returns true if the requested exact item amount is available in storage.
    /// </summary>
    public static bool CanGet(List<AmountOf<ItemDefinition>> storedItems, AmountOf<ItemDefinition> request)
    {
        AmountOf<ItemDefinition> existing = FindStoredEntry(storedItems, request.Item);
        if (existing == null)
            return false;

        return existing.Amount >= request.Amount;
    }



    /// <summary>
    /// Tries to remove the requested item amount from storage.
    /// The method succeeds only if the exact item exists in storage in sufficient quantity.
    /// </summary>
    /// <param name="storedItems">Current list of stored exact item definitions and their amounts.</param>
    /// <param name="request">Requested exact item and amount to remove from storage.</param>
    /// <returns>True if the item amount was removed successfully; otherwise false.</returns>
    public static void Get(List<AmountOf<ItemDefinition>> storedItems, AmountOf<ItemDefinition> request)
    {
        AmountOf<ItemDefinition> existing = FindStoredEntry(storedItems, request.Item);
        existing.Amount -= request.Amount;

        if (existing.Amount <= 0f)
        {
            storedItems.Remove(existing);
        }

    }

    /// <summary>
    /// Calculates the total stored amount of all exact items that belong to the given exact item type.
    /// </summary>
    /// <param name="storedItems">Current list of stored exact item definitions and their amounts.</param>
    /// <param name="itemType">Exact item type to sum.</param>
    /// <returns>Total stored amount of all items with the given exact item type.</returns>
    /// 
    public static float GetStoredAmountByType(List<AmountOf<ItemDefinition>> storedItems, ItemType itemType)
    {
        float total = 0f;

        for (int i = 0; i < storedItems.Count; i++)
        {
            AmountOf<ItemDefinition> entry = storedItems[i];

            if (entry.Item.ItemType == itemType)
                total += entry.Amount;
        }

        return total;
    }
    /// <summary>
    /// Calculates the total stored amount of all exact items that belong to the given broad item category.
    /// </summary>
    /// <param name="storedItems">Current list of stored exact item definitions and their amounts.</param>
    /// <param name="itemSuperType">Broad item category to sum.</param>
    /// <returns>Total stored amount of all items with the given super type.</returns>
    public static float GetStoredAmountBySuperType(List<AmountOf<ItemDefinition>> storedItems, ItemSuperType itemSuperType)
    {

        float total = 0f;

        for (int i = 0; i < storedItems.Count; i++)
        {
            AmountOf<ItemDefinition> entry = storedItems[i];
            if (entry.Item.ItemSuperType == itemSuperType)
                total += entry.Amount;
        }

        return total;
    }

    /// <summary>
    /// Finds the storage entry for the given exact item definition.
    /// Returns null if the item is not currently stored.
    /// </summary>
    /// <param name="storedItems">Current list of stored exact item definitions and their amounts.</param>
    /// <param name="item">Exact item definition to look up.</param>
    /// <returns>The matching stored item entry, or null if not found.</returns>
    private static AmountOf<ItemDefinition> FindStoredEntry(List<AmountOf<ItemDefinition>> storedItems, ItemDefinition item)
    {
        for (int i = 0; i < storedItems.Count; i++)
        {
            AmountOf<ItemDefinition> entry = storedItems[i];
            if (entry.Item == item)
                return entry;
        }

        return null;
    }

    /// <summary>
    /// Checks whether the provided storage request contains all required references and a positive amount.
    /// This method validates only basic input presence, not storage capacity or item availability.
    /// </summary>
    /// <param name="capacities">List of storage capacities grouped by item super type.</param>
    /// <param name="storedItems">Current list of stored exact item definitions and their amounts.</param>
    /// <param name="request">Requested exact item and amount for a storage operation.</param>
    /// <returns>True if all required references exist and the amount is positive; otherwise false.</returns>
    /// 
    public static bool CheckRequest(
        List<AmountOf<ItemSuperType>> capacities,
        List<AmountOf<ItemDefinition>> storedItems,
        AmountOf<ItemDefinition> request)
    {

        return capacities != null && storedItems != null && request != null && request.Item != null && request.Amount > 0f;

    }

}