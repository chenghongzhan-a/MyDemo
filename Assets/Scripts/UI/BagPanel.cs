using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
public class BagPanel : BasePanel
{
	private GamePanel gamePanel;

	//鼠标上拿着的物品预制体
	public GameObject heldItemPrefab;
	//鼠标上拿着的数量
	public int heldItemCount;
	//鼠标上是否有东西
	public bool hasHeldItem = false;
	//鼠标根节点，控制显示/隐藏
	public GameObject mouseRoot;
	//显示图标
	public Image imgMouse;
	//显示数量
	public TMP_Text txtMouse;
	//BagPanel是否打开
	public bool isOpen = false;

	public HotbarSlot[] bagSlotData = new HotbarSlot[30];

	public static BagPanel Instance;

	public GameObject contentRecipe;
	public GameObject contentShow;
    private int craftRefreshVersion;

    private bool bagReady;
    private bool hotbarReady;
    public override void HideMe()
	{
		Time.timeScale = 1f;
		//关闭背包时，如果鼠标上还拿着物品，尝试放回背包
		if (hasHeldItem && heldItemPrefab != null)
		{
			var itemBase = heldItemPrefab.GetComponent<ItemBase>();
			if (itemBase != null)
			{
				// 先尝试放进空槽位
				for (int i = 0; i < bagSlotData.Length; i++)
				{
					if (bagSlotData[i].item == null)
					{
						bagSlotData[i].item = heldItemPrefab;
						bagSlotData[i].count = heldItemCount;
						hasHeldItem = false;
						heldItemPrefab = null;
						heldItemCount = 0;
						mouseRoot.SetActive(false);
						SaveBagToArchive();
						return;
					}
				}
				Debug.LogWarning("HideMe: 背包已满，鼠标上的物品无法放回，物品将丢失！");
			}
		}
	}

	public override void ShowMe()
	{
        //必须在统计前赋值，否则UpdateHandWork里gamePanel是null
        gamePanel = FindObjectOfType<GamePanel>();

        //本次打开先归零
        bagReady = false;
        hotbarReady = false;

        //快捷栏如果已经由GamePanel恢复完，直接标记就绪
        if (gamePanel != null && gamePanel.inventoryReady)
            hotbarReady = true;

        //背包异步恢复，完成后会调OnBagDataLoaded
        LoadBagFromArchive();

        UpdateHeldCursorVisual();
        Time.timeScale = 0f;
    }

    protected override void ClickBtn(string btnName)
	{
		// 背包格子按钮 "Button0" ~ "Button29"
		if (btnName.StartsWith("Button"))
		{
			int slotIndex = int.Parse(btnName.Replace("Button", ""));
			OnBagSlotClicked(slotIndex);
		}
		SaveBagToArchive();
	}

	protected override void Awake()
	{
		base.Awake();
		Instance = this;
		//从存档数据的加载
		LoadBagFromArchive();

		// 初始化背包槽位数据，关联每个按钮下的imgItem和txtNumber
		for (int i = 0; i < bagSlotData.Length; i++)
		{
			bagSlotData[i] = new HotbarSlot();
			Button btn = GetControl<Button>($"Button{i}");
			if (btn != null)
			{
				bagSlotData[i].image = btn.transform.Find("imgItem")?.GetComponent<Image>();
				bagSlotData[i].countText = btn.transform.Find("txtNumber")?.GetComponent<TMP_Text>();
			}
		}
	}

