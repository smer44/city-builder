using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(RectTransform))]
public class SpawnInfoOnClick2D : MonoBehaviour
{
    [SerializeField] private GameObject infoPrefab;
    [SerializeField] private RectTransform infoParent;
    [SerializeField] private Vector2 infoOffset = new(12f, -12f);
    [SerializeField] private bool closeWhenClickOutside = true;

    private static SpawnInfoOnClick2D currentOpenOwner;
    private static int consumedClickFrame = -1;

    protected RectTransform cachedRectTransform;
    private GameObject spawnedInfo;
    private RectTransform spawnedInfoRectTransform;

    public GameObject InfoPrefab => infoPrefab;
    public RectTransform InfoParent => infoParent;
    public GameObject SpawnedInfo => spawnedInfo;
    public static bool HasOpenInfo => currentOpenOwner != null;
    public static bool WasClickConsumedThisFrame => consumedClickFrame == Time.frameCount;

    protected RectTransform ClickableRectTransform
    {
        get
        {
            if (cachedRectTransform == null)
                cachedRectTransform = GetComponent<RectTransform>();

            return cachedRectTransform;
        }
    }

    protected virtual void Update()
    {
        Mouse mouse = Mouse.current;
        if (mouse == null || !mouse.leftButton.wasPressedThisFrame)
            return;

        Vector2 screenPosition = mouse.position.ReadValue();

        if (currentOpenOwner != null && currentOpenOwner.IsPointerInsideOpenInfo(screenPosition))
        {
            ConsumeClick();
            return;
        }

        if (IsPointerInsideClickable(screenPosition))
        {
            ShowInfoAt(screenPosition);
            ConsumeClick();
            return;
        }

        if (closeWhenClickOutside && currentOpenOwner == this)
        {
            CloseInfo();
            ConsumeClick();
        }
    }

    protected virtual void OnDisable()
    {
        CloseInfo();
    }

    public void ShowInfo()
    {
        Camera eventCamera = GetEventCamera(ClickableRectTransform);
        Vector2 screenPosition = RectTransformUtility.WorldToScreenPoint(eventCamera, ClickableRectTransform.position);
        ShowInfoAt(screenPosition);
    }

    public void ShowInfoAt(Vector2 screenPosition)
    {
        if (infoPrefab == null)
        {
            Debug.LogWarning($"{nameof(SpawnInfoOnClick2D)}: info prefab is not assigned.", this);
            return;
        }

        if (currentOpenOwner != null && currentOpenOwner != this)
            currentOpenOwner.CloseInfo();

        currentOpenOwner = this;

        if (spawnedInfo == null)
            CreateInfoObject();

        if (spawnedInfo == null)
            return;

        spawnedInfo.SetActive(false);

        bool notified = NotifySpawnedInfo(spawnedInfo);
        if (!notified)
            Debug.LogWarning($"{nameof(SpawnInfoOnClick2D)}: info prefab '{spawnedInfo.name}' has no compatible spawn receiver.", spawnedInfo);

        PositionInfoObject(screenPosition);
        spawnedInfo.SetActive(true);
        spawnedInfo.transform.SetAsLastSibling();
    }

    public void CloseInfo()
    {
        if (spawnedInfo != null)
            Destroy(spawnedInfo);

        spawnedInfo = null;
        spawnedInfoRectTransform = null;

        if (currentOpenOwner == this)
            currentOpenOwner = null;
    }

    public bool ContainsScreenPoint(Vector2 screenPosition)
    {
        return IsPointerInsideClickable(screenPosition);
    }

    protected virtual bool NotifySpawnedInfo(GameObject infoObject)
    {
        return NotifySpawnReceivers<SpawnInfoOnClick2D>(infoObject, this);
    }

    protected static bool NotifySpawnReceivers<TSource>(GameObject infoObject, TSource source)
    {
        if (infoObject == null)
            return false;

        IOnSpawn<TSource>[] spawnReceivers = infoObject.GetComponentsInChildren<IOnSpawn<TSource>>(true);

        foreach (IOnSpawn<TSource> spawnReceiver in spawnReceivers)
            spawnReceiver.OnSpawn(source);

        return spawnReceivers.Length > 0;
    }

    private static void ConsumeClick()
    {
        consumedClickFrame = Time.frameCount;
    }

    private void CreateInfoObject()
    {
        Transform parent = GetInfoParent();
        spawnedInfo = Instantiate(infoPrefab, parent, false);
        spawnedInfo.name = $"{infoPrefab.name} ({name})";
        spawnedInfoRectTransform = spawnedInfo.transform as RectTransform;
    }

    private Transform GetInfoParent()
    {
        if (infoParent != null)
            return infoParent;

        Canvas canvas = ClickableRectTransform.GetComponentInParent<Canvas>();
        if (canvas != null)
            return canvas.transform;

        return transform.parent != null ? transform.parent : transform;
    }

    private void PositionInfoObject(Vector2 screenPosition)
    {
        if (spawnedInfoRectTransform == null)
            return;

        RectTransform parentRectTransform = spawnedInfoRectTransform.parent as RectTransform;
        if (parentRectTransform == null)
            return;

        Camera eventCamera = GetEventCamera(parentRectTransform);
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRectTransform, screenPosition, eventCamera, out Vector2 localPoint))
            return;

        spawnedInfoRectTransform.anchoredPosition = localPoint + infoOffset;
    }

    private bool IsPointerInsideClickable(Vector2 screenPosition)
    {
        return IsPointerInside(ClickableRectTransform, screenPosition);
    }

    private bool IsPointerInsideOpenInfo(Vector2 screenPosition)
    {
        if (spawnedInfoRectTransform == null)
            return false;

        return IsPointerInside(spawnedInfoRectTransform, screenPosition);
    }

    private static bool IsPointerInside(RectTransform rectTransform, Vector2 screenPosition)
    {
        if (rectTransform == null)
            return false;

        return RectTransformUtility.RectangleContainsScreenPoint(rectTransform, screenPosition, GetEventCamera(rectTransform));
    }

    private static Camera GetEventCamera(RectTransform rectTransform)
    {
        if (rectTransform == null)
            return null;

        Canvas canvas = rectTransform.GetComponentInParent<Canvas>();
        if (canvas == null || canvas.renderMode == RenderMode.ScreenSpaceOverlay)
            return null;

        return canvas.worldCamera != null ? canvas.worldCamera : Camera.main;
    }
}
