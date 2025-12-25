using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class ObjectBase_AIBase : ObjectBase
{
    // Cached components
    private NavMeshAgent _agent;
    private Animator _animator;
    private Collider _collider;

    private int mDestinationItemNumber = -1; //이동 목표 아이템
    private Vector3 mDestinationPositionThen = new Vector3(); //이동하려는 당시의 이동 목표 아이템의 위치 (가까워지면 Destination -1)

    private bool mIsFixed = false;
    int mCommandID = -1;

    private int mBeAttacked_By_Enemy = -1; //현재 나를 공격한 적

    [HideInInspector]
    public bool mIsRespawning = false;
    [HideInInspector]
    public int mCurrentHP = 0;

    [HideInInspector]
    public GameData.Weapon mUsingWeapon = GameData.Weapon.None; //쓰고있는 총 종류
    [HideInInspector]
    public Dictionary<GameData.Weapon, int> mCurrentAmmo = new Dictionary<GameData.Weapon, int>(); // 총 종류에 따른 총알 페어 딕셔너리

    private float mShootTimer;
    private bool mShootisPossible = false;


    protected override void Awake()
    {
        base.Awake();
        mObjectType = GameData.ObjectType.AI;
        // Cache components once
        _agent = GetComponent<NavMeshAgent>();
        _animator = GetComponent<Animator>();
        _collider = GetComponent<Collider>();
    }



    protected override void Start()
    {
        base.Start();
        respawn(); //게임을 시작하면 리스폰 시킨다 **************
    }


    protected override void Update()
    {
        base.Update();
        if (!gameObject.activeSelf) return;

        checkReachedItem();
        if (Vector3.Distance(mDestinationPositionThen, transform.position) <= 0.5f) reachedDestination();
        checkShootDelay();
        updateMotion();
        think();
    }

    public void checkReachedItem() //이동하면서 닿은 아이템들
    {
        foreach (var pair in GameManager.mAll_Of_Game_Objects)
        {
            var go = pair.Value;
            if (!go.activeSelf) continue;

            ObjectBase_ItemBase lItem = go.GetComponent<ObjectBase_ItemBase>();
            if (lItem == null) continue;
            if (Vector3.Distance(go.transform.position, transform.position) >= 0.5f) continue;

            lItem.action(mID);
        }
    }

    public void reachedDestination() //이동 목표지점 닿았을 때
    {
        mDestinationItemNumber = -1;
        mCommandID = -1;
        mIsFixed = false;
    }


    void checkShootDelay()
    {
        // 무기가 없으면 쿨타임 체크 불필요
        if (mUsingWeapon == GameData.Weapon.None) return;

        mShootTimer += Time.deltaTime;
        if (!mShootisPossible && GameData.mWeaponDataDictionary[mUsingWeapon].mShootDelay < mShootTimer)
        {
            mShootisPossible = true;
            mShootTimer = 0.0f;
        }
    }

    public void updateMotion()
    {
        if (_agent == null || _animator == null) return;

        bool isMoving = _agent.velocity.magnitude >= 1f;
        _animator.SetBool("isMoving", isMoving);
        _animator.SetBool("isHolding", !isMoving);
    }


    virtual public void think()
    {
        // 자식 클래스에서 구현
    }


    #region 탐색
    public virtual bool isVisible(int pItemNumber)
    {
        if (!_collider) _collider = GetComponent<Collider>();

        var targetGO = GameManager.mAll_Of_Game_Objects.ContainsKey(pItemNumber)
            ? GameManager.mAll_Of_Game_Objects[pItemNumber]
            : null;
        if (!targetGO) return false;

        Collider lTargetCollider = targetGO.GetComponent<Collider>();
        if (lTargetCollider == null) return false;

        Vector3 lDirectionToEnemy = (lTargetCollider.bounds.center - _collider.bounds.center).normalized; //pItemNumber 방향으로

        if (Vector3.Angle(transform.forward, lDirectionToEnemy) < 90) // 시야각 180도에서 보이면서
        {
            // 레이캐스트로 시야 내의 적과 장애물 여부 확인
            RaycastHit hit;

            if (Physics.Raycast(_collider.bounds.center, lDirectionToEnemy, out hit, 100)) // pItemNumber 방향으로 쏘았을 때 맞았는데
            {
                if (hit.collider.gameObject == targetGO) //맞은게 pItemNumber면
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
            var go = pair.Value;
            var objBase = go.GetComponent<ObjectBase>();
            if (!go.activeSelf) continue; // 죽은 애는 찾지 않는다
            if (objBase == null) continue; // ObjectBase 컴포넌트가 없는 경우 넘어간다.
            if (objBase.mID == pID) continue;// 자기 자신을 찾았다면 넘어간다.
            if (objBase.mObjectType != pObjectType) continue;// 찾고자 하는 오브젝트 타입이 아니면 넘어간다.

            if (pObjectType == GameData.ObjectType.AI) //AI는 팀 구분할 필요가 있음
            {
                if (pTeamType == GameData.TeamType.Teammate)
                {
                    if (go.GetComponent(GetType()) != null)
                    {
                        lIsSearchable.Add(objBase);
                    }
                }
                else if (pTeamType == GameData.TeamType.Enemy)
                {
                    if (go.GetComponent(GetType()) == null)
                    {
                        lIsSearchable.Add(objBase);
                    }
                }
                else if (pTeamType == GameData.TeamType.All)
                {
                    lIsSearchable.Add(objBase);
                }
            }
            else if (pObjectType == GameData.ObjectType.RespawnPlace) //리스폰 지점은 아군의 지점인지 적의 지점인지 구분할 필요가 있음
            {
                var place = go.GetComponent<ObjectBase_RespawnPlaceBase>();
                if (place == null) continue;

                if (pTeamType == GameData.TeamType.Teammate)
                {
                    if (place.mPlaceOwner != null && place.mPlaceOwner.GetType() == GetType())
                    {
                        lIsSearchable.Add(objBase);
                    }
                }
                else if (pTeamType == GameData.TeamType.Enemy)
                {
                    if (place.mPlaceOwner == null || place.mPlaceOwner.GetType() != GetType())
                    {
                        lIsSearchable.Add(objBase);
                    }
                }
                else if (pTeamType == GameData.TeamType.All)
                {
                    lIsSearchable.Add(objBase);
                }
            }
            else //각종 아이템들
            {
                lIsSearchable.Add(objBase);
            }
        }

        ////////////검색
        int lSearchItemNumber = -1;

        switch (pSearchType)
        {
            case GameData.SearchType.Visible: // 타입에 맞는 보이는 ObjectBase를 자식으로 가진 GameObject 탐색
                {
                    var myGO = GameManager.mAll_Of_Game_Objects.ContainsKey(pID) ? GameManager.mAll_Of_Game_Objects[pID] : null;
                    if (myGO == null) break;
                    Collider lMyCollider = myGO.GetComponent<Collider>();
                    if (!lMyCollider) break;

                    float closest = float.MaxValue;
                    RaycastHit hit;

                    foreach (var obj in lIsSearchable)
                    {
                        var targetCol = obj.gameObject.GetComponent<Collider>();
                        if (!targetCol) continue;

                        Vector3 lDirectionToEnemy = (targetCol.bounds.center - lMyCollider.bounds.center).normalized;

                        if (Vector3.Angle(transform.forward, lDirectionToEnemy) > 90) continue; // 시야각 180도 넘어가면 컨티뉴
                        if (!Physics.Raycast(lMyCollider.bounds.center, lDirectionToEnemy, out hit, 200)) continue;// 아무것도 안맞았으면 컨티뉴
                        if (hit.collider.gameObject != GameManager.mAll_Of_Game_Objects[obj.mID]) continue; //맞은게 목표물이 아니면 컨티뉴

                        // 가장 가까운 가시 목표 선택
                        float d = Vector3.Distance(myGO.transform.position, obj.transform.position);
                        if (d < closest)
                        {
                            closest = d;
                            lSearchItemNumber = obj.mID;
                        }
                    }
                }
                break;
            case GameData.SearchType.Closest: // 타입에 맞는 가까운 ObjectBase를 자식으로 가진 GameObject 탐색
                {
                    var myGO = GameManager.mAll_Of_Game_Objects.ContainsKey(pID) ? GameManager.mAll_Of_Game_Objects[pID] : null;
                    if (myGO == null) break;

                    float lClosestDistance = float.MaxValue;

                    foreach (var obj in lIsSearchable)
                    {
                        float distance = Vector3.Distance(myGO.transform.position, obj.gameObject.transform.position);

                        if (distance < lClosestDistance)
                        {
                            lSearchItemNumber = obj.mID;
                            lClosestDistance = distance;
                        }
                    }
                }
                break;

            case GameData.SearchType.Farthest: //타입에 맞는 멀리있는 ObjectBase를 자식으로 가진 GameObject 탐색
                {
                    var myGO = GameManager.mAll_Of_Game_Objects.ContainsKey(pID) ? GameManager.mAll_Of_Game_Objects[pID] : null;
                    if (myGO == null) break;

                    float lFarthestDistance = float.MinValue;
                    foreach (var obj in lIsSearchable)
                    {
                        float distance = Vector3.Distance(myGO.transform.position, obj.gameObject.transform.position);

                        if (distance > lFarthestDistance)
                        {
                            lSearchItemNumber = obj.mID;
                            lFarthestDistance = distance;
                        }
                    }
                }
                break;

            case GameData.SearchType.Safe: // 타입에 맞는 적과 멀리 떨어지고 나와 가까운 ObjectBase를 자식으로 가진 GameObject 탐색
                {
                    int lVisibleEnemy = searchItemNumber(pID, GameData.SearchType.Visible, GameData.ObjectType.AI, GameData.TeamType.Enemy);
                    if (lVisibleEnemy == -1) return -1; //내가 볼 수있는 적이 있어야 한다.

                    var myGO = GameManager.mAll_Of_Game_Objects.ContainsKey(pID) ? GameManager.mAll_Of_Game_Objects[pID] : null;
                    var enemyGO = GameManager.mAll_Of_Game_Objects.ContainsKey(lVisibleEnemy) ? GameManager.mAll_Of_Game_Objects[lVisibleEnemy] : null;
                    if (myGO == null || enemyGO == null) break;

                    float lSafestDistence = float.MaxValue; //가장 가까우면서 안전한 아이템을 찾아야한다. 최초값은 높이

                    foreach (var obj in lIsSearchable)
                    {
                        float lMeToEnemyDistance = Vector3.Distance(myGO.transform.position, enemyGO.transform.position);
                        float lEnemyToItemDistance = Vector3.Distance(enemyGO.transform.position, obj.gameObject.transform.position);
                        float lMeToItemDistance = Vector3.Distance(myGO.transform.position, obj.gameObject.transform.position);

                        if (lEnemyToItemDistance > lMeToEnemyDistance && lMeToItemDistance < lSafestDistence) //적과의 거리보다 적과 아이템의 거리가 더 멀면 비교적 안전 && 그 중 가까운 아이템
                        {
                            lSearchItemNumber = obj.mID;
                            lSafestDistence = lMeToItemDistance;
                        }
                    }
                }
                break;
            case GameData.SearchType.Random: // 타입에 맞는 랜덤 ObjectBase를 자식으로 가진 GameObject 탐색
                {
                    if (lIsSearchable.Count == 0) break;
                    lSearchItemNumber = lIsSearchable[UnityEngine.Random.Range(0, lIsSearchable.Count)].mID; //랜덤 인덱스 + 랜덤 마크
                }
                break;
        }
        return lSearchItemNumber; // 찾지 못했으면 -1을 반환
    }

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
        if (mUsingWeapon == GameData.Weapon.None) return 0f;
        int current = mCurrentAmmo.ContainsKey(mUsingWeapon) ? mCurrentAmmo[mUsingWeapon] : 0;
        return (float)current / (float)GameData.mWeaponDataDictionary[mUsingWeapon].mMaxBullets;
    }

    public void attacked(int pBeShotID) //내가 pID를 공격했을 때
    {
        reachedDestination(); //공격하면 정신차린다
    }

    public void beAttackedBy(int pShooterID)
    {
        reachedDestination(); //공격 당하면 정신차린다

        ObjectBase_AIBase lShooterAI = GameManager.mAll_Of_Game_Objects[pShooterID].GetComponent<ObjectBase_AIBase>();
        if (lShooterAI == null) return;

        transform.LookAt(lShooterAI.transform); //나를 공격한 애를 바라본다.
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

        GameDataSaver.SaveKillDeathResultsToCSV(gameObject.name, GameManager.mAll_Of_Game_Objects[pDeadID].name);
        //refreshDestination();// 적을 따라가다가 적이 죽으면 한번 도착했다고 새로고침 해줘야 다른 판단을 한다.
        //아이템으로 이동하다가 적이 죽어도 판단하에 계속 아이템으로 갈 것
    }

    public virtual void bekilledBy(int pKillerID) //내가 죽었을 때
    {
        reachedDestination();
        gameObject.SetActive(false);
    }


    public virtual void respawn()
    {
        int teammateRespawn = searchItemNumber(mID, GameData.SearchType.Random, GameData.ObjectType.RespawnPlace, GameData.TeamType.Teammate);
        int anyRespawn = teammateRespawn != -1 ? teammateRespawn : searchItemNumber(mID, GameData.SearchType.Random, GameData.ObjectType.RespawnPlace, GameData.TeamType.All);
        if (anyRespawn != -1 && GameManager.mAll_Of_Game_Objects.ContainsKey(anyRespawn))
        {
            transform.position = GameManager.mAll_Of_Game_Objects[anyRespawn].transform.position;
        }

        gameObject.SetActive(true);

        mCurrentHP = GameData.mMaxHP;
        resetWeapon();
        obtainWeapon(GameData.Weapon.Pistol);
    }


    #endregion

    public void shoot(int pID)
    {
        if (pID < 0) return; //인덱스
        if (mUsingWeapon == GameData.Weapon.None) return; //무기 없음
        if (!GameManager.mAll_Of_Game_Objects.ContainsKey(pID)) return;

        transform.LookAt(GameManager.mAll_Of_Game_Objects[pID].transform); //바라보게 한다

        if (!mShootisPossible) return; //쿨타임
        mShootisPossible = false;

        int currentAmmo = mCurrentAmmo.ContainsKey(mUsingWeapon) ? mCurrentAmmo[mUsingWeapon] : 0;
        if (currentAmmo <= 0) return; //총알
        mCurrentAmmo[mUsingWeapon] = currentAmmo - 1;

        if (!_collider) _collider = GetComponent<Collider>();
        Collider lTargetCollider = GameManager.mAll_Of_Game_Objects[pID].GetComponent<Collider>();
        if (!_collider || !lTargetCollider) return;

        Vector3 shotDirection = lTargetCollider.bounds.center - _collider.bounds.center; //정확한 방향

        float angleRange = 0.0f;

        Quaternion yawRotation = Quaternion.AngleAxis(Random.Range(-angleRange, angleRange), Vector3.up); //상하 랜덤 오차
        Quaternion pitchRotation = Quaternion.AngleAxis(Random.Range(-angleRange, angleRange), Vector3.right); //좌우 랜덤 오차
        Vector3 imprecision = (yawRotation * pitchRotation) * shotDirection.normalized; // 오차 결과 방향

        Ray ray = new Ray(_collider.bounds.center, shotDirection.normalized + imprecision); //방향 + 오차 결과 방향

        RaycastHit hit;

        if (!Physics.Raycast(ray, out hit, 200)) return; //아무것도 안맞았으면 리턴
        Debug.DrawLine(_collider.bounds.center, hit.point, Color.red, 1f); // 적중 지점을 빨간색 선으로 표시

        ObjectBase_AIBase targetAI = hit.collider.gameObject.GetComponent<ObjectBase_AIBase>();
        if (targetAI == null) return;//AI가 맞은게 아니면 리턴
        attacked(targetAI.mID); //hit.ID 를 공격했다
        targetAI.beAttackedBy(mID); //내 ID로부터 공격당했다
    }


    public float calculateNavMeshPathDistance(Vector3 sourcePosition, Vector3 targetPosition)
    {
        NavMeshPath path = new NavMeshPath();
        if (NavMesh.CalculatePath(sourcePosition, targetPosition, NavMesh.AllAreas, path))
        {
            float totalDistance = 0.0f;

            if (path.corners.Length > 1)
            {
                for (int i = 0; i < path.corners.Length - 1; i++)
                {
                    Vector3 segment = path.corners[i + 1] - path.corners[i];
                    totalDistance += segment.magnitude;
                }
            }

            return totalDistance;
        }
        else
        {
            // 경로를 찾을 수 없는 경우, 양수(예: -1f)로 나타냅니다.
            return -1.0f;
        }
    }

    public void changeSpeed(float pAngularSpeed, float pSpeed, float pAcceleration, float pStoppingDistance) //쓰이지 않음
    {
        if (_agent == null) _agent = GetComponent<NavMeshAgent>();
        if (_agent == null) return;

        _agent.angularSpeed = pAngularSpeed; // 회전 속도
        _agent.speed = pSpeed; // 최대 속도
        _agent.acceleration = pAcceleration; // 가속도
        _agent.stoppingDistance = pStoppingDistance; // 멈추는 거리
    }

    public void moveStop()
    {
        if (_agent == null) _agent = GetComponent<NavMeshAgent>();
        if (_agent == null) return;
        _agent.velocity = Vector3.zero;
    }

    public void moveLeftRight()
    {
        // 좌우 이동 패턴은 필요 시 구현
    }

    public void moveTo(int pID, bool pIsFixed, int pCommandID) //이 함수를 가장 많이 사용하게 해야함
    {
        if (_agent == null) _agent = GetComponent<NavMeshAgent>();
        if (_agent == null) return;

        // 고정 목적지인 경우 도착 전까지 다른 명령 무시
        if (mIsFixed) return;
        if (pCommandID == mCommandID) return; //이전 명령ID와 같으면 리턴
        if (pID == -1) return; // -1은 못찾은 결과이므로 허용하지 않음
        if (!GameManager.mAll_Of_Game_Objects.ContainsKey(pID)) return;

        mIsFixed = pIsFixed;
        mCommandID = pCommandID;
        mDestinationItemNumber = pID; // pID가 현재 목표, 도착지점이거나 공격하거나 공격받거나 죽거나 죽이면 -1로 변경된다.
        mDestinationPositionThen = GameManager.mAll_Of_Game_Objects[mDestinationItemNumber].transform.position;
        _agent.SetDestination(mDestinationPositionThen);
    }
}

