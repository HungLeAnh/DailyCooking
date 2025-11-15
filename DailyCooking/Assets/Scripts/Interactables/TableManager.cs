using System;
using System.Collections.Generic;
using UnityEngine;

public class TableManager : SimpleSingleton<TableManager>
{
    private List<Table> tables = new List<Table>();

    private void Awake()
    {
        KitchenGameManager.Instance.OnStateChanged += KitchenGameManager_OnStateChanged;
    }

    private void KitchenGameManager_OnStateChanged(object sender, EventArgs e)
    {
        foreach (var table in tables)
        {
            table.ResetTable();
        }
    }

    public void RegisterTable(Table table)
    {
        if (!tables.Contains(table))
        {
            tables.Add(table);
        }
    }

    public void UnregisterTable(Table table)
    {
        if (tables.Contains(table))
        {
            tables.Remove(table);
        }
    }

    public Table GetAvailableTable()
    {
        foreach (Table table in tables)
        {
            if (table.GetAvailableSeat() != -1)
            {
                return table;
            }
        }
        return null; // No available table
    }
}