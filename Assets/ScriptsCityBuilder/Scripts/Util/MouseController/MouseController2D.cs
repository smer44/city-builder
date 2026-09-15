using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class MouseController2D : AbstractMouseController
{
    [SerializeField] private Field2DPlaser fieldPlacer;

    private readonly List<RaycastResult> raycastResults = new();

    public Field2DPlaser FieldPlacer => fieldPlacer;
    public DefaultMouseState2D DefaultMouseState { get; } = new();
    public PlacementSelectedMouseState2D PlacementSelectedMouseState { get; } = new();

    private void Awake()
    {
        ChangeState(DefaultMouseState);
    }

    // SelectionBar buttons pass their prefab through this UnityEvent callback.
    public void SelectPrefabToPlace(GameObject prefab)
    {
        if (prefab == null)
        {
            ChangeState(DefaultMouseState);
            return;
        }

        PlacementSelectedMouseState.SelectPrefab(prefab);
        ChangeState(PlacementSelectedMouseState);
    }

    public bool CanPlaceAtScreenPosition(Vector2 screenPosition)
    {
        if (fieldPlacer == null || !fieldPlacer.isActiveAndEnabled || fieldPlacer.ParentTransform == null)
            return false;

        GameObject hit = FindClickedObject(screenPosition);
        return hit != null
            && hit.transform.IsChildOf(fieldPlacer.ParentTransform)
            && hit.GetComponentInParent<Selectable>() == null
            && hit.GetComponentInParent<RessourceOnSpawnUI>() == null
            && hit.GetComponentInParent<ProductionNodePanelUI>() == null;
    }

    protected override GameObject FindClickedObject(Vector2 screenPosition)
    {
        EventSystem eventSystem = EventSystem.current;
        if (eventSystem == null)
            return null;

        PointerEventData pointerData = new(eventSystem)
        {
            position = screenPosition,
            button = PointerEventData.InputButton.Left,
        };

        eventSystem.RaycastAll(pointerData, raycastResults);
        return raycastResults.Count == 0 ? null : raycastResults[0].gameObject;
    }

    protected override void OnDisable()
    {
        ChangeState(DefaultMouseState);
        base.OnDisable();
    }
}
