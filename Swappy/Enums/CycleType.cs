namespace Swappy.Enums;

public enum CycleType
{
    /// <summary>
    /// Never schedule update checks. Updates must be checked manually.
    /// </summary>
    Never,
    
    /// <summary>
    /// Schedule update checks on server startup only.
    /// </summary>
    OnStartup,
    
    /// <summary>
    /// Schedule update checks every round.
    /// </summary>
    EachRound
    
    // More cycle will be added in the future.
}