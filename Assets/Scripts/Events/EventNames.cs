/// <summary>
/// Constants for all event names in the game.
/// Centralized to avoid typos and make events discoverable.
/// </summary>
public static class EventNames
{
    // Level events
    public const string LevelStart = "levelStart";
    public const string LevelEnd = "levelEnd";
    public const string LevelEndPerfect = "levelEndPerfect";

    // Block events
    public const string BlockPushed = "blockPushed";
    public const string WrongBlockPushed = "wrongBlockPushed";
    public const string BlockCleared = "blockCleared";
    
    // Combination events
    public const string BlocksCombined = "blocksCombined";
    public const string BlocksCombineFailed = "blocksCombineFailed";

    // Interaction events
    public const string ItemCollected = "itemCollected";
    public const string FurnitureMoved = "furnitureMoved";
    public const string FurnitureMoveFailure = "furnitureMoveFailure";
}
