using UnityEngine;

public class LoaderCalBack : MonoBehaviour
{
    private bool isFirstUpadate = true;
    private void Update()
    {
        if (isFirstUpadate)
        {
            isFirstUpadate = false;

            Loader.LoaderCallback();
        }
    }
}
