using UnityEngine;
using System.Diagnostics;

public class AIBase_FA_OW : ObjectBase_AIBase
{
    private int mVisibleAI = -1;
    private int mClosestHeal = -1;
    private int mClosestAmmo = -1;
    private int mClosestTeammate = -1;
    private int mClosestOccupy = -1;
    private int mClosestEnemy = -1;

    private static int sBenchCounter = 0;
    private static int sBenchRuns = 0;
    private static double sSumBaseNs = 0.0;
    private static double sSumSafetyNs = 0.0;

    override public void think()
    {
        // Periodic benchmark every ~600 frames; accumulate averages
        sBenchCounter++;
        if (sBenchCounter >= 600)
        {
            sBenchCounter = 0; // reset for next window

            // Warmup calls (not timed)
            _ = searchItemNumberWithSafetyScore(mID, GameData.SearchType.Closest, GameData.ObjectType.Heal);
            _ = searchItemNumber(mID, GameData.SearchType.Closest, GameData.ObjectType.Heal);

            var swBase = new Stopwatch();
            var swSafety = new Stopwatch();

            // Measure baseline searches (repeat to stabilize)
            swBase.Start();
            for (int i = 0; i < 200; i++)
            {
                _ = searchItemNumber(mID, GameData.SearchType.Closest, GameData.ObjectType.Heal);
                _ = searchItemNumber(mID, GameData.SearchType.Closest, GameData.ObjectType.Ammo);
            }
            swBase.Stop();

            // Measure safety-score searches (repeat to stabilize)
            swSafety.Start();
            for (int i = 0; i < 200; i++)
            {
                _ = searchItemNumberWithSafetyScore(mID, GameData.SearchType.Closest, GameData.ObjectType.Heal);
                _ = searchItemNumberWithSafetyScore(mID, GameData.SearchType.Closest, GameData.ObjectType.Ammo);
            }
            swSafety.Stop();

            double tickNs = 1_000_000_000.0 / Stopwatch.Frequency;
            double baseNs = swBase.ElapsedTicks * tickNs;
            double safetyNs = swSafety.ElapsedTicks * tickNs;

            sBenchRuns++;

            // Skip the first three records (no logging, no recording)
            if (sBenchRuns <= 3)
            {
                return;
            }

            sSumBaseNs += baseNs;
            sSumSafetyNs += safetyNs;

            double avgBaseNs = sSumBaseNs / (sBenchRuns - 3); // average over recorded runs
            double avgSafetyNs = sSumSafetyNs / (sBenchRuns - 3);

            // No Debug.Log for early runs; only aggregate and store averages
            StatsAggregator.Instance.RecordBench(nameof(AIBase_FA_OW), avgBaseNs, avgSafetyNs);
        }

        mVisibleAI = searchItemNumberWithSafetyScore(mID, GameData.SearchType.Visible, GameData.ObjectType.AI, GameData.TeamType.Enemy);
        mClosestHeal = searchItemNumberWithSafetyScore(mID, GameData.SearchType.Closest, GameData.ObjectType.Heal);
        mClosestAmmo = searchItemNumberWithSafetyScore(mID, GameData.SearchType.Closest, GameData.ObjectType.Ammo);
        mClosestTeammate = searchItemNumberWithSafetyScore(mID, GameData.SearchType.Closest, GameData.ObjectType.AI, GameData.TeamType.Teammate);
        mClosestOccupy = searchItemNumberWithSafetyScore(mID, GameData.SearchType.Closest, GameData.ObjectType.OccupyPlace);
        mClosestEnemy = searchItemNumberWithSafetyScore(mID, GameData.SearchType.Closest, GameData.ObjectType.AI, GameData.TeamType.Enemy);

        //공격
        if (mVisibleAI != -1) // 적이 보이면
        {
            Shoot(mVisibleAI); // 적을 공격
        }

        //이동
        if (GetHpPercentage() < 0.25 || GetAmmoPercentage() < 0.25)
        {
            if (GetHpPercentage() <= GetAmmoPercentage() && mClosestHeal != -1)
            {
                MoveTo(mClosestHeal, false, 0);
            }
            else if (GetHpPercentage() > GetAmmoPercentage() && mClosestAmmo != -1)
            {
                MoveTo(mClosestAmmo, false, 1);
            }
            else
            {
                if (mClosestTeammate != -1 && GridPathfinder.GridManhattanDistance(this.gameObject.transform.position, ObjectManager.mAll_Of_Game_Objects[mClosestTeammate].transform.position) > 10)
                {
                    MoveTo(mClosestTeammate, false, 2);
                }
            }
        }
        else
        {
            if (mClosestTeammate != -1)
            {
                if(GridPathfinder.GridManhattanDistance(this.gameObject.transform.position, ObjectManager.mAll_Of_Game_Objects[mClosestTeammate].transform.position) > 10)
                {
                    MoveTo(mClosestTeammate, false, 3);
                }
                else
                {
                    if (mClosestOccupy != -1)
                    {
                        MoveTo(mClosestOccupy, false, 4);
                    }
                    else
                    {
                        if(mClosestEnemy != -1) MoveTo(mClosestEnemy, false, 5);
                    }
                }

            }
            else
            {
                if (mClosestOccupy != -1)
                {
                    MoveTo(mClosestOccupy, false, 6);
                }
                else
                {
                    if (mClosestEnemy != -1)  MoveTo(mClosestEnemy, false, 7);
                }
            }
        }
    }
}

