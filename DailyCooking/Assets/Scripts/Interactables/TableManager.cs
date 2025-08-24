using System.Collections.Generic;
using UnityEngine;

public class TableManager : SimpleSingleton<TableManager>
{
    private List<Table> tables = new List<Table>();

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