using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

// Process field clicks before UI button callbacks can select a placement prefab.
[DefaultExecutionOrder(-100)]
public abstract class AbstractMouseController : MonoBehaviour
{
    [SerializeField] private Vector2 infoOffset = new(12f, -12f);
    [SerializeField] private TMP_Text stateInfoText;

    private static AbstractMouseController currentOpenOwner;

    private GameObject spawnedInfo;

    public GameObject clickedObject;

    public AbstractMouseState CurrentState { get; private set; }

    public static bool HasOpenInfo => currentOpenOwner != null
        && currentOpenOwner.spawnedInfo != null
        && currentOpenOwner.spawnedInfo.activeInHierarchy;

    public void ChangeState(AbstractMouseState newState)
    {
        if (newState == null)
            return;

        if (newState != CurrentState)
        {
            CurrentState?.OnExit(this);
            CurrentState = newState;
            CurrentState.OnEnter(this);
        }

        // Selecting another prefab can update the current state without re-entering it.
        if (stateInfoText != null)
            stateInfoText.text = CurrentState.InfoStr();
    }

    protected virtual void Update()
    {
        AbstractMouseState state = CurrentState;
        state?.Update(this);

        // Escape takes priority over a mouse click arriving in the same frame.
        if (state == null || state != CurrentState)
            return;

        Mouse mouse = Mouse.current;
        if (mouse == null)
            return;

        if (mouse.leftButton.wasPressedThisFrame)
            CurrentState.OnLeftMouseClick(this);
        if (mouse.rightButton.wasPressedThisFrame)
            CurrentState.OnRightMouseClick(this);
        if (mouse.middleButton.wasPressedThisFrame)
            CurrentState.OnMiddleMouseClick(this);
    }

    protected abstract GameObject FindClickedObject(Vector2 screenPosition);

    public void HandleClick(Vector2 screenPosition)
    {
        GameObject hit = FindClickedObject(screenPosition);

        // Keep the panel and its selected node alive for UI button callbacks.
        if (hit != null && (hit.GetComponentInParent<RessourceOnSpawnUI>() != null
            || hit.GetComponentInParent<ProductionNodePanelUI>() != null))
            return;

        clickedObject = hit;
        if (clickedObject != null)
            Debug.Log($"{GetType().Name}: clicked '{clickedObject.name}'.", clickedObject);

        ProductionNode node = clickedObject == null
            ? null : clickedObject.GetComponentInParent<ProductionNode>();
        if (node != null)
        {
            clickedObject = node.gameObject;
            ShowInfo(screenPosition, node);
            return;
        }

        RessourceOnField ressource = clickedObject == null
            ? null
            : clickedObject.GetComponentInParent<RessourceOnField>();
        if (ressource != null)
        {
            // The hit can be on a child; expose the object holding the resource.
            clickedObject = ressource.gameObject;
            ShowInfo(screenPosition);
            return;
        }

        if (HasOpenInfo)
        {
            currentOpenOwner.CloseInfo();
        }
    }

    private void ShowInfo(Vector2 screenPosition, ProductionNode node = null)
    {
        if (spawnedInfo != null && spawnedInfo.transform.parent == clickedObject.transform)
            return;

        if (currentOpenOwner != null)
            currentOpenOwner.CloseInfo();

        if (node != null)
        {
            ProductionNodePanelUI panel = ProductionNodePanelUI.Create(clickedObject.transform);
            spawnedInfo = panel.gameObject;
            panel.OnSpawn(this);
        }
        else
        {
            RessourceOnSpawnUI panel = RessourceOnSpawnUI.Create(clickedObject.transform);
            spawnedInfo = panel.gameObject;
            panel.OnSpawn(this);
        }
        currentOpenOwner = this;

        // Keep the panel above neighboring items while retaining its selected parent.
        clickedObject.transform.SetAsLastSibling();
        spawnedInfo.transform.SetAsLastSibling();

        if (clickedObject.transform is not RectTransform parentRect
            || spawnedInfo.transform is not RectTransform infoRect)
            return;

        Canvas canvas = parentRect.GetComponentInParent<Canvas>();
        Camera eventCamera = canvas == null || canvas.renderMode == RenderMode.ScreenSpaceOverlay
            ? null
            : canvas.worldCamera != null ? canvas.worldCamera : Camera.main;

        infoRect.anchorMin = parentRect.pivot;
        infoRect.anchorMax = parentRect.pivot;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                parentRect, screenPosition, eventCamera, out Vector2 localPoint))
        {
            infoRect.anchoredPosition = localPoint + infoOffset;
        }
    }

    public void CloseInfo()
    {
        if (spawnedInfo != null)
        {
            spawnedInfo.SetActive(false);
            if (Application.isPlaying)
                Destroy(spawnedInfo);
            else
                DestroyImmediate(spawnedInfo);
            spawnedInfo = null;
        }

        if (currentOpenOwner == this)
            currentOpenOwner = null;
    }

    protected virtual void OnDisable()
    {
        CloseInfo();
    }
}
