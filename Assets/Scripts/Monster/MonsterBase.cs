using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 怪物AI状态枚举
/// </summary>
public enum E_MonsterState
{
    Idle,       // 待机
    Patrol,     // 巡逻
    Chase,      // 追击
    Attack,     // 攻击
    Dead,       // 死亡
}

/// <summary>
/// 掉落物品配置
/// </summary>
[System.Serializable]
public struct DropEntry
{
    [Tooltip("AB包中Material下的物品名，需与一致")]
    public string itemName;

    [Tooltip("掉落概率")]
    [Range(0f, 1f)]
    public float probability;

    public int minCount;
    public int maxCount;
}

public abstract class MonsterBase : MonoBehaviour
{
    [Header("基本属性")]
    public string monsterName;
    [Header("生命")]
    public float maxHealth = 100f;
    public float nowHealth;
    [Header("攻击")]
    public float atk = 10f;
    [Header("移动速度")]
    public float moveSpeed = 2f;

    [Header("AI参数")]
    [Header("追击范围")]
    public float chaseRange = 5f;
    [Header("攻击范围")]
    public float atkRange = 1.5f;
    [Header("攻击CD")]
    public float atkCoolDown = 1f;
    [Header("巡逻半径")]
    public float patrolRadius = 3f;

    [Tooltip("离开追击范围后持续追击的缓冲时间，避免反复横跳")]
    public float chaseLingerTime = 2f;

    [Header("掉落配置")]
    public DropEntry[] dropTable;

    [Header("受伤闪烁")]
    public float hitFlashDuration = 0.1f;

    //组件引用
    protected Transform player;
    protected SpriteRenderer sr;
    protected Animator animator;

    //运行时状态
    protected E_MonsterState currentState;
    //出生坐标
    protected Vector2 spawnPoint;
    //锁定的目标
    protected Vector2 patrolTarget;
    /// <summary>
    /// 巡逻等待时间
    /// </summary>
    protected float patrolWaitTimer;
    /// <summary>
    /// 巡逻移动时间
    /// </summary>
    protected float patrolMoveTimer;
    /// <summary>
    /// 攻击间隔时间
    /// </summary>
    protected float attackTimer;
    /// <summary>
    /// 追击缓冲计时器
    /// </summary>
    protected float chaseLingerTimer;
    /// <summary>
    /// 是否死亡
    /// </summary>
    protected bool isDead;

    //存档追踪 由MapGenerator生成时注入
    public Vector2Int chunkCoord;
    public Vector2Int localPos;
    /// <summary>
    /// 唯一UID
    /// </summary>
    public int monsterUID;

    //类可覆写的行为参数
    protected virtual float PatrolWaitMin => 1f;
    protected virtual float PatrolWaitMax => 3f;
    protected virtual float PatrolMoveMin => 1.5f;
    protected virtual float PatrolMoveMax => 4f;

    protected virtual void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();

        nowHealth = maxHealth;
        spawnPoint = transform.position;

