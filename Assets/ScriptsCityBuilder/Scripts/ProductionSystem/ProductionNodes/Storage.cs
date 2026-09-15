using System.Collections.Generic;
using UnityEngine;

public class Storage : MonoBehaviour
{
    [SerializeField] public StorageSize StorageSize;
    [SerializeField] public List<AmountOf<ItemDefinition>> StoredItems = new();



}