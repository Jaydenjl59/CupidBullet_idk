using System.Collections.Generic;

// anything the story needs to remember between screens goes here.
// (It resets when the game closes. Saving to disk is later.)
public static class GameData
{
    public class BattleResult
    {
        public string battleId;
        public bool win;
        public int playerHP;
        public int playerMaxHP;
        public float duration; // seconds fight lasted

        public float HPPercent => playerMaxHP > 0 ? (float)playerHP / playerMaxHP : 0f;
    }

    // every fight's result found by id ("battle_1").
    private static readonly Dictionary<string, BattleResult> battles = new Dictionary<string, BattleResult>();

    // the most recent fight or null if there hasn't been one yet.
    public static BattleResult LastBattle { get; private set; }

    public static void RecordBattle(BattleResult result)
    {
        battles[result.battleId] = result;
        LastBattle = result;
    }

    public static BattleResult GetBattle(string battleId)
    {
        return battles.TryGetValue(battleId, out BattleResult result) ? result : null;
    }

    // general story flags the visual novel can set and check such as"rival_clear".
    private static readonly HashSet<string> flags = new HashSet<string>();

    public static void SetFlag(string flag) => flags.Add(flag);
    public static void ClearFlag(string flag) => flags.Remove(flag);
    public static bool HasFlag(string flag) => flags.Contains(flag);

    // call this from the title screen when the player starts a new game.
    public static void ResetAll()
    {
        battles.Clear();
        flags.Clear();
        LastBattle = null;
    }
}