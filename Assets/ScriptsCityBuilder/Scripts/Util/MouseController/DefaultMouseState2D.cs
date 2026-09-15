using UnityEngine.InputSystem;

public class DefaultMouseState2D : AbstractMouseState
{
    public override string InfoStr() => nameof(DefaultMouseState2D);

    public override void OnLeftMouseClick(AbstractMouseController owner)
    {
        if (Mouse.current != null)
            owner.HandleClick(Mouse.current.position.ReadValue());
    }

    public override void OnRightMouseClick(AbstractMouseController owner) { }
    public override void OnMiddleMouseClick(AbstractMouseController owner) { }
}
