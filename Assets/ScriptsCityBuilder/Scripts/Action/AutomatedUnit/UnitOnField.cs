using System.Collections.Generic;
using UnityEngine;

public class UnitOnField : MonoBehaviour, OnSpawn<AbstractFieldPlacer>
{
    [SerializeField] private AbstractFieldPlacer field;
    [SerializeField] private UnitOnFieldData data;
    [SerializeField] private UnitAction action;
    [SerializeField] private UnitWork assignedWork;
    [SerializeField] private Transform home;
    [SerializeField, Min(0f)] private float moveSpeed = 100f;
    [SerializeField, Min(0f)] private float targetReachDistance = 1f;

    private Vector3 homePosition;
    private bool hasHomePosition;
    private bool actionStarted;
    private readonly Dictionary<ItemType, AmountOf<ItemType>> carriedItems = new();

    public IReadOnlyDictionary<ItemType, AmountOf<ItemType>> CarriedItems => carriedItems;

    public float CollectFrom(RessourceOnField ressource, float quantity)
    {
        if (ressource == null)
            return 0f;

        ItemType itemType = ressource.ItemAmount.Item;
        float collected = ressource.TakeAmount(quantity);
        if (collected <= 0f)
            return 0f;

        if (carriedItems.TryGetValue(itemType, out AmountOf<ItemType> carried))
            carried.Amount += collected;
        else
            carriedItems.Add(itemType, new AmountOf<ItemType> { Item = itemType, Amount = collected });

        return collected;
    }

    private void Start()
    {
        SetField(field != null ? field : GetComponentInParent<AbstractFieldPlacer>());
    }

    private void Update()
    {
        TickAction(Time.deltaTime);
    }

    public UnitOnFieldData Data
    {
        get => data;
        set => data = value;
    }

    public UnitAction Action
    {
        get => action;
        set => SetAction(value);
    }

    public UnitWork AssignedWork => assignedWork;
    public AbstractFieldPlacer Field => field;
    public bool HasAssignedWork => assignedWork != null;
    public bool IsFreeForJob => assignedWork == null;
    public bool IsIdle => action == null && assignedWork == null;
    public float MoveSpeed => moveSpeed;
    public float TargetReachDistance => targetReachDistance;

    public Vector3 HomePosition
    {
        get
        {
            if (home != null && field != null)
                return field.GetPosition(home);

            EnsureHomePosition();
            return homePosition;
        }
    }

    public void OnSpawn(AbstractFieldPlacer spawnedField)
    {
        SetField(spawnedField);
    }

    public void SetField(AbstractFieldPlacer newField)
    {
        if (field != newField)
        {
            if (field != null)
            {
                CancelCurrentAction();
                hasHomePosition = false;
            }

            field = newField;
        }

        EnsureHomePosition();
        BeginActionIfNeeded();
    }

    public void SetHome(Transform newHome)
    {
        home = newHome;

        if (home == null)
            return;

        if (field == null)
        {
            hasHomePosition = false;
            return;
        }

        homePosition = field.GetPosition(home);
        hasHomePosition = true;
    }

    public void SetHomePosition(Vector3 newHomePosition)
    {
        home = null;
        homePosition = newHomePosition;
        hasHomePosition = true;
    }

    public bool AssignWork(UnitWork work)
    {
        if (work == null || assignedWork != null || field == null)
            return false;

        assignedWork = work;
        SetAction(work);
        return true;
    }

    private void TickAction(float delta)
    {
        if (action == null || field == null)
            return;

        BeginActionIfNeeded();

        if (!action.Tick(field, this, delta))
            return;

        FinishCurrentAction();
    }

    private void SetAction(UnitAction newAction)
    {
        if (action == newAction)
            return;

        CancelCurrentAction();
        action = newAction;
        actionStarted = false;
        BeginActionIfNeeded();
    }

    private void BeginActionIfNeeded()
    {
        if (action == null || actionStarted || field == null)
            return;

        action.Begin(field, this);
        actionStarted = true;
    }

    private void FinishCurrentAction()
    {
        UnitAction finishedAction = action;
        action = null;
        actionStarted = false;

        finishedAction.Finish(field, this);

        if (finishedAction == assignedWork)
        {
            FinishAssignedWork(assignedWork);
            return;
        }

        finishedAction.DisposeRuntimeInstance();
    }

    private void CancelCurrentAction()
    {
        if (action == null)
            return;

        UnitAction canceledAction = action;
        action = null;
        actionStarted = false;

        canceledAction.Cancel(field, this);

        if (canceledAction == assignedWork)
        {
            ReleaseAssignedWorkSlot(assignedWork);
            assignedWork = null;
        }

        canceledAction.DisposeRuntimeInstance();
    }

    private void FinishAssignedWork(UnitWork finishedWork)
    {
        ReleaseAssignedWorkSlot(finishedWork);
        assignedWork = null;

        finishedWork.DisposeRuntimeInstance();
        SetAction(UnitReturnHomeAction.Create());
    }

    private void ReleaseAssignedWorkSlot(UnitWork work)
    {
        if (work == null)
            return;

        RessourceOnField ressource = work.RessourceToWorkOn;
        if (ressource != null)
            ressource.ReleasePlace();
    }

    private void EnsureHomePosition()
    {
        if (hasHomePosition || field == null)
            return;

        homePosition = field.GetPosition(home != null ? home : transform);
        hasHomePosition = true;
    }
}
