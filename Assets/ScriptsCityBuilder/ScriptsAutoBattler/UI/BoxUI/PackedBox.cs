using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public enum PackedAxisSizeMode
{
    TopDown,
    BottomUp
}

public enum PackedBoxMode
{
    VBox,
    HBox
}

[DisallowMultipleComponent]
[AddComponentMenu("Layout/Packed Box")]
[ExecuteAlways]
public sealed class PackedBox : LayoutGroup
{
    [SerializeField]
    private PackedBoxMode packingMode = PackedBoxMode.VBox;

    [SerializeField]
    private PackedAxisSizeMode widthMode = PackedAxisSizeMode.TopDown;

    [SerializeField]
    private PackedAxisSizeMode heightMode = PackedAxisSizeMode.TopDown;

    [SerializeField, Min(0f)]
    private float spacing = 0f;

    [Tooltip("If weighted children request more than 100% of the available pool, normalize them instead of producing negative default space.")]
    [SerializeField]
    private bool normalizeWeightedOverflow = true;

    [Tooltip("If true, non-square child sizes are clamped upward to their minimum size on the packed axis.")]
    [SerializeField, FormerlySerializedAs("respectChildMinHeight")]
    private bool respectChildMinSize = true;

    private readonly List<float> calculatedSizes = new();
    private Vector2Int lastScreenSize;

    public PackedBoxMode PackingMode => packingMode;

    public PackedAxisSizeMode WidthMode => widthMode;

    public PackedAxisSizeMode HeightMode => heightMode;

    public float Spacing
    {
        get => spacing;
        set
        {
            if (Mathf.Approximately(spacing, value))
                return;

            spacing = Mathf.Max(0f, value);
            SetDirty();
        }
    }

    private int MainAxis => packingMode == PackedBoxMode.VBox ? 1 : 0;

    public override void CalculateLayoutInputHorizontal()
    {
        base.CalculateLayoutInputHorizontal();
        float minWidth = CalculateMinSize(0);
        SetLayoutInputForAxis(minWidth, minWidth, -1f, 0);
    }

    public override void CalculateLayoutInputVertical()
    {
        float minHeight = CalculateMinSize(1);
        SetLayoutInputForAxis(minHeight, minHeight, -1f, 1);
    }

    public override void SetLayoutHorizontal()
    {
        SetLayoutOnAxis(0);
    }

    public override void SetLayoutVertical()
    {
        SetLayoutOnAxis(1);
    }

    private void SetLayoutOnAxis(int axis)
    {
        if (IsBottomUp(axis))
            SetSelfSize(axis, CalculateMinSize(axis));

        if (axis == MainAxis)
            SetChildrenOnMainAxis(axis);
        else
            SetChildrenOnCrossAxis(axis);
    }

    private void SetChildrenOnMainAxis(int axis)
    {
        int crossAxis = 1 - axis;

        if (IsBottomUp(crossAxis))
            SetSelfSize(crossAxis, CalculateMinSize(crossAxis));

        CalculateChildSizesOnMainAxis(axis);

        float usedSizeWithoutPadding = Sum(calculatedSizes);

        if (calculatedSizes.Count > 0)
            usedSizeWithoutPadding += spacing * (calculatedSizes.Count - 1);

        float position = GetStartOffset(axis, usedSizeWithoutPadding);

        for (int i = 0; i < rectChildren.Count; i++)
        {
            float size = calculatedSizes[i];
            SetChildAlongAxis(rectChildren[i], axis, position, size);
            position += size + spacing;
        }
    }

    private void SetChildrenOnCrossAxis(int axis)
    {
        float size = GetContentSize(axis);
        float position = GetPaddingStart(axis);

        foreach (RectTransform child in rectChildren)
            SetChildAlongAxis(child, axis, position, size);
    }

    private void CalculateChildSizesOnMainAxis(int axis)
    {
        calculatedSizes.Clear();

        if (IsBottomUp(axis))
        {
            foreach (RectTransform child in rectChildren)
                calculatedSizes.Add(GetChildMinSize(child, axis));

            return;
        }

        int crossAxis = 1 - axis;
        float squareSize = GetContentSize(crossAxis);
        float availableSize = Mathf.Max(0f, GetContentSize(axis) - GetTotalSpacing());
        float squareTotal = 0f;
        float weightedTotal = 0f;
        int defaultCount = 0;

        foreach (RectTransform child in rectChildren)
        {
            PackedBoxElement element = GetElement(child);

            switch (GetChildMode(element))
            {
                case PackedBoxChildMode.Square:
                    squareTotal += squareSize;
                    break;

                case PackedBoxChildMode.Weighted:
                    weightedTotal += element.Weight;
                    break;

                case PackedBoxChildMode.Default:
                    defaultCount++;
                    break;
            }
        }

        float remainingAfterSquares = Mathf.Max(0f, availableSize - squareTotal);
        float defaultPool = remainingAfterSquares * (1f - GetEffectiveWeightTotal(weightedTotal));
        float defaultSize = defaultCount > 0 ? Mathf.Max(0f, defaultPool) / defaultCount : 0f;

        foreach (RectTransform child in rectChildren)
        {
            PackedBoxElement element = GetElement(child);
            PackedBoxChildMode mode = GetChildMode(element);
            float size = mode switch
            {
                PackedBoxChildMode.Square => squareSize,
                PackedBoxChildMode.Weighted => CalculateWeightedSize(element, remainingAfterSquares, weightedTotal),
                _ => defaultSize
            };

            if (respectChildMinSize && mode != PackedBoxChildMode.Square)
                size = Mathf.Max(size, GetChildMinSize(child, axis));

            calculatedSizes.Add(Mathf.Max(0f, size));
        }
    }

    private float CalculateMinSize(int axis)
    {
        return axis == MainAxis
            ? CalculateMainAxisMinSize(axis)
            : CalculateCrossAxisMinSize(axis);
    }

    private float CalculateMainAxisMinSize(int axis)
    {
        float minSize = GetPaddingTotal(axis) + GetTotalSpacing();

        foreach (RectTransform child in rectChildren)
            minSize += GetPackedMinSize(child, axis);

        return minSize;
    }

    private float CalculateCrossAxisMinSize(int axis)
    {
        float maxChildSize = 0f;

        foreach (RectTransform child in rectChildren)
            maxChildSize = Mathf.Max(maxChildSize, GetPackedMinSize(child, axis));

        return GetPaddingTotal(axis) + maxChildSize;
    }

    private float GetPackedMinSize(RectTransform child, int axis)
    {
        if (IsBottomUp(axis))
            return GetChildMinSize(child, axis);

        PackedBoxElement element = GetElement(child);

        if (GetChildMode(element) != PackedBoxChildMode.Square)
            return GetChildMinSize(child, axis);

        return Mathf.Max(GetChildMinSize(child, 0), GetChildMinSize(child, 1));
    }

    private float CalculateWeightedSize(PackedBoxElement element, float pool, float totalWeight)
    {
        if (element == null || element.Weight <= 0f)
            return 0f;

        if (normalizeWeightedOverflow && totalWeight > 1f)
            return pool * (element.Weight / totalWeight);

        return pool * element.Weight;
    }

    private float GetEffectiveWeightTotal(float totalWeight)
    {
        return normalizeWeightedOverflow && totalWeight > 1f ? 1f : totalWeight;
    }

    private bool IsBottomUp(int axis)
    {
        return GetAxisMode(axis) == PackedAxisSizeMode.BottomUp;
    }

    private PackedAxisSizeMode GetAxisMode(int axis)
    {
        return axis == 0 ? widthMode : heightMode;
    }

    private static PackedBoxChildMode GetChildMode(PackedBoxElement element)
    {
        return element != null ? element.Mode : PackedBoxChildMode.Default;
    }

    private static PackedBoxElement GetElement(RectTransform child)
    {
        return child.GetComponent<PackedBoxElement>();
    }

    private static float GetChildMinSize(RectTransform child, int axis)
    {
        float value = axis == 0
            ? LayoutUtility.GetMinWidth(child)
            : LayoutUtility.GetMinHeight(child);

        return Mathf.Max(0f, value);
    }

    private float GetContentSize(int axis)
    {
        return Mathf.Max(0f, rectTransform.rect.size[axis] - GetPaddingTotal(axis));
    }

    private float GetPaddingStart(int axis)
    {
        return axis == 0 ? padding.left : padding.top;
    }

    private float GetPaddingTotal(int axis)
    {
        return axis == 0
            ? padding.left + padding.right
            : padding.top + padding.bottom;
    }

    private float GetTotalSpacing()
    {
        return rectChildren.Count > 0 ? spacing * (rectChildren.Count - 1) : 0f;
    }

    private static float Sum(List<float> values)
    {
        float total = 0f;

        foreach (float value in values)
            total += value;

        return total;
    }

    private void SetSelfSize(int axis, float size)
    {
        size = Mathf.Max(0f, size);

        if (Mathf.Approximately(rectTransform.rect.size[axis], size))
            return;

        RectTransform.Axis rectAxis = axis == 0
            ? RectTransform.Axis.Horizontal
            : RectTransform.Axis.Vertical;

        rectTransform.SetSizeWithCurrentAnchors(rectAxis, size);
    }

#if UNITY_EDITOR
    protected override void OnValidate()
    {
        base.OnValidate();

        spacing = Mathf.Max(0f, spacing);
        SetDirty();
    }
#endif

    protected override void OnEnable()
    {
        base.OnEnable();
        RememberScreenSize();
    }

    protected override void OnRectTransformDimensionsChange()
    {
        base.OnRectTransformDimensionsChange();
        SetDirty();
    }

    private void Update()
    {
        if (Screen.width == lastScreenSize.x && Screen.height == lastScreenSize.y)
            return;

        RememberScreenSize();
        SetDirty();
    }

    private void RememberScreenSize()
    {
        lastScreenSize = new Vector2Int(Screen.width, Screen.height);
    }
}
