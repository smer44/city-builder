using System;
using System.Collections.Generic;
using UnityEngine;

public static class StorageFunctions
{   

    /// <summary>
    /// Converts item amounts into a dictionary, summing entries with the same key.
    /// </summary>
    public static Dictionary<T, float> ListToDict<T>(List<AmountOf<T>> amounts)
    {
        return ListToDict(amounts, entry => entry.Item, entry => entry.Amount);
    }

    public static Dictionary<ItemType, float> ListToDict(List<ItemAmount> amounts)
    {
        return ListToDict(amounts, entry => entry.Item.ItemType, entry => entry.Amount);
    }

    public static Dictionary<TKey, float> ListToDict<TSource, TKey>(
        IEnumerable<TSource> amounts,
        Func<TSource, TKey> keySelector,
        Func<TSource, float> amountSelector)
    {
        Dictionary<TKey, float> result = new();
        foreach (TSource entry in amounts)
        {
            TKey key = keySelector(entry);
            result.TryGetValue(key, out float current);
            result[key] = current + amountSelector(entry);
        }

        return result;
    }

    /// <summary>
    /// Returns the configured storage capacity for the given broad item supertype category.
    /// If the category is not present in the capacity dictionary, returns 0.
    /// </summary>
    /// <param name="capacities">Storage capacities grouped by item super type.</param>
    /// <param name="superType">Broad item category whose capacity should be returned.</param>
    /// <returns>The maximum capacity for the given super type, or 0 if no capacity entry exists.</returns>
    public static float GetCapacity(Dictionary<ItemSuperType, float> capacities, ItemSuperType superType)
    {
        capacities.TryGetValue(superType, out float capacity);
        return capacity;
    }

    /// <summary>
    /// Calculates the requested output amounts, grouped by their super types.
    /// </summary>
    public static Dictionary<ItemSuperType, float> GetOutputBySuperType(List<ItemAmount> outputs)
    {
        return ListToDict(outputs, output => output.Item.ItemSuperType, output => output.Amount);
    }

    /// <summary>
    /// Adds source amounts into the target dictionary, creating missing keys.
    /// </summary>
    public static void AddDictionaryValues<T>(Dictionary<T, float> target, Dictionary<T, float> source)
    {
        foreach (var pair in source)
        {
            target.TryGetValue(pair.Key, out float current);
            target[pair.Key] = current + pair.Value;
        }
    }

    /// <summary>
    /// Subtracts source amounts from the target dictionary, treating missing keys as zero.
    /// Logs a warning if a resulting amount is below zero.
    /// </summary>
    public static void SubstractDictionaryValues<T>(Dictionary<T, float> target, Dictionary<T, float> source)
    {
        foreach (var pair in source)
        {
            target.TryGetValue(pair.Key, out float current);
            float remaining = current - pair.Value;
            target[pair.Key] = remaining;

            if (remaining < 0f)
                Debug.LogWarning($"Subtracting {pair.Value} from dictionary key '{pair.Key}' ({current}) resulted in a negative amount: {remaining}.");
        }
    }

    /// <summary>
    /// Groups requested inputs by item type, optionally including existing input reservations.
    /// Does not modify the reservation dictionary.
    /// </summary>
    public static Dictionary<ItemType, float> GetInputByType(
        List<ItemAmount> inputs,
        Dictionary<ItemType, float> reservedInputs = null)
    {
        Dictionary<ItemType, float> inputByType = ListToDict(inputs);
        if (reservedInputs != null)
            AddDictionaryValues(inputByType, reservedInputs);

        return inputByType;
    }

    /// <summary>
    /// Returns true if the requested item amount can be stored
    /// within the capacity of the item's broad super type.
    /// </summary>
    public static bool CanPut(Storage storage, AmountOf<ItemDefinition> request)
    {
        if (storage == null || !CheckRequest(storage.StorageCapacities, storage.StoredItems, request))
            return false;

        storage.RegisterItemDefinition(request.Item);
        ItemDefinition item = request.Item;
        float amountToAdd = request.Amount;

        float typeCapacity = GetCapacity(storage.StorageCapacities, item.ItemSuperType);
        if (typeCapacity <= 0f)
            return false;

        float currentlyStoredForType = GetStoredAmountBySuperType(storage, item.ItemSuperType);
        float freeSpaceForType = typeCapacity - currentlyStoredForType;

        return freeSpaceForType >= amountToAdd;
    }



