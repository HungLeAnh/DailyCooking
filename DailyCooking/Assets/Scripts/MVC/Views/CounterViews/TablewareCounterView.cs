using System;
using System.Collections.Generic;
using UnityEngine;
public class TablewareCounterView : BaseCounterView
{

    [SerializeField] private KitchenObjectSO _tablewareKitchenObjectSO;
    [SerializeField] private float _spawnTimerMax = 4f;
    [SerializeField] private int _tablewareSpawnAmountMax = 4;

    [SerializeField] private Transform _tablewareVisualPrefab;
    [SerializeField] float tablewareOffsetX = .1f;
    [SerializeField] float tablewareOffsetY = 0.5f;
    [SerializeField] Vector3 tablewareRotation = Vector3.zero;

    private List<GameObject> _tablewareVisualGameObjectList;

    private void Awake()
    {
        _tablewareVisualGameObjectList = new List<GameObject>();
    }

    public void OnTablewareRemoved()
    {
        if (_tablewareVisualGameObjectList == null ||
            _tablewareVisualGameObjectList.Count < 0)
            return;

        GameObject tablewareGameObject = _tablewareVisualGameObjectList[_tablewareVisualGameObjectList.Count - 1];
        _tablewareVisualGameObjectList.Remove(tablewareGameObject);
        Destroy(tablewareGameObject);
    }

    public void OnTablewareSpawned()
    {
        Transform tablewareVisualTransform = Instantiate(_tablewareVisualPrefab, CounterTopPoint);

        tablewareVisualTransform.localPosition = new Vector3(tablewareOffsetX * _tablewareVisualGameObjectList.Count, tablewareOffsetY, 0);
        tablewareVisualTransform.Rotate(tablewareRotation);
        _tablewareVisualGameObjectList.Add(tablewareVisualTransform.gameObject);
    }
}