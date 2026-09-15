using System.Collections.Generic;
using UnityEngine;

public class ProductionOverview : MonoBehaviour, Registery<AbstractFieldPlacer, ProductionNode>
{
    public Dictionary<ItemType, List<ProductionNode>> ProducersByOutput = new();

    private void Awake()
    {
        // Dynamically created fields can register objects before this component's Start.
        ProducersByOutput ??= new Dictionary<ItemType, List<ProductionNode>>();
    }

    private void Start()
    {
        ProducersByOutput ??= new Dictionary<ItemType, List<ProductionNode>>();
    }

    public bool Register(AbstractFieldPlacer field, ProductionNode node) => Register(node);
    public bool UnRegister(AbstractFieldPlacer field, ProductionNode node) => UnRegister(node);

    public bool Register(ProductionNode node)
    {
        if (node == null)
            return false;

        // Storage and output nodes have no recipes, but still belong in the overview.
        if (node.AvailableRecipes == null || node.AvailableRecipes.Count == 0)
            return RegisterOutput(ItemType.None, node);

        bool registered = false;
        foreach (Recipe recipe in node.AvailableRecipes)
        {
            if (recipe == null || recipe.Outputs == null)
                continue;

            foreach (ItemAmount output in recipe.Outputs)
            {
                if (!IsUsableOutput(output))
                    continue;

                registered |= RegisterOutput(output.Item.ItemType, node);
            }
        }

        return registered;
    }

    private bool RegisterOutput(ItemType itemType, ProductionNode node)
    {
        if (!ProducersByOutput.TryGetValue(itemType, out List<ProductionNode> producers))
        {
            producers = new List<ProductionNode>();
            ProducersByOutput.Add(itemType, producers);
        }

        // Several recipes or item definitions can produce the same ItemType.
        if (producers.Contains(node))
            return false;

        producers.Add(node);
        return true;
    }

    public bool UnRegister(ProductionNode node)
    {
        if (node == null)
            return false;

        bool removed = false;
        List<ItemType> emptyOutputs = new();
        // Use the index so removal still works if recipes have changed since registration.
        foreach (var pair in ProducersByOutput)
        {
            if (!pair.Value.Remove(node))
                continue;

            removed = true;
            if (pair.Value.Count == 0)
                emptyOutputs.Add(pair.Key);
        }

        foreach (ItemType itemType in emptyOutputs)
            ProducersByOutput.Remove(itemType);

        return removed;
    }

    internal static bool IsUsableOutput(ItemAmount output)
    {
        return output != null && output.Item != null && output.Item.ItemType != ItemType.None &&
            output.Amount > 0f && !float.IsNaN(output.Amount) && !float.IsInfinity(output.Amount);
    }
}
