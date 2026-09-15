using UnityEngine;

[CreateAssetMenu(fileName = "RessourceOnFieldData", menuName = "RTS/Ressource On Field Data")]
public class RessourceOnFieldData : ScriptableObject
{
    [SerializeField] private string ressourceName;
    [SerializeField, Min(1)] private int maxPlaces = 1;

    public string RessourceName => string.IsNullOrWhiteSpace(ressourceName) ? name : ressourceName;
    public int MaxPlaces => Mathf.Max(1, maxPlaces);
}
