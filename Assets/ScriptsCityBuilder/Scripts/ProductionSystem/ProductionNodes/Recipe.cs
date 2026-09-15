using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Recipe_", menuName = "Production/Recipe")]
public class Recipe : ScriptableObject
{
    
    [Header("Identity")]
    [SerializeField] public string DisplayName;
    [TextArea]
    [SerializeField] public string Description;

    [SerializeField] public float Time;

    [SerializeField] public float Labor;


    [Header("Flow")]
    [SerializeField] public List<ItemAmount> Inputs = new();
    [SerializeField] public List<ItemAmount> Outputs = new();




}