	private void Update()
	{
		if (mouseRoot)
		{
			//把鼠标屏幕坐标转换成 Canvas 坐标
			Vector2 localPos;
			RectTransform canvasRect = GetComponent<RectTransform>();
			RectTransformUtility.ScreenPointToLocalPointInRectangle(
				canvasRect,
				Input.mousePosition,
				GameObject.Find("UICamera(Clone)").GetComponent<Camera>(),
				out localPos
			);
			mouseRoot.GetComponent<RectTransform>().localPosition = localPos;
		}
	}
	/// <summary>
	/// 当点击背包格子时触发
	/// </summary>
	/// <param name="slotIndex"></param>
	void OnBagSlotClicked(int slotIndex)
	{
		//从按钮上读取当前槽位
		Button btn = GetControl<Button>($"Button{slotIndex}");
		Image slotIcon = btn.transform.Find("imgItem").GetComponent<Image>();
		TMP_Text slotText = btn.transform.Find("txtNumber").GetComponent<TMP_Text>();

		//通过图标是否可见来判断槽位有没有物品
		bool slotHasItem = slotIcon.sprite != null;

		//鼠标没东西
		if (!hasHeldItem)
		{
			if (!slotHasItem) return;  //槽是空的，无操作

			// 把槽位里的物品"拿"到鼠标上
			heldItemPrefab = bagSlotData[slotIndex].item;  //备份原始数据
			var pickItemBase = heldItemPrefab != null ? heldItemPrefab.GetComponent<ItemBase>() : null;
			// 防御：如果 ItemBase 组件缺失，不做任何操作，保护物品不丢失
			if (pickItemBase == null)
			{
				Debug.LogError($"OnBagSlotClicked: 槽位 {slotIndex} 的物品 prefab 上没有 ItemBase 组件，取消操作");
				return;
			}
			heldItemCount = bagSlotData[slotIndex].count;
			hasHeldItem = true;

			//清空槽位数据和显示
			bagSlotData[slotIndex].item = null;
			bagSlotData[slotIndex].count = 0;
			bagSlotData[slotIndex].countText.text = "";
			slotIcon.sprite = null;
			slotIcon.color = new Color(1, 1, 1, 0);  //透明
			slotText.text = "";

			//更新鼠标上的图标
			UpdateHeldCursorVisual();
		}

		//鼠标有东西
		else
		{
			//槽位没东西
			if (!slotHasItem)
			{
				//槽位空 把鼠标物品放进去
				bagSlotData[slotIndex].item = heldItemPrefab;
				bagSlotData[slotIndex].count = heldItemCount;
				bagSlotData[slotIndex].countText.text = heldItemCount.ToString();
				//鼠标设置为没有东西
				hasHeldItem = false;
				//鼠标上的预设体引用为空
				heldItemPrefab = null;
				//鼠标上的预设体数量为0
				heldItemCount = 0;
			}
			else
			{
				var tempPrefab = bagSlotData[slotIndex].item;
				var tempCount = bagSlotData[slotIndex].count;
				//如果槽位有东西 要先判断是否是同类且可堆叠的 如果是的话就堆叠数量
				//是同类 且可堆叠
				if (tempPrefab == heldItemPrefab && tempCount != 0)
				{
					bagSlotData[slotIndex].count += heldItemCount;
					bagSlotData[slotIndex].countText.text = bagSlotData[slotIndex].count.ToString();
					//鼠标设置为没有东西
					hasHeldItem = false;
					//鼠标上的预设体引用为空
					heldItemPrefab = null;
					//鼠标上的预设体数量为0
					heldItemCount = 0;
				}
				else
				{
					//不是同类 或者不可堆叠
					bagSlotData[slotIndex].item = heldItemPrefab;
					bagSlotData[slotIndex].count = heldItemCount;
					bagSlotData[slotIndex].countText.text = heldItemCount.ToString();

					heldItemPrefab = tempPrefab;
					heldItemCount = tempCount;
					//hasHeldItem 保持 true，因为交换后鼠标上还有东西
				}
			}

			//刷新槽位视觉显示
			RefreshSlotVisual(slotIndex);
			//刷新鼠标视觉
			UpdateHeldCursorVisual();
		}
	}
	/// <summary>
	/// 与GamePanel的hotbar槽位交换物品
	/// 在BagPanel打开时点击hotbar槽位调用此方法
	/// </summary>
	public void SwapWithHotbar(int hotbarIndex, GamePanel gamePanel)
	{
		// 安全检查
		if (gamePanel == null || gamePanel.hotbarSlots == null) return;
		if (hotbarIndex < 0 || hotbarIndex >= gamePanel.hotbarSlots.Length) return;

		var hotbarSlot = gamePanel.hotbarSlots[hotbarIndex];
		if (hotbarSlot == null) return;

		bool slotHasItem = hotbarSlot.item != null;

		//情况A：鼠标没东西
		if (!hasHeldItem)
		{
			if (!slotHasItem) return;

			// 把hotbar槽位的物品捡到鼠标上
			heldItemPrefab = hotbarSlot.item;
			heldItemCount = hotbarSlot.count;
			hasHeldItem = true;

			// 清空hotbar槽位
			hotbarSlot.item = null;
			hotbarSlot.count = 0;
			if (hotbarSlot.image != null)
			{
				hotbarSlot.image.sprite = null;
				hotbarSlot.image.color = new Color(1, 1, 1, 0);
			}
			if (hotbarSlot.countText != null)
				hotbarSlot.countText.text = "";

			UpdateHeldCursorVisual();
		}
		//情况B：鼠标有东西
		else
		{
			//槽位没东西
			if (!slotHasItem)
			{
				//hotbar槽位空 把鼠标物品放进去
				hotbarSlot.item = heldItemPrefab;
				hotbarSlot.count = heldItemCount;

				hasHeldItem = false;
				heldItemPrefab = null;
				heldItemCount = 0;
			}
			else
			{
				//hotbar槽位有物品 判断物品种类 如果相同且允许堆叠就堆叠
				var tempPrefab = hotbarSlot.item;
				var tempCount = hotbarSlot.count;
				//物品种类相同且可堆叠
				if (tempPrefab == heldItemPrefab && tempCount != 0)
				{
					hotbarSlot.count += heldItemCount;
					//清空手上的物品
					hasHeldItem = false;
					heldItemPrefab = null;
					heldItemCount = 0;
				}
				else
				{
					//物品种类不同 交换
					hotbarSlot.item = heldItemPrefab;
					hotbarSlot.count = heldItemCount;

					heldItemPrefab = tempPrefab;
					heldItemCount = tempCount;
				}
			}

			// 刷新hotbar槽位视觉显示
			gamePanel.RefreshHotbarSlotVisual(hotbarIndex);
			UpdateHeldCursorVisual();
		}
		SaveBagToArchive();
	}