        if (player == null)
        {
            var go = GameObject.FindGameObjectWithTag("Player");
            if (go != null) player = go.transform;
        }
    }

    protected virtual void Start()
    {
        //初始化怪物UID
        monsterUID = GenerateUID();
        //生成后可立马攻击
        attackTimer = atkCoolDown;
        //进入待机状态
        EnterState(E_MonsterState.Idle);
    }

    protected virtual void Update()
    {
        if (isDead || player == null) return;

        attackTimer += Time.deltaTime;
        //根据目前的状态执行对应的逻辑
        switch (currentState)
        {
            case E_MonsterState.Idle: IdleUpdate(); break;
            case E_MonsterState.Patrol: PatrolUpdate(); break;
            case E_MonsterState.Chase: ChaseUpdate(); break;
            case E_MonsterState.Attack: AttackUpdate(); break;
        }

        DestroySelf();

        sr.sortingLayerName = "Objects";
        sr.sortingOrder = -(int)(transform.position.y * 100);
    }


    /// <summary>
    /// 受伤逻辑
    /// </summary>
    /// <param name="amount"></param>
    public virtual void TakeDamage(float amount)
    {
        if (isDead) return;

        nowHealth -= amount;
        //受击之后变红
        StartCoroutine(FlashCoroutine());

        //待机/巡逻中受击之后立刻追击
        if (currentState == E_MonsterState.Idle ||
            currentState == E_MonsterState.Patrol)
        {
            EnterState(E_MonsterState.Chase);
        }

        if (nowHealth <= 0)
        {
            nowHealth = 0;
            Die();
        }
    }

    /// <summary>
    /// 初始化坐标信息
    /// </summary>
    /// <param name="chunk"></param>
    /// <param name="local"></param>
    public void SetArchiveInfo(Vector2Int chunk, Vector2Int local)
    {
        chunkCoord = chunk;
        localPos = local;
    }

    /// <summary>
    /// 改变怪物逻辑
    /// </summary>
    /// <param name="newState"></param>
    protected virtual void EnterState(E_MonsterState newState)
    {
        currentState = newState;

        switch (newState)
        {
            case E_MonsterState.Idle:
                patrolWaitTimer = Random.Range(PatrolWaitMin, PatrolWaitMax);
                animator.SetBool("isRun", false);
                break;

            case E_MonsterState.Patrol:
                PickNewPatrolTarget();
                patrolMoveTimer = Random.Range(PatrolMoveMin, PatrolMoveMax);
                animator.SetBool("isRun", true);
                break;

            case E_MonsterState.Chase:
                chaseLingerTimer = 0f;
                animator.SetBool("isRun", true);
                break;

            case E_MonsterState.Attack:
                attackTimer = atkCoolDown;
                break;
        }
    }
    /// <summary>
    /// 待机逻辑
    /// </summary>
    protected virtual void IdleUpdate()
    {
        if (IsPlayerInRange(chaseRange))
        {
            EnterState(E_MonsterState.Chase);
            return;
        }

        patrolWaitTimer -= Time.deltaTime;
        if (patrolWaitTimer <= 0f)
            EnterState(E_MonsterState.Patrol);
    }
    /// <summary>
    /// 巡逻逻辑
    /// </summary>
    protected virtual void PatrolUpdate()
    {
        //如果玩家在追击范围内
        if (IsPlayerInRange(chaseRange))
        {
            //进入追击状态
            EnterState(E_MonsterState.Chase);
            return;
        }
        //通过巡逻初始坐标和自己坐标去获得一个追击方向向量
        Vector2 dir = (patrolTarget - (Vector2)transform.position).normalized;
        MoveTowards(dir);
        //巡逻时间减少
        patrolMoveTimer -= Time.deltaTime;
        float dist = Vector2.Distance(transform.position, patrolTarget);
        //如果巡逻时间为0或者走到了巡逻点 就进入待机状态
        if (dist < 0.1f || patrolMoveTimer <= 0f)
            EnterState(E_MonsterState.Idle);
    }

    /// <summary>
    /// 追击逻辑
    /// </summary>
    protected virtual void ChaseUpdate()
    {
        //获得与玩家之间的距离
        float dist = DistanceToPlayer();
        //如果距离已经到了攻击范围 就切换到攻击状态
        if (dist <= atkRange)
        {
            EnterState(E_MonsterState.Attack);
            return;
        }
        //如果与玩家之间的距离大于追击距离
        if (dist > chaseRange)
        {
            //就开始计算追击缓冲计时器 如果大于追击缓冲时间 就放弃追逐 进入巡逻状态
            chaseLingerTimer += Time.deltaTime;
            if (chaseLingerTimer >= chaseLingerTime)
            {
                EnterState(E_MonsterState.Patrol);
                return;
            }
        }
        else
        {
            //小于追击距离就继续追击
            chaseLingerTimer = 0f;
        }
        //不断地去找到玩家的位置来追击玩家
        Vector2 dir = (player.position - transform.position).normalized;
        MoveTowards(dir);
    }
    /// <summary>
    /// 攻击逻辑
    /// </summary>
    protected virtual void AttackUpdate()
    {
        float dist = DistanceToPlayer();
        //如果距离大于攻击范围 就变成追击状态
        if (dist > atkRange)
        {
            EnterState(E_MonsterState.Chase);
            return;
        }
        //把自己的面部朝向玩家
        FacePlayer();
        //attackTimer在Update里面更新
        if (attackTimer >= atkCoolDown)
        {
            PerformAttack();
            attackTimer = 0f;
        }
    }

    /// <summary>执行一次攻击，子类覆写以播放动画和音效</summary>
    protected virtual void PerformAttack()
    {
        animator.SetBool("isAtk", true);
    }

    /// <summary>死亡</summary>
    protected virtual void Die()
    {
        isDead = true;

        // 标记存档：此位置怪物已死，下次加载不再生成
        // （ArchiveManager 中需新增 MarkMonsterKilled / IsMonsterKilled）
        //ArchiveManager.Instance.MarkMonsterKilled(chunkCoord, localPos, monsterUID);

        // 禁用碰撞，避免死亡动画期间再次被命中
        var col = GetComponent<Collider2D>();
        if (col != null) col.enabled = false;
        //生成掉落物
        SpawnDrops();
        //开始死亡逻辑的协程
        StartCoroutine(DeathCoroutine());
    }

    /// <summary>死亡协程：可在此播放死亡动画后销毁</summary>
    protected virtual IEnumerator DeathCoroutine()
    {
        // 如果有死亡动画: yield return new WaitForSeconds(animLength);
        yield return null;
        Destroy(gameObject);
    }
    /// <summary>
    /// 生成掉落物
    /// </summary>
    protected virtual void SpawnDrops()
    {
        if (dropTable == null || dropTable.Length == 0) 
            return;
        //遍历掉落物数组
        foreach (var entry in dropTable)
        {
            //如果随机值大于掉落率 就不掉落这个掉落物
            if (Random.value > entry.probability) 
                continue;
            //生成掉落数量
            int count = Random.Range(entry.minCount, entry.maxCount + 1);
            //生成的值小于等于0就不掉落这个掉落物
            if (count <= 0) 
                continue;
            //加载掉落物
            ABResMgr.Instance.LoadResAsync<GameObject>("Material", entry.itemName, (prefab) =>
            {
                if (prefab == null)
                {
                    Debug.LogWarning($"MonsterBase: 掉落物预制体未找到 {entry.itemName}");
                    return;
                }

                for (int i = 0; i < count; i++)
                {
                    // 在怪物脚下随机散布
                    Vector2 dropPos = (Vector2)transform.position + Random.insideUnitCircle * 0.8f;
                    var drop = Instantiate(prefab, dropPos, Quaternion.identity);
                    drop.name = entry.itemName;
                    drop.GetComponent<SpriteRenderer>().sortingLayerName = GetComponent<SpriteRenderer>().sortingLayerName;
                    var item = drop.GetComponent<ItemBase>();
                    if (item != null)
                    {
                        item.isWorldItem = true;
                        item.count = 1;
                        item.transform.localScale *= 4;
                        //item.RegisterToWorld(); // 走现有世界物品存档逻辑
                    }
                }
            });
        }
    }

    /// <summary>
    /// 移动到目标点逻辑
    /// </summary>
    /// <param name="direction">方向向量</param>
    protected virtual void MoveTowards(Vector2 direction)
    {
        transform.Translate(direction * moveSpeed * Time.deltaTime, Space.World);
        UpdateFacing(direction);
    }
    /// <summary>
    /// 面向玩家逻辑
    /// </summary>
    protected virtual void FacePlayer()
    {
        if (player == null) return;
        Vector2 dir = (player.position - transform.position).normalized;
        UpdateFacing(dir);
    }

    /// <summary>
    /// 改变怪物的朝向 左右反转
    /// </summary>
    /// <param name="direction"></param>
    protected virtual void UpdateFacing(Vector2 direction)
    {
        if (direction.x > 0.01f)
            transform.localEulerAngles = Vector3.zero;
        else if (direction.x < -0.01f)
            transform.localEulerAngles = new Vector3(0, 180, 0);
    }

    /// <summary>
    /// 判断玩家和怪物自身之间的距离
    /// </summary>
    /// <returns></returns>
    protected float DistanceToPlayer()
    {
        if (player == null) return float.MaxValue;
        return Vector2.Distance(transform.position, player.position);
    }
    /// <summary>
    /// 判断玩家是不是在自己的巡逻范围内
    /// </summary>
    /// <param name="range"></param>
    /// <returns></returns>
    protected bool IsPlayerInRange(float range)
    {
        return DistanceToPlayer() < range ? true : false;
    }

    /// <summary>
    /// 随机获得一个巡逻目标点
    /// </summary>
    protected virtual void PickNewPatrolTarget()
    {
        //随机生成一个方向去巡逻
        Vector2 candidate = spawnPoint + Random.insideUnitCircle * patrolRadius;

        if (MapGenerator.Instance != null)
        {
            TileType tile = MapGenerator.Instance.GetTileAtWorld(candidate);
            if (tile == TileType.deepSea)
                //不可通行则回到巡逻点
                candidate = spawnPoint; 
        }
        //改变巡逻目标点
        patrolTarget = candidate;
    }
    /// <summary>
    /// 通过时间得到一个唯一UID记录怪物
    /// </summary>
    /// <returns></returns>
    protected int GenerateUID()
    {
        unchecked
        {
            return (int)(Time.realtimeSinceStartupAsDouble * 1000) + Random.Range(0, 10000);
        }
    }

    /// <summary>
    /// 受击之后变红
    /// </summary>
    /// <returns></returns>
    protected IEnumerator FlashCoroutine()
    {
        if (sr == null) yield break;
        sr.color = Color.red;
        yield return new WaitForSeconds(hitFlashDuration);
        if (sr != null) sr.color = Color.white;
    }

    protected void DestroySelf()
    {
        if (DistanceToPlayer() >= 40)
        {
            Destroy(this.gameObject);
            MonsterSpawnManager.Instance.monsterCount--;
        }
    }

    /// <summary>
    /// 在 Scene 视图中绘制 Gizmos，便于调试 AI 范围
    /// 选中怪物时即可在 Scene 视图中看到：
    ///   黄色虚线圆 = 追击范围 (chaseRange)
    ///   红色虚线圆 = 攻击范围 (attackRange)
    ///   蓝色虚线圆 = 巡逻范围 (patrolRadius)，以出生点为圆心
    /// 
    /// 运行时出生点可能因移动而偏离原始位置，所以用 spawnPoint 而非 transform.position
    /// </summary>
    protected virtual void OnDrawGizmosSelected()
    {
        // 运行中使用记录的出生点，编辑模式用当前 Transform 位置
        Vector2 center = Application.isPlaying ? spawnPoint : (Vector2)transform.position;

        // 追击范围（黄色）
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(center, chaseRange);

        // 攻击范围（红色）
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(center, atkRange);

        // 巡逻范围（蓝色），以出生点为圆心
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(center, patrolRadius);
    }
}
