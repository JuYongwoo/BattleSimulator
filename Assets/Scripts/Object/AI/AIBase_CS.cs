using UnityEngine;

public class AIBase_CS : ObjectBase_AIBase
{
    private int mVisibleEnemy = -1;
    private int mClosestAmmo = -1;
    private int mClosestTeammate = -1;
    private int mClosestOccupy = -1;
    private int mClosestEnemy = -1;

    override public void think()
    {

        base.think();

        mVisibleEnemy = searchItemNumber(mID, GameData.SearchType.Visible, GameData.ObjectType.AI, GameData.TeamType.Enemy);
        mClosestAmmo = searchItemNumber(mID, GameData.SearchType.Closest, GameData.ObjectType.Ammo);
        mClosestTeammate = searchItemNumber(mID, GameData.SearchType.Closest, GameData.ObjectType.AI, GameData.TeamType.Teammate);
        mClosestOccupy = searchItemNumber(mID, GameData.SearchType.Closest, GameData.ObjectType.OccupyPlace);
        mClosestEnemy = searchItemNumber(mID, GameData.SearchType.Closest, GameData.ObjectType.AI, GameData.TeamType.Enemy);



        //공격
        if (mVisibleEnemy != -1) // 적이 보이면
        {
            moveStop(); //이동을 멈추고
            shoot(mVisibleEnemy);

        }

        //이동

        if (getAmmoPercentage() < 0.25) //총알이 부족하다 //0.25로 한 이유는 웬만해선 붙어다니는 것이 이 게임 AI의 핵심
        {
            if (mClosestAmmo != -1) //총알 아이템 존재
            {
                moveTo(mClosestAmmo, false, 0); // 총알 아이템으로 이동

            }
            else //상태가 좋지 않지만 아이템이 존재하지 않을 때
            {
                if (mClosestTeammate != -1 && Vector3.Distance(this.gameObject.transform.position, GameManager.mAll_Of_Game_Objects[mClosestTeammate].transform.position) > 10) //팀원이 있는데 멀면
                {
                    moveTo(mClosestTeammate, false, 1);
                }

            }
        }
        else //상태가 괜찮을 때 
        {
            if (mClosestTeammate != -1)  //팀원이 존재하면
            {
                if (Vector3.Distance(this.gameObject.transform.position, GameManager.mAll_Of_Game_Objects[mClosestTeammate].transform.position) > 10) //멀면 팀원에게 이동
                {
                    moveTo(mClosestTeammate, false, 2);
                }
                else //모였으면
                {
                    if (mClosestOccupy != -1) //점령전이면
                    {
                        moveTo(mClosestOccupy, false, 3); //점령지로 이동
                    }
                    else //점령전이 아니면
                    {
                        if (mClosestEnemy != -1) moveTo(mClosestEnemy, false, 4); //가까운 적으로 이동
                    }
                }

            }
            else //팀원이 존재하지 않으면
            {
                if (mClosestOccupy != -1) //점령전이면
                {
                    moveTo(mClosestOccupy, false, 5); //점령지로 이동
                }
                else //점령전이 아니면
                {
                    if (mClosestEnemy != -1) moveTo(mClosestEnemy, false, 6); //가까운 적으로 이동 //데스매치에선 폭파 수행지가 아닌 상대가 목표
                }
            }

        }

    }

}

