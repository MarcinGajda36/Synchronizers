namespace PerKeySynchronizersTests;
using System;

public class IntWrapper(int x) : IEquatable<IntWrapper?>
{
    public int X { get; } = x;

    public override bool Equals(object? obj) => Equals(obj as IntWrapper);
    public bool Equals(IntWrapper? other) => other is not null && X == other.X;
    public override int GetHashCode() => HashCode.Combine(X);
}
