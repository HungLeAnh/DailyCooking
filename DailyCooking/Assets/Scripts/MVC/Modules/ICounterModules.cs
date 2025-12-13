using UnityEngine;

public interface ICounterModules
{
    void Initialize();
    void DestroyCounter(BaseCounterController controller);
    void AddCounterController(BaseCounterController controller);
}
