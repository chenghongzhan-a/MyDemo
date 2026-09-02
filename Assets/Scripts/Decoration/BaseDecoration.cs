using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BaseDecoration : MonoBehaviour
{
    [Header("血量")]
    public int maxHealth;
    public int currentHealth;

    [Header("凋落物")]
    public GameObject[] drops;

    public Sprite[] sprites;
    /// <summary>
    /// 给装饰物用的 用来判断是否有多个形态的 比如草丛有几种不同的样式
    /// </summary>
    public bool isSprites = false;

    public SpriteRenderer sr;

    public float hitFlashDuration = 0.1f;

    //[Header("需要使用的工具类型")]
    //public ToolType requiredTool;

    //地图创建相关信息 
    //地图创建相关信息 地图创建时赋值 用于记录
    [HideInInspector] public Vector2Int chunkCoord;
    [HideInInspector] public Vector2Int localPos;
    [HideInInspector] public ObjectType objectType;

    private void Awake()
    {
        currentHealth = maxHealth;
        sr = GetComponent<SpriteRenderer>();
        if (isSprites)
        {
            GetComponent<SpriteRenderer>().sprite = sprites[Random.Range(0, sprites.Length)];
        }
    }

    public virtual void TakeDamage(int damage)
    {
        currentHealth -= damage;
        StartCoroutine(FlashCoroutine());
        if (currentHealth <= 0)
        {
            OnDestroyed();
        }
    }
    private void OnMouseDown()
    {
        
    }

    protected virtual void OnDestroyed()
    {
        //1.记录到地图信息中 下次加载不会加载这部分
        //先判断是否是玩家放置的物品 如果是 就从玩家防止的物品dic中移除
        if (GetComponent<PlayerPlacedObject>())
        {
            ArchiveManager.Instance.MarkObjectUnplaced(MapGenerator.Instance.WorldToChunk(this.transform.position), MapGenerator.Instance.WorldToLocal(this.transform.position));
        }
        else
        {
            //如果不是的话 就从自然生成的字典中移除
            ArchiveManager.Instance.MarkObjectRemoved(chunkCoord, localPos, objectType);
        }
        //存储地图信息
        ArchiveManager.Instance.Save(ArchiveManager.Instance.id);
        //2.生成掉落物
        //尝试获取对象上的Grow
        Grow g = GetComponent<Grow>();
        //如果有 就证明是可生长的物品
        if (g != null)
        {
            //如果生长完成为false 就证明没有生长结束 就不会产生掉落物
            if (!g.isFullyGrown)
            {
                //直接移除装饰物
                Destroy(gameObject);
                return;
            }
        }
        
        //生成掉落物
        RandomDrops();

        //移除装饰物
        Destroy(gameObject);
    }

    private void RandomDrops()
    {
        for (int i = 0; i < drops.Length; i++)
        {
            for (int j = 0; j < Random.Range(2, 5); j++)
            {
                GameObject obj = Instantiate(drops[i]);
                obj.name = drops[i].name.Replace("(Clone)", "");
                obj.transform.position = this.transform.position;
                obj.transform.rotation = Quaternion.identity;
                obj.transform.localScale *= 4;
                obj.GetComponent<SpriteRenderer>().sortingLayerName = GetComponent<SpriteRenderer>().sortingLayerName;
                obj.GetComponent<DropItemBounce>().UseBounce();
            }
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
}
