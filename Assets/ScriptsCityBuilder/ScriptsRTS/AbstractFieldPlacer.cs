using System.Collections.Generic;
using UnityEngine;

public abstract class AbstractFieldPlacer : MonoBehaviour
{
    [SerializeField] private UnitController unitController;
    [SerializeField] private JobManagementSystem jobManagementSystem;
    [SerializeField] private ProductionOverview productionOverview;
    [Tooltip("Components implementing Registery<AbstractFieldPlacer, RessourceOnField> or Registery<AbstractFieldPlacer, ProductionNode>.")]
    [SerializeField] private MonoBehaviour[] registerListeners = new MonoBehaviour[0];

    public abstract Transform ParentTransform { get; }
    public UnitController UnitController => unitController;
    public JobManagementSystem JobManagementSystem => jobManagementSystem;
    public ProductionOverview ProductionOverview => productionOverview;

    protected virtual void Start()
    {
        InitializeExistingObjects();
    }

    public abstract Vector3 GetPosition(Transform target);
    public abstract bool MoveTowards(UnitOnField unit, Vector3 targetPosition, float delta);

    public bool MoveTowards(UnitOnField unit, Transform target, float delta)
    {
        return target == null || MoveTowards(unit, GetPosition(target), delta);
    }

    public bool MoveTowardsHome(UnitOnField unit, float delta)
    {
        return unit == null || MoveTowards(unit, unit.HomePosition, delta);
    }

    public void SetUnitPosition(UnitOnField unit, Vector3 position)
    {
        if (unit != null)
            SetPlacedObjectPosition(unit.gameObject, position);
    }

    protected abstract bool IsPointerInsideField(Vector2 screenPosition);

    // The concrete field defines the coordinate space and how it is applied to an object.
    protected abstract bool TryGetPlacementPosition(Vector2 screenPosition, out Vector3 position);
    protected abstract void SetPlacedObjectPosition(GameObject placedObject, Vector3 position);

    public void TryPlaceAtScreenPosition(GameObject prefab, Vector2 screenPosition)
    {
        if (prefab == null || !IsPointerInsideField(screenPosition))
            return;

        Transform parent = ParentTransform;
        if (parent == null)
        {
            Debug.LogWarning($"{GetType().Name}: parent Transform is not assigned.", this);
            return;
        }

        if (!TryGetPlacementPosition(screenPosition, out Vector3 position))
            return;

        GameObject placedObject = Instantiate(prefab, parent, false);
        SetPlacedObjectPosition(placedObject, position);
        NotifySpawnReceivers(placedObject);
        RegisterObjects(placedObject);
    }

    public bool RegisterRessource(RessourceOnField ressource)
    {
        if (ressource == null)
            return false;

        ressource.SetField(this);
        if (jobManagementSystem == null && !HasRegisterListener<RessourceOnField>())
        {
            Debug.LogWarning($"{GetType().Name}: {nameof(JobManagementSystem)} is not assigned.", this);
            return false;
        }

        return RegisterWithListeners(ressource);
    }

    public bool RegisterProductionNode(ProductionNode node)
    {
        if (node == null)
            return false;

        node.SetField(this);
        return RegisterWithListeners(node);
    }

    public bool UnRegisterRessource(RessourceOnField ressource)
    {
        return UnRegisterWithListeners(ressource);
    }

    public bool UnRegisterProductionNode(ProductionNode node)
    {
        return UnRegisterWithListeners(node);
    }

    private IEnumerable<MonoBehaviour> GetRegisterListeners()
    {
        // Keep existing inspector references working, even when also listed in the array.
        HashSet<MonoBehaviour> visited = new();
        if (jobManagementSystem != null && visited.Add(jobManagementSystem))
            yield return jobManagementSystem;
        if (productionOverview != null && visited.Add(productionOverview))
            yield return productionOverview;

        if (registerListeners == null)
            yield break;

        foreach (MonoBehaviour listener in registerListeners)
            if (listener != null && visited.Add(listener))
                yield return listener;
    }

    private bool HasRegisterListener<T>() where T : Component
    {
        foreach (MonoBehaviour listener in GetRegisterListeners())
            if (listener is Registery<AbstractFieldPlacer, T>)
                return true;

        return false;
    }

    private bool RegisterWithListeners<T>(T component) where T : Component
    {
        bool registered = false;
        foreach (MonoBehaviour listener in GetRegisterListeners())
            if (listener is Registery<AbstractFieldPlacer, T> registery)
                registered |= registery.Register(this, component);

        return registered;
    }

    private bool UnRegisterWithListeners<T>(T component) where T : Component
    {
        if (component == null)
            return false;

        bool removed = false;
        foreach (MonoBehaviour listener in GetRegisterListeners())
            if (listener is Registery<AbstractFieldPlacer, T> registery)
                removed |= registery.UnRegister(this, component);

        return removed;
    }

    private void InitializeExistingObjects()
    {
        Transform parent = ParentTransform;
        if (parent == null)
        {
            Debug.LogWarning($"{GetType().Name}: parent Transform is not assigned.", this);
            return;
        }

        UnitOnField[] units = parent.GetComponentsInChildren<UnitOnField>(true);
        foreach (UnitOnField unit in units)
            unit.SetField(this);

        RegisterObjects(parent.gameObject);
    }

    private void RegisterObjects(GameObject root)
    {
        RessourceOnField[] ressources = root.GetComponentsInChildren<RessourceOnField>(true);
        foreach (RessourceOnField ressource in ressources)
            RegisterRessource(ressource);

        ProductionNode[] nodes = root.GetComponentsInChildren<ProductionNode>(true);
        foreach (ProductionNode node in nodes)
            RegisterProductionNode(node);
    }

    private void NotifySpawnReceivers(GameObject placedObject)
    {
        IOnSpawn<AbstractFieldPlacer>[] spawnReceivers = placedObject.GetComponentsInChildren<IOnSpawn<AbstractFieldPlacer>>(true);

        foreach (IOnSpawn<AbstractFieldPlacer> spawnReceiver in spawnReceivers)
            spawnReceiver.OnSpawn(this);
    }
}
