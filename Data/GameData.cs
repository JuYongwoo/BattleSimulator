using System.Collections.Generic;

public class GameData
{

    public enum ObjectType : int
    {
        None, // 초기값, 절대 쓰이지 않는다.
        Ammo,
        Heal,
        AI,
        RespawnPlace,
        OccupyPlace,
        Weapon
    }
    public enum SearchType : int
    {
        None,
        Visible,
        Closest,
        Farthest,
        Safe,
        Random
    }

    public enum TeamType : int
    {
        All, // 초기값, searchITem에서 Ammo를 찾을 때 All일 경우 전부 찾는다, Ammo는 팀같은게 없다.
        Teammate,
        Enemy,
    }
    

    public enum Weapon : int
    {
        None,
        Rifle,
        Pistol,
        Rocket,
        Cnt
    }

    public enum MotionType : int
    {
        Moving,
        Holding,
    }

    public struct WeaponData
    {
        public int mDamage;
        public int mMaxBullets;
        public int mInitBullets;
        public float mShootDelay;

    }

    public static Dictionary<Weapon, WeaponData> mWeaponDataDictionary = new Dictionary<Weapon, WeaponData>(); //사용될 딕셔너리 //웨폰 종류를 키로 받아 웨폰 정보를 값으로준다.
    public const int mMaxHP = 100;

    // static 생성자
    static GameData() //데이터 설정 위치
    {
        mWeaponDataDictionary[Weapon.Pistol] = new WeaponData
        {
            mDamage = 10,
            mMaxBullets = 30,
            mInitBullets = 15,
            mShootDelay = 0.2f
        };
        mWeaponDataDictionary[Weapon.Rifle] = new WeaponData
        {
            mDamage = 10,
            mMaxBullets = 60,
            mInitBullets = 30,
            mShootDelay = 0.1f
        };
        mWeaponDataDictionary[Weapon.Rocket] = new WeaponData
        {
            mDamage = 50,
            mMaxBullets = 10,
            mInitBullets = 10,
            mShootDelay = 0.5f
        };

    }
}
