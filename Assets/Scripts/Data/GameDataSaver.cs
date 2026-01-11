using System.IO;
using System.Text;
using UnityEngine;

public class GameDataSaver
{
    private static readonly string programStartTime = System.DateTime.Now.ToString("yyyyMMdd_HHmmss");

    public static void SaveCalcTimeResultsToCSV(double pStandardNanoTime, double pSafetyScoreNanoTime)
    {
        string fileName = $"CalcTime_{programStartTime}.csv";
        string filePath = Application.dataPath + "/" + fileName;
        string data = $"StandardCalcNanoSec,{pStandardNanoTime},SafetyScoreCalcNanoSec,{pSafetyScoreNanoTime}\n";
        File.AppendAllText(filePath, data);
        StatsAggregator.Instance.RecordBench(nameof(AIBase_FA_OW), pStandardNanoTime, pSafetyScoreNanoTime);
    }

    // Disable per-event kill/death CSV appends; only aggregate
    public static void SaveKillDeathResultsToCSV(string pKillerAIName, string pKilledAiName)
    {
        // no file write; just aggregate
        StatsAggregator.Instance.RecordKill(pKillerAIName);
        StatsAggregator.Instance.RecordDeath(pKilledAiName);
    }

    public static void SaveOccupyResultsToCSV(params (string GameName, int Score)[] gameScores)
    {
        // no file write for periodic occupy; aggregate occupy seconds
        foreach (var gameScore in gameScores)
        {
            StatsAggregator.Instance.AddOccupyTime(gameScore.GameName, gameScore.Score);
        }
    }
}
