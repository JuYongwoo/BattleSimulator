using System.Collections.Generic;
using UnityEngine;

public class ObjectBase_AIBase : ObjectBase
{
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

    // Grid path state
    private readonly List<Vector3> mCurrentPath = new List<Vector3>();
    private int mPathIndex = 0;
    private Vector3 mLastPosition;
    public float mMoveSpeed = 3.5f; // movement speed units/sec

    protected override void Awake()
    {
        base.Awake();
        mObjectType = GameData.ObjectType.AI;
    }

    protected void Start()
    {
        Respawn(); //게임을 시작하면 리스폰 시킨다 **************
        mLastPosition = transform.position;
    }

    protected void Update()
    {
        if (this.gameObject.activeSelf == false) return;
        FollowPath();
        CheckReachedItem();
        if (Vector3.Distance(mDestinationPositionThen, this.gameObject.transform.position) <= 0.6f) ReachedDestination(); // 약간 완화
        CheckShootDelay();
        UpdateMotion();
        think();
    }

    public void CheckReachedItem() //이동하면 서 닿은 아이템들
    {
        foreach (var pair in ObjectManager.mAll_Of_Game_Objects)
        {
            ObjectBase_ItemBase lItem = pair.Value.GetComponent<ObjectBase_ItemBase>();
            if (lItem == null) continue;
            if (Vector3.Distance(pair.Value.transform.position, this.gameObject.transform.position) >= 0.6f) continue; // 약간 완화
            if (!pair.Value.activeSelf) continue;

            lItem.action(mID);
        }
    }

    public void ReachedDestination() //이동 목표지점 닿았을 때
    {
        mDestinationItemNumber = -1;
        mCommandID = -1;
        mIsFixed = false;
        mCurrentPath.Clear();
        mPathIndex = 0;
    }

    void CheckShootDelay()
    {
        mShootTimer += Time.deltaTime;
        if (!mShootisPossible && GameData.mWeaponDataDictionary[mUsingWeapon].mShootDelay < mShootTimer)
        {
            mShootisPossible = true;
            mShootTimer = 0.0f;
        }
    }

