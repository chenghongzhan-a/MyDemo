using System.Collections;
using System.Collections.Generic;
using System.Net;
using UnityEngine;
using UnityEngine.Events;

public class RoleController : MonoBehaviour
{
    Animator animator;
    Vector3 euler;
    private float speed = 2;
    public float speedMultiplier = 1f;
    public float hungrySpeed;
    public float thirstySpeed;
    public float healthUpSpeed;
    public float healthDownSpeed;
    public Transform handPoint;
    public ItemBase itemHandle;
    GameObject newItem;
    PlayerArchiveInfo player;

    private UnityAction onLeftClick;
    private UnityAction onRightClick;
    public float RealSpeed
    {
        get
        {
            return speed * speedMultiplier;
        }
    }
    private void Awake()
    {
        animator = GetComponent<Animator>();

        onLeftClick = () =>
        {
            if (itemHandle != null)
                itemHandle.OnLeftClick();
        };
        onRightClick = () =>
        {
            if (itemHandle != null)
                itemHandle.OnRightClick();
        };
    }
    // Start is called before the first frame update
    void Start()
    {
        EventCenter.Instance.AddEventListener<float>(E_EventType.E_MonsterAttack, OnMonsterAttack);
        player = ArchiveManager.Instance.currentArchive;
    }

    // Update is called once per frame
    void Update()
    {
        #region WSAD移动
        float horizontal = 0;
        float vertical = 0;

        if (Input.GetKey(KeyCode.D)) horizontal = 1;
        if (Input.GetKey(KeyCode.A)) horizontal = -1;
        if (Input.GetKey(KeyCode.W)) vertical = 1;
        if (Input.GetKey(KeyCode.S)) vertical = -1;

        Vector2 direction = new Vector2(horizontal, vertical);
        direction = Vector2.ClampMagnitude(direction, 1f);  // 限制最大长度为1

        if (direction != Vector2.zero)
        {
            animator.SetBool("isMove", true);
            transform.Translate(direction * RealSpeed * Time.deltaTime, Space.World);
            if (direction.x > 0)
            {
                euler = this.transform.eulerAngles;
                euler.y = 0;
                this.transform.localEulerAngles = euler;
            }
            if (direction.x < 0)
            {
                euler = this.transform.eulerAngles;
                euler.y = 180;
                this.transform.localEulerAngles = euler;
            }
        }
        else
        {
            animator.SetBool("isMove", false);
        }
        #endregion

        #region 手中物品指向鼠标
        // 1. 将鼠标屏幕坐标转成世界坐标
        Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        // 注意：2D 游戏中需要把 Z 轴设为 0
        mouseWorldPos.z = 0f;
        handPoint.LookAt(mouseWorldPos);
        // 2. 计算方向
        Vector3 v = mouseWorldPos - handPoint.position;

        // 3. 让手部朝向鼠标
        float angle = Mathf.Atan2(v.y, v.x) * Mathf.Rad2Deg;
        handPoint.rotation = Quaternion.Euler(0, 0, angle);
        #endregion

        #region 开火 吃东西
        if (Input.GetMouseButton(0))
        {
            EventCenter.Instance.EventTrigger(E_EventType.E_Left);
        }
        if (Input.GetMouseButtonDown(1))
        {
            EventCenter.Instance.EventTrigger(E_EventType.E_Right);
        }
        #endregion

        #region 饥饿值和饥渴值的流失
        player.hunger -= hungrySpeed * Time.deltaTime;
        player.thirst -= thirstySpeed * Time.deltaTime;
        //因为饥饿值或饥渴值扣血
        if (player.hunger <= 0 || player.thirst <= 0)
        {
            player.health -= healthDownSpeed * Time.deltaTime;
        }
        #endregion

        #region 生命回复
        //因为饥饿值和饥渴值比较高而回血
        if (player.hunger >= 60 && player.thirst >= 60)
        {
            player.health += healthUpSpeed * Time.deltaTime;
        }
        #endregion

        //设置Player层级
        GetComponent<SpriteRenderer>().sortingLayerName = "Objects";
        GetComponent<SpriteRenderer>().sortingOrder = -(int)(transform.position.y * 100);
    }
    /// <summary>
    /// 控制玩家手上的物品的层级
    /// </summary>
    private void LateUpdate()
    {
        if (newItem != null)
        {
            var sr = newItem.GetComponentInChildren<SpriteRenderer>();
            if (sr != null)
                sr.sortingLayerName = "Objects";
                sr.sortingOrder = -(int)(transform.position.y * 100) + 1;
        }
    }

    private void OnDestroy()
    {
        EventCenter.Instance.RemoveEventListener<float>(E_EventType.E_MonsterAttack, OnMonsterAttack);
    }
    /// <summary>
    /// 添加手上拿的物品的点击监听
    /// </summary>
    public void AddItemBase()
    {
        EventCenter.Instance.AddEventListener(E_EventType.E_Left, onLeftClick);
        EventCenter.Instance.AddEventListener(E_EventType.E_Right, onRightClick);
    }
    /// <summary>
    /// 移除手上拿的物品的点击监听
    /// </summary>
    public void RemoveItemBase()
    {
        EventCenter.Instance.RemoveEventListener(E_EventType.E_Left, onLeftClick);
        EventCenter.Instance.RemoveEventListener(E_EventType.E_Right, onRightClick);
    }
    /// <summary>
    /// 更换玩家手上显示的物品 点击物品栏时调用
    /// </summary>
    /// <param name="itemPrefab"></param>
    public void EquipItem(GameObject itemPrefab)
    {
        if (newItem != null)
        {
            Destroy(newItem.gameObject);
            newItem = null;
            RemoveItemBase();
        }
        if (itemPrefab != null)
        {
            newItem = Instantiate(itemPrefab, handPoint);
            newItem.transform.localPosition = Vector3.zero;
            newItem.transform.localRotation = Quaternion.identity;
            SpriteRenderer renderer = newItem.GetComponent<SpriteRenderer>();
            //设置手上拿的物品的层级
            if (renderer != null)
            {
                renderer.sortingLayerName = "Objects";
                renderer.sortingOrder = GetComponent<SpriteRenderer>().sortingOrder + 10;
            }
            itemHandle = newItem.GetComponent<ItemBase>();
            AddItemBase();
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.layer == LayerMask.NameToLayer("Drop"))
        {
            UIMgr.Instance.GetPanel<GamePanel>((panel) =>
            {
                ABResMgr.Instance.LoadResAsync<GameObject>("Material", collision.gameObject.name, (obj) =>
                {
                    panel.AddItem(obj);
                });
                Destroy(collision.gameObject);
                //保存玩家物品栏 背包
            });
        }
    }

    /// <summary>
    /// 玩家受到攻击的方法
    /// </summary>
    /// <param name="damage"></param>
    private void OnMonsterAttack(float damage)
    {
        //扣血
        player.health -= damage;
    }
}
