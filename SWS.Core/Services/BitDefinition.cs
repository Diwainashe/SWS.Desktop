namespace SWS.Core.Services;

/// <summary>
/// Defines how a single bit should be interpreted.
/// </summary>
public abstract record BitDefinition(int Bit)
{
    public sealed record Flag(int Bit, string Label) : BitDefinition(Bit);

    /// <summary>
    /// A bit that always represents one of two meanings.
    /// Example: 0=Stop, 1=Run
    /// </summary>
    public sealed record TwoState(int Bit, string WhenZero, string WhenOne) : BitDefinition(Bit);
}