    public void UpdateMotion()
    {
        // infer velocity from position delta since we do not use NavMeshAgent anymore
        Vector3 velocity = (transform.position - mLastPosition) / Mathf.Max(Time.deltaTime, 0.0001f);
        mLastPosition = transform.position;

        if (velocity.magnitude < 1f)
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

    private void FollowPath()
    {
        if (mDestinationItemNumber != -1 && mCurrentPath.Count == 0)
        {
            // 경로가 비었으면 목적지로 직접 미세 조정 이동
            Vector3 toDest = mDestinationPositionThen - transform.position;
            toDest.y = 0f;
            if (toDest.sqrMagnitude > 0.0001f)
            {
                Vector3 dir = toDest.normalized;
                transform.position += dir * mMoveSpeed * Time.deltaTime;
            }
            return;
        }

        if (mPathIndex >= mCurrentPath.Count)
        {
            // 경로 끝에 도달: 목적지에 충분히 가깝지 않으면 목적지로 직접 접근
            Vector3 toDest = mDestinationPositionThen - transform.position;
            toDest.y = 0f;
            if (toDest.magnitude <= 0.6f)
            {
                ReachedDestination();
                return;
            }
            else
            {
                if (toDest.sqrMagnitude > 0.0001f)
                {
                    Vector3 dir = toDest.normalized;
                    transform.position += dir * mMoveSpeed * Time.deltaTime;
                }
                return;
            }
        }

        Vector3 target = mCurrentPath[mPathIndex];
        Vector3 to = target - transform.position;
        to.y = 0f;
        float dist = to.magnitude;
        if (dist < 0.05f)
        {
            mPathIndex++;
            if (mPathIndex >= mCurrentPath.Count)
            {
                // 마지막 웨이포인트에 도달했지만 목적지까지 남아있을 수 있음
                return;
            }
            target = mCurrentPath[mPathIndex];
            to = target - transform.position;
            to.y = 0f;
        }
        if (to.sqrMagnitude > 0.0001f)
        {
            Vector3 dir = to.normalized;
            transform.position += dir * mMoveSpeed * Time.deltaTime;
        }
    }

    #region 탐색
    public virtual bool isVisible(int pItemNumber)
    {
        Collider lMyCollider = this.gameObject.GetComponent<Collider>();
        Collider lTargetCollider = ObjectManager.mAll_Of_Game_Objects[pItemNumber].GetComponent<Collider>();

        Vector3 lDirectionToEnemy = (lTargetCollider.bounds.center - lMyCollider.bounds.center).normalized; //pItemNumber 방향으로

        if (Vector3.Angle(transform.forward, lDirectionToEnemy) < 90) // 시야각 180도에서 보이면서
        {
            // 레이캐스트로 시야 내의 적과 장애물 여부 확인
            RaycastHit hit;

            if (Physics.Raycast(lMyCollider.bounds.center, lDirectionToEnemy, out hit, 100)) // pItemNumber 방향으로 쏘았을 때 맞았는데
            {
                if (hit.collider.gameObject == ObjectManager.mAll_Of_Game_Objects[pItemNumber]) //맞은게 pItemNumber면
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

        foreach (var pair in ObjectManager.mAll_Of_Game_Objects)
        {
            if (!pair.Value.activeSelf) continue; // 죽은 애는 찾지 않는다
            if (pair.Value.GetComponent<ObjectBase>() == null) continue; // ObjectBase 컴포넌트가 없는 경우 넘어간다.
            if (pair.Value.GetComponent<ObjectBase>().mID == pID) continue; // 자기 자신을 찾았다면 넘어간다.
            if (pair.Value.GetComponent<ObjectBase>().mObjectType != pObjectType) continue; // 찾고자 하는 오브젝트 타입이 아니면 넘어간다.

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
                else if (pTeamType == GameData.TeamType.All)
                {
                    lIsSearchable.Add(pair.Value.GetComponent<ObjectBase>());
                }
            }
            else if (pObjectType == GameData.ObjectType.RespawnPlace) //리스폰 지점은 아군의 지점인지 적의 지점인지 구분할 필요가 있음
            {
                if (pTeamType == GameData.TeamType.Teammate)
                {
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
                else if (pTeamType == GameData.TeamType.All)
                {
                    lIsSearchable.Add(pair.Value.GetComponent<ObjectBase>());
                }
            }
            else //각종 아이템들
            {
                lIsSearchable.Add(pair.Value.GetComponent<ObjectBase>());
            }
        }

        int lSearchItemNumber = -1;

        switch (pSearchType)
        {
            case GameData.SearchType.Visible: // 타입에 맞는 보이는 ObjectBase를 자식으로 가진 GameObject 탐색
                Collider lMyCollider = ObjectManager.mAll_Of_Game_Objects[pID].GetComponent<Collider>();
                RaycastHit hit;

                foreach (var obj in lIsSearchable)
                {
                    ObjectBase lObjectBase = obj;

                    Vector3 lDirectionToEnemy = (lObjectBase.gameObject.GetComponent<Collider>().bounds.center - lMyCollider.bounds.center).normalized;

                    if (Vector3.Angle(transform.forward, lDirectionToEnemy) > 90) continue; // 시야각 180도 넘어가면 컨티뉴
                    if (!Physics.Raycast(lMyCollider.bounds.center, lDirectionToEnemy, out hit, 200)) continue; // 아무것도 안맞았으면 컨티뉴
                    if (hit.collider.gameObject != ObjectManager.mAll_Of_Game_Objects[lObjectBase.mID]) continue; //맞은게 목표물이 아니면 컨티뉴

                    lSearchItemNumber = hit.collider.GetComponent<ObjectBase>().mID;
                }
                break;
            case GameData.SearchType.Closest: // 타입에 맞는 가까운 ObjectBase를 자식으로 가진 GameObject 탐색
                float lClosestDistance = float.MaxValue;

                foreach (var obj in lIsSearchable)
                {
                    float distance = Vector3.Distance(ObjectManager.mAll_Of_Game_Objects[pID].transform.position, obj.gameObject.transform.position);

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
                    float distance = Vector3.Distance(ObjectManager.mAll_Of_Game_Objects[pID].transform.position, obj.gameObject.transform.position);

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
                    float lMeToEnemyDistance = Vector3.Distance(ObjectManager.mAll_Of_Game_Objects[pID].transform.position, ObjectManager.mAll_Of_Game_Objects[lVisibleEnemy].transform.position);
                    float lEnemyToItemDistance = Vector3.Distance(ObjectManager.mAll_Of_Game_Objects[lVisibleEnemy].transform.position, obj.gameObject.transform.position);
                    float lMeToItemDistance = Vector3.Distance(ObjectManager.mAll_Of_Game_Objects[pID].transform.position, obj.gameObject.transform.position);

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
    #endregion

    #region 무기
    public void ObtainWeapon(GameData.Weapon pWeapon)
    {
        mUsingWeapon = pWeapon; //먹은 무기를 들게 한다.

        if (mCurrentAmmo.ContainsKey(pWeapon)) //가지고 있는 무기면 시작 총알 더한다
        {
            mCurrentAmmo[pWeapon] += GameData.mWeaponDataDictionary[pWeapon].mInitBullets;
        }
        else //가지고 있지 않은 무기면 시작 총알 대입한다.
        {
            mCurrentAmmo[pWeapon] = GameData.mWeaponDataDictionary[pWeapon].mInitBullets;
        }
    }

    public bool ChangeWeapon(GameData.Weapon pWeapon)
    {
        if (mCurrentAmmo.ContainsKey(pWeapon)) //가지고 있는 무기면
        {
            mUsingWeapon = pWeapon; //무기 변경
            return true; //잘 변경되었을 땐 true
        }
        else
        {
            return false;
        }
    }

    public void ResetWeapon()
    {
        mUsingWeapon = GameData.Weapon.None;
        mCurrentAmmo.Clear();
    }
    #endregion

    #region 판정
    public float GetHpPercentage()
    {
        return (float)mCurrentHP / (float)GameData.mMaxHP;
    }

    public float GetAmmoPercentage()
    {
        return (float)mCurrentAmmo[mUsingWeapon] / (float)GameData.mWeaponDataDictionary[mUsingWeapon].mMaxBullets;
    }

    public void Attacked(int pBeShotID) //내가 pID를 공격했을 때
    {
        ReachedDestination(); //공격하면 정신차린다
    }

    public void AttackedBy(int pShooterID)
    {
        ReachedDestination(); //공격 당하면 정신차린다

        ObjectBase_AIBase lShooterAI = ObjectManager.mAll_Of_Game_Objects[pShooterID].GetComponent<ObjectBase_AIBase>();

        this.gameObject.transform.LookAt(lShooterAI.gameObject.transform); //나를 공격한 애를 바라본다.
        mBeAttacked_By_Enemy = pShooterID; //나를 공격한 애는 pShooterID
        mCurrentHP -= GameData.mWeaponDataDictionary[lShooterAI.mUsingWeapon].mDamage; // 적 무기 데미지만큼 체력 깎는다

        if (mCurrentHP <= 0) //내가죽었으면
        {
            lShooterAI.Killed(mID);
            killedBy(pShooterID);
        }
    }

    public virtual void Killed(int pDeadID) //내가 누군가를 죽였을 때
    {
        ReachedDestination();
        GameDataSaver.SaveKillDeathResultsToCSV(this.gameObject.name, ObjectManager.mAll_Of_Game_Objects[pDeadID].name);
    }

    public virtual void killedBy(int pKillerID) //내가 죽었을 때
    {
        ReachedDestination();
        this.gameObject.SetActive(false);
    }

    public virtual void Respawn()
    {
        if (searchItemNumber(mID, GameData.SearchType.Random, GameData.ObjectType.RespawnPlace, GameData.TeamType.Teammate) != -1) // 내 전용 리스폰 지역이 있는가?
        {
            this.gameObject.transform.position = ObjectManager.mAll_Of_Game_Objects[searchItemNumber(mID, GameData.SearchType.Random, GameData.ObjectType.RespawnPlace, GameData.TeamType.Teammate)].transform.position;
        }
        else
        {
            this.gameObject.transform.position = ObjectManager.mAll_Of_Game_Objects[searchItemNumber(mID, GameData.SearchType.Random, GameData.ObjectType.RespawnPlace, GameData.TeamType.All)].transform.position;
        }

        this.gameObject.SetActive(true);

        mCurrentHP = GameData.mMaxHP;
        ResetWeapon();
        ObtainWeapon(GameData.Weapon.Pistol);

        mCurrentPath.Clear();
        mPathIndex = 0;
    }
    #endregion

    public void Shoot(int pID)
    {
        if (pID < 0) return; //인덱스
        transform.LookAt(ObjectManager.mAll_Of_Game_Objects[pID].transform); //바라보게 한다

        if (!mShootisPossible) return; //쿨타임
        mShootisPossible = false;

        if (mCurrentAmmo[mUsingWeapon] <= 0) return; //총알
        mCurrentAmmo[mUsingWeapon]--;

        Collider lMyCollider = GetComponent<Collider>();
        Collider lTargetCollider = ObjectManager.mAll_Of_Game_Objects[pID].GetComponent<Collider>();

        Vector3 shotDirection = lTargetCollider.bounds.center - lMyCollider.bounds.center; //정확한 방향

        float angleRange = 0.0f;

        Quaternion yawRotation = Quaternion.AngleAxis(Random.Range(-angleRange, angleRange), Vector3.up); //상하 랜덤 오차
        Quaternion pitchRotation = Quaternion.AngleAxis(Random.Range(-angleRange, angleRange), Vector3.right); //좌우 랜덤 오차
        Vector3 imprecision = (yawRotation * pitchRotation) * shotDirection.normalized; // 오차 결과 방향

        Ray ray = new Ray(lMyCollider.bounds.center, shotDirection.normalized + imprecision); //방향 + 오차 결과 방향

        RaycastHit hit;

        if (!Physics.Raycast(ray, out hit, 200)) return; //아무것도 안맞았으면 리턴
        Debug.DrawLine(lMyCollider.bounds.center, hit.point, Color.red, 1f); // 적중 지점을 빨간색 선으로 표시

        if (hit.collider.gameObject.GetComponent<ObjectBase_AIBase>() == null) return; //AI가 맞은게 아니면 리턴
        Attacked(hit.collider.gameObject.GetComponent<ObjectBase_AIBase>().mID); //hit.ID 를 공격했다
        hit.collider.gameObject.GetComponent<ObjectBase_AIBase>().AttackedBy(mID); //내 ID로부터 공격당했다
    }

    public float CalculateNavMeshPathDistance(Vector3 sourcePosition, Vector3 targetPosition)
    {
        // Replaced with grid path distance
        var path = GridPathfinder.FindPath(sourcePosition, targetPosition);
        if (path == null || path.Count == 0) return 1.0f;
        float total = 0f;
        Vector3 prev = sourcePosition;
        foreach (var p in path)
        {
            total += Vector3.Distance(prev, p);
            prev = p;
        }
        return total;
    }

    public void ChangeSpeed(float pAngularSpeed, float pSpeed, float pAcceleration, float pStoppingDistance) //쓰이지 않음
    {
        // No NavMeshAgent usage; map to movement speed
        mMoveSpeed = pSpeed;
    }

    public void MoveStop()
    {
        // Stop following path
        mCurrentPath.Clear();
        mPathIndex = 0;
    }

    public void moveLeftRight()
    {
    }

    public void MoveTo(int pID, bool pIsFixed, int pCommandID) //이 함수를 가장 많이 사용하게 해야함
    {
        if (mIsFixed) return; //기존 목적지가 고정으로 설정되었다면 어떤 명령이든 도착하기 전까진 무시한다.
        if (pCommandID == mCommandID) return; //이전 명령ID와 같으면 리턴

        if (pID == -1) return; // -1은 못찾은 결과이므로 허용하지 않음

        mIsFixed = pIsFixed;
        mCommandID = pCommandID;
        mDestinationItemNumber = pID; // pID가 현재 목표, 도착지점이거나 공격하거나 공격받거나 죽거나 죽이면 -1로 변경된다.
        mDestinationPositionThen = ObjectManager.mAll_Of_Game_Objects[mDestinationItemNumber].transform.position;

        // Compute grid path and start following
        mCurrentPath.Clear();
        mPathIndex = 0;
        var path = GridPathfinder.FindPath(transform.position, mDestinationPositionThen);
        if (path != null && path.Count > 0)
        {
            mCurrentPath.AddRange(path);
            // 마지막에 실제 목적지 좌표를 웨이포인트로 추가해 반 셀 위치도 정확히 접근
            mCurrentPath.Add(mDestinationPositionThen);
        }
    }
}

