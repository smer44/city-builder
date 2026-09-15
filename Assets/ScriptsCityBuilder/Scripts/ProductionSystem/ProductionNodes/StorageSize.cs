using UnityEngine;

using System.Collections.Generic;

[CreateAssetMenu(fileName = "StorageSize_", menuName = "Production/Storage Size")]
public class StorageSize : ScriptableObject
{
    [SerializeField] public string StorageSizeId;
    [SerializeField] public List<AmountOf<ItemSuperType>> Capacities = new();


}