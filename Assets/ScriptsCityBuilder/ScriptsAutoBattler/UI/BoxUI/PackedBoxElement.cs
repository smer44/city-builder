using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;
using UnityEngine.UI;

public enum PackedBoxChildMode
{
    Default,
    Weighted,
    Square
}

public enum PackedBoxSizeUnit
{
    Pixels,
    ScreenWidthFraction,
    ScreenHeightFraction,
    ScreenMinSideFraction,
    ScreenMaxSideFraction
}

[Serializable]
public struct PackedBoxSizeValue
{
    public PackedBoxSizeUnit unit;

    [Min(0f)]
    public float value;

    public PackedBoxSizeValue(PackedBoxSizeUnit unit, float value)
    {
        this.unit = unit;
        this.value = value;
    }

    public float Resolve()
    {
        return unit switch
        {
            PackedBoxSizeUnit.Pixels => value,
            PackedBoxSizeUnit.ScreenWidthFraction => Screen.width * value,
            PackedBoxSizeUnit.ScreenHeightFraction => Screen.height * value,
            PackedBoxSizeUnit.ScreenMinSideFraction => Mathf.Min(Screen.width, Screen.height) * value,
            PackedBoxSizeUnit.ScreenMaxSideFraction => Mathf.Max(Screen.width, Screen.height) * value,
            _ => value
        };
    }
}

[DisallowMultipleComponent]
[AddComponentMenu("Layout/Packed Box Element")]
public sealed class PackedBoxElement : UIBehaviour, ILayoutElement
{
    [SerializeField]
    private PackedBoxChildMode mode = PackedBoxChildMode.Default;

    [Tooltip("Used only in Weighted mode. 0.4 means 40% of the space left after square children.")]
    [SerializeField, Min(0f)]
    private float weight = 0.25f;

    [Header("Optional minimum size")]
    [SerializeField]
    private bool hasMinWidth;

    [SerializeField, FormerlySerializedAs("minWidth")]
    private PackedBoxSizeValue minWidthValue = new PackedBoxSizeValue(PackedBoxSizeUnit.Pixels, 0f);

    [SerializeField]
    private bool hasMinHeight;

    [SerializeField, FormerlySerializedAs("minHeight")]
    private PackedBoxSizeValue minHeightValue = new PackedBoxSizeValue(PackedBoxSizeUnit.Pixels, 0f);

    [Header("Unity layout priority")]
    [SerializeField]
    private int priority = 1;

    public PackedBoxChildMode Mode => mode;

    public float Weight => Mathf.Max(0f, weight);

    public bool HasMinWidth => hasMinWidth;

    public bool HasMinHeight => hasMinHeight;

    public float ResolvedMinWidth => hasMinWidth ? minWidthValue.Resolve() : -1f;

    public float ResolvedMinHeight => hasMinHeight ? minHeightValue.Resolve() : -1f;

    public float minWidth => ResolvedMinWidth;

    public float minHeight => ResolvedMinHeight;

    public float preferredWidth => -1f;

    public float preferredHeight => -1f;

    public float flexibleWidth => -1f;

    public float flexibleHeight => -1f;

    public int layoutPriority => priority;

    public void CalculateLayoutInputHorizontal()
    {
    }

    public void CalculateLayoutInputVertical()
    {
    }

#if UNITY_EDITOR
    protected override void OnValidate()
    {
        base.OnValidate();

        weight = Mathf.Max(0f, weight);
        minWidthValue.value = Mathf.Max(0f, minWidthValue.value);
        minHeightValue.value = Mathf.Max(0f, minHeightValue.value);

        SetDirty();
    }
#endif

    protected override void OnEnable()
    {
        base.OnEnable();
        SetDirty();
    }

    protected override void OnDisable()
    {
        SetDirty();
        base.OnDisable();
    }

    private void SetDirty()
    {
        if (!IsActive())
            return;

        if (transform is RectTransform rectTransform)
            LayoutRebuilder.MarkLayoutForRebuild(rectTransform);
    }
}
