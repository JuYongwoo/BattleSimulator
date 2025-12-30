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

        // 파일에 저장할 문자열 생성
        string data = $"StandardCalcNanoSec,{pStandardNanoTime},SafetyScoreCalcNanoSec,{pSafetyScoreNanoTime}\n";

        // 파일에 데이터 추가
        File.AppendAllText(filePath, data);
    }

    public static void SaveKillDeathResultsToCSV(string pKillerAIName, string pKilledAiName)
    {

        string fileName = $"KillDeath_{programStartTime}.csv";

        string filePath = Application.dataPath + "/" + fileName;

        // 파일에 저장할 문자열 생성
        string data = $"{pKillerAIName}{","}{"Killed"}{","}{pKilledAiName}{","}{System.DateTime.Now.ToString("yyyyMMdd_HHmmss")}\n";

        // 파일에 데이터 추가
        File.AppendAllText(filePath, data);
    }
    public static void SaveOccupyResultsToCSV(params (string GameName, int Score)[] gameScores)
    {
        string fileName = $"Occupy_{programStartTime}.csv";
        string filePath = Application.dataPath + "/" + fileName;

        StringBuilder dataBuilder = new StringBuilder();

        foreach (var gameScore in gameScores)
        {
            dataBuilder.Append($"{gameScore.GameName},{gameScore.Score},");
        }

        dataBuilder.Append($"{System.DateTime.Now.ToString("yyyyMMdd_HHmmss")}\n");

        File.AppendAllText(filePath, dataBuilder.ToString());
    }

}
