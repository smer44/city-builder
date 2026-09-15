using System;
using UnityEngine;

[Serializable]
public class AmountOf<T>
{
    [SerializeField] public T Item;
    [SerializeField] [Min(0f)] public float Amount;

    
}

