using System.Collections.Generic;
using UnityEngine;

public class TablewareCounterVisual : MonoBehaviour
{
    [SerializeField] private TablewareCounterController _tablewareCounter;
    [SerializeField] private Transform _tablewareVisualPrefab;
    [SerializeField] float tablewareOffsetX = .1f;
    [SerializeField] float tablewareOffsetY = 0.5f;
    [SerializeField] Vector3 tablewareRotation = Vector3.zero;

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

        tablewareVisualTransform.localPosition = new Vector3(tablewareOffsetX * _tablewareVisualGameObjectList.Count, tablewareOffsetY, 0);
        tablewareVisualTransform.Rotate(tablewareRotation);
        _tablewareVisualGameObjectList.Add(tablewareVisualTransform.gameObject);
    }
}
