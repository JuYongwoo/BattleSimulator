
public class AIBase_FA_Xonotic : ObjectBase_AIBase
{

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
            //마주친 적을 등록하지 않는다.
            Shoot(mVisibleEnemy);

        }


        //움직임
        if (GetHpPercentage() < 0.5 || GetAmmoPercentage() < 0.5) //상태가 좋지 않다 "필요한 아이템 탐색"
        {
            if (GetHpPercentage() <= GetAmmoPercentage() && mClosestHeal != -1) //체력이 더 부족
            {
                if (mVisibleEnemy != -1) MoveTo(mSafestHeal, true, 0); //적이 보일 땐 반드시 안전한 힐팩으로
                else MoveTo(mClosestHeal, false, 1); // 체력 아이템으로 이동

            }
            else if (GetHpPercentage() > GetAmmoPercentage() && mClosestAmmo != -1) //총알이 더 부족
            {
                if (mVisibleEnemy != -1) MoveTo(mSafestAmmo, true, 2); //적이 보일 땐 반드시 안전한 총알로
                else MoveTo(mClosestAmmo, false, 3); // 총알 아이템으로 이동
            }
            else //상태가 좋지 않지만 아이템이 존재하지 않을 때
            {
                //Stop
            }
        }
        else //상태가 괜찮을 때 적을 찾는다
        {

            if (mClosestOccupy != -1)//점령전이면
            {
                MoveTo(mClosestOccupy, false, 4);

            }
            else if(mClosestEnemy != -1)  //데스매치면
            {
                MoveTo(mClosestEnemy, false, 5);
                /*
                if (mClosestAmmo != -1 || mClosestHeal != -1) // 아이템이 존재
                {
                    if (mClosestAmmo != -1 && mClosestHeal == -1) //총알 아이템만 존재한다
                    {
                        moveTo(mClosestAmmo, false, lCommandID++); //총알로 이동
                    }
                    else if (mClosestAmmo == -1 && mClosestHeal != -1) //체력 아이템만 존재한다
                    {
                        moveTo(mClosestHeal, false, lCommandID++); //체력으로 이동
                    }
                    else //둘다 존재한다
                    {
                        if (Vector3.Distance(this.gameObject.transform.position, GameManager.mAll_Of_Game_Objects[mClosestAmmo].transform.position)
                                       < Vector3.Distance(this.gameObject.transform.position, GameManager.mAll_Of_Game_Objects[mClosestHeal].transform.position))
                        {
                            moveTo(mClosestAmmo, false, lCommandID++); // 가장 가까운 아이템 하나
                        }
                        else
                        {
                            moveTo(mClosestHeal, false, lCommandID++);
                        }
                    }
                }
                */
            }
        }





    }


}

