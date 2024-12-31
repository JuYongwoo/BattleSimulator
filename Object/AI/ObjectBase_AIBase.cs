using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.AI;

public class ObjectBase_AIBase : ObjectBase
{


    public int mDestinationItemNumber = -1; //이동 목표 아이템
    public Vector3 mDestinationPositionThen = new Vector3(); //이동하려는 당시의 이동 목표 아이템의 위치 (가까워지면 Destination -1)

    bool mIsFixed = false;
    //bool mIsIter = true;
    int mCommandID = -1;

    private int mBeAttacked_By_Enemy = -1; //현재 나를 공격한 적


    public bool mIsRespawning = false;
    public int mCurrentHP = 0;

    public GameData.Weapon mUsingWeapon = GameData.Weapon.None; //쓰고있는 총 종류
    public Dictionary<GameData.Weapon, int> mCurrentAmmo = new Dictionary<GameData.Weapon, int>(); // 총 종류에 따른 총알 페어 딕셔너리

    private float mShootTimer;
    private bool mShootisPossible = false;


    protected override void Awake()
    {
        base.Awake();
        mObjectType = GameData.ObjectType.AI;
    }



    protected override void Start()
    {
        base.Start();
        respawn(); //게임을 시작하면 리스폰 시킨다 **************


    }


    protected override void Update()
    {
        base.Update();
        if (this.gameObject.activeSelf == false) return;
        checkReachedItem();
        if (Vector3.Distance(mDestinationPositionThen, this.gameObject.transform.position) <= 0.5f)  reachedDestination();
        checkShootDelay();
        updateMotion();
        think();

    }
    public void checkReachedItem() //이동하면 서 닿은 아이템들
    {
        foreach(var pair in GameManager.mAll_Of_Game_Objects)
        {
            ObjectBase_ItemBase lItem = pair.Value.GetComponent<ObjectBase_ItemBase>();
            if (lItem == null) continue;
            if (Vector3.Distance(pair.Value.transform.position, this.gameObject.transform.position) >= 0.5f) continue;
            if (!pair.Value.activeSelf) continue;

            lItem.action(mID);
        }
    }
    public void reachedDestination() //이동 목표지점 닿았을 때
    {
        
        mDestinationItemNumber = -1;
        //mIsForced = false;
        //mIsIter = true;
        mCommandID = -1;
        mIsFixed = false;
    }


    void checkShootDelay()
    {
        mShootTimer += Time.deltaTime;
        if (!mShootisPossible && GameData.mWeaponDataDictionary[mUsingWeapon].mShootDelay < mShootTimer)
        {
            mShootisPossible = true;
            mShootTimer = 0.0f;
        }
    }

    public void updateMotion()
    {
        if (this.gameObject.GetComponent<NavMeshAgent>().velocity.magnitude < 1)
        {

            this.gameObject.GetComponent<Animator>().SetBool("isMoving", false);
            this.gameObject.GetComponent<Animator>().SetBool("isHolding", true);

        }
        else
        {
            this.gameObject.GetComponent<Animator>().SetBool("isMoving", true);
            this.gameObject.GetComponent<Animator>().SetBool("isHolding", false);
        }
    }


    virtual public void think()
    {

    }


    #region 탐색
    public virtual bool isVisible(int pItemNumber)
    {
        Collider lMyCollider = this.gameObject.GetComponent<Collider>();
        Collider lTargetCollider = GameManager.mAll_Of_Game_Objects[pItemNumber].GetComponent<Collider>();

        Vector3 lDirectionToEnemy = (lTargetCollider.bounds.center - lMyCollider.bounds.center).normalized; //pItemNumber 방향으로

        if (Vector3.Angle(transform.forward, lDirectionToEnemy) < 90) // 시야각 180도에서 보이면서
        {
            // 레이캐스트로 시야 내의 적과 장애물 여부 확인
            RaycastHit hit;

            if (Physics.Raycast(lMyCollider.bounds.center, lDirectionToEnemy, out hit, 100)) // pItemNumber 방향으로 쏘았을 때 맞았는데
            {
                if (hit.collider.gameObject == GameManager.mAll_Of_Game_Objects[pItemNumber]) //맞은게 pItemNumber면
                {
                    return true;
                    
                }
            }
        }

        return false;
    }

