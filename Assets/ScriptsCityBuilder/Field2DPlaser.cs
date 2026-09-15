using UnityEngine;

public class Field2DPlaser : AbstractFieldPlacer
{
    [SerializeField] private RectTransform parentRectTransform;

    public RectTransform ParentRectTransform => parentRectTransform;
    public override Transform ParentTransform => parentRectTransform;

    public override Vector3 GetPosition(Transform target)
    {
        if (target is RectTransform rectTransform)
            return rectTransform.anchoredPosition;

        return target.localPosition;
    }

    public override bool MoveTowards(UnitOnField unit, Vector3 targetPosition, float delta)
    {
        if (unit == null)
            return true;

        Vector2 currentPosition = GetPosition(unit.transform);
        Vector2 targetPosition2D = targetPosition;
        float reachDistanceSqr = unit.TargetReachDistance * unit.TargetReachDistance;

        if ((targetPosition2D - currentPosition).sqrMagnitude <= reachDistanceSqr)
            return true;

        float maxDistanceDelta = unit.MoveSpeed * delta;
        Vector2 nextPosition = Vector2.MoveTowards(currentPosition, targetPosition2D, maxDistanceDelta);
        SetUnitPosition(unit, nextPosition);

        return (targetPosition2D - nextPosition).sqrMagnitude <= reachDistanceSqr;
    }

    protected override bool IsPointerInsideField(Vector2 screenPosition)
    {
        if (parentRectTransform == null)
        {
            Debug.LogWarning($"{nameof(Field2DPlaser)}: parent RectTransform is not assigned.");
            return false;
        }

        return RectTransformUtility.RectangleContainsScreenPoint(
            parentRectTransform,
            screenPosition,
            GetEventCamera());
    }

    protected override bool TryGetPlacementPosition(Vector2 screenPosition, out Vector3 position)
    {
        position = default;

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                parentRectTransform,
                screenPosition,
                GetEventCamera(),
                out Vector2 localPoint))
        {
            return false;
        }

        position = new Vector3(localPoint.x, localPoint.y, 0f);
        return true;
    }

    protected override void SetPlacedObjectPosition(GameObject placedObject, Vector3 position)
    {
        if (placedObject.transform is RectTransform placedRectTransform)
        {
            placedRectTransform.anchoredPosition = new Vector2(position.x, position.y);
        }
        else
        {
            placedObject.transform.localPosition = position;
        }
    }

    private Camera GetEventCamera()
    {
        Canvas canvas = parentRectTransform.GetComponentInParent<Canvas>();

        if (canvas == null || canvas.renderMode == RenderMode.ScreenSpaceOverlay)
            return null;

        return canvas.worldCamera != null ? canvas.worldCamera : Camera.main;
    }
}
