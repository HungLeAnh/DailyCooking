using System;
using Unity.Netcode;

// Quality history of a piece of food: the sum and count of the gesture-step stars it went through.
// Count 0 means "no gesture step" (e.g. raw from the fridge); such food is left out of dish averages.
public struct QualityRecord : INetworkSerializable, IEquatable<QualityRecord>
{
    public const int MinStars = 1;
    public const int MaxStars = 3;

    public ushort Sum;
    public byte Count;

    public static QualityRecord None => default;

    // Average star value (1-3), rounded half up; 0 when there was no gesture step.
    public int Stars => Count == 0 ? 0 : QualityRules.RoundHalfUp((float)Sum / Count);

    public QualityRecord WithStep(int stars)
    {
        int clamped = stars < MinStars ? MinStars : stars > MaxStars ? MaxStars : stars;
        return new QualityRecord { Sum = (ushort)(Sum + clamped), Count = (byte)(Count + 1) };
    }

    public QualityRecord Plus(QualityRecord other)
    {
        return new QualityRecord { Sum = (ushort)(Sum + other.Sum), Count = (byte)(Count + other.Count) };
    }

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref Sum);
        serializer.SerializeValue(ref Count);
    }

    public bool Equals(QualityRecord other) => Sum == other.Sum && Count == other.Count;
    public override bool Equals(object obj) => obj is QualityRecord other && Equals(other);
    public override int GetHashCode() => (Sum << 8) | Count;
}
