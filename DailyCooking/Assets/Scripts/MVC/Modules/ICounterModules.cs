using UnityEngine;

public interface ICounterModules
{
    void Initialize();
    void DestroyCounter(BaseCounterView baseCounterView);
    void AddCounterController(BaseCounterController controller);
}
