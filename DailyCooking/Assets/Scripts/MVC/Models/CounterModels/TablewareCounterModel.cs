public class TablewareCounterModel : BaseCounterModel
{
    private float _spawnTimer;
    private int _tablewareSpawnAmount;

    public int TablewareSpawnAmount { get => _tablewareSpawnAmount; set => _tablewareSpawnAmount = value; }
    public float SpawnTimer { get => _spawnTimer; set => _spawnTimer = value; }
}
