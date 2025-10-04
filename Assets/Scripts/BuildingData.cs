using UnityEngine;

[CreateAssetMenu(menuName = "Game/BuildingData")]
public class BuildingData : ScriptableObject
{
    [Header("Basic Building Info")]
    public GameObject prefab;
    public Vector2Int size = Vector2Int.one;
    public int cost = 100;
    
    [Header("Building Type")]
    public BuildingType buildingType = BuildingType.Structure;
    
    [Header("Farm Plot Settings (if applicable)")]
    public bool isFarmPlot = false;
    
    public enum BuildingType
    {
        Structure,
        FarmPlot,
        Decoration
    }
}