using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Serialization;

public class JobManagementSystem : MonoBehaviour, Registery<AbstractFieldPlacer, RessourceOnField>
{
    [SerializeField, FormerlySerializedAs("field2DPlaser")] private AbstractFieldPlacer fieldPlacer;
    [SerializeField] private UnitController unitController;

    private readonly Dictionary<string, List<GameObject>> ressourcesByWorkName = new();

    public AbstractFieldPlacer FieldPlacer => fieldPlacer;
    public UnitController UnitController => unitController;
    public IReadOnlyDictionary<string, List<GameObject>> RessourcesByWorkName => ressourcesByWorkName;

    private void Awake()
    {
        ResolveReferences();
    }

    public bool RegisterRessource(RessourceOnField ressource)
    {
        return Register(ressource == null ? null : ressource.Field, ressource);
    }

    public bool Register(AbstractFieldPlacer field, RessourceOnField ressource)
    {
        if (ressource == null)
            return false;

        if ((fieldPlacer != null && field != null && field != fieldPlacer) ||
            (ressource.Field != null && field != null && ressource.Field != field) ||
            (fieldPlacer != null && ressource.Field != null && ressource.Field != fieldPlacer))
        {
            Debug.LogWarning($"{nameof(JobManagementSystem)}: ignored resource '{ressource.name}' because it belongs to another field.", this);
            return false;
        }

        ressource.SetField(field != null ? field : ressource.Field);
        ressource.SetJobManagementSystem(this);
        return RegisterInternal(ressource);
    }

    public bool UnRegister(AbstractFieldPlacer field, RessourceOnField ressource)
    {
        if (ressource == null || (fieldPlacer != null && field != null && field != fieldPlacer) ||
            (ressource.Field != null && field != null && ressource.Field != field))
            return false;

        bool removed = false;
        List<string> emptyWorkNames = new();
        foreach (var pair in ressourcesByWorkName)
        {
            if (!pair.Value.Remove(ressource.gameObject))
                continue;

            removed = true;
            if (pair.Value.Count == 0)
                emptyWorkNames.Add(pair.Key);
        }

        foreach (string workName in emptyWorkNames)
            ressourcesByWorkName.Remove(workName);

        return removed;
    }

    public bool AssignJobNaive(string jobName)
    {
        return AssignJobNaive(jobName, null);
    }

    /// <summary>Assigns a worker to this resource only; never falls back to another resource.</summary>
    public bool AssignJobToResource(string jobName, RessourceOnField ressource)
    {
        if (ressource == null || ressource.JobManagementSystem != this)
            return false;

        return AssignJobNaive(jobName, ressource);
    }

