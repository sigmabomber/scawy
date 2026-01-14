namespace Doody.GameEvents
{
    // Define event types for objectives
    public struct EnemyKilledEvent
    {
        public string EnemyType;
        public int Count;
    }

    public struct ItemCollectedEvent
    {
        public string ItemId;
    }

    public struct ObjectiveProgressEvent
    {
        public string ObjectiveId;
        public int Amount;
    }

    // You can also make generic ones
    public struct GameActionEvent
    {
        public string ActionType;
        public string TargetId;
        public int Value;
    }
}