    public virtual int searchItemNumber(int pID, GameData.SearchType pSearchType, GameData.ObjectType pObjectType, GameData.TeamType pTeamType = GameData.TeamType.All)
    {

        // 검색가능여부 정리
        var lIsSearchable = new List<ObjectBase>();

        foreach (var pair in GameManager.mAll_Of_Game_Objects)
        {


            //적절한 애들을 필터, 타입에 맞는 것만 true

            if (!pair.Value.activeSelf) continue; // 죽은 애는 찾지 않는다
            if (pair.Value.GetComponent<ObjectBase>() == null) continue; // ObjectBase 컴포넌트가 없는 경우 넘어간다.
            if (pair.Value.GetComponent<ObjectBase>().mID == pID) continue;// 자기 자신을 찾았다면 넘어간다.
            if (pair.Value.GetComponent<ObjectBase>().mObjectType != pObjectType) continue;// 찾고자 하는 오브젝트 타입이 아니면 넘어간다.


            // 원하는 오브젝트 타입이 아니면 넘어간다.

            if (pObjectType == GameData.ObjectType.AI) //AI는 팀 구분할 필요가 있음
            {

                if (pTeamType == GameData.TeamType.Teammate)
                {
                    if (pair.Value.GetComponent(this.GetType()) != null)
                    {
                        lIsSearchable.Add(pair.Value.GetComponent<ObjectBase>());
                    }
                }
                else if (pTeamType == GameData.TeamType.Enemy)
                {
                    if (pair.Value.GetComponent(this.GetType()) == null)
                    {
                        lIsSearchable.Add(pair.Value.GetComponent<ObjectBase>());

                    }
                }
                else if(pTeamType == GameData.TeamType.All)
                {
                        lIsSearchable.Add(pair.Value.GetComponent<ObjectBase>());

                }
            }

            else if (pObjectType == GameData.ObjectType.RespawnPlace) //리스폰 지점은 아군의 지점인지 적의 지점인지 구분할 필요가 있음
            {


                //All인 경우엔 아무것도 컨티뉴 하지 않는다.

                if (pTeamType == GameData.TeamType.Teammate)
                {
                    //null이면 자동으로 컨티뉴 된다. 
                    if (pair.Value.GetComponent<ObjectBase_RespawnPlaceBase>().mPlaceOwner.GetType() == this.GetType())
                    {
                        lIsSearchable.Add(pair.Value.GetComponent<ObjectBase>());

                    }
                }
                else if (pTeamType == GameData.TeamType.Enemy)
                {
                    if (pair.Value.GetComponent<ObjectBase_RespawnPlaceBase>().mPlaceOwner.GetType() != this.GetType())
                    {
                        lIsSearchable.Add(pair.Value.GetComponent<ObjectBase>());

                    }
                }
                else if(pTeamType == GameData.TeamType.All)
                {
                    lIsSearchable.Add(pair.Value.GetComponent<ObjectBase>());

                }



            }
            else //각종 아이템들
            {
                lIsSearchable.Add(pair.Value.GetComponent<ObjectBase>());

            }



        }




        ////////////검색


        int lSearchItemNumber = -1;

        switch (pSearchType)
        {
            case GameData.SearchType.Visible: // 타입에 맞는 보이는 ObjectBase를 자식으로 가진 GameObject 탐색
                Collider lMyCollider = GameManager.mAll_Of_Game_Objects[pID].GetComponent<Collider>();
                RaycastHit hit;

                foreach (var obj in lIsSearchable) //TODO JYW 보이는 것 중에서 그래도 가장 가까운 것을 리턴하도록 바꿔야한다. Visible은 보이는것 + 가까운 것으로 봐야함.
                {
                    ObjectBase lObjectBase = obj;

                    Vector3 lDirectionToEnemy = (lObjectBase.gameObject.GetComponent<Collider>().bounds.center - lMyCollider.bounds.center).normalized;

                    if (Vector3.Angle(transform.forward, lDirectionToEnemy) > 90) continue; // 시야각 180도 넘어가면 컨티뉴
                    if (!Physics.Raycast(lMyCollider.bounds.center, lDirectionToEnemy, out hit, 200)) continue;// 아무것도 안맞았으면 컨티뉴
                    if (hit.collider.gameObject != GameManager.mAll_Of_Game_Objects[lObjectBase.mID]) continue; //맞은게 목표물이 아니면 컨티뉴

                    lSearchItemNumber = hit.collider.GetComponent<ObjectBase>().mID;
                }
                break;
            case GameData.SearchType.Closest: // 타입에 맞는 가까운 ObjectBase를 자식으로 가진 GameObject 탐색

                float lClosestDistance = float.MaxValue;

                foreach (var obj in lIsSearchable)
                {

                    float distance = Vector3.Distance(GameManager.mAll_Of_Game_Objects[pID].transform.position, obj.gameObject.transform.position);

                    if (distance < lClosestDistance)
                    {
                        lSearchItemNumber = obj.mID;
                        lClosestDistance = distance;
                    }
                }

                break;

            case GameData.SearchType.Farthest: //타입에 맞는 멀리있는 ObjectBase를 자식으로 가진 GameObject 탐색

                float lFarthestDistance = float.MinValue;
                foreach (var obj in lIsSearchable)
                {

                    float distance = Vector3.Distance(GameManager.mAll_Of_Game_Objects[pID].transform.position, obj.gameObject.transform.position);

                    if (distance > lFarthestDistance)
                    {
                        lSearchItemNumber = obj.mID;
                        lFarthestDistance = distance;
                    }
                }
                break;

            case GameData.SearchType.Safe: // 타입에 맞는 적과 멀리 떨어지고 나와 가까운 ObjectBase를 자식으로 가진 GameObject 탐색
                int lVisibleEnemy = searchItemNumber(pID, GameData.SearchType.Visible, GameData.ObjectType.AI, GameData.TeamType.Enemy);
                if (lVisibleEnemy == -1) return -1; //내가 볼 수있는 적이 있어야 한다.

                float lSafestDistence = float.MaxValue; //가장 가까우면서 안전한 아이템을 찾아야한다. 최초값은 높이

                foreach (var obj in lIsSearchable)
                {
                                                                                             // 원하는 오브젝트 타입이 아니면 넘어간다.

                    float lMeToEnemyDistance = Vector3.Distance(GameManager.mAll_Of_Game_Objects[pID].transform.position, GameManager.mAll_Of_Game_Objects[lVisibleEnemy].transform.position);
                    float lEnemyToItemDistance = Vector3.Distance(GameManager.mAll_Of_Game_Objects[lVisibleEnemy].transform.position, obj.gameObject.transform.position);
                    float lMeToItemDistance = Vector3.Distance(GameManager.mAll_Of_Game_Objects[pID].transform.position, obj.gameObject.transform.position);

                    if (lEnemyToItemDistance > lMeToEnemyDistance && lMeToItemDistance < lSafestDistence) //적과의 거리보다 적과 아이템의 거리가 더 멀면 비교적 안전 && 그 중 가까운 아이템
                    {
                        lSearchItemNumber = obj.mID;
                        lSafestDistence = lMeToItemDistance;
                    }
                }

                break;
            case GameData.SearchType.Random: // 타입에 맞는 랜덤 ObjectBase를 자식으로 가진 GameObject 탐색


                if (lIsSearchable.Count == 0) break;
                lSearchItemNumber = lIsSearchable[UnityEngine.Random.Range(0, lIsSearchable.Count)].mID; //랜덤 인덱스 + 랜덤 마크

                break;
        }
        return lSearchItemNumber; // 찾지 못했으면 -1을 반환
    }

