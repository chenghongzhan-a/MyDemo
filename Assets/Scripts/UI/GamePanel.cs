using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GamePanel : BasePanel
{
    PlayerArchiveInfo playerData = new PlayerArchiveInfo();
    RoleController player;

    public HotbarSlot[] hotbarSlots = new HotbarSlot[10];

    public bool inventoryReady;

    public override void HideMe()
    {

    }

    public override void ShowMe()
    {
        inventoryReady = false;

        playerData = ArchiveManager.Instance.currentArchive;
        player = GameObject.Find("Player").GetComponent<RoleController>();
        LoadInventoryFromArchive();
        //UIMgr.Instance.ShowPanel<BagPanel>();
        //UIMgr.Instance.HidePanel<BagPanel>();
    }

    private void Update()
    {
        //更新玩家血条
        GetControl<Image>("imgHp").fillAmount = playerData.health / playerData.maxHealth;
        GetControl<Image>("imgHungry").fillAmount = playerData.hunger / playerData.maxHunger;
        GetControl<Image>("imgThirst").fillAmount = playerData.thirst / playerData.maxThirst;
    }

    protected override void ClickBtn(string btnName)
    {
        switch (btnName)
        {
            case "btnSetting":
                UIMgr.Instance.ShowPanel<GamePausePanel>();
                break;
            case "btnBag":
                UIMgr.Instance.ShowPanel<BagPanel>(E_UILayer.Middle, (panel) =>
                {
                    if (panel.isOpen)
                    {
                        UIMgr.Instance.HidePanel<BagPanel>();
                        panel.isOpen = false;
                    }
                    else
                    {
                        panel.isOpen = true;
                    }
                });
                break;
        }
    }

    /// <summary>
    /// tog1~tog10 通过点击的tog的名字得到Tog的索引
    /// </summary>
    private int GetSlotIndexFromToggle(string togName)
    {
        switch (togName)
        {
            case "tog1":  return 0;
            case "tog2":  return 1;
            case "tog3":  return 2;
            case "tog4":  return 3;
            case "tog5":  return 4;
            case "tog6":  return 5;
            case "tog7":  return 6;
            case "tog8":  return 7;
            case "tog9":  return 8;
            case "tog10": return 9;
            default:      return -1;
        }
    }

    /// <summary>
    /// tog点击时做的事情
    /// </summary>
    /// <param name="togName"></param>
    /// <param name="value"></param>
    protected override void ToggleValueChange(string togName, bool value)
    {
        if (!value) return;

        int slotIndex = GetSlotIndexFromToggle(togName);
        if (slotIndex == -1) return;

        // 背包打开时，点击hotbar槽位视为拾取物品到鼠标
        if (BagPanel.Instance != null && BagPanel.Instance.isOpen)
        {
            BagPanel.Instance.SwapWithHotbar(slotIndex, this);
            EquipSlotItem(slotIndex);
            ResetToggle(slotIndex);
            return;
        }

        // 背包关闭时，执行物品装备逻辑
        EquipSlotItem(slotIndex);
    }

    //给玩家手上放东西或收回东西
    private void EquipSlotItem(int slotIndex)
    {
        if (player == null) return;

        GameObject itemPrefab = null;

        //获取物品栏上是否有物品
        if (hotbarSlots != null && slotIndex < hotbarSlots.Length)
        {
            itemPrefab = hotbarSlots[slotIndex].item;
        }


        player.EquipItem(itemPrefab);
    }

    /// <summary>
    /// 刷新快捷栏物品显示
    /// </summary>
    public void RefreshHotbarSlotVisual(int slotIndex)
    {
        if (hotbarSlots == null || slotIndex < 0 || slotIndex >= hotbarSlots.Length) return;
        var slot = hotbarSlots[slotIndex];
        if (slot == null) return;

        if (slot.item != null)
        {
            var itemBase = slot.item.GetComponent<ItemBase>();
            if (itemBase == null) return;
            if (slot.image != null)
            {
                slot.image.sprite = itemBase.icon;
                var imgRect = (RectTransform)slot.image.transform;
                imgRect.anchorMin = imgRect.anchorMax = new Vector2(0.5f, 0.5f);
                imgRect.anchoredPosition = Vector2.zero;
                slot.image.SetNativeSize();
                slot.image.color = new Color(1, 1, 1, 1);
            }
            if (slot.countText != null)
                slot.countText.text = itemBase.isStack ? slot.count.ToString() : "";
        }
        else
        {
            if (slot.image != null)
            {
                slot.image.sprite = null;
                slot.image.color = new Color(1, 1, 1, 0);
            }
            if (slot.countText != null)
                slot.countText.text = "";
        }
    }

    /// <summary>
    /// 将指定toggle设为关闭状态，这样下次还可以再次点击
    /// </summary>
    public void ResetToggle(int slotIndex)
    {
        var toggle = GetControl<Toggle>($"tog{slotIndex + 1}");
        if (toggle != null)
            toggle.SetIsOnWithoutNotify(false);
    }

    /// <summary>
    /// 向物品栏添加物品，可堆叠则数量+1，否则找空位放入，满了返回false
    /// </summary>
    public bool AddItem(GameObject itemPrefab)
    {
        if (itemPrefab == null) return false;

        var newItem = itemPrefab.GetComponent<ItemBase>();
        if (newItem == null) return false;

        //1.可堆叠 → 找已有的同类物品，数量+1
        if (newItem.isStack)
        {
            //查找是否有同类
            for (int i = 0; i < hotbarSlots.Length; i++)
            {
                var slot = hotbarSlots[i];
                //如果有
                if (slot.item != null)
                {
                    var existItem = slot.item.GetComponent<ItemBase>();
                    if (existItem != null && existItem.itemName == newItem.itemName)
                    {
                        hotbarSlots[i].count++;
                        //刷新物品栏显示
                        ReFreshHotBarSlot();
                        return true;
                    }
                }
            }
        }
        //2.如果没有同类 或者不能堆叠 找空槽位放入
        for (int i = 0; i < hotbarSlots.Length; i++)
        {
            var slot = hotbarSlots[i];
            if (slot.item == null)
            {
                slot.item = itemPrefab;
                slot.count = 1;
                //刷新物品栏显示
                ReFreshHotBarSlot();
                return true;
            }
        }
        //3.如果物品栏满了 就往背包里面加东西
        if (BagPanel.Instance != null && BagPanel.Instance.TryAddItem(itemPrefab))
        {
            return true;
        }

        //4.物品栏和背包都满了
        return false;
    }

    /// <summary>
    /// 刷新物品栏的显示
    /// </summary>
    public void ReFreshHotBarSlot()
    {
        for (int i = 0; i < hotbarSlots.Length; i++)
        {
            var slot = hotbarSlots[i];
            // 如果物品栏有东西
            if (slot.item != null)
            {
                var itemBase = slot.item.GetComponent<ItemBase>();
                slot.image.sprite = itemBase.icon;
                var imgRect = (RectTransform)slot.image.transform;
                imgRect.anchorMin = imgRect.anchorMax = new Vector2(0.5f, 0.5f);
                imgRect.anchoredPosition = Vector2.zero;
                slot.image.SetNativeSize();
                slot.image.color = new Color(slot.image.color.r, slot.image.color.g, slot.image.color.b, 1);
                //如果物品是可堆叠的 就显示它的数量
                if (itemBase.isStack)
                {
                    slot.countText.text = slot.count.ToString();
                }
                else
                {
                    //不可堆叠就隐藏数量的显示
                    slot.countText.text = null;
                }
            }
            else
            {
                //如果没有
                slot.image.sprite = null;
                slot.countText.text = null;
                slot.image.color = new Color(slot.image.color.r, slot.image.color.g, slot.image.color.b, 0);
            }
        }
    }

    /// <summary>
    /// 将当前物品栏数据写入 ArchiveManager.currentArchive
    /// </summary>
    public void SaveInventoryToArchive()
    {
        if (ArchiveManager.Instance.currentArchive == null)
        {
            Debug.LogWarning("SaveInventoryToArchive: currentArchive 为空，跳过保存");
            return;
        }

        // 清空旧的物品栏存档数据
        ArchiveManager.Instance.currentArchive.inventory.Clear();

        for (int i = 0; i < hotbarSlots.Length; i++)
        {
            var slot = hotbarSlots[i];
            if (slot.item != null)
            {
                var itemBase = slot.item.GetComponent<ItemBase>();
                if (itemBase != null)
                {
                    ArchiveManager.Instance.currentArchive.inventory.Add(new InventorySlotData
                    {
                        slotIndex = i,
                        itemName = itemBase.itemName,
                        count = hotbarSlots[i].count,
                    });
                }
            }
        }

        Debug.Log($"物品栏已保存: {ArchiveManager.Instance.currentArchive.inventory.Count} 个槽位有物品");
    }

    /// <summary>
    /// 从 ArchiveManager.currentArchive 恢复物品栏数据
    /// </summary>
    public void LoadInventoryFromArchive()
    {
        if (playerData == null || playerData.inventory == null || playerData.inventory.Count == 0)
        {
            inventoryReady = true;
            ReFreshHotBarSlot();
            BagPanel.Instance?.OnInventoryReady();
            return;
        }

        for (int i = 0; i < hotbarSlots.Length; i++)
            hotbarSlots[i].item = null;

        int loadedCount = 0;
        int totalToLoad = playerData.inventory.Count;

        foreach (var slotData in playerData.inventory)
        {
            if (slotData.slotIndex < 0 || slotData.slotIndex >= hotbarSlots.Length)
            {
                loadedCount++;
                continue;
            }

            ABResMgr.Instance.LoadResAsync<GameObject>("material", slotData.itemName, (prefab) =>
            {
                if (prefab != null)
                {
                    hotbarSlots[slotData.slotIndex].item = prefab;
                    hotbarSlots[slotData.slotIndex].count = slotData.count;
                }
                loadedCount++;

                if (loadedCount >= totalToLoad)
                {
                    inventoryReady = true;
                    ReFreshHotBarSlot();
                    BagPanel.Instance?.OnInventoryReady();
                }
            });
        }
    }

    /// <summary>
    /// 减少或者清空道具栏以及手上拿着的物品
    /// </summary>
    public void ConsumeEquippedItem()
    {
        //找到当前选中的热键栏槽位
        int activeIndex = -1;
        for (int i = 0; i < hotbarSlots.Length; i++)
        {
            var tog = GetControl<Toggle>($"tog{i + 1}");
            if (tog != null && tog.isOn)
            {
                activeIndex = i;
                break;
            }
        }
        if (activeIndex == -1) return;

        var slot = hotbarSlots[activeIndex];
        if (slot.item == null) return;

        slot.count--;

        if (slot.count <= 0)
        {
            slot.item = null;
            slot.count = 0;
            //清空手上的物品
            player.EquipItem(null);
            //取消Toggle选中
            ResetToggle(activeIndex);
        }
        //刷新物品栏显示
        RefreshHotbarSlotVisual(activeIndex);
    }
}
