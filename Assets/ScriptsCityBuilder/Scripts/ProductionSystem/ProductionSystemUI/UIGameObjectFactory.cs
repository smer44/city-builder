using System;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public static class UIGameObjectFactory
{
    [Serializable]
    public sealed class PanelFormatting
    {
        [Min(1f)] public float Width = 220f;
        [Min(1f)] public float Height = 156f;
        [Tooltip("Each row's height as 0-100 percent of the full panel height, from top to bottom. The sum must not exceed 100.")]
        public float[] RowHeightPercentages = { 100f };
        public Color BackgroundColor = new(0.08f, 0.09f, 0.1f, 0.94f);
    }

    [Serializable]
    public sealed class RowFormatting
    {
        public TMP_FontAsset Font;
        [Min(1f)] public float FontSize = 14f;
        public FontStyles FontStyle = FontStyles.Normal;
        public Color TextColor = new(0.88f, 0.9f, 0.92f, 1f);
        [Tooltip("Relative column widths, e.g. 2, 1, 1 gives 50%, 25%, 25%. Empty uses equal widths.")]
        public float[] ItemWidthWeights = Array.Empty<float>();
    }

    public sealed class Panel
    {
        public RectTransform Root { get; }
        public Row[] Rows { get; }

        internal Panel(RectTransform root, Row[] rows)
        {
            Root = root;
            Rows = rows;
        }
    }

    public sealed class Row
    {
        public RectTransform Root { get; }
        public TextMeshProUGUI[] Labels { get; }

        internal Row(RectTransform root, TextMeshProUGUI[] labels)
        {
            Root = root;
            Labels = labels;
        }
    }

    /// <summary>
    /// Creates a panel under a Canvas descendant, using one row format for all rows.
    /// Omit rows to create an empty panel and populate it with individually formatted CreateRow calls.
    /// </summary>
    public static Panel CreatePanel(Transform parent, string name, PanelFormatting panelFormatting,
        RowFormatting rowFormatting, params string[][] rows)
    {
        ValidateRows(panelFormatting, rowFormatting, rows);
        RectTransform root = CreateRect(parent, name);
        root.anchorMin = root.anchorMax = new Vector2(0.5f, 0.5f);
        root.pivot = new Vector2(0f, 1f);
        return CreatePanel(root, panelFormatting, rowFormatting, rows);
    }

    /// <summary>Builds rows on an existing panel root, preserving its position and anchors.</summary>
    public static Panel CreatePanel(RectTransform root, PanelFormatting panelFormatting,
        RowFormatting rowFormatting, params string[][] rows)
    {
        if (root == null)
            throw new ArgumentNullException(nameof(root));
        ValidateRows(panelFormatting, rowFormatting, rows);

        root.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, panelFormatting.Width);
        root.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, panelFormatting.Height);
        Image background = root.GetComponent<Image>();
        if (background == null)
            background = root.gameObject.AddComponent<Image>();
        background.color = panelFormatting.BackgroundColor;

        Row[] generatedRows = new Row[rows.Length];
        for (int i = 0; i < rows.Length; i++)
            generatedRows[i] = CreateRow(root, i, panelFormatting, rowFormatting, rows[i]);
        return new Panel(root, generatedRows);
    }

    /// <summary>
    /// Creates an individual row in the indexed percentage slot. Rows may use different fonts and weights.
    /// Anchors keep row heights and column widths proportional when the panel is resized.
    /// </summary>
    public static Row CreateRow(RectTransform panel, int rowIndex, PanelFormatting panelFormatting,
        RowFormatting rowFormatting, params string[] entries)
    {
        if (panel == null)
            throw new ArgumentNullException(nameof(panel));
        ValidatePanelFormatting(panelFormatting);
        float totalWeight = ValidateRowFormatting(rowFormatting, entries);
        if (rowIndex < 0 || rowIndex >= panelFormatting.RowHeightPercentages.Length)
            throw new ArgumentOutOfRangeException(nameof(rowIndex));

        float top = 1f;
        for (int i = 0; i < rowIndex; i++)
            top -= panelFormatting.RowHeightPercentages[i] / 100f;
        RectTransform row = CreateRect(panel, $"Row {rowIndex}");
        StretchRect(row, new Vector2(0f, top - panelFormatting.RowHeightPercentages[rowIndex] / 100f),
            new Vector2(1f, top));

        TextMeshProUGUI[] labels = new TextMeshProUGUI[entries.Length];
        bool equalWidths = rowFormatting.ItemWidthWeights == null || rowFormatting.ItemWidthWeights.Length == 0;
        float left = 0f;
        for (int i = 0; i < entries.Length; i++)
        {
            float right = left + (equalWidths ? 1f : rowFormatting.ItemWidthWeights[i]) / totalWeight;
            RectTransform cell = CreateRect(row, $"Cell {i}");
            StretchRect(cell, new Vector2(left, 0f), new Vector2(i == entries.Length - 1 ? 1f : right, 1f));
            TextMeshProUGUI label = cell.gameObject.AddComponent<TextMeshProUGUI>();
            label.text = entries[i] ?? string.Empty;
            label.font = rowFormatting.Font != null ? rowFormatting.Font : TMP_Settings.defaultFontAsset;
            label.fontSize = rowFormatting.FontSize;
            label.fontStyle = rowFormatting.FontStyle;
            label.color = rowFormatting.TextColor;
            label.alignment = TextAlignmentOptions.MidlineLeft;
            label.margin = new Vector4(10f, 0f, 10f, 0f);
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.overflowMode = TextOverflowModes.Ellipsis;
            label.richText = false;
            label.raycastTarget = false;
            labels[i] = label;
            left = right;
        }
        return new Row(row, labels);
    }

    /// <summary>
    /// Replaces a label's slot with a button, retaining the label instance, text, font and column position.
    /// The returned button can be configured further, and existing label references remain usable.
    /// </summary>
    public static Button ReplaceLabelWithButton(TextMeshProUGUI label, UnityAction onClicked)
    {
        if (label == null)
            throw new ArgumentNullException(nameof(label));

        RectTransform original = label.rectTransform;
        RectTransform rect = CreateRect(original.parent, $"{label.name} Button");
        rect.SetSiblingIndex(original.GetSiblingIndex());
        rect.anchorMin = original.anchorMin;
        rect.anchorMax = original.anchorMax;
        rect.pivot = original.pivot;
        rect.sizeDelta = original.sizeDelta;
        rect.anchoredPosition3D = original.anchoredPosition3D;
        rect.localRotation = original.localRotation;
        rect.localScale = original.localScale;

        Image background = rect.gameObject.AddComponent<Image>();
        background.color = new Color(0.22f, 0.25f, 0.29f, 1f);
        Button button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = background;
        if (onClicked != null)
            button.onClick.AddListener(onClicked);

        original.SetParent(rect, false);
        original.localRotation = Quaternion.identity;
        original.localScale = Vector3.one;
        StretchRect(original, Vector2.zero, Vector2.one);
        label.alignment = TextAlignmentOptions.Center;
        label.raycastTarget = false;
        return button;
    }

    private static RectTransform CreateRect(Transform parent, string name)
    {
        GameObject obj = new(name, typeof(RectTransform));
        obj.layer = parent != null ? parent.gameObject.layer : LayerMask.NameToLayer("UI");
        obj.transform.SetParent(parent, false);
        return (RectTransform)obj.transform;
    }

    private static void StretchRect(RectTransform rect, Vector2 min, Vector2 max)
    {
        rect.anchorMin = min;
        rect.anchorMax = max;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
    }

    private static void ValidateRows(PanelFormatting panelFormatting, RowFormatting rowFormatting, string[][] rows)
    {
        ValidatePanelFormatting(panelFormatting);
        if (rows == null || (rows.Length != 0 && rows.Length != panelFormatting.RowHeightPercentages.Length))
            throw new ArgumentException("Supply one row for each row height percentage.", nameof(rows));
        foreach (string[] row in rows)
            ValidateRowFormatting(rowFormatting, row);
    }

    private static void ValidatePanelFormatting(PanelFormatting formatting)
    {
        if (formatting == null)
            throw new ArgumentNullException(nameof(formatting));
        RequirePositiveFinite(formatting.Width, nameof(formatting.Width));
        RequirePositiveFinite(formatting.Height, nameof(formatting.Height));
        if (formatting.RowHeightPercentages == null || formatting.RowHeightPercentages.Length == 0)
            throw new ArgumentException("Supply at least one row height percentage.", nameof(formatting));
        double total = 0;
        foreach (float percentage in formatting.RowHeightPercentages)
        {
            RequirePositiveFinite(percentage, nameof(formatting.RowHeightPercentages));
            total += percentage;
        }
        if (total > 100.0001)
            throw new ArgumentException("Row height percentages must add up to at most 100.", nameof(formatting));
    }

    private static float ValidateRowFormatting(RowFormatting formatting, string[] entries)
    {
        if (formatting == null)
            throw new ArgumentNullException(nameof(formatting));
        RequirePositiveFinite(formatting.FontSize, nameof(formatting.FontSize));
        if (entries == null || entries.Length == 0)
            throw new ArgumentException("Supply at least one text entry.", nameof(entries));
        float[] weights = formatting.ItemWidthWeights;
        if (weights == null || weights.Length == 0)
            return entries.Length;
        if (weights.Length != entries.Length)
            throw new ArgumentException("Supply one width weight per text entry.", nameof(formatting));
        float total = 0f;
        foreach (float weight in weights)
        {
            RequirePositiveFinite(weight, nameof(formatting.ItemWidthWeights));
            total += weight;
        }
        RequirePositiveFinite(total, nameof(formatting.ItemWidthWeights));
        return total;
    }

    private static void RequirePositiveFinite(float value, string name)
    {
        if (float.IsNaN(value) || float.IsInfinity(value) || value <= 0f)
            throw new ArgumentOutOfRangeException(name, "Value must be finite and greater than zero.");
    }

    public static TextMeshProUGUI CreateTransferLabel(
    RectTransform root,
    TextMeshProUGUI labelInstance,
    string labelText)
    {
        //TextMeshProUGUI labelInstance = Instantiate(labelPrefab, root, false);
        labelInstance.text = labelText;
        //labelInstance.enableWordWrapping = false;
        labelInstance.overflowMode = TextOverflowModes.Ellipsis;

        LayoutElement layout = labelInstance.GetComponent<LayoutElement>();
        if (layout == null)
            layout = labelInstance.gameObject.AddComponent<LayoutElement>();

        layout.minHeight = 24f;
        layout.preferredHeight = 30f;
        layout.flexibleWidth = 1f;

        return labelInstance;
    }

    public static Button CreateTransferButton(
        RectTransform root,
        Button buttonInstance,
        string buttonText,
        UnityEngine.Events.UnityAction onClicked)
    {
        //Button buttonInstance = Instantiate(buttonPrefab, root, false);

        buttonInstance.onClick.RemoveAllListeners();
        buttonInstance.onClick.AddListener(onClicked);

        TextMeshProUGUI buttonLabel = buttonInstance.GetComponentInChildren<TextMeshProUGUI>();
        if (buttonLabel != null)
            buttonLabel.text = buttonText;

        LayoutElement layout = buttonInstance.GetComponent<LayoutElement>();
        if (layout == null)
            layout = buttonInstance.gameObject.AddComponent<LayoutElement>();

        layout.minHeight = 30f;
        layout.preferredHeight = 36f;
        layout.flexibleWidth = 1f;

        return buttonInstance;
    }


}
