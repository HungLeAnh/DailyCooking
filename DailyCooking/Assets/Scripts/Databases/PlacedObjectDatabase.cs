using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "BuildingSystem/PlacedObjectDatabase", fileName = "PlacedObjectDatabase")]
public class PlacedObjectDatabase : ScriptableObject
{
    [SerializeField] private List<PlacedObjectTypeSO> placeObjects;

    public List<PlacedObjectTypeSO> PlacedObjects { get => placeObjects; set => placeObjects = value; }

}
