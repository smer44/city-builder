using UnityEngine;
using UnityEngine.InputSystem;

public class PlacementSelectedMouseState2D : AbstractMouseState
{
    public GameObject SelectedPrefab { get; private set; }

    public override string InfoStr()
    {
        return $"{nameof(PlacementSelectedMouseState2D)}: {(SelectedPrefab != null ? SelectedPrefab.name : "None")}";
    }

    public void SelectPrefab(GameObject prefab)
    {
        SelectedPrefab = prefab;
    }

    public override void OnEnter(AbstractMouseController owner)
    {
        owner.CloseInfo();
        owner.clickedObject = null;
    }

    public override void Update(AbstractMouseController owner)
    {
        if (owner is MouseController2D controller
            && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            controller.ChangeState(controller.DefaultMouseState);
        }
    }

    public override void OnLeftMouseClick(AbstractMouseController owner)
    {
        if (owner is not MouseController2D controller || SelectedPrefab == null || Mouse.current == null)
            return;

        Vector2 screenPosition = Mouse.current.position.ReadValue();
        if (controller.CanPlaceAtScreenPosition(screenPosition))
            controller.FieldPlacer.TryPlaceAtScreenPosition(SelectedPrefab, screenPosition);
    }

    public override void OnRightMouseClick(AbstractMouseController owner) { }
    public override void OnMiddleMouseClick(AbstractMouseController owner) { }

    public override void OnExit(AbstractMouseController owner)
    {
        SelectedPrefab = null;
    }
}
