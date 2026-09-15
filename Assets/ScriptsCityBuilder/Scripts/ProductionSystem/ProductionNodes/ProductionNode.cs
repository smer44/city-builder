using System;
using System.Collections.Generic;
using UnityEngine;

public enum ProductionSignal
{
    Started,
    SkippedNotEnoughInputs,
    SkippedNotEnoughLabor,
    SkippedNotEnoughOutputSpace,
    RecipeOutputStoredOverCapacity,
    Completed
}


[System.Serializable]
public class RecipeExecution
{
    public Recipe Recipe;
    public float Timer;

    public RecipeExecution(Recipe recipe)
    {
        Recipe = recipe;
        Timer = recipe.Time;
    }
}


public class ProductionNode : Storage
{
    public event Action<ProductionNode, Recipe, ProductionSignal> OnProductionSignal;

    private AbstractFieldPlacer field;
    public AbstractFieldPlacer Field => field;

    public void SetField(AbstractFieldPlacer newField)
    {
        field = newField;
    }

    private void OnDestroy()
    {
        if (field != null)
            field.UnRegisterProductionNode(this);
    }


    [Header("Recipes")]
    [SerializeField] public List<Recipe> AvailableRecipes = new();
    [SerializeField] public List<RecipeExecution> Work = new();
    [SerializeField] private List<Recipe> queuedRecipes = new();
    //[SerializeField] public int CurrentRecipeIndex = 0;

    [Header("Labor")]
    [SerializeField] public float TotalLabor = 0f;
    [Header("Runtime")]
    [SerializeField] private float usedLabor = 0f;

    public float FreeLabor => TotalLabor - usedLabor;

    /// <summary>Requests one execution, retaining it until the node can start it.</summary>
    public bool QueueRecipe(Recipe recipe)
    {
        if (recipe == null || AvailableRecipes == null || !AvailableRecipes.Contains(recipe))
            return false;

        ValidateRecipe(recipe);
        queuedRecipes.Add(recipe);
        if (isActiveAndEnabled && isProducing)
            StartQueuedRecipes();
        return true;
    }

    /// <summary>Waiting and running executions; each completion reduces this by one.</summary>
    public int GetPendingRecipeCount(Recipe recipe)
    {
        if (recipe == null)
            return 0;

        int count = 0;
        foreach (Recipe queued in queuedRecipes)
            if (queued == recipe)
                count++;
        foreach (RecipeExecution execution in Work)
            if (execution != null && execution.Recipe == recipe)
                count++;
        return count;
    }

    private void StartQueuedRecipes()
    {
        for (int i = 0; i < queuedRecipes.Count;)
        {
            Recipe recipe = queuedRecipes[i];
            if (recipe == null)
                queuedRecipes.RemoveAt(i);
            else if (CanStartRecipe(recipe))
            {
                queuedRecipes.RemoveAt(i);
                StartRecipe(recipe);
            }
            else
                i++;
        }
    }


    [SerializeField] public bool isProducing = true;

    //[SerializeField] private float activeRecipeRemainingTime = 0f;


    void FixedUpdate()
    {
        if (isProducing)
        {
            UpdateWork(Time.fixedDeltaTime);
        }
        
    }


    /// <summary>
    /// Advances all running recipe executions.
    /// Any execution whose timer reaches 0 or below is completed:
    /// - outputs are stored
    /// - labor is released
    /// - execution is removed from Work
    /// </summary>
    public void UpdateWork(float deltaTime)
    {
        for (int i = Work.Count - 1; i >= 0; i--)
        {
            RecipeExecution execution = Work[i];
            execution.Timer -= deltaTime;

            if (execution.Timer > 0f)
                continue;

            CompleteExecution(execution);
            Work.RemoveAt(i);
            SendSignal(execution.Recipe, ProductionSignal.Completed);
        }

        if (isProducing)
            StartQueuedRecipes();
    }


    private void CompleteExecution(RecipeExecution execution)
    {
        Recipe recipe = execution.Recipe;

        PutOutputs(recipe);
        usedLabor -= recipe.Labor;

        if (usedLabor < 0f)
            usedLabor = 0f;

    }

    /// <summary>
    /// Tries to start a recipe execution:
    /// - checks labor
    /// - checks inputs
    /// - checks planned output space
    /// - consumes inputs
    /// - reserves labor
    /// - adds execution to Work
    /// </summary>
    public bool TryStartRecipe(Recipe recipe)
    {
        ValidateRecipe(recipe);
        if (!CanStartRecipe(recipe))
            return false;

        StartRecipe(recipe);
        return true;
    }