    /*

    public virtual int searchClosestObject(GameData.ObjectType pObjectType, GameData.TeamType pTeamType = GameData.TeamType.None)
    {
        float closestDistance = float.MaxValue;
        int itemNumber = -1;

        for (int i = 0; i < GameManager.mAll_Of_Game_Objects.Count; i++)
        {
            if (!GameManager.mAll_Of_Game_Objects[i].activeSelf
                || GameManager.mAll_Of_Game_Objects[i] == this.gameObject
                || GameManager.mAll_Of_Game_Objects[i].GetComponent<ObjectBase>() == null
                || GameManager.mAll_Of_Game_Objects[i].GetComponent<ObjectBase>().mObjectType != pObjectType) continue; // 죽은 애는 찾지 않는다.// 자기 자신을 찾았다면 넘어간다. // ObjectBase 컴포넌트가 없는 경우도 처리
            // 원하는 오브젝트 타입이 아니면 넘어간다.

            float distance = Vector3.Distance(this.gameObject.transform.position, GameManager.mAll_Of_Game_Objects[i].transform.position);

            if (distance < closestDistance)
            {
                itemNumber = i;
                closestDistance = distance;
            }
        }

        return itemNumber; // 못 찾았으면 -1 반환
    }


    public virtual int searchSafeObject(GameData.ObjectType pObjectType, GameData.TeamType pTeamType = GameData.TeamType.None) // 상대방으로부터 가장 멀고 나로부터 가장 가까운 안전한 아이템을 찾는다.
    {
        if (mBeAttacked_By_Enemy == -1) return -1; //나를 공격한 사람이 존재해야 한다.

        float lFarthestDistance = float.MinValue;
        int itemNumber = -1;

        for (int i = 0; i < GameManager.mAll_Of_Game_Objects.Count; i++)
        {
            if (!GameManager.mAll_Of_Game_Objects[i].activeSelf
                || GameManager.mAll_Of_Game_Objects[i] == this.gameObject
                || GameManager.mAll_Of_Game_Objects[i].GetComponent<ObjectBase>() == null
                || GameManager.mAll_Of_Game_Objects[i].GetComponent<ObjectBase>().mObjectType != pObjectType) continue; // 죽은 애는 찾지 않는다.// 자기 자신을 찾았다면 넘어간다. // ObjectBase 컴포넌트가 없는 경우도 처리
            // 원하는 오브젝트 타입이 아니면 넘어간다.

            float lEnemyToItemDistance = Vector3.Distance(GameManager.mAll_Of_Game_Objects[mBeAttacked_By_Enemy].transform.position, GameManager.mAll_Of_Game_Objects[i].transform.position);
            float lMeToItemDistance = Vector3.Distance(this.gameObject.transform.position, GameManager.mAll_Of_Game_Objects[i].transform.position);

            if (lEnemyToItemDistance - lMeToItemDistance > lFarthestDistance)
            {
                itemNumber = i;
                lFarthestDistance = lEnemyToItemDistance - lMeToItemDistance;
            }
        }

        return itemNumber; // 못 찾았으면 -1 반환
    }


    public virtual int searchRandomObject(GameData.ObjectType pObjectType, GameData.TeamType pTeamType = GameData.TeamType.None)
    {
        List<int> correctObjects = new List<int>();

        for (int i = 0; i < GameManager.mAll_Of_Game_Objects.Count; i++)
        {
            ObjectBase objectBase = GameManager.mAll_Of_Game_Objects[i].GetComponent<ObjectBase>();

            if (objectBase != null && objectBase.mObjectType == pObjectType)
            {
                correctObjects.Add(i);
            }
        }

        if (correctObjects.Count == 0)
        {
            // No matching objects found, return a default value or handle it as needed
            return -1; // Default value, change it if needed
        }

        int randomIndex = UnityEngine.Random.Range(0, correctObjects.Count);
        int randomObjectNumber = correctObjects[randomIndex];

        return randomObjectNumber + mRandomMark;
    }


    public int searchVisibleObject(GameData.ObjectType pObjectType, GameData.TeamType pTeamType = GameData.TeamType.None)
    {
        // 일반 시야
        ObjectBase[] allObjectBases = GameObject.FindObjectsOfType<ObjectBase>();
        Collider lMyCollider = GetComponent<Collider>();

        foreach (var objObjectBase in allObjectBases)
        {
            if (!GameManager.mAll_Of_Game_Objects[objObjectBase.mID].activeSelf
                || GameManager.mAll_Of_Game_Objects[objObjectBase.mID] == this.gameObject
                || objObjectBase == null
                || objObjectBase.mObjectType != pObjectType) continue; // 죽은 애는 찾지 않는다.// 자기 자신을 찾았다면 넘어간다. // ObjectBase 컴포넌트가 없는 경우도 처리
                                                                       // 원하는 오브젝트 타입이 아니면 넘어간다.

            Collider lTargetCollider = objObjectBase.gameObject.GetComponent<Collider>();

            Vector3 directionToEnemy = (lTargetCollider.bounds.center - lMyCollider.bounds.center).normalized;

            if (Vector3.Angle(transform.forward, directionToEnemy) < 90) // 시야각 180도에서 보이면서
            {
                // 레이캐스트로 시야 내의 적과 장애물 여부 확인
                RaycastHit hit;

                if (Physics.Raycast(lMyCollider.bounds.center, directionToEnemy, out hit, 100)) // 쏘았을 때 맞았는데
                {
                    if (hit.collider.gameObject == GameManager.mAll_Of_Game_Objects[objObjectBase.mID]) //맞은게 목표물이면
                    {
                        return hit.collider.GetComponent<ObjectBase>().mID; // 해당 AI의 아이템 넘버를 반환한다
                    }
                }
            }
        }

        return -1; // 찾지 못했으면 -1을 반환
    }

    */