    /// <summary>
    /// Adds the requested item amount into storage if capacity allows.
    /// The method succeeds only if the storage has enough remaining capacity for the item's super type.
    /// </summary>
    /// <param name="storage">Storage containing quantities and capacities by type.</param>
    /// <param name="request">Requested item and amount to store.</param>
    /// 
    public static bool Put(Storage storage, AmountOf<ItemDefinition> request)
    {
        if (!CanPut(storage, request))
            return false;

        ItemType itemType = request.Item.ItemType;
        storage.StoredItems.TryGetValue(itemType, out float current);
        storage.StoredItems[itemType] = current + request.Amount;
        return true;
    }

    /// <summary>
    /// Returns true if the requested amount of the item type is available in storage.
    /// </summary>
    public static bool CanGet(Dictionary<ItemType, float> storedItems, AmountOf<ItemDefinition> request)
    {
        return storedItems != null && request != null && request.Item != null && request.Amount > 0f &&
            storedItems.TryGetValue(request.Item.ItemType, out float current) && current >= request.Amount;
    }



    /// <summary>
    /// Tries to remove the requested item amount from storage.
    /// The method succeeds only if the item type exists in storage in sufficient quantity.
    /// </summary>
    /// <param name="storedItems">Current quantities grouped by item type.</param>
    /// <param name="request">Requested item and amount to remove from storage.</param>
    public static void Get(Dictionary<ItemType, float> storedItems, AmountOf<ItemDefinition> request)
    {
        if (!CanGet(storedItems, request))
            throw new InvalidOperationException($"Not enough stored items of type '{request.Item.ItemType}'.");

        storedItems[request.Item.ItemType] -= request.Amount;
    }

    /// <summary>
    /// Calculates the total stored amount of all exact items that belong to the given exact item type.
    /// </summary>
    /// <param name="storedItems">Current list of stored exact item definitions and their amounts.</param>
    /// <param name="itemType">Exact item type to sum.</param>
    /// <returns>Total stored amount of all items with the given exact item type.</returns>
    /// 
    public static float GetStoredAmountByTypeOld(List<AmountOf<ItemDefinition>> storedItems, ItemType itemType)
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
    /// Calculates the total stored amount of item types that belong to the given broad category.
    /// </summary>
    /// <param name="storage">Storage containing quantities and their item type classifications.</param>
    /// <param name="itemSuperType">Broad item category to sum.</param>
    /// <returns>Total stored amount of all items with the given super type.</returns>
    public static float GetStoredAmountBySuperType(Storage storage, ItemSuperType itemSuperType)
    {

        float total = 0f;

        foreach (var entry in storage.StoredItems)
        {
            if (entry.Value <= 0f)
                continue;

            if (!storage.ItemSuperTypes.TryGetValue(entry.Key, out ItemSuperType superType))
                throw new InvalidOperationException($"No super type is registered for stored item type '{entry.Key}'.");

            if (superType == itemSuperType)
                total += entry.Value;
        }

        return total;
    }

    /// <summary>
    /// Checks whether the provided storage request contains all required references and a positive amount.
    /// This method validates only basic input presence, not storage capacity or item availability.
    /// </summary>
    /// <param name="capacities">Storage capacities grouped by item super type.</param>
    /// <param name="storedItems">Current quantities grouped by item type.</param>
    /// <param name="request">Requested item and amount for a storage operation.</param>
    /// <returns>True if all required references exist and the amount is positive; otherwise false.</returns>
    /// 
    public static bool CheckRequest(
        Dictionary<ItemSuperType, float> capacities,
        Dictionary<ItemType, float> storedItems,
        AmountOf<ItemDefinition> request)
    {

        return capacities != null && storedItems != null && request != null && request.Item != null && request.Amount > 0f;

    }

}