    private void StartRecipe(Recipe recipe)
    {
        GetInputs(recipe);
        usedLabor += recipe.Labor;
        Work.Add(new RecipeExecution(recipe));
        SendSignal(recipe, ProductionSignal.Started);
    }

    public bool CanStartRecipe(Recipe recipe)
    {
        if (!HasEnoughLabor(recipe))
        {
            SendSignal(recipe, ProductionSignal.SkippedNotEnoughLabor);
            return false;
        }

        if (!HasEnoughInputs(recipe))
        {
            SendSignal(recipe, ProductionSignal.SkippedNotEnoughInputs);
            return false;
        }

        if (!HasEnoughOutputSpace(recipe))
        {
            SendSignal(recipe, ProductionSignal.SkippedNotEnoughOutputSpace);
            return false;
        }
        return true;

    }



    private void ValidateRecipe(Recipe recipe)
    {
        if (recipe == null)
            throw new ArgumentNullException(nameof(recipe));

        if (recipe.Time < 0f)
            throw new InvalidOperationException($"Recipe '{recipe.DisplayName}' has negative Time.");

        if (recipe.Labor < 0f)
            throw new InvalidOperationException($"Recipe '{recipe.DisplayName}' has negative Labor.");
    }

    private bool HasEnoughLabor(Recipe recipe)
    {
        return FreeLabor >= recipe.Labor;
    }


    private bool HasEnoughInputs(Recipe recipe)
    {
        Dictionary<ItemDefinition, float> needed = new();

        for (int i = 0; i < recipe.Inputs.Count; i++)
        {
            ItemAmount input = recipe.Inputs[i];

            if (needed.ContainsKey(input.Item))
                needed[input.Item] += input.Amount;
            else
                needed.Add(input.Item, input.Amount);
        }

        foreach (var pair in needed)
        { 
            float stored =   StorageFunctions.GetStoredAmountByType(StoredItems, pair.Key.ItemType);
            if (stored < pair.Value)
                return false;
        }

        return true;
    }

    private bool HasEnoughOutputSpace(Recipe recipe)
    {
        Dictionary<ItemSuperType, float> producedBySuperType = new();

        for (int i = 0; i < recipe.Outputs.Count; i++)
        {
            ItemAmount output = recipe.Outputs[i];
            ItemSuperType superType = output.Item.ItemSuperType;

            if (producedBySuperType.ContainsKey(superType))
                producedBySuperType[superType] += output.Amount;
            else
                producedBySuperType.Add(superType, output.Amount);
        }

        foreach (var pair in producedBySuperType)
        {
            if (StorageSize == null)
                return false;
            float capacity = StorageFunctions.GetCapacity(StorageSize.Capacities, pair.Key);
            float stored = StorageFunctions.GetStoredAmountBySuperType(StoredItems, pair.Key);
            float free = capacity - stored;

            if (free < pair.Value)
                return false;
        }

        return true;
    }



    private void SendSignal(Recipe recipe, ProductionSignal signal)
    {
        OnProductionSignal?.Invoke(this, recipe, signal);
    }


    private void PutOutputs(Recipe recipe)
    {
        for (int i = 0; i < recipe.Outputs.Count; i++)
        {
            ItemAmount output = recipe.Outputs[i];

            bool stored = StorageFunctions.Put(StorageSize.Capacities, StoredItems, output.CreateRuntimeAmount());

            // Your current Put() already stores the output first and returns false afterward
            // when capacity is exceeded. So on false we only signal; we do not add again.
            if (!stored)
            {
                SendSignal(recipe, ProductionSignal.RecipeOutputStoredOverCapacity);
            }
        }
    }


    private void GetInputs(Recipe recipe)
    {
        for (int i = 0; i < recipe.Inputs.Count; i++)
        {   
            var input = recipe.Inputs[i].CreateRuntimeAmount();
            bool ok = StorageFunctions.CanGet(StoredItems, input);           

            if (!ok)
                throw new InvalidOperationException($"Failed to consume input '{recipe.Inputs[i].Item.name}' for recipe '{recipe.DisplayName}'.");
            else
            {
                 StorageFunctions.Get(StoredItems, input);
            }
        }
    }

}
