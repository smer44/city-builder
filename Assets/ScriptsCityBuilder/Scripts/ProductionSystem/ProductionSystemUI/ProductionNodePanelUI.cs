using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform))]
public class ProductionNodePanelUI : MonoBehaviour, OnSpawn<AbstractMouseController>, OnSpawn<SpawnInfoOnClick2D>
{
    [SerializeField] private UIGameObjectFactory.PanelFormatting panelFormatting = new()
    {
        Width = 380f,
        Height = 108f
    };
    [SerializeField] private UIGameObjectFactory.RowFormatting rowFormatting = new()
    {
        ItemWidthWeights = new[] { 2.5f, 1.2f, 1.3f }
    };
    [SerializeField, Min(24f)] private float rowHeight = 36f;

    private sealed class RecipeRow
    {
        public Recipe Recipe;
        public TMP_Text Name;
        public TMP_Text Pending;
        public Button AddButton;
    }

    private ProductionNode currentNode;
    private TMP_Text nameText;
    private readonly List<Recipe> displayedRecipes = new();
    private readonly List<RecipeRow> rows = new();
    private readonly List<GameObject> generatedRows = new();

    public static ProductionNodePanelUI Create(Transform parent)
    {
        GameObject obj = new("ProductionNodeUI", typeof(RectTransform));
        obj.layer = parent != null ? parent.gameObject.layer : LayerMask.NameToLayer("UI");
        obj.transform.SetParent(parent, false);
        RectTransform rect = (RectTransform)obj.transform;
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0f, 1f);
        ProductionNodePanelUI ui = obj.AddComponent<ProductionNodePanelUI>();
        ui.GenerateUI();
        return ui;
    }

    private void Awake() => GenerateUI();
    private void Update() => Refresh();

    public void OnSpawn(AbstractMouseController source)
    {
        ShowNode(source == null || source.clickedObject == null
            ? null : source.clickedObject.GetComponentInParent<ProductionNode>());
    }

    public void OnSpawn(SpawnInfoOnClick2D source)
    {
        ShowNode(source == null ? null : source.GetComponentInParent<ProductionNode>());
    }

    public void ShowNode(ProductionNode node)
    {
        currentNode = node;
        Refresh();
        gameObject.SetActive(true);
    }

    public void Hide()
    {
        currentNode = null;
        gameObject.SetActive(false);
    }

    public void AddRecipe(Recipe recipe)
    {
        if (currentNode != null)
            currentNode.QueueRecipe(recipe);
        Refresh();
    }

    public void Refresh()
    {
        GenerateUI();
        nameText.text = currentNode == null ? "Unknown production node" : currentNode.name;
        foreach (RecipeRow row in rows)
        {
            row.Name.text = string.IsNullOrWhiteSpace(row.Recipe.DisplayName)
                ? row.Recipe.name : row.Recipe.DisplayName;
            row.Pending.text = currentNode.GetPendingRecipeCount(row.Recipe).ToString();
            row.AddButton.interactable = currentNode != null;
        }
    }

    /// <summary>Rebuilds only when the recipe list changes; count updates reuse the existing rows.</summary>
    public void GenerateUI()
    {
        List<Recipe> recipes = currentNode == null ? null : currentNode.AvailableRecipes;
        if (nameText != null && RecipesMatch(recipes))
            return;

        foreach (GameObject row in generatedRows)
        {
            row.SetActive(false);
            if (Application.isPlaying)
                Destroy(row);
            else
                DestroyImmediate(row);
        }
        generatedRows.Clear();
        displayedRecipes.Clear();
        rows.Clear();
        if (recipes != null)
            displayedRecipes.AddRange(recipes);

        foreach (Recipe recipe in displayedRecipes)
        {
            if (recipe != null && !rows.Exists(row => row.Recipe == recipe))
                rows.Add(new RecipeRow { Recipe = recipe });
        }

        int rowCount = 2 + Mathf.Max(1, rows.Count);
        panelFormatting.Height = rowCount * rowHeight;
        panelFormatting.RowHeightPercentages = new float[rowCount];
        for (int i = 0; i < rowCount; i++)
            panelFormatting.RowHeightPercentages[i] = 100f / rowCount;

        UIGameObjectFactory.CreatePanel((RectTransform)transform, panelFormatting, rowFormatting);
        var titleFormatting = new UIGameObjectFactory.RowFormatting
        {
            Font = rowFormatting.Font, FontSize = 18f,
            FontStyle = FontStyles.Bold, TextColor = Color.white
        };
        nameText = CreateRow(0, titleFormatting, "Production node").Labels[0];
        UIGameObjectFactory.Row header = CreateRow(1, rowFormatting, "Recipe", "Pending", "Produce");
        foreach (TMP_Text label in header.Labels)
            label.fontStyle = FontStyles.Bold;

        if (rows.Count == 0)
        {
            titleFormatting.FontSize = rowFormatting.FontSize;
            titleFormatting.FontStyle = FontStyles.Normal;
            CreateRow(2, titleFormatting, "No recipes available.");
        }

        for (int i = 0; i < rows.Count; i++)
        {
            RecipeRow row = rows[i];
            UIGameObjectFactory.Row cells = CreateRow(i + 2, rowFormatting, "", "0", "Add one");
            row.Name = cells.Labels[0];
            row.Pending = cells.Labels[1];
            row.Pending.alignment = TextAlignmentOptions.Center;
            row.AddButton = UIGameObjectFactory.ReplaceLabelWithButton(cells.Labels[2], () => AddRecipe(row.Recipe));
            RectTransform buttonRect = (RectTransform)row.AddButton.transform;
            buttonRect.offsetMin = new Vector2(6f, 4f);
            buttonRect.offsetMax = new Vector2(-6f, -4f);
        }
    }

    private UIGameObjectFactory.Row CreateRow(int index, UIGameObjectFactory.RowFormatting formatting, params string[] entries)
    {
        UIGameObjectFactory.Row row = UIGameObjectFactory.CreateRow(
            (RectTransform)transform, index, panelFormatting, formatting, entries);
        generatedRows.Add(row.Root.gameObject);
        return row;
    }

    private bool RecipesMatch(List<Recipe> recipes)
    {
        if (displayedRecipes.Count != (recipes?.Count ?? 0))
            return false;
        foreach (RecipeRow row in rows)
            if (row.Recipe == null)
                return false;
        for (int i = 0; i < displayedRecipes.Count; i++)
            if (displayedRecipes[i] != recipes[i])
                return false;
        return true;
    }
}
