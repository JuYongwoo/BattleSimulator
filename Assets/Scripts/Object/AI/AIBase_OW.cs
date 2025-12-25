using UnityEngine;

public class AIBase_OW : ObjectBase_AIBase
{
    private int mVisibleAI = -1;
    private int mClosestHeal = -1;
    private int mClosestAmmo = -1;
    private int mClosestTeammate = -1;
    private int mClosestOccupy = -1;
    private int mClosestEnemy = -1;

    override public void think()
    {

        mVisibleAI = searchItemNumber(mID, GameData.SearchType.Visible, GameData.ObjectType.AI, GameData.TeamType.Enemy);
        mClosestHeal = searchItemNumber(mID, GameData.SearchType.Closest, GameData.ObjectType.Heal);
        mClosestAmmo = searchItemNumber(mID, GameData.SearchType.Closest, GameData.ObjectType.Ammo);
        mClosestTeammate = searchItemNumber(mID, GameData.SearchType.Closest, GameData.ObjectType.AI, GameData.TeamType.Teammate);
        mClosestOccupy = searchItemNumber(mID, GameData.SearchType.Closest, GameData.ObjectType.OccupyPlace);
        mClosestEnemy = searchItemNumber(mID, GameData.SearchType.Closest, GameData.ObjectType.AI, GameData.TeamType.Enemy);



        //공격
        if (mVisibleAI != -1) // 적이 보이면
        {
            Shoot(mVisibleAI); // 적을 공격
        }

        //이동

        if (GetHpPercentage() < 0.25 || GetAmmoPercentage() < 0.25) //상태가 좋지 않다 "필요한 아이템 탐색" //0.25로 한 이유는 웬만해선 붙어다니는 것이 이 게임 AI의 핵심
        {
            if (GetHpPercentage() <= GetAmmoPercentage() && mClosestHeal != -1) //체력이 더 부족
            {
                MoveTo(mClosestHeal, false, 0); // 체력 아이템으로 이동

            }
            else if (GetHpPercentage() > GetAmmoPercentage() && mClosestAmmo != -1) //총알이 더 부족
            {
                MoveTo(mClosestAmmo, false, 1); // 총알 아이템으로 이동

            }
            else //상태가 좋지 않지만 아이템이 존재하지 않을 때
            {
                if (mClosestTeammate != -1 && GridPathfinder.GridManhattanDistance(this.gameObject.transform.position, GameManager.mAll_Of_Game_Objects[mClosestTeammate].transform.position) > 10) //팀원이 있는데 멀면
                {
                    MoveTo(mClosestTeammate, false, 2);
                }

            }
        }
        else //상태가 괜찮을 때
        {
            if (mClosestTeammate != -1)  //팀원이 존재하면
            {
                if(GridPathfinder.GridManhattanDistance(this.gameObject.transform.position, GameManager.mAll_Of_Game_Objects[mClosestTeammate].transform.position) > 10) //멀면 팀원에게 이동
                {
                    MoveTo(mClosestTeammate, false, 3);
                }
                else //모였으면
                {
                    if (mClosestOccupy != -1) //점령전이면
                    {
                        MoveTo(mClosestOccupy, false, 4); //점령지로 이동
                    }
                    else //점령전이 아니면
                    {
                        if(mClosestEnemy != -1) MoveTo(mClosestEnemy, false, 5); //가까운 적으로 이동
                    }
                }

            }
            else //팀원이 존재하지 않으면
            {
                if (mClosestOccupy != -1) //점령전이면
                {
                    MoveTo(mClosestOccupy, false, 6); //점령지로 이동
                }
                else //점령전이 아니면
                {
                    if (mClosestEnemy != -1)  MoveTo(mClosestEnemy, false, 7); //가까운 적으로 이동 //데스매치에선 점령지가 아닌 상대가 목표
                }
            }
            //오버워치에서 힐팩은 최대체력에서 먹어지지 않는다.
            
        }




    }
}

