using System.Collections.Generic;
using UnityEngine;

public class TablewareCounterVisual : MonoBehaviour
{
    [SerializeField] private TablewareCounter _tablewareCounter;
    [SerializeField] private Transform _tablewareVisualPrefab;

    private List<GameObject> _tablewareVisualGameObjectList;

    private void Awake()
    {
        _tablewareVisualGameObjectList = new List<GameObject>();
    }

    private void Start()
    {
        _tablewareCounter.OnTablewareSpawned += TablewareCounter_OnTablewareSpawned;
        _tablewareCounter.OnTablewareRemoved += TablewareCounter_OnTablewareRemoved;
    }

    private void TablewareCounter_OnTablewareRemoved(object sender, System.EventArgs e)
    {
        if (_tablewareVisualGameObjectList == null || 
            _tablewareVisualGameObjectList.Count < 0)
            return;

        GameObject tablewareGameObject = _tablewareVisualGameObjectList[_tablewareVisualGameObjectList.Count - 1];
        _tablewareVisualGameObjectList.Remove(tablewareGameObject);
        Destroy(tablewareGameObject);
    }

    private void TablewareCounter_OnTablewareSpawned(object sender, System.EventArgs e)
    {
        Transform tablewareVisualTransform = Instantiate(_tablewareVisualPrefab, _tablewareCounter.GetKitchenObjectFollowTransform());
        float tablewareOffsetY = .1f;
        tablewareVisualTransform.localPosition = new Vector3(0, tablewareOffsetY * _tablewareVisualGameObjectList.Count, 0);
        _tablewareVisualGameObjectList.Add(tablewareVisualTransform.gameObject);
    }
}