    #endregion


    #region 무기
    public void obtainWeapon(GameData.Weapon pWeapon)
    {


            mUsingWeapon = pWeapon; //먹은 무기를 들게 한다.

        if (mCurrentAmmo.ContainsKey(pWeapon)) //가지고 있는 무기면 시작 총알 더한다
        {
            mCurrentAmmo[pWeapon] += GameData.mWeaponDataDictionary[pWeapon].mInitBullets;

        }
        else  //가지고 있지 않은 무기면 시작 총알 대입한다.
        {
            mCurrentAmmo[pWeapon] = GameData.mWeaponDataDictionary[pWeapon].mInitBullets;
        }

    }
    public bool changeWeapon(GameData.Weapon pWeapon)
    {
        if (mCurrentAmmo.ContainsKey(pWeapon)) //가지고 있는 무기면
        {
            mUsingWeapon = pWeapon; //무기 변경
            return true;//잘 변경되었을 땐 true
        }
        else
        {
            return false;
        }



    }
    public void resetWeapon()
    {
        mUsingWeapon = GameData.Weapon.None;

        mCurrentAmmo.Clear();

    }
    #endregion


    #region 판정

    public float getHpPercentage()
    {
        return (float)mCurrentHP / (float)GameData.mMaxHP;
    }

    public float getAmmoPercentage()
    {
        return (float)mCurrentAmmo[mUsingWeapon] / (float)GameData.mWeaponDataDictionary[mUsingWeapon].mMaxBullets;

    }

    public void attacked(int pBeShotID) //내가 pID를 공격했을 때
    {
        reachedDestination(); //공격하면 정신차린다
    }

    public void beAttackedBy(int pShooterID)
    {
        reachedDestination(); //공격 당하면 정신차린다

        ObjectBase_AIBase lShooterAI = GameManager.mAll_Of_Game_Objects[pShooterID].GetComponent<ObjectBase_AIBase>();

        this.gameObject.transform.LookAt(lShooterAI.gameObject.transform); //나를 공격한 애를 바라본다.
        mBeAttacked_By_Enemy = pShooterID; //나를 공격한 애는 pShooterID
        mCurrentHP -= GameData.mWeaponDataDictionary[lShooterAI.mUsingWeapon].mDamage; // 적 무기 데미지만큼 체력 깎는다

        if (mCurrentHP <= 0) //내가죽었으면
        {
            lShooterAI.killed(mID);
            bekilledBy(pShooterID);
        }
    }
    public virtual void killed(int pDeadID) //내가 누군가를 죽였을 때
    {
        reachedDestination();

        GameDataSaver.SaveKillDeathResultsToCSV(this.gameObject.name, GameManager.mAll_Of_Game_Objects[pDeadID].name);
        //refreshDestination();// 적을 따라가다가 적이 죽으면 한번 도착했다고 새로고침 해줘야 다른 판단을 한다.
        //아이템으로 이동하다가 적이 죽어도 판단하에 계속 아이템으로 갈 것
    }
    public virtual void bekilledBy(int pKillerID) //내가 죽었을 때
    {
        reachedDestination();

        this.gameObject.SetActive(false);
    }


