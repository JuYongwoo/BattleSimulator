using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Text;

public class StatsAggregator : Singleton<StatsAggregator>
{
    // Per AI type aggregates
    private readonly Dictionary<string, int> mKills = new Dictionary<string, int>(32);
    private readonly Dictionary<string, int> mDeaths = new Dictionary<string, int>(32);
    private readonly Dictionary<string, float> mOccupySeconds = new Dictionary<string, float>(32);

    // Optional benchmark per type (e.g., AIBase_FA_OW)
    private readonly Dictionary<string, (double baseNs, double faNs)> mBench = new Dictionary<string, (double, double)>(8);

    public void RecordKill(string aiTypeName)
    {
        if (!mKills.ContainsKey(aiTypeName)) mKills[aiTypeName] = 0;
        mKills[aiTypeName]++;
    }
    public void RecordDeath(string aiTypeName)
    {
        if (!mDeaths.ContainsKey(aiTypeName)) mDeaths[aiTypeName] = 0;
        mDeaths[aiTypeName]++;
    }
    public void AddOccupyTime(string aiTypeName, float seconds)
    {
        if (!mOccupySeconds.ContainsKey(aiTypeName)) mOccupySeconds[aiTypeName] = 0f;
        mOccupySeconds[aiTypeName] += seconds;
    }
    public void RecordBench(string aiTypeName, double baseNs, double faNs)
    {
        mBench[aiTypeName] = (baseNs, faNs);
    }

    private void OnApplicationQuit()
    {
        SaveFinalStatsToCSV();
    }

    private void SaveFinalStatsToCSV()
    {
        string fileName = $"FinalStats_{System.DateTime.Now.ToString("yyyyMMdd_HHmmss")}.csv";
        string filePath = Application.dataPath + "/" + fileName;
        var sb = new StringBuilder();
        // Header
        sb.AppendLine("AIType,TotalKills,TotalDeaths,KillDeathRatio,OccupySeconds,BaseNs,FaNs");
        // Collect union of types present in kills/deaths/bench/occupy
        var types = new HashSet<string>(mKills.Keys);
        foreach (var t in mDeaths.Keys) types.Add(t);
        foreach (var t in mBench.Keys) types.Add(t);
        foreach (var t in mOccupySeconds.Keys) types.Add(t);
        foreach (var t in types)
        {
            int k = mKills.TryGetValue(t, out var kk) ? kk : 0;
            int d = mDeaths.TryGetValue(t, out var dd) ? dd : 0;
            double kd = (k + d) > 0 ? (double)k / (double)(k + d) : 0.0;
            float occ = mOccupySeconds.TryGetValue(t, out var oo) ? oo : 0f;
            (double baseNs, double faNs) bench = (0.0, 0.0);
            if (mBench.TryGetValue(t, out var b)) bench = b;
            sb.Append(t).Append(',').Append(k).Append(',').Append(d).Append(',').Append(kd.ToString("F3"))
              .Append(',').Append(occ.ToString("F2")).Append(',')
              .Append(bench.baseNs.ToString("F0")).Append(',').Append(bench.faNs.ToString("F0")).Append('\n');
        }
        File.AppendAllText(filePath, sb.ToString());
    }
}
