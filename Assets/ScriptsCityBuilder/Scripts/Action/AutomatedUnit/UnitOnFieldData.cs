using UnityEngine;

[CreateAssetMenu(fileName = "UnitOnFieldData", menuName = "RTS/Unit On Field Data")]
public class UnitOnFieldData : ScriptableObject
{
    private static readonly string[] FirstNames =
    {
        "Alex",
        "Dana",
        "Erin",
        "Ira",
        "Kai",
        "Mira",
        "Niko",
        "Rae",
        "Sam",
        "Tara",
    };

    private static readonly string[] LastNames =
    {
        "Ashford",
        "Baker",
        "Cole",
        "Dawson",
        "Fisher",
        "Hayes",
        "Morgan",
        "Reed",
        "Stone",
        "Vale",
    };

    [SerializeField] private string firstName;
    [SerializeField] private string lastName;

    public string FirstName => firstName;
    public string LastName => lastName;

    public void AutoFill()
    {
        firstName = FirstNames[Random.Range(0, FirstNames.Length)];
        lastName = LastNames[Random.Range(0, LastNames.Length)];
    }
}
