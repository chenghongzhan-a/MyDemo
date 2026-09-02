using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class MapGenerator : MonoBehaviour
{
    [Header("地图种子")]
    [Tooltip("世界种子。相同种子 + 相同配置 = 相同地图。")]
    public int seed = 12345;

    [Tooltip("是否使用随机种子。")]
    public bool useRandomSeed = true;

    [Header("区块配置")]
    [Tooltip("拖入 ChunkConfig 资产。所有区块参数统一在这里管理。")]
    public ChunkConfig chunkConfig;

    /// <summary>
    /// 全局区块大小，供外部无需持有MapGenerator引用即可使用WorldToChunk等静态方法
    /// </summary>
    public static int ChunkSize
    {
        get { return Instance != null && Instance.chunkConfig != null ? Instance.chunkConfig.chunkSize : 16; }
    }

    /// <summary>
    /// MapGenerator单例引用，在Awake中自动设置
    /// </summary>
    public static MapGenerator Instance { get; private set; }

    [Header("玩家引用")]
    [Tooltip("玩家的Transform")]
    public Transform player;

    [Header("Tile资源")]
    public TileBase tileDeepSea;      // 深海瓦片
    public TileBase tileShallowSea;   // 浅海瓦片
    public TileBase tileSand;         // 沙滩瓦片
    public TileBase tileGrass;        // 草地瓦片
    public TileBase tileForest;       // 森林瓦片
    public TileBase tileSwamp;        // 沼泽瓦片
    public TileBase tilePond;         // 池塘瓦片
    public TileBase tileDirt;         // 泥土瓦片
    public TileBase tileSnow;         // 雪地瓦片

    [Header("装饰物预制体")]
    [Tooltip("树木预制体 留空则不生成树木")]
    public GameObject treePrefab;
    [Tooltip("石头预制体 留空则不生成石头")]
    public GameObject rockPrefab;
    [Tooltip("灌木预制体 留空则不生成灌木")]
    public GameObject bushPrefab;
    [Tooltip("花朵预制体 留空则不生成花朵")]
    public GameObject flowerPrefab;

    [Tooltip("生成的装饰物的父节点")]
    public Transform objectsParent;

    //运行时私有变量
    //噪声偏移量申明
    private float elevationOffset;     // 高度噪声的全局偏移
    private float moistureOffset;     // 湿度噪声的全局偏移
    private float tempOffset;         // 温度噪声的全局偏移
    private float objectOffset;       // 对象放置噪声的全局偏移

    //区块追踪
    /// <summary>
    /// 当前已加载的区块GameObject
    /// </summary>
    private Dictionary<Vector2Int, GameObject> activeChunks = new Dictionary<Vector2Int, GameObject>();

    /// <summary>
    /// 已生成的区块数据缓存
    /// </summary>
    private Dictionary<Vector2Int, ChunkData> chunkDataCache = new Dictionary<Vector2Int, ChunkData>();

    //玩家追踪
    /// <summary>
    /// 玩家上一帧所在的区块坐标
    /// </summary>
    private Vector2Int lastPlayerChunk;

    //帧计数器
    private int frameCounter = 0;

    //协程引用
    private Coroutine generationCoroutine;

    //共享Grid
    private Transform gridTransform;

    public Camera playerCamera;
    void Awake()
    {
        playerCamera = GameObject.Find("PlayerCamera(Clone)").GetComponent<Camera>();
        Instance = this;

        //确保配置存在
        if (chunkConfig == null)
        {
            Debug.LogError("MapGenerator: 未设置 ChunkConfig！请在 Inspector 中拖入配置文件。");
        }
        seed = ArchiveManager.Instance.currentArchive.worldSeed;
        player.position = new Vector3(ArchiveManager.Instance.currentArchive.playerPosX, ArchiveManager.Instance.currentArchive.playerPosY); ;

    }

    /// <summary>
    /// 初始化种子、噪声偏移、查找玩家、首次加载地图。
    /// </summary>
    void Start()
    {
        //生成全局噪声偏移量
        GenerateGlobalOffsets();

        //创建共享Grid
        CreateGrid();

        //自动查找Player
        AutoFindPlayer();

        //记录玩家初始区块位置
        if (player != null)
        {
            lastPlayerChunk = WorldToChunk(player.position);
        }

        //首次加载地图
        if (chunkConfig != null && player != null)
        {
            StartGenerationCoroutine();
        }
        else
        {
            Debug.LogWarning("MapGenerator: 缺少玩家引用或区块配置，无法开始生成。");
        }
    }
    void Update()
    {
        // 每隔N帧检查一次
        frameCounter++;
        if (frameCounter < chunkConfig.updateInterval)
            return;
        frameCounter = 0;

        //缺少关键引用时跳过
        if (player == null || chunkConfig == null)
            return;

        //获取玩家当前所在的区块坐标
        Vector2Int currentChunk = WorldToChunk(player.position);

        //只有当玩家跨入新区块时才触发更新
        if (currentChunk != lastPlayerChunk)
        {
            lastPlayerChunk = currentChunk;
            //启动协程更新
            StartGenerationCoroutine(); 
        }      
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
        //清除所有区块
        ClearAllChunks();
    }

    //初始化方法
    /// <summary>
    /// 生成所有噪声层的全局偏移量。
    /// 这些偏移量确保同一张地图的每次运行产生相同结果，
    /// 而不同的种子产生不同的地图。
    /// </summary>
    private void GenerateGlobalOffsets()
    {
        //使用确定性的伪随机数生成器
        //seed不变的话生成的值就不变
        System.Random rng = new System.Random(seed);

        //为不同噪声层生成不同的偏移量
        elevationOffset = (float)(rng.NextDouble() * 10000.0);
        moistureOffset = (float)(rng.NextDouble() * 10000.0);
        objectOffset = (float)(rng.NextDouble() * 10000.0);
        tempOffset = (float)(rng.NextDouble() * 10000.0);

        if (chunkConfig.verboseLogging)
        {
            Debug.Log($"噪声偏移: 高度={elevationOffset:F1}, 湿度={moistureOffset:F1}, 对象={objectOffset:F1}");
        }
    }

    /// <summary>
    /// 创建共享 Grid
    /// </summary>
    private void CreateGrid()
    {
        GameObject gridGO = new GameObject("ChunkGrid");
        gridGO.transform.SetParent(transform);
        gridGO.transform.localPosition = Vector3.zero;
        Grid grid = gridGO.AddComponent<Grid>();
        grid.cellSize = new Vector3(chunkConfig.cellSize, chunkConfig.cellSize, 1f);
        grid.cellLayout = GridLayout.CellLayout.Rectangle;
        grid.cellSwizzle = GridLayout.CellSwizzle.XYZ;

        gridTransform = gridGO.transform;
        Debug.Log($"共享 Grid 已创建，cellSize = {chunkConfig.cellSize}");
    }

    /// <summary>
    /// 自动查找Tag为Player的对象
    /// </summary>
    private void AutoFindPlayer()
    {
        if (player != null) return; // 已手动设置

        GameObject playerGO = GameObject.FindGameObjectWithTag("Player");
        if (playerGO != null)
        {
            player = playerGO.transform;
            Debug.Log($"自动找到玩家: {playerGO.name}");
        }
        else
        {
            Debug.LogWarning("未找到 Tag=\"Player\" 的对象。");
        }
    }

    //坐标转换
    /// <summary>
    /// 世界坐标 → 区块坐标
    /// </summary>
    public static Vector2Int WorldToChunk(Vector3 worldPos, int chunkSize)
    {
        int chunkX = Mathf.FloorToInt(worldPos.x / chunkSize);
        int chunkY = Mathf.FloorToInt(worldPos.y / chunkSize);
        return new Vector2Int(chunkX, chunkY);
    }

    /// <summary>
    /// 世界坐标 → 区块坐标（实例方法，使用当前配置的chunkSize）
    /// </summary>
    public Vector2Int WorldToChunk(Vector3 worldPos)
    {
        return WorldToChunk(worldPos, chunkConfig.chunkSize);
    }

    /// <summary>
    /// 世界坐标 → 区块内的本地坐标
    /// </summary>
    public static Vector2Int WorldToLocal(Vector3 worldPos, int chunkSize)
    {
        int localX = Mathf.FloorToInt(worldPos.x) % chunkSize;
        int localY = Mathf.FloorToInt(worldPos.y) % chunkSize;

        if (localX < 0) localX += chunkSize;
        if (localY < 0) localY += chunkSize;

        return new Vector2Int(localX, localY);
    }

    /// <summary>
    /// 世界坐标 → 区块内的本地坐标（实例方法，使用当前配置的chunkSize）
    /// </summary>
    public Vector2Int WorldToLocal(Vector3 worldPos)
    {
        return WorldToLocal(worldPos, chunkConfig.chunkSize);
    }

    //区块管理（加载/卸载）
    /// <summary>
    /// 启动区块更新协程
    /// 如果之前有正在运行的协程，先停止它。
    /// </summary>
    private void StartGenerationCoroutine()
    {
        if (generationCoroutine != null)
        {
            StopCoroutine(generationCoroutine);
        }
        generationCoroutine = StartCoroutine(UpdateChunksCoroutine());
    }

    /// <summary>
    /// 协程：分帧更新区块
    /// </summary>
    private IEnumerator UpdateChunksCoroutine()
    {
        if (player == null || chunkConfig == null)
            yield break;

        Vector2Int centerChunk = WorldToChunk(player.position);

        //确定需要加载的区块集合
        HashSet<Vector2Int> chunksToKeep = new HashSet<Vector2Int>();

        int loadRadius = chunkConfig.loadRadius;
        for (int dx = -loadRadius; dx <= loadRadius; dx++)
        {
            for (int dy = -loadRadius; dy <= loadRadius; dy++)
            {
                chunksToKeep.Add(new Vector2Int(centerChunk.x + dx, centerChunk.y + dy));
            }
        }

        //加载新区块（分帧执行）
        int chunksGeneratedThisFrame = 0;
        foreach (Vector2Int coord in chunksToKeep)
        {
            if (!activeChunks.ContainsKey(coord))
            {
                LoadChunk(coord);
                chunksGeneratedThisFrame++;

                //每帧限制生成数量，超出则暂停一帧
                if (chunksGeneratedThisFrame >= chunkConfig.maxChunksPerFrame)
                {
                    chunksGeneratedThisFrame = 0;
                    yield return null; // 等待下一帧
                }
            }
        }

        //卸载超范围区块
        //待卸载的区块
        List<Vector2Int> toUnload = new List<Vector2Int>();
        //判断哪些区块需要卸载
        foreach (var kvp in activeChunks)
        {
            Vector2Int coord = kvp.Key;
            int distX = Mathf.Abs(coord.x - centerChunk.x);
            int distY = Mathf.Abs(coord.y - centerChunk.y);

            if (Mathf.Max(distX, distY) > chunkConfig.unloadRadius)
            {
                toUnload.Add(coord);
            }
        }

        foreach (Vector2Int coord in toUnload)
        {
            UnloadChunk(coord);
        }

        if (chunkConfig.verboseLogging)
        {
            Debug.Log($"区块状态: 已加载={activeChunks.Count}, 缓存数据={chunkDataCache.Count}, 本次卸载={toUnload.Count}");
        }
    }

    /// <summary>
    /// 加载一个区块
    /// </summary>
    private void LoadChunk(Vector2Int chunkCoord)
    {
        //获取或生成数据
        ChunkData data = GetOrGenerateChunkData(chunkCoord);
        if (data == null) return;

        //创建区块GameObject，挂在一个Grid 下
        GameObject chunkGO = new GameObject($"Chunk_{chunkCoord.x}_{chunkCoord.y}");
        chunkGO.transform.SetParent(gridTransform);
        BoxCollider2D collider = chunkGO.AddComponent<BoxCollider2D>();
        //计算碰撞体大小：chunkSize * cellSize 才是实际大小
        float chunkWorldSize = chunkConfig.chunkSize * chunkConfig.cellSize;
        collider.size = new Vector2(chunkWorldSize, chunkWorldSize);
        //设置碰撞体偏移位置 因为碰撞体原始位置在区块的左下角
        float halfSize = chunkWorldSize / 2f;
        collider.offset = new Vector2(halfSize, halfSize);
        //使用localPosition，而不是世界坐标
        chunkGO.transform.localPosition = new Vector3(
            chunkCoord.x * chunkConfig.chunkSize * chunkConfig.cellSize,
            chunkCoord.y * chunkConfig.chunkSize * chunkConfig.cellSize,
            0f
        );

        //添加 Tilemap 组件
        Tilemap tilemap = chunkGO.AddComponent<Tilemap>();
        TilemapRenderer renderer = chunkGO.AddComponent<TilemapRenderer>();
        renderer.sortingLayerName = "Default";
        renderer.sortingOrder = 0;
        renderer.sortingLayerName = "Terrain";
        renderer.sortingOrder = 0;

        //tileAnchor保持默认(0.5, 0.5, 0)，
        //渲染瓦片
        RenderChunkTilemap(tilemap, data);

        //生成装饰物
        if (chunkConfig.spawnObjects)
        {
            SpawnChunkObjects(data, chunkGO.transform);
        }

        //生成玩家放置的物品
        SpawnPlacedObjects(chunkCoord, data, chunkGO.transform);

        //生成存档中的掉落物
        SpawnDroppedItems(chunkCoord, data);

        //记录状态
        activeChunks[chunkCoord] = chunkGO;
        data.isActive = true;

        //调试边框
        if (chunkConfig.showDebugBounds)
        {
            DrawChunkBounds(chunkGO, chunkCoord);
        }

        if (chunkConfig.verboseLogging)
        {
            Debug.Log($"加载区块: ({chunkCoord.x}, {chunkCoord.y}), 世界位置=({data.worldPos.x}, {data.worldPos.y})");
        }
    }

    /// <summary>
    /// 卸载一个区块
    /// 只销毁GameObject，保留chunkDataCache中的数据。
    /// </summary>
    private void UnloadChunk(Vector2Int chunkCoord)
    {
        //如果不是已经加载的区块 直接退出
        if (!activeChunks.TryGetValue(chunkCoord, out GameObject chunkGO))
            return;

        //更新数据状态
        if (chunkDataCache.TryGetValue(chunkCoord, out ChunkData data))
        {
            data.isActive = false;

            //销毁装饰物GameObject
            if (data.spawnedObjects != null)
            {
                foreach (GameObject obj in data.spawnedObjects)
                {
                    if (obj != null)
                    {
                        var ppo = obj.GetComponent<PlayerPlacedObject>();
                        if (ppo != null)
                            ppo.RefreshArchive();
                    }
                    if (obj != null) Destroy(obj);
                }
                data.spawnedObjects.Clear();
            }
        }

        //销毁区块GameObject
        Destroy(chunkGO);
        activeChunks.Remove(chunkCoord);

        if (chunkConfig.verboseLogging)
        {
            Debug.Log($"卸载区块: ({chunkCoord.x}, {chunkCoord.y})");
        }
    }

    
    //地形生成

    /// <summary>
    /// 获取或生成区块数据
    /// 先从缓存中查找，缓存中没有则调用噪声计算生成新的。
    /// 生成单个区块信息
    /// </summary>
    private ChunkData GetOrGenerateChunkData(Vector2Int chunkCoord)
    {
        if (chunkDataCache.TryGetValue(chunkCoord, out ChunkData cached))
        {
            return cached;
        }

        return GenerateChunkData(chunkCoord);
    }

    /// <summary>
    /// 通过柏林噪声生成一个区块的地形数据
    /// </summary>
    private ChunkData GenerateChunkData(Vector2Int chunkCoord)
    {
        int size = chunkConfig.chunkSize;
        ChunkData data = new ChunkData(chunkCoord, size);

        //遍历区块内每个瓦片
        for (int localX = 0; localX < size; localX++)
        {
            for (int localY = 0; localY < size; localY++)
            {
                //计算世界坐标
                int worldX = data.worldPos.x + localX;
                int worldY = data.worldPos.y + localY;
                //计算高度噪声
                float noiseX = worldX * chunkConfig.noiseScale + elevationOffset;
                float noiseY = worldY * chunkConfig.noiseScale + elevationOffset;

                float elevation = NoiseHelper.FBM(
                    noiseX, noiseY,
                    chunkConfig.octaves,
                    chunkConfig.persistence,
                    chunkConfig.lacunarity
                );

                //计算湿度噪声
                float moistX = worldX * chunkConfig.moistureScale + moistureOffset;
                float moistY = worldY * chunkConfig.moistureScale + moistureOffset;

                float moisture = NoiseHelper.Perlin(moistX, moistY, 0f);

                //计算温度噪声
                float tempX = worldX * chunkConfig.temperatureScale + tempOffset;
                float tempY = worldY * chunkConfig.temperatureScale + tempOffset;

                float temp = NoiseHelper.Perlin(tempX, tempY, 0f);

                //根据高度、湿度、温度确定瓦片类型
                TileType tileType = DetermineTileType(elevation, moisture, temp);

                //存储瓦片数据
                data.tiles[localX, localY] = new TileData(tileType);
            }
        }

        //确定装饰物位置
        if (chunkConfig.spawnObjects)
        {
            GenerateObjectPlacements(data, chunkCoord);
        }

        data.isGenerated = true;
        chunkDataCache[chunkCoord] = data;

        return data;
    }

    /// <summary>
    /// 根据高度、湿度、温度确定瓦片类型
    /// </summary>
    private TileType DetermineTileType(float elevation, float moisture, float temp)
    {
        // 深海区域
        if (elevation < chunkConfig.deepSeaThreshold)
        {
            return TileType.deepSea;
        }

        // 浅海区域
        if (elevation < chunkConfig.shallowSeaThreshold)
        {
            return TileType.shallowSea;
        }

        //沙滩区域（浅海和陆地之间的过渡）
        if (elevation < chunkConfig.sandThreshold)
        {
            return TileType.sand;
        }

        //草地/沼泽/池塘区域（中等高度）
        if (elevation < chunkConfig.grassThreshold)
        {
            //根据湿度细分
            if (moisture > chunkConfig.pondMoistureThreshold)
            {
                return TileType.pond;   //极高湿度生成池塘
            }
            if (moisture > chunkConfig.swampMoistureThreshold)
            {
                return TileType.swamp;  //高湿度生成沼泽
            }
            if (temp < chunkConfig.temperatureThreshold)
            {
                return TileType.snow;   //低温生成雪地
            }
            return TileType.grass;      //正常湿度和温度生成草地
        }
        if (temp < chunkConfig.temperatureThreshold)
        {
            return TileType.snow;   //低温生成雪地森林
        }
        // 高海拔 → 森林
        return TileType.forest;
    }

    //对象/装饰物生成
    /// <summary>
    /// 为区块生成装饰物的放置信息
    /// </summary>
    private void GenerateObjectPlacements(ChunkData data, Vector2Int chunkCoord)
    {
        data.objects.Clear();

        int size = data.chunkSize;

        for (int localX = 0; localX < size; localX++)
        {
            for (int localY = 0; localY < size; localY++)
            {
                //已经达到上限，停止放置
                if (data.objects.Count >= chunkConfig.maxObjectsPerChunk)
                    return;

                TileData tile = data.tiles[localX, localY];

                //水体不放置装饰物
                if (tile.type == TileType.deepSea || tile.type == TileType.shallowSea || tile.type == TileType.pond)
                    continue;

                //使用噪声决定是否在此位置放置装饰物
                int worldX = data.worldPos.x + localX;
                int worldY = data.worldPos.y + localY;
                float objNoise = NoiseHelper.GetObjectNoise(worldX, worldY, chunkConfig.objectNoiseScale, objectOffset);

                //噪声值 > 0.6 才放置
                if (objNoise < 0.65f) continue;

                //根据地形类型和噪声确定装饰物类型
                ObjectType objType = PickObjectType(tile.type, objNoise);

                if (objType != ObjectType.none)
                {
                    data.objects.Add(new ObjectSpawnInfo(localX, localY, objType));
                }
            }
        }
    }

    /// <summary>
    /// 根据地貌类型和噪声值选择装饰物类型
    /// </summary>
    private ObjectType PickObjectType(TileType tileType, float noise)
    {
        switch (tileType)
        {
            case TileType.grass:
                // 草地：噪声极高→树木，高→灌木，中→花朵
                if (noise > 0.85f) return ObjectType.tree;
                if (noise > 0.72f) return ObjectType.bush;
                return ObjectType.flower;

            case TileType.forest:
                // 森林：大量树木 + 少量石头
                if (noise > 0.65f) return ObjectType.tree;
                return ObjectType.rock;

            case TileType.swamp:
                // 沼泽：灌木为主
                if (noise > 0.70f) return ObjectType.bush;
                return ObjectType.flower;

            case TileType.sand:
                // 沙滩：偶尔有石头
                if (noise > 0.85f) return ObjectType.rock;
                return ObjectType.none;

            default:
                return ObjectType.none;
        }
    }

    /// <summary>
    /// 在场景中实例化装饰物 GameObject
    /// </summary>
    private void SpawnChunkObjects(ChunkData data, Transform chunkParent)
    {
        if (data.spawnedObjects == null)
            data.spawnedObjects = new List<GameObject>();

        foreach (var info in data.objects)
        {
            //如果这个位置的装饰物已经被移除 那么就直接跳过
            if (ArchiveManager.Instance.IsObjectRemoved(data.chunkCoord, info.localPos))
                continue;
            //根据装饰物类型获取对应的预设体
            GameObject prefab = GetObjectPrefab(info.type);
            if (prefab == null) continue;

            //在区块的本地坐标内，加一点随机偏移避免整齐排列
            //+0.5 让对象居中于瓦片
            Vector3 worldPos = new Vector3(
                data.worldPos.x + info.localPos.x + 0.5f,  
                data.worldPos.y + info.localPos.y + 0.5f,
                0f
            );
            //实例化装饰物预设体
            GameObject instance = Instantiate(prefab, worldPos, Quaternion.identity);
            instance.GetComponent<SpriteRenderer>().sortingLayerName = "Objects";
            instance.GetComponent<SpriteRenderer>().sortingOrder = -(int)(worldPos.y * 100);
            //给实例化的装饰物预设体赋值
            var harvestable = instance.GetComponent<BaseDecoration>();
            if (harvestable != null)
            {
                harvestable.chunkCoord = data.chunkCoord;
                harvestable.localPos = info.localPos;
                harvestable.objectType = info.type;
            }

            //设置父节点
            if (objectsParent != null)
                instance.transform.SetParent(objectsParent);
            else
                instance.transform.SetParent(chunkParent);
            //存入此区块的装饰物记录dic中
            data.spawnedObjects.Add(instance);
        }
    }

    /// <summary>
    /// 手动在世界坐标处生成单个装饰物（供玩家种树等外部调用）
    /// 处理了区块归属、父节点设置、数据追踪的全部逻辑
    /// </summary>
    public GameObject SpawnSingleObject(ObjectType type, Vector3 worldPos)
    {
        //确定所属区块
        Vector2Int chunkCoord = WorldToChunk(worldPos);
        Vector2Int localPos = WorldToLocal(worldPos);

        //获取预制体
        GameObject prefab = GetObjectPrefab(type);
        if (prefab == null)
        {
            Debug.LogError($"MapGenerator: 未配置 {type} 的预制体");
            return null;
        }

        //实例化（居中于瓦片）
        Vector3 snappedPos = new Vector3(
            Mathf.Floor(worldPos.x) + 0.5f,
            Mathf.Floor(worldPos.y) + 0.5f,
            0f
        );
        //实例化装饰物
        GameObject instance = Instantiate(prefab, snappedPos, Quaternion.identity);
        //给装饰物上面添加 标记信息脚本
        PlayerPlacedObject p = instance.AddComponent<PlayerPlacedObject>();
        p.p = new PlacedObjectInfo();
        p.p.worldX = snappedPos.x;
        p.p.worldY = snappedPos.y;
        p.p.prefabName= prefab.name;
        //记录到内存中
        ArchiveManager.Instance.MarkObjectPlaced(WorldToChunk(instance.transform.position), WorldToLocal(instance.transform.position), p.p);

        //设置 BaseDecoration 的区块信息
        var decoration = instance.GetComponent<BaseDecoration>();
        if (decoration != null)
        {
            decoration.chunkCoord = chunkCoord;
            decoration.localPos = localPos;
            decoration.objectType = type;
        }


        //存入区块数据追踪列表（卸载时自动清理）
        ChunkData data = GetOrGenerateChunkData(chunkCoord);
        if (data != null)
        {
            if (data.spawnedObjects == null)
                data.spawnedObjects = new List<GameObject>();
            data.spawnedObjects.Add(instance);
        }

        return instance;
    }
    /// <summary>
    /// 恢复玩家放置的物品
    /// </summary>
    /// <param name="chunkCoord">区块坐标</param>
    /// <param name="data">区块信息</param>
    /// <param name="chunkParent">区块自身的Transform 用来设置父对象</param>
    private void SpawnPlacedObjects(Vector2Int chunkCoord, ChunkData data, Transform chunkParent)
    {
        int size = data.chunkSize;

        for (int localX = 0; localX < size; localX++)
        {
            for (int localY = 0; localY < size; localY++)
            {
                Vector2Int localPos = new Vector2Int(localX, localY);
                string key = $"{chunkCoord.x}_{chunkCoord.y}_{localPos.x}_{localPos.y}";

                if (!ArchiveManager.Instance.currentWorldMod.placedObjects.TryGetValue(key, out PlacedObjectInfo info))
                    continue;

                ABResMgr.Instance.LoadResAsync<GameObject>("decoration", info.prefabName, (prefab) =>
                {
                    if (prefab == null)
                    {
                        Debug.LogWarning($"SpawnPlacedObjects: 预制体未找到 {info.prefabName}");
                        return;
                    }

                    Vector3 worldPos = new Vector3(info.worldX, info.worldY, 0f);
                    GameObject instance = Instantiate(prefab, worldPos, Quaternion.identity);
                    instance.name = info.prefabName;
                    //设置层级
                    var sr = instance.GetComponent<SpriteRenderer>();
                    if (sr != null)
                    {
                        sr.sortingLayerName = "Objects";
                        sr.sortingOrder = -(int)(instance.transform.position.y * 100);
                    }

                    //挂上PlayerPlacedObject标记组件，绑定字典中已有的PlacedObjectInfo
                    PlayerPlacedObject ppo = instance.AddComponent<PlayerPlacedObject>();
                    ppo.p = info;

                    //恢复生长状态
                    Grow grow = instance.GetComponent<Grow>();
                    if (grow != null)
                    {
                        grow.currentStage = info.growStage;
                        grow.nowGorwTime = info.nowGorwTime;
                        if (info.growStage >= grow.spriteS.Length - 1)
                            grow.isFullyGrown = true;
                        else
                            grow.isFullyGrown = false;
                        //更新 sprite 到对应阶段
                        if (grow.spriteS != null && info.growStage < grow.spriteS.Length)
                            instance.GetComponent<SpriteRenderer>().sprite = grow.spriteS[info.growStage];
                    }

                    //设置父节点
                    if (objectsParent != null)
                        instance.transform.SetParent(objectsParent);
                    else
                        instance.transform.SetParent(chunkParent);

                    //加入追踪列表，卸载区块时一并销毁
                    if (data.spawnedObjects == null)
                        data.spawnedObjects = new List<GameObject>();
                    data.spawnedObjects.Add(instance);
                });
            }
        }
    }

    /// <summary>
    /// 根据存档数据生成区块内的掉落物
    /// 遍历区块内每个瓦片，检查是否有之前存档的掉落物，有则实例化
    /// </summary>
    private void SpawnDroppedItems(Vector2Int chunkCoord, ChunkData data)
    {
        int size = data.chunkSize;

        for (int localX = 0; localX < size; localX++)
        {
            for (int localY = 0; localY < size; localY++)
            {
                Vector2Int localPos = new Vector2Int(localX, localY);
                if (!ArchiveManager.Instance.TryGetDroppedItems(chunkCoord, localPos, out List<ItemSaveData> items))
                    continue;

                foreach (ItemSaveData itemData in items)
                {
                    //根据物品名加载预制体
                    ABResMgr.Instance.LoadResAsync<GameObject>("material", itemData.itemName, (prefab) =>
                    {
                        if (prefab == null)
                        {
                            Debug.LogWarning($"掉落物预制体未找到: {itemData.itemName}");
                            return;
                        }

                        //在瓦片中心位置实例化
                        Vector3 worldPos = new Vector3(
                            data.worldPos.x + localX + 0.5f,
                            data.worldPos.y + localY + 0.5f,
                            0f
                        );

                        GameObject instance = Instantiate(prefab, worldPos, Quaternion.identity);
                        instance.name = prefab.name;
                        //恢复物品运行时数据
                        ItemBase itemBase = instance.GetComponent<ItemBase>();
                        if (itemBase != null)
                        {
                            itemBase.LoadFromSaveData(itemData);
                        }

                        //设置父节点
                        if (objectsParent != null)
                            instance.transform.SetParent(objectsParent);

                        //加入追踪列表，卸载区块时一并销毁
                        if (data.spawnedObjects == null)
                            data.spawnedObjects = new List<GameObject>();
                        data.spawnedObjects.Add(instance);
                    });
                }
            }
        }
    }

    /// <summary>
    /// 根据装饰物类型获取对应的预制体
    /// </summary>
    private GameObject GetObjectPrefab(ObjectType type)
    {
        switch (type)
        {
            case ObjectType.tree:   return treePrefab;
            case ObjectType.rock:   return rockPrefab;
            case ObjectType.bush:   return bushPrefab;
            case ObjectType.flower: return flowerPrefab;
            default:                return null;
        }
    }

    //Tilemap 渲染

    /// <summary>
    /// 将区块数据渲染到 Tilemap 组件上
    /// 遍历区块内每个瓦片，根据类型设置对应的 Tile。
    /// </summary>
    private void RenderChunkTilemap(Tilemap tilemap, ChunkData data)
    {
        int size = data.chunkSize;
        //遍历坐标逐个设置瓦片
        for (int x = 0; x < size; x++)
        {
            for (int y = 0; y < size; y++)
            {
                Vector3Int tilePos = new Vector3Int(x, y, 0);
                Vector2Int localPos = new Vector2Int(x, y);
                TileType finalType = data.tiles[x, y].type;
                if (ArchiveManager.Instance.TryGetTileOverride(data.chunkCoord, localPos, out TileType overrideType))
                {
                    finalType = overrideType;
                } 
                //获得瓦片资源
                TileBase tile = TileTypeToTile(finalType);

                if (tile != null)
                {
                    //设置瓦片
                    tilemap.SetTile(tilePos, tile);
                }
            }
        }
    }

    /// <summary>
    /// 将 TileType 枚举映射到具体的 Tile 资源
    /// </summary>
    private TileBase TileTypeToTile(TileType type)
    {
        switch (type)
        {
            case TileType.deepSea:    return tileDeepSea;
            case TileType.shallowSea: return tileShallowSea;
            case TileType.sand:       return tileSand;
            case TileType.grass:      return tileGrass;
            case TileType.forest:     return tileForest;
            case TileType.swamp:      return tileSwamp;
            case TileType.pond:       return tilePond;
            case TileType.dirt:       return tileDirt;
            case TileType.snow:       return tileSnow;
            default:
                Debug.LogWarning($"未知瓦片类型: {type}，使用草地作为默认值");
                return tileGrass;
        }
    }

    //公共 API
    /// <summary>
    /// 获取指定世界坐标处的瓦片类型
    /// </summary>
    public TileType GetTileAtWorld(Vector3 worldPos)
    {
        Vector2Int chunkCoord = WorldToChunk(worldPos);
        Vector2Int localCoord = WorldToLocal(worldPos);

        if (chunkDataCache.TryGetValue(chunkCoord, out ChunkData data))
        {
            TileType type = data.GetTile(localCoord.x, localCoord.y).type;
            //检查玩家是否修改过这个瓦片
            if (ArchiveManager.Instance.TryGetTileOverride(chunkCoord, localCoord, out TileType overrideType))
                return overrideType;
            return type;
        }

        data = GetOrGenerateChunkData(chunkCoord);
        if (data != null)
        {
            TileType type = data.GetTile(localCoord.x, localCoord.y).type;
            if (ArchiveManager.Instance.TryGetTileOverride(chunkCoord, localCoord, out TileType overrideType))
                return overrideType;
            return type;
        }
        //默认返回深海
        return TileType.deepSea;
    }

    /// <summary>
    /// 修改指定世界坐标处的瓦片类型（同时更新存档数据和视觉显示）
    /// 供挖地、铺路等工具调用
    /// </summary>
    /// <param name="worldPos">世界坐标</param>
    /// <param name="newType">新的瓦片类型</param>
    public void SetTileAtWorld(Vector3 worldPos, TileType newType)
    {
        Vector2Int chunkCoord = WorldToChunk(worldPos);
        Vector2Int localPos   = WorldToLocal(worldPos);

        //记录修改到存档数据
        ArchiveManager.Instance.MarkTileChanged(chunkCoord, localPos, newType);

        //立即更新视觉（如果区块已加载）
        if (activeChunks.TryGetValue(chunkCoord, out GameObject chunkGO))
        {
            Tilemap tilemap = chunkGO.GetComponent<Tilemap>();
            if (tilemap != null)
            {
                tilemap.SetTile(new Vector3Int(localPos.x, localPos.y, 0), TileTypeToTile(newType));
            }
        }

        //3.自动保存 地图存档通过id保存
        ArchiveManager.Instance.Save(ArchiveManager.Instance.id);

        Debug.Log($"瓦片修改: 区块({chunkCoord.x},{chunkCoord.y}) 本地({localPos.x},{localPos.y}) → {newType}");
    }

    /// <summary>
    /// 强制重新生成整个世界
    /// 清除所有缓存和已加载区块，用新种子重新生成。
    /// //测试用
    /// </summary>
    public void RegenerateMap()
    {
        ClearAllChunks();
        GenerateGlobalOffsets();

        if (player != null)
        {
            lastPlayerChunk = WorldToChunk(player.position);
        }

        StartGenerationCoroutine();
        Debug.Log("地图已重新生成");
    }

    /// <summary>
    /// 清除所有已加载的区块和缓存数据
    /// </summary>
    public void ClearAllChunks()
    {
        //先停止生成协程
        if (generationCoroutine != null)
        {
            StopCoroutine(generationCoroutine);
            generationCoroutine = null;
        }

        //销毁所有区块GameObject
        foreach (var kvp in activeChunks)
        {
            if (kvp.Value != null)
            {
                Destroy(kvp.Value);
            }
        }
        activeChunks.Clear();
        //清理缓存
        chunkDataCache.Clear();
        Debug.Log("所有区块已清除");
    }

    /// <summary>
    /// 获取当前已加载区块的数量
    /// </summary>
    public int GetLoadedChunkCount()
    {
        return activeChunks.Count;
    }

    /// <summary>
    /// 获取缓存中区块数据的数量
    /// </summary>
    public int GetCachedChunkCount()
    {
        return chunkDataCache.Count;
    }

    //调试工具

    /// <summary>
    /// 在区块周围绘制绿色边框（用于调试可视化）
    /// </summary>
    private void DrawChunkBounds(GameObject chunkGO, Vector2Int coord)
    {
        LineRenderer line = chunkGO.AddComponent<LineRenderer>();
        line.material = new Material(Shader.Find("Sprites/Default"));
        line.startColor = new Color(0, 1, 0, 0.5f);  // 半透明绿色
        line.endColor   = new Color(0, 1, 0, 0.5f);
        line.startWidth = 0.1f;
        line.endWidth   = 0.1f;
        line.sortingOrder = 100; // 确保绘制在最上层

        int s = chunkConfig.chunkSize;
        Vector3[] corners = new Vector3[5]
        {
            new Vector3(0, 0, -1),
            new Vector3(s, 0, -1),
            new Vector3(s, s, -1),
            new Vector3(0, s, -1),
            new Vector3(0, 0, -1), // 回到起点闭合
        };

        line.positionCount = 5;
        line.SetPositions(corners);
    }

    /// <summary>
    /// 在 Scene 视图中绘制 Gizmos（仅编辑器模式下可见）
    /// 显示玩家所在区块和加载范围
    /// </summary>
    void OnDrawGizmosSelected()
    {
        if (chunkConfig == null) return;

        //确定中心点（编辑器模式下使用场景视图相机位置，运行时使用玩家位置）
        Vector3 center;
        if (Application.isPlaying && player != null)
        {
            center = player.position;
        }
        else
        {
            center = transform.position;
        }

        Vector2Int centerChunk = WorldToChunk(center);

        //绘制加载范围
        Gizmos.color = Color.yellow;
        int loadRadius = chunkConfig.loadRadius;
        float chunkWorldSize = chunkConfig.chunkSize;
        float loadRectSize = (loadRadius * 2 + 1) * chunkWorldSize;
        Vector3 loadCenter = new Vector3(
            (centerChunk.x + 0.5f) * chunkWorldSize,
            (centerChunk.y + 0.5f) * chunkWorldSize,
            0
        );
        Gizmos.DrawWireCube(loadCenter, new Vector3(loadRectSize, loadRectSize, 1));

        //绘制卸载范围
        Gizmos.color = Color.red;
        int unloadRadius = chunkConfig.unloadRadius;
        float unloadRectSize = (unloadRadius * 2 + 1) * chunkWorldSize;
        Gizmos.DrawWireCube(loadCenter, new Vector3(unloadRectSize, unloadRectSize, 1));

        //标注玩家所在区块
        Gizmos.color = Color.green;
        Vector3 playerChunkCenter = new Vector3(
            (centerChunk.x + 0.5f) * chunkWorldSize,
            (centerChunk.y + 0.5f) * chunkWorldSize,
            0
        );
        Gizmos.DrawWireCube(playerChunkCenter, new Vector3(chunkWorldSize, chunkWorldSize, 1));
    }
}
