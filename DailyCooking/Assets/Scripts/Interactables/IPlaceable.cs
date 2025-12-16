public interface IPlaceable
{
    public bool IsPlaced { get; set; }
    public bool CanRemove();
}