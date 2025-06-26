using UnityEngine;

public static class GameSettings
{
    public static string Player1Name { get; set; } = "Player 1";
    public static string Player2Name { get; set; } = "Player 2";
    public static GameModeType SelectedMode { get; set; } = GameModeType.Multiplayer;
    public static GameDifficulty SelectedDifficulty { get; set; } = GameDifficulty.Easy;

    public static GameDifficulty ParseDifficulty(string difficultyString)
    {
        switch (difficultyString.ToLower())
        {
            case "easy":
                return GameDifficulty.Easy;
            case "normal":
                return GameDifficulty.Normal;
            case "hard":
                return GameDifficulty.Hard;
            default:
                Debug.LogWarning($"Unknown difficulty string: {difficultyString}. Defaulting to Normal.");
                return GameDifficulty.Normal;
        }
    }

    public static GameModeType ParseMode(string modeString)
    {
        switch (modeString)
        {
            case "SinglePlayer":
                return GameModeType.SinglePlayer;
            case "MultiPlayer":
                return GameModeType.Multiplayer;
            default:
                Debug.LogWarning($"Unknown mode string: {modeString}. Defaulting to Multiplayer.");
                return GameModeType.Multiplayer;
        }
    }

    public static string GetSceneName()
    {
        return $"{SelectedMode}_{SelectedDifficulty}";
    }
}