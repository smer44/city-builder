using UnityEngine;

[CreateAssetMenu(fileName = "UnitWork", menuName = "RTS/Unit Work")]
public class UnitWork : UnitAction
{
    [SerializeField] private string workName;
    [SerializeField, Min(0f)] private float workTime = 1f;
    [SerializeField, Min(0f)] private float quantity = 1f;

    [System.NonSerialized] private GameObject objectToWorkOn;
    private float timeLeft;
    private bool reachedWorkObject;
    private bool completed;

    public string WorkName => string.IsNullOrWhiteSpace(workName) ? base.ActionName : workName;
    public override string ActionName => WorkName;
    public float WorkTime => workTime;
    public float Quantity => Mathf.Max(0f, quantity);
    public float TimeLeft => timeLeft;
    public GameObject ObjectToWorkOn => objectToWorkOn;

    public Transform ObjectToWorkOnTransform
    {
        get
        {
            if (objectToWorkOn == null)
                return null;

            return objectToWorkOn.transform;
        }
    }

    public RessourceOnField RessourceToWorkOn
    {
        get
        {
            if (objectToWorkOn == null)
                return null;

            return objectToWorkOn.GetComponent<RessourceOnField>();
        }
    }

    public UnitWork CreateAssignedWork(GameObject targetObject)
    {
        UnitWork assignedWork = Instantiate(this);
        assignedWork.name = WorkName;
        assignedWork.MarkAsRuntimeInstance();
        assignedWork.objectToWorkOn = targetObject;
        assignedWork.timeLeft = Mathf.Max(0f, workTime);
        assignedWork.reachedWorkObject = false;
        assignedWork.completed = false;
        return assignedWork;
    }

    public override void Begin(AbstractFieldPlacer field, UnitOnField unit)
    {
        timeLeft = Mathf.Max(0f, workTime);
        reachedWorkObject = false;
        completed = false;
    }

    public override bool Tick(AbstractFieldPlacer field, UnitOnField unit, float delta)
    {
        if (unit == null)
            return true;

        Transform target = ObjectToWorkOnTransform;
        if (target == null)
            return true;

        if (field == null)
            return false;

        if (!reachedWorkObject)
        {
            reachedWorkObject = field.MoveTowards(unit, target, delta);
            if (!reachedWorkObject)
                return false;
        }

        timeLeft -= delta;
        return timeLeft <= 0f;
    }

    public override void Finish(AbstractFieldPlacer field, UnitOnField unit)
    {
        if (completed || !reachedWorkObject || timeLeft > 0f || unit == null)
            return;

        completed = true;
        RessourceOnField ressource = RessourceToWorkOn;
        if (ressource != null)
            unit.CollectFrom(ressource, Quantity);
    }
}
