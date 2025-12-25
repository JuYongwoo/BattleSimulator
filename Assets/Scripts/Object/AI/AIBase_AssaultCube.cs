
public class AIBase_AssaultCube : ObjectBase_AIBase
{

    int mLastEnemy = -1;

    private int mClosestEnemy = -1;
    private int mVisibleEnemy = -1;
    private int mClosestHeal = -1;
    private int mClosestAmmo = -1;
    private int mClosestOccupy = -1;

    private int mSafestHeal = -1;
    private int mSafestAmmo = -1;

    override public void think()
    {
        base.think();

        mSafestHeal = searchItemNumber(mID, GameData.SearchType.Safe, GameData.ObjectType.Heal);
        mSafestAmmo = searchItemNumber(mID, GameData.SearchType.Safe, GameData.ObjectType.Ammo);
        mClosestEnemy = searchItemNumber(mID, GameData.SearchType.Closest, GameData.ObjectType.AI, GameData.TeamType.Enemy);
        mVisibleEnemy = searchItemNumber(mID, GameData.SearchType.Visible, GameData.ObjectType.AI, GameData.TeamType.Enemy);
        mClosestHeal = searchItemNumber(mID, GameData.SearchType.Closest, GameData.ObjectType.Heal);
        mClosestAmmo = searchItemNumber(mID, GameData.SearchType.Closest, GameData.ObjectType.Ammo);
        mClosestOccupy = searchItemNumber(mID, GameData.SearchType.Closest, GameData.ObjectType.OccupyPlace);


        //공격
        if (mVisibleEnemy != -1) // 적이 보이면
        {
            mLastEnemy = mVisibleEnemy; // 마주친 적으로 등록
            shoot(mLastEnemy);

        }

        //움직임
        if (getHpPercentage() < 0.5 || getAmmoPercentage() < 0.5) //상태가 좋지 않다 "필요한 아이템 탐색"
        {
            if (getHpPercentage() <= getAmmoPercentage() && mClosestHeal != -1) //체력이 더 부족
            {
                moveTo(mClosestHeal, false, 0); // 가까운 체력 아이템으로 이동

            }
            else if (getHpPercentage() > getAmmoPercentage() && mClosestAmmo != -1) //총알이 더 부족
            {
                moveTo(mClosestAmmo, false, 1); // 가까운 총알 아이템으로 이동
            }
            else //상태가 좋지 않지만 아이템이 존재하지 않을 때
            {
                //Stop
            }
        }
        else //상태가 괜찮을 때
        {


            if (mClosestOccupy != -1)//점령전이면
            {
                moveTo(mClosestOccupy, false, 2);

            }
            else //데스매치면
            {
                if (mLastEnemy != -1) //마지막으로 만난 적군에게 이동
                {
                    moveTo(mLastEnemy, false, 3);
                }
                else if (mClosestEnemy != -1)
                {
                    moveTo(mClosestEnemy, false, 4);

                }
            }



        }




        if (mLastEnemy != -1 && !GameManager.mAll_Of_Game_Objects[mLastEnemy].activeSelf) mLastEnemy = -1; //등록한 죽었다면 초기화
    }


    public override void respawn()
    {
        base.respawn(); // 기존 respawn수행
        mLastEnemy = -1; // 내가 죽었으면 상대 초기화

    }

    public override void killed(int pDeadAIsID)
    {
        base.killed(pDeadAIsID);
        if(pDeadAIsID == mLastEnemy) mLastEnemy = -1; //쫓던 상대를 죽였으면 상대 초기화
    }

}

