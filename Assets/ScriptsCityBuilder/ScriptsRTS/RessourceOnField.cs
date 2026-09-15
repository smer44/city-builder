using UnityEngine;
using UnityEngine.Serialization;


public class RessourceOnField : MonoBehaviour
{
    [SerializeField] private RessourceOnFieldData data;
    [SerializeField] private ItemAmount initialItems;
    [SerializeField, FormerlySerializedAs("possibleActions")] private UnitWork[] possibleWorks;
    [SerializeField, Min(0)] private int occupiedPlaces;

    private AbstractFieldPlacer field;
    private JobManagementSystem jobManagementSystem;
    private AmountOf<ItemType> itemAmount;

    // Copy the asset's values: stock belongs to this location, not to the shared asset.
    public AmountOf<ItemType> ItemAmount => itemAmount ??= initialItems == null
        ? new AmountOf<ItemType>()
        : new AmountOf<ItemType>
        {
            Item = initialItems.Item == null ? ItemType.None : initialItems.Item.ItemType,
            Amount = Mathf.Max(0f, initialItems.Amount)
        };

    public bool HasItems => ItemAmount.Item != ItemType.None && ItemAmount.Amount > 0f;

    public RessourceOnFieldData Data
    {
        get => data;
        set => data = value;
    }

    public UnitWork[] PossibleWorks
    {
        get => possibleWorks;
        set => possibleWorks = value;
    }

    public AbstractFieldPlacer Field => field;
    public JobManagementSystem JobManagementSystem => jobManagementSystem;
    public int OccupiedPlaces => occupiedPlaces;
    public int MaxPlaces => data == null ? 1 : data.MaxPlaces;

    private void Awake()
    {
        _ = ItemAmount;
    }

    public float TakeAmount(float quantity)
    {
        if (!HasItems || !(quantity > 0f) || float.IsInfinity(quantity))
            return 0f;

        float taken = Mathf.Min(quantity, ItemAmount.Amount);
        ItemAmount.Amount -= taken;
        return taken;
    }


    private void OnDestroy()
    {
        if (field != null)
            field.UnRegisterRessource(this);
    }

    public void SetField(AbstractFieldPlacer newField)
    {
        field = newField;
    }

    public void SetJobManagementSystem(JobManagementSystem newJobManagementSystem)
    {
        jobManagementSystem = newJobManagementSystem;
    }

    public bool HasFreeSlots()
    {
        return occupiedPlaces < MaxPlaces;
    }

    public bool TryOccupyPlace()
    {
        if (!HasFreeSlots())
            return false;

        occupiedPlaces++;
        return true;
    }

    public void ReleasePlace()
    {
        occupiedPlaces = Mathf.Max(0, occupiedPlaces - 1);
    }

    public UnitWork GetPossibleWork(string workName)
    {
        if (possibleWorks == null || string.IsNullOrWhiteSpace(workName))
            return null;

        foreach (UnitWork work in possibleWorks)
        {
            if (work == null || work.ActionName != workName)
                continue;

            return work;
        }

        return null;
    }

}
