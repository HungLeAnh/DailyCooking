// Puts the host's join snapshot back together from its chunks. Only one snapshot is assembled at a
// time: a chunk with a new id (or a different count) drops whatever was partly received.
public sealed class SnapshotAssembler
{
    private int snapshotId = -1;
    private byte[][] chunks;

    public bool Add(int id, int index, int count, byte[] chunk, out byte[][] complete)
    {
        complete = null;
        if (count <= 0 || index < 0 || index >= count || chunk == null)
            return false;
        if (id != snapshotId || chunks == null || chunks.Length != count)
        {
            snapshotId = id;
            chunks = new byte[count][];
        }
        chunks[index] = chunk;
        for (int i = 0; i < chunks.Length; i++)
        {
            if (chunks[i] == null)
                return false;
        }
        complete = chunks;
        Reset();
        return true;
    }

    public void Reset()
    {
        snapshotId = -1;
        chunks = null;
    }
}
