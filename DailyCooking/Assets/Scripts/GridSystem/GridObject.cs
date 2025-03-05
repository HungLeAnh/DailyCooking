public class GridObject
{
    private GridXZ<GridObject> grid;
    private int x;
    private int z;
    private PlacedObjectView placedObject;

    public GridObject(GridXZ<GridObject> grid, int x, int z)
    {
        this.grid = grid;
        this.x = x;
        this.z = z;
    }
    public void SetPlacedObject(PlacedObjectView placedObject)
    {
        this.placedObject = placedObject;
        grid.TriggerGridObjectChanged(x,z);
    }
    public PlacedObjectView GetPlacedObject(PlacedObjectView placedObject)
    {
        return placedObject;
    }
    public void ClearTransform()
    {
        placedObject = null;
        grid.TriggerGridObjectChanged(x, z);

    }
    public bool CanBuild()
    {
        return placedObject == null;
    }

    public override string ToString()
    {
        return x + ", " + z;
    }
}