	//强制居中锚点并按精灵原始尺寸显示
	private void SyncIconSize(Image icon)
	{
		var rect = (RectTransform)icon.transform;
		rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
		rect.anchoredPosition = Vector2.zero;
		icon.SetNativeSize();
	}

	//刷新鼠标上的图片和文字
	public void UpdateHeldCursorVisual()
	{
		//如果鼠标上有东西
		if (hasHeldItem && heldItemPrefab != null)
		{
			var itemBase = heldItemPrefab.GetComponent<ItemBase>();
			// 防御：如果 ItemBase 组件不存在，清空鼠标状态，防止物品丢失
			if (itemBase == null)
			{
				Debug.LogError($"UpdateHeldCursorVisual: heldItemPrefab '{heldItemPrefab.name}' 上没有 ItemBase 组件，鼠标物品已丢弃");
				hasHeldItem = false;
				heldItemPrefab = null;
				heldItemCount = 0;
				mouseRoot.SetActive(false);
				return;
			}
			imgMouse.sprite = itemBase.icon;
			var mouseRect = (RectTransform)imgMouse.transform;
			mouseRect.anchorMin = mouseRect.anchorMax = new Vector2(0.5f, 0.5f);
			mouseRect.anchoredPosition = Vector2.zero;
			imgMouse.SetNativeSize();
			imgMouse.color = new Color(1, 1, 1, 1);
			txtMouse.text = itemBase.isStack ? heldItemCount.ToString() : "";
			mouseRoot.SetActive(true);
		}
		else
		{
			mouseRoot.SetActive(false);
		}
	}
	//刷新单个槽位的图标和文字
	public void RefreshSlotVisual(int slotIndex)
	{
		var data = bagSlotData[slotIndex];
		Button btn = GetControl<Button>($"Button{slotIndex}");
		Image icon = btn.transform.Find("imgItem").GetComponent<Image>();
		TMP_Text text = btn.transform.Find("txtNumber").GetComponent<TMP_Text>();

		if (data.item != null)
		{
			var itemBase = data.item.GetComponent<ItemBase>();
			if (itemBase == null)
			{
				// ItemBase 缺失，隐藏该槽位显示
				icon.sprite = null;
				icon.color = new Color(1, 1, 1, 0);
				text.text = "";
				return;
			}
			icon.sprite = itemBase.icon;
			SyncIconSize(icon);
			icon.color = new Color(1, 1, 1, 1);
			text.text = itemBase.isStack ? data.count.ToString() : "";
		}
		else
		{
			icon.sprite = null;
			icon.color = new Color(1, 1, 1, 0);
			text.text = "";
		}
	}
	//保存背包数据到存档
	public void SaveBagToArchive()
	{
		//获取之前的存档数据
		var archive = ArchiveManager.Instance.currentArchive;
		//清除之前的存档数据
		archive.bagInventory.Clear();
		//写入新的存档数据
		for (int i = 0; i < bagSlotData.Length; i++)
		{
			if (bagSlotData[i].item != null)
			{
				var itemBase = bagSlotData[i].item.GetComponent<ItemBase>();
				archive.bagInventory.Add(new InventorySlotData
				{
					slotIndex = i,
					itemName = itemBase.itemName,
					count = bagSlotData[i].count,
				});
			}
		}
	}
    //从存档加载背包数据到面板
    public void LoadBagFromArchive()
    {
        var archive = ArchiveManager.Instance.currentArchive;

        //先清空旧数据，避免上次打开的残留影响统计
        for (int i = 0; i < bagSlotData.Length; i++)
        {
            bagSlotData[i].item = null;
            bagSlotData[i].count = 0;
        }

        int totalToLoad = archive.bagInventory.Count;
        int loadedCount = 0;

        if (totalToLoad == 0)
        {
            OnBagDataLoaded();
            return;
        }

        foreach (var slotData in archive.bagInventory)
        {
            if (slotData.slotIndex < 0 || slotData.slotIndex >= bagSlotData.Length)
            {
                loadedCount++;
                continue;
            }

            ABResMgr.Instance.LoadResAsync<GameObject>("material", slotData.itemName, (prefab) =>
            {
                if (prefab != null)
                {
                    bagSlotData[slotData.slotIndex].item = prefab;
                    bagSlotData[slotData.slotIndex].count = slotData.count;
                    RefreshSlotVisual(slotData.slotIndex);
                }

                loadedCount++;

                if (loadedCount >= totalToLoad)
                {
                    OnBagDataLoaded();
                }
            });
        }
    }

    /// <summary>
    /// 给背包添加物品
    /// </summary>
    /// <param name="itemPrefab"></param>
    /// <returns></returns>
    public bool TryAddItem(GameObject itemPrefab)
	{
		if (itemPrefab == null) return false;
		var newItem = itemPrefab.GetComponent<ItemBase>();
		if (newItem == null) return false;

		// 1.可堆叠
		if (newItem.isStack)
		{
			for (int i = 0; i < bagSlotData.Length; i++)
			{
				if (bagSlotData[i].item != null)
				{
					var existItem = bagSlotData[i].item.GetComponent<ItemBase>();
					if (existItem != null && existItem.itemName == newItem.itemName)
					{
						bagSlotData[i].count++;
						RefreshSlotVisual(i);
						SaveBagToArchive();
						return true;
					}
				}
			}
		}

		// 2.不可堆叠
		for (int i = 0; i < bagSlotData.Length; i++)
		{
			if (bagSlotData[i].item == null)
			{
				bagSlotData[i].item = itemPrefab;
				bagSlotData[i].count = 1;
				RefreshSlotVisual(i);
				SaveBagToArchive();
				return true;
			}
		}

		// 3.背包满了
		return false;
	}

	/// <summary>
	/// 更新背包工作台的产出显示
	/// </summary>
	private void UpdateHandWork()
	{
        craftRefreshVersion++;
        int version = craftRefreshVersion;

        foreach (Transform child in contentRecipe.transform)
			Destroy(child.gameObject);

		//统计背包+快捷栏所有物品数量
		var itemCounts = new Dictionary<string, int>();
		//统计背包
		CountMaterials(bagSlotData, itemCounts);
		//统计快捷栏
		if (gamePanel != null)
			CountMaterials(gamePanel.hotbarSlots, itemCounts);

		RecipeManager.Instance.GetRecipesForStation(0, (list) =>
		{
            //过期回调直接丢弃
            if (version != craftRefreshVersion) return;

            for (int i = 0; i < list.Count; i++)
			{
				int index = i;

				//判断材料是否足够
				bool canCraft = true;
				foreach (var input in list[index].inputs)
				{
					if (!itemCounts.TryGetValue(input.itemName, out int have) || have < input.count)
					{
						canCraft = false;
						break;
					}
				}

				ABResMgr.Instance.LoadResAsync<GameObject>("ui", "Recipe", (recipe) =>
				{
                    //创建之前再检查一次
                    if (version != craftRefreshVersion) return;

                    var obj = Instantiate(recipe);
					obj.transform.SetParent(contentRecipe.transform);
					obj.transform.localPosition = Vector3.zero;
					obj.transform.localScale = Vector3.one;

					//图标
					var img = obj.transform.Find("imgItem").GetComponent<Image>();
					img.sprite = list[index].output.sprite;
					img.color = new Color(1, 1, 1, 1);
					var rect = (RectTransform)img.transform;
					rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
					rect.anchoredPosition = Vector2.zero;
					img.SetNativeSize();

					//数量文本
					var txt = obj.GetComponentInChildren<TMP_Text>();
					txt.text = list[index].output.count > 0 ? list[index].output.count.ToString() : "";

					//按钮状态
					var btn = obj.GetComponent<Button>();
					btn.interactable = canCraft;
					img.color = canCraft ? Color.white : new Color(0.35f, 0.35f, 0.35f, 1f);

					//点击合成
					if (canCraft)
					{
						btn.onClick.AddListener(() =>
						{
							TryCraftRecipe(list[index]);
						});
					}
				});
			}
		});
	}

	/// <summary>
	/// 统计一组槽位中每种物品的数量，累加到传入的字典中
	/// </summary>
	private void CountMaterials(HotbarSlot[] slots, Dictionary<string, int> result)
	{
		foreach (var slot in slots)
		{
			if (slot.item == null) continue;
			var ib = slot.item.GetComponent<ItemBase>();
			if (ib == null) continue;
			if (!result.ContainsKey(ib.itemName))
				result[ib.itemName] = 0;
			result[ib.itemName] += slot.count;
		}
	}

	/// <summary>
	/// 执行合成：扣材料，产物放到鼠标上
	/// </summary>
	private void TryCraftRecipe(RecipeCreate recipe)
	{
		//鼠标上已经有不同东西或不可堆叠东西时不允许合成
		if (hasHeldItem)
		{
			//如果是同类物品且可堆叠
			if(heldItemPrefab.name == recipe.output.itemName && recipe.output.isStack)
			{
				heldItemCount += recipe.output.count;
				txtMouse.text = heldItemCount.ToString();
				//扣材料：先从快捷栏扣，再从背包扣
				foreach (var input in recipe.inputs)
				{
					int remaining = input.count;

					if (gamePanel != null)
					DeductFromSlots(gamePanel.hotbarSlots, input.itemName, ref remaining);
					if (remaining > 0)
					DeductFromSlots(bagSlotData, input.itemName, ref remaining);
				}
				//刷新 UI 并保存
				for (int i = 0; i < bagSlotData.Length; i++)
					RefreshSlotVisual(i);
				if (gamePanel != null)
				{
					for (int i = 0; i < gamePanel.hotbarSlots.Length; i++)
					{
						gamePanel.RefreshHotbarSlotVisual(i);
					}
					gamePanel.SaveInventoryToArchive();
				}
				SaveBagToArchive();

				//材料变了，重建配方列表状态
				UpdateHandWork();
				return;
			}
			else
			{
				return;
			}
		}

		//扣材料：先从快捷栏扣，再从背包扣
		foreach (var input in recipe.inputs)
		{
			int remaining = input.count;

			if (gamePanel != null)
				DeductFromSlots(gamePanel.hotbarSlots, input.itemName, ref remaining);
			if (remaining > 0)
				DeductFromSlots(bagSlotData, input.itemName, ref remaining);
		}

		//产物放到鼠标上
		ABResMgr.Instance.LoadResAsync<GameObject>("material", recipe.output.itemName, (prefab) =>
		{
			if (prefab == null) return;
			heldItemPrefab = prefab;
			heldItemCount = recipe.output.count;
			hasHeldItem = true;
			UpdateHeldCursorVisual();
		});

		//刷新UI并保存
		for (int i = 0; i < bagSlotData.Length; i++)
			RefreshSlotVisual(i);
		if (gamePanel != null)
		{
			for (int i = 0; i < gamePanel.hotbarSlots.Length; i++)
				gamePanel.RefreshHotbarSlotVisual(i);
			gamePanel.SaveInventoryToArchive();
		}
		SaveBagToArchive();

		//材料变了，刷新配方列表状态
		UpdateHandWork();
	}

	/// <summary>
	/// 从指定槽位数组中扣除指定物品的指定数量
	/// </summary>
	private void DeductFromSlots(HotbarSlot[] slots, string itemName, ref int remaining)
	{
		foreach (var slot in slots)
		{
			if (slot.item == null || remaining <= 0) continue;
			var ib = slot.item.GetComponent<ItemBase>();
			if (ib == null || ib.itemName != itemName) continue;

			int deduct = Mathf.Min(slot.count, remaining);
			slot.count -= deduct;
			remaining -= deduct;
			if (slot.count <= 0) slot.item = null;
		}
	}

    private void OnBagDataLoaded()
    {
        bagReady = true;
        TryRefreshCraftWhenReady();
    }

    public void OnInventoryReady()
    {
        hotbarReady = true;
        TryRefreshCraftWhenReady();
    }

    private void TryRefreshCraftWhenReady()
    {
        if (bagReady && hotbarReady)
        {
            bagReady = false;
            hotbarReady = false;
            UpdateHandWork();
        }
    }
}