    private bool AssignJobNaive(string jobName, RessourceOnField targetRessource)
    {
        ResolveReferences();

        StringBuilder trace = new StringBuilder(1024);
        AppendAssignmentTraceHeader(trace, jobName);
        if (targetRessource != null)
            trace.Append("Requested resource=").Append(DescribeObject(targetRessource)).AppendLine();

        if (string.IsNullOrWhiteSpace(jobName))
            return FinishAssignmentTrace(trace, false, "Job name is blank.");

        if (!ressourcesByWorkName.TryGetValue(jobName, out List<GameObject> ressourceObjects))
        {
            trace.Append("Known job buckets: ").Append(FormatRegisteredJobBuckets()).AppendLine();
            return FinishAssignmentTrace(trace, false, $"No registered resources for job '{jobName}'.");
        }

        int countBeforeCleanup = ressourceObjects.Count;
        RemoveMissingRessources(ressourceObjects);
        trace.Append("Resources for requested job: before cleanup=")
            .Append(countBeforeCleanup)
            .Append(", after cleanup=")
            .Append(ressourceObjects.Count)
            .AppendLine();

        if (ressourceObjects.Count == 0)
            return FinishAssignmentTrace(trace, false, $"Resource list for job '{jobName}' is empty after cleanup.");

        for (int i = 0; i < ressourceObjects.Count; i++)
        {
            GameObject ressourceObject = ressourceObjects[i];
            if (targetRessource != null && ressourceObject != targetRessource.gameObject)
                continue;

            trace.Append("Resource[").Append(i).Append("]: ").Append(DescribeObject(ressourceObject)).AppendLine();

            if (ressourceObject == null || !ressourceObject.TryGetComponent(out RessourceOnField ressource))
            {
                trace.Append("  skipped: object is missing or has no ")
                    .Append(nameof(RessourceOnField))
                    .AppendLine(".");
                continue;
            }

            trace.Append("  data=").Append(ressource.Data == null ? "<null>" : ressource.Data.name)
                .Append(", slots=").Append(ressource.OccupiedPlaces).Append("/").Append(ressource.MaxPlaces)
                .Append(", position=").Append(DescribePosition(ressource.transform))
                .Append(", possibleWorks=").Append(FormatPossibleWorks(ressource))
                .AppendLine();

            if (!ressource.HasFreeSlots())
            {
                trace.Append("  skipped: no free resource slots.").AppendLine();
                continue;
            }

            if (!ressource.HasItems)
            {
                trace.AppendLine("  skipped: resource is depleted or has no items configured.");
                continue;
            }

            UnitWork work = ressource.GetPossibleWork(jobName);
            if (work == null || work.Quantity <= 0f)
            {
                trace.Append("  skipped: no matching ")
                    .Append(nameof(UnitWork))
                    .Append(" for job '")
                    .Append(jobName)
                    .AppendLine("'.");
                continue;
            }

            trace.Append("  matching work=").Append(DescribeWork(work)).AppendLine();

            UnitOnField unit = SelectUnitNaive(jobName, ressource);
            if (unit == null)
            {
                AppendUnitControllerTrace(trace, ressource.transform);
                return FinishAssignmentTrace(trace, false, $"No free unit available for job '{jobName}'.");
            }

            trace.Append("  selected unit=").Append(DescribeUnit(unit, ressource.transform)).AppendLine();

            if (!ressource.TryOccupyPlace())
            {
                trace.Append("  skipped: slot occupation failed after selecting unit; slots now ")
                    .Append(ressource.OccupiedPlaces)
                    .Append("/")
                    .Append(ressource.MaxPlaces)
                    .AppendLine(".");
                continue;
            }

            trace.Append("  occupied resource slot: slots now ")
                .Append(ressource.OccupiedPlaces)
                .Append("/")
                .Append(ressource.MaxPlaces)
                .AppendLine();

            UnitWork assignedWork = work.CreateAssignedWork(ressourceObject);
            trace.Append("  created runtime work=").Append(DescribeWork(assignedWork))
                .Append(", target=").Append(DescribeObject(ressourceObject))
                .AppendLine();

            if (unit.AssignWork(assignedWork))
                return FinishAssignmentTrace(trace, true, $"Assigned job '{jobName}' to unit '{unit.name}'.");

            trace.Append("  unit rejected work assignment; releasing resource slot and disposing runtime work. Unit state=")
                .Append(DescribeUnit(unit, ressource.transform))
                .AppendLine();
            ressource.ReleasePlace();
            assignedWork.DisposeRuntimeInstance();
            trace.Append("  released resource slot: slots now ")
                .Append(ressource.OccupiedPlaces)
                .Append("/")
                .Append(ressource.MaxPlaces)
                .AppendLine();
        }

        return FinishAssignmentTrace(trace, false, $"No assignable resource/unit combination found for job '{jobName}'.");
    }

    [ContextMenu("Assign Chop Job Example")]
    public void AssignChopJobExample()
    {
        AssignJobNaive("Chop");
    }

    public UnitOnField SelectUnitNaive(string work, RessourceOnField ressource)
    {
        if (string.IsNullOrWhiteSpace(work) || ressource == null)
            return null;

        ResolveReferences();

        if (unitController == null)
        {
            Debug.LogWarning($"{nameof(JobManagementSystem)}: unit controller is not assigned.", this);
            return null;
        }

        AbstractFieldPlacer workField = fieldPlacer != null ? fieldPlacer : ressource.Field;
        return unitController.ClosestFreeUnit(workField, ressource.transform);
    }

    private bool RegisterInternal(RessourceOnField ressource)
    {
        UnitWork[] possibleWorks = ressource.PossibleWorks;
        if (possibleWorks == null)
            return false;

        bool registeredAnyWork = false;

        foreach (UnitWork work in possibleWorks)
        {
            if (work == null)
                continue;

            string workName = GetWorkName(work);
            if (!ressourcesByWorkName.TryGetValue(workName, out List<GameObject> ressources))
            {
                ressources = new List<GameObject>();
                ressourcesByWorkName.Add(workName, ressources);
            }

            GameObject ressourceObject = ressource.gameObject;
            if (!ressources.Contains(ressourceObject))
            {
                ressources.Add(ressourceObject);
                registeredAnyWork = true;
            }
        }

        return registeredAnyWork;
    }

    private void ResolveReferences()
    {
        ResolveUnitController();
    }

    private void ResolveUnitController()
    {
        if (unitController != null)
            return;

        if (fieldPlacer != null && fieldPlacer.UnitController != null)
        {
            unitController = fieldPlacer.UnitController;
            return;
        }
    }

