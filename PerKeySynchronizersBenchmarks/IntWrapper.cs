namespace PerKeySynchronizersBenchmarks;

using System;

public class IntWrapper(int x)
    : IEquatable<IntWrapper?>
{
    public int X { get; } = x;

    public bool Equals(IntWrapper? other)
        => other is not null && X == other.X;
    public override bool Equals(object? obj)
        => Equals(obj as IntWrapper);
    public override int GetHashCode()
        => X;
}
