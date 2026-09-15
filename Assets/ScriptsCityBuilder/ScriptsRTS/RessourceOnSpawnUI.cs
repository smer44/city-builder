using UnityEngine;
using UnityEngine.UI;
using TMPro;

[RequireComponent(typeof(RectTransform))]
public class RessourceOnSpawnUI : MonoBehaviour, OnSpawn<SpawnInfoOnClick2D>, OnSpawn<AbstractMouseController>
{
    [SerializeField] private UIGameObjectFactory.PanelFormatting panelFormatting = new()
    {
        Width = 220f,
        Height = 156f,
        RowHeightPercentages = new[] { 22f, 16f, 16f, 18f, 28f }
    };
    [SerializeField] private UIGameObjectFactory.RowFormatting rowFormatting = new();
    [SerializeField, HideInInspector] private TMP_Text nameText;
    [SerializeField, HideInInspector] private TMP_Text quantityText;
    [SerializeField, HideInInspector] private TMP_Text occupiedPlacesText;
    [SerializeField, HideInInspector] private TMP_Text possibleWorksText;
    [SerializeField, HideInInspector] private Button addJobButton;
    [SerializeField, HideInInspector] private TMP_Text addJobButtonText;

    private RessourceOnField ressource;

    public static RessourceOnSpawnUI Create(Transform parent)
    {
        GameObject obj = new("RessourceUI", typeof(RectTransform));
        obj.layer = parent != null ? parent.gameObject.layer : LayerMask.NameToLayer("UI");
        obj.transform.SetParent(parent, false);
        RectTransform rect = (RectTransform)obj.transform;
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0f, 1f);
        RessourceOnSpawnUI ui = obj.AddComponent<RessourceOnSpawnUI>();
        ui.GenerateUI();
        return ui;
    }

    private void Awake() => GenerateUI();

    /// <summary>Builds and binds the resource layout once; refreshes only update its contents.</summary>
    public void GenerateUI()
    {
        if (nameText != null && addJobButton != null)
            return;

        UIGameObjectFactory.Panel panel = UIGameObjectFactory.CreatePanel(
            (RectTransform)transform, panelFormatting, rowFormatting,
            new[] { "Resource" }, new[] { "Quantity: -" }, new[] { "Slots: -" },
            new[] { "Works: -" }, new[] { "Add job" });

        nameText = panel.Rows[0].Labels[0];
        nameText.fontSize = 18f;
        nameText.fontStyle = FontStyles.Bold;
        nameText.color = Color.white;
        quantityText = panel.Rows[1].Labels[0];
        occupiedPlacesText = panel.Rows[2].Labels[0];
        possibleWorksText = panel.Rows[3].Labels[0];
        TextMeshProUGUI buttonLabel = panel.Rows[4].Labels[0];
        buttonLabel.fontSize = 16f;
        buttonLabel.color = Color.white;
        addJobButtonText = buttonLabel;
        addJobButton = UIGameObjectFactory.ReplaceLabelWithButton(buttonLabel, AddJob);
        RectTransform buttonRect = (RectTransform)addJobButton.transform;
        buttonRect.offsetMin = new Vector2(10f, 6f);
        buttonRect.offsetMax = new Vector2(-10f, -6f);
        addJobButton.interactable = false;
    }

    public void OnSpawn(AbstractMouseController source)
    {
        ressource = source == null || source.clickedObject == null
            ? null
            : source.clickedObject.GetComponent<RessourceOnField>();
        Refresh();
    }

    public void OnSpawn(SpawnInfoOnClick2D source)
    {
        ressource = source == null ? null : source.GetComponent<RessourceOnField>();
        Refresh();
    }

    public void AddJob()
    {
        UnitWork work = GetJobWork();
        if (work == null || ressource.JobManagementSystem == null)
            return;

        ressource.JobManagementSystem.AssignJobToResource(work.WorkName, ressource);
        Refresh();
    }

    private void Update()
    {
        Refresh();
    }

    private void Refresh()
    {
        GenerateUI();
        UnitWork work = GetJobWork();
        if (addJobButton != null)
            addJobButton.interactable = work != null && ressource.JobManagementSystem != null && ressource.HasFreeSlots();

        SetText(addJobButtonText, work == null ? "Add job" : $"Add job: {work.WorkName}");

        if (ressource == null)
        {
            SetText(nameText, "Unknown resource");
            SetText(quantityText, "Quantity: -");
            SetText(occupiedPlacesText, "Slots: -");
            SetText(possibleWorksText, "Works: -");
            return;
        }

        string ressourceName = ressource.Data == null ? ressource.name : ressource.Data.RessourceName;
        SetText(nameText, ressourceName);
        AmountOf<ItemType> items = ressource.ItemAmount;
        SetText(quantityText, items.Item == ItemType.None ? "Quantity: -" : $"{items.Item}: {items.Amount:0.##}");
        SetText(occupiedPlacesText, $"Slots: {ressource.OccupiedPlaces}/{ressource.MaxPlaces}");
        SetText(possibleWorksText, $"Works: {FormatPossibleWorks()}");
    }

    private UnitWork GetJobWork()
    {
        if (ressource == null || !ressource.HasItems || ressource.PossibleWorks == null)
            return null;

        // Job assignment uses work names (for example, "Chop"), not resource display names.
        foreach (UnitWork work in ressource.PossibleWorks)
        {
            if (work != null && work.Quantity > 0f && !string.IsNullOrWhiteSpace(work.WorkName))
                return work;
        }

        return null;
    }

    private string FormatPossibleWorks()
    {
        UnitWork[] possibleWorks = ressource.PossibleWorks;
        if (possibleWorks == null || possibleWorks.Length == 0)
            return "-";

        string result = string.Empty;
        for (int i = 0; i < possibleWorks.Length; i++)
        {
            UnitWork work = possibleWorks[i];
            if (work == null)
                continue;

            if (!string.IsNullOrEmpty(result))
                result += ", ";

            result += work.WorkName;
        }

        return string.IsNullOrEmpty(result) ? "-" : result;
    }

    private static void SetText(TMP_Text text, string value)
    {
        if (text != null)
            text.text = value;
    }
}