    private static void RemoveMissingRessources(List<GameObject> ressources)
    {
        if (ressources == null)
            return;

        for (int i = ressources.Count - 1; i >= 0; i--)
        {
            if (ressources[i] == null)
                ressources.RemoveAt(i);
        }
    }

    private static string GetWorkName(UnitWork work)
    {
        return work.WorkName;
    }

    private void AppendAssignmentTraceHeader(StringBuilder trace, string jobName)
    {
        trace.AppendLine($"{nameof(JobManagementSystem)}.{nameof(AssignJobNaive)}");
        trace.Append("Requested job='").Append(jobName ?? "<null>").AppendLine("'");
        trace.Append("System=").Append(DescribeObject(this))
            .Append(", field=").Append(DescribeObject(fieldPlacer))
            .Append(", unitController=").Append(DescribeObject(unitController))
            .AppendLine();
        trace.Append("Registered job buckets: ").Append(FormatRegisteredJobBuckets()).AppendLine();
    }

    private bool FinishAssignmentTrace(StringBuilder trace, bool result, string reason)
    {
        trace.Append("Result=").Append(result ? "assigned" : "not assigned")
            .Append("; reason=").Append(reason)
            .AppendLine();

        Debug.Log(trace.ToString(), this);
        return result;
    }

    private void AppendUnitControllerTrace(StringBuilder trace, Transform target)
    {
        if (unitController == null)
        {
            trace.AppendLine("  unit controller: <null>");
            return;
        }

        IReadOnlyList<UnitOnField> units = unitController.Units;
        trace.Append("  unit controller=").Append(DescribeObject(unitController))
            .Append(", field=").Append(DescribeObject(unitController.Field))
            .Append(", registeredUnits=").Append(units == null ? 0 : units.Count)
            .AppendLine();

        if (units == null || units.Count == 0)
            return;

        for (int i = 0; i < units.Count; i++)
            trace.Append("    unit[").Append(i).Append("]: ").Append(DescribeUnit(units[i], target)).AppendLine();
    }

    private string FormatRegisteredJobBuckets()
    {
        if (ressourcesByWorkName.Count == 0)
            return "<none>";

        StringBuilder builder = new StringBuilder();
        bool first = true;
        foreach (KeyValuePair<string, List<GameObject>> pair in ressourcesByWorkName)
        {
            if (!first)
                builder.Append(", ");

            builder.Append(pair.Key).Append("(").Append(pair.Value == null ? 0 : pair.Value.Count).Append(")");
            first = false;
        }

        return builder.ToString();
    }

    private static string FormatPossibleWorks(RessourceOnField ressource)
    {
        if (ressource == null || ressource.PossibleWorks == null || ressource.PossibleWorks.Length == 0)
            return "<none>";

        StringBuilder builder = new StringBuilder();
        for (int i = 0; i < ressource.PossibleWorks.Length; i++)
        {
            if (i > 0)
                builder.Append(", ");

            UnitWork work = ressource.PossibleWorks[i];
            builder.Append(work == null ? "<null>" : DescribeWork(work));
        }

        return builder.ToString();
    }

    private static string DescribeWork(UnitWork work)
    {
        if (work == null)
            return "<null>";

        return $"{work.name}/workName='{work.WorkName}'/time={work.WorkTime}";
    }

    private static string DescribeUnit(UnitOnField unit, Transform target)
    {
        if (unit == null)
            return "<null>";

        string distance = "n/a";
        string position = "n/a";
        if (unit.Field != null)
        {
            Vector3 unitPosition = unit.Field.GetPosition(unit.transform);
            position = FormatVector3(unitPosition);
            if (target != null)
            {
                float sqrDistance = (unitPosition - unit.Field.GetPosition(target)).sqrMagnitude;
                distance = sqrDistance.ToString("0.##");
            }
        }

        return $"{unit.name}/free={unit.IsFreeForJob}/assigned={DescribeWork(unit.AssignedWork)}/pos={position}/targetSqrDistance={distance}";
    }

    private string DescribePosition(Transform target)
    {
        if (target == null)
            return "<null>";

        Vector3 position = fieldPlacer == null ? target.position : fieldPlacer.GetPosition(target);
        return $"{target.name}/position={FormatVector3(position)}";
    }

    private static string DescribeObject(Object obj)
    {
        return obj == null ? "<null>" : $"{obj.name} ({obj.GetType().Name})";
    }

    private static string FormatVector3(Vector3 value)
    {
        return $"({value.x:0.##}, {value.y:0.##}, {value.z:0.##})";
    }
}
