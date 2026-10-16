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
    Completed,
    Interrupted

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
    private Dictionary<ItemSuperType, float> reservedSupertypeOutputs = new();
    // Inputs committed to running executions; already consumed from storage at start.
    private Dictionary<ItemType, float> reservedInputs = new();

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
                ReserveOutputs(recipe);
                ReserveInputs(recipe);
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
    /// Interrupts executions that can no longer continue and returns their unused inputs.
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

            if (!CanContinueRecipe(execution))
            {
                EndRecipeInterrupted(execution);
                Work.RemoveAt(i);
                SendSignal(execution.Recipe, ProductionSignal.Interrupted);
                continue;
            }

            execution.Timer -= deltaTime;

            if (execution.Timer > 0f)
                continue;

            EndRecipeGood(execution);
            Work.RemoveAt(i);
            SendSignal(execution.Recipe, ProductionSignal.Completed);
        }

        if (isProducing)
            StartQueuedRecipes();
    }

    protected virtual bool CanContinueRecipe(RecipeExecution execution)
    {
        return true;
    }

    /// <summary>
    /// Returns the unused inputs, assuming consumption is proportional to elapsed execution time.
    /// </summary>
    private List<AmountOf<ItemDefinition>> GetRemainingGoods(RecipeExecution execution)
    {
        Recipe recipe = execution.Recipe;
        float remainingFraction = recipe.Time > 0f ? Mathf.Clamp01(execution.Timer / recipe.Time) : 0f;
        List<AmountOf<ItemDefinition>> remainingGoods = new();

        for (int i = 0; i < recipe.Inputs.Count; i++)
        {
            AmountOf<ItemDefinition> remaining = recipe.Inputs[i].CreateRuntimeAmount();
            remaining.Amount *= remainingFraction;
            if (remaining.Amount > 0f)
                remainingGoods.Add(remaining);
        }

        return remainingGoods;
    }

    private void EndRecipeInterrupted(RecipeExecution execution)
    {
        Recipe recipe = execution.Recipe;
        List<AmountOf<ItemDefinition>> remainingGoods = GetRemainingGoods(execution);

        for (int i = 0; i < remainingGoods.Count; i++)
        {
            AmountOf<ItemDefinition> remaining = remainingGoods[i];
            if (!StorageFunctions.Put(this, remaining))
                Debug.LogWarning($"Could not restore {remaining.Amount} of input '{remaining.Item.name}' after interrupting recipe '{recipe.DisplayName}': insufficient storage capacity.");
        }

        FreeReservedOutputs(recipe);
        usedLabor -= recipe.Labor;
    }


    private void EndRecipeGood(RecipeExecution execution)
    {
        Recipe recipe = execution.Recipe;
        FreeReservedOutputs(recipe);
        PutOutputs(recipe);
        usedLabor -= recipe.Labor;

        //wrong
        if (usedLabor < 0f)
            usedLabor = 0f;

    }

    /// <summary>
    /// Tries to start a recipe execution:
    /// - checks labor
    /// - checks inputs
    /// - checks planned output space
    /// - reserves output space and inputs
    /// - consumes inputs
    /// - reserves labor
    /// - adds execution to Work
    /// </summary>
    public bool TryStartRecipe(Recipe recipe)
    {
        ValidateRecipe(recipe);
        if (!CanStartRecipe(recipe))
            return false;

        ReserveOutputs(recipe);
        //ReserveInputs(recipe);
        StartRecipe(recipe);
        return true;
    }

    private void ReserveOutputs(Recipe recipe)
    {
        Dictionary<ItemSuperType, float> outputBySuperType = StorageFunctions.GetOutputBySuperType(recipe.Outputs);
        StorageFunctions.AddDictionaryValues(reservedSupertypeOutputs, outputBySuperType);
    }

    private void FreeReservedOutputs(Recipe recipe)
    {
        Dictionary<ItemSuperType, float> outputBySuperType = StorageFunctions.GetOutputBySuperType(recipe.Outputs);
        StorageFunctions.SubstractDictionaryValues(reservedSupertypeOutputs, outputBySuperType);
    }

    private void ReserveInputs(Recipe recipe)
    {
        reservedInputs = StorageFunctions.GetInputByType(recipe.Inputs, reservedInputs);
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
        foreach (ItemAmount input in recipe.Inputs)
            RegisterItemDefinition(input.Item);
        foreach (ItemAmount output in recipe.Outputs)
            RegisterItemDefinition(output.Item);

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
        // Running executions' inputs have already been removed from StoredItems.
        Dictionary<ItemType, float> inputsDict = StorageFunctions.ListToDict(recipe.Inputs);

        foreach (var pair in inputsDict)
        { 
            StoredItems.TryGetValue(pair.Key, out float stored);
            if (stored < pair.Value)
                return false;
        }

        return true;
    }

    private bool HasEnoughOutputSpace(Recipe recipe)
    {
        Dictionary<ItemSuperType, float> outputBySuperType = StorageFunctions.GetOutputBySuperType(recipe.Outputs);

        foreach (var pair in outputBySuperType)
        {
            float capacity = StorageFunctions.GetCapacity(StorageCapacities, pair.Key);
            float stored = StorageFunctions.GetStoredAmountBySuperType(this, pair.Key);
            reservedSupertypeOutputs.TryGetValue(pair.Key, out float reserved);
            float free = capacity - stored - reserved;

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

            bool stored = StorageFunctions.Put(this, output.CreateRuntimeAmount());
            if (!stored)
            {
                SendSignal(recipe, ProductionSignal.RecipeOutputStoredOverCapacity);
            }
        }
    }


    private void GetInputs(Recipe recipe)
    {
        Dictionary<ItemType, float> inputsDict = StorageFunctions.ListToDict(recipe.Inputs);
        foreach (var pair in inputsDict)
        {
            StoredItems.TryGetValue(pair.Key, out float stored);
            if (stored < pair.Value)
                throw new InvalidOperationException($"Failed to consume input type '{pair.Key}' for recipe '{recipe.DisplayName}'.");
        }

        StorageFunctions.SubstractDictionaryValues(StoredItems, inputsDict);
    }

}