    public virtual void respawn()
    {

        if(searchItemNumber(mID, GameData.SearchType.Random, GameData.ObjectType.RespawnPlace, GameData.TeamType.Teammate) != -1) // 내 전용 리스폰 지역이 있는가?
        {
            this.gameObject.transform.position = GameManager.mAll_Of_Game_Objects[searchItemNumber(mID, GameData.SearchType.Random, GameData.ObjectType.RespawnPlace, GameData.TeamType.Teammate)].transform.position;
        }
        else
        {
            this.gameObject.transform.position = GameManager.mAll_Of_Game_Objects[searchItemNumber(mID, GameData.SearchType.Random, GameData.ObjectType.RespawnPlace, GameData.TeamType.All)].transform.position;
        }



        this.gameObject.SetActive(true);


        mCurrentHP = GameData.mMaxHP;
        resetWeapon();
        obtainWeapon(GameData.Weapon.Pistol);







    }


    #endregion

    public void shoot(int pID)
    {
        if (pID < 0) return; //인덱스
        transform.LookAt(GameManager.mAll_Of_Game_Objects[pID].transform); //바라보게 한다

        if(!mShootisPossible) return; //쿨타임
        mShootisPossible = false;

        if (mCurrentAmmo[mUsingWeapon] <= 0) return; //총알
        mCurrentAmmo[mUsingWeapon]--;

        Collider lMyCollider = GetComponent<Collider>();
        Collider lTargetCollider = GameManager.mAll_Of_Game_Objects[pID].GetComponent<Collider>();

        Vector3 shotDirection = lTargetCollider.bounds.center - lMyCollider.bounds.center; //정확한 방향

        float angleRange;
        //if (this.gameObject.GetComponent<NavMeshAgent>().velocity == Vector3.zero) angleRange = 0f; // 서서 쏠 땐 정확하다
        //else angleRange = 0.5f;

        angleRange = 0.0f;

        Quaternion yawRotation = Quaternion.AngleAxis(Random.Range(-angleRange, angleRange), Vector3.up); //상하 랜덤 오차
        Quaternion pitchRotation = Quaternion.AngleAxis(Random.Range(-angleRange, angleRange), Vector3.right); //좌우 랜덤 오차
        Vector3 imprecision = (yawRotation * pitchRotation) * shotDirection.normalized; // 오차 결과 방향

        Ray ray = new Ray(lMyCollider.bounds.center, shotDirection.normalized + imprecision); //방향 + 오차 결과 방향

        RaycastHit hit;

        if (!Physics.Raycast(ray, out hit, 200)) return; //아무것도 안맞았으면 리턴
        Debug.DrawLine(lMyCollider.bounds.center, hit.point, Color.red, 1f); // 적중 지점을 빨간색 선으로 표시

        if (hit.collider.gameObject.GetComponent<ObjectBase_AIBase>() == null) return;//AI가 맞은게 아니면 리턴
        attacked(hit.collider.gameObject.GetComponent<ObjectBase_AIBase>().mID); //hit.ID 를 공격했다
        hit.collider.gameObject.GetComponent<ObjectBase_AIBase>().beAttackedBy(mID); //내 ID로부터 공격당했다

        // 총알 소모
    }


    public float calculateNavMeshPathDistance(Vector3 sourcePosition, Vector3 targetPosition)
    {
        NavMeshPath path = new NavMeshPath();
        if (NavMesh.CalculatePath(sourcePosition, targetPosition, NavMesh.AllAreas, path))
        {
            // 경로의 총 거리를 초기화합니다.
            float totalDistance = 0.0f;

            // 경로 길이가 충분히 길지 확인합니다.
            if (path.corners.Length > 1)
            {
                // 경로의 모든 코너를 순회하며 거리를 계산합니다.
                for (int i = 0; i < path.corners.Length - 1; i++)
                {
                    // 연속된 두 코너 사이의 거리를 계산합니다.
                    Vector3 segment = path.corners[i + 1] - path.corners[i];
                    totalDistance += segment.magnitude;
                }
            }

            return totalDistance;
        }
        else
        {
            // 경로를 찾을 수 없는 경우, -1을 반환하여 경로 없음을 나타냅니다.
            return 1.0f;
        }
    }





    public void changeSpeed(float pAngularSpeed, float pSpeed, float pAcceleration, float pStoppingDistance) //쓰이지 않음
    {
        this.gameObject.GetComponent<NavMeshAgent>().angularSpeed = pAngularSpeed; // 회전 속도
        this.gameObject.GetComponent<NavMeshAgent>().speed = pSpeed; // 최대 속도
        this.gameObject.GetComponent<NavMeshAgent>().acceleration = pAcceleration; // 가속도
        this.gameObject.GetComponent<NavMeshAgent>().stoppingDistance = pStoppingDistance; // 멈추는 거리
    }





    public void moveStop()
    {
        this.gameObject.GetComponent<NavMeshAgent>().velocity = Vector3.zero;

    }

    public void moveLeftRight()
    {

    }

    public void moveTo(int pID, bool pIsFixed, int pCommandID) //이 함수를 가장 많이 사용하게 해야함
    {


        //TODO JYW 1. 고정시킬 것인가 2. 반복 가능한가, 3. ID //반복시킬 것인가를 넣으면 두 아이템 왔다갔다함
        //고정시킬것인가로 받았으면
        if (mIsFixed) return; //기존 목적지가 고정으로 설정되었다면 어떤 명령이든 도착하기 전까진 무시한다.(예: 안전한 곳으로 이동할 땐 적이 안보이게 되도 일단 안전한 곳으로 가야한다) // 특수한 상황만
        if (pCommandID == mCommandID) return; //이전 명령ID와 같으면 리턴

        if (pID == -1) return; // -1은 못찾은 결과이므로 허용하지 않음

        mIsFixed = pIsFixed;
        mCommandID = pCommandID;
        mDestinationItemNumber = pID; // pID가 현재 목표, 도착지점이거나 공격하거나 공격받거나 죽거나 죽이면 -1로 변경된다.
        mDestinationPositionThen = GameManager.mAll_Of_Game_Objects[mDestinationItemNumber].transform.position;
        this.gameObject.GetComponent<NavMeshAgent>().SetDestination(mDestinationPositionThen);
    }


}

