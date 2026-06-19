#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 主场景 / 商城顶部资源条统一搭建：槽位命名、尺寸、配色与绑定字段一致。
/// </summary>
public static class TopResourceBarEditorUtility
{
    public const string DiamondSlotName = "TopResourceDiamond";
    public const string GoldSlotName = "TopResourceGold";
    public const string AdTicketSlotName = "TopResourceAdTicket";
    public const string StaminaSlotName = "TopResourceStamina";

    public const float PillWidth = 230f;
    public const float PillHeight = 58f;
    public const float StaminaPillHeight = 76f;

    private static readonly Color PanelDeep = Hex("#071632");
    private static readonly Color NeonBlue = Hex("#4DB7FF");
    private static readonly Color NeonGold = Hex("#FFD15A");
    private static readonly Color TextWhite = Hex("#F5FAFF");
    private static readonly Color AdTicketAccent = Hex("#FF5E7E");
    private static readonly Color StaminaAccent = Hex("#5BE7FF");
    private static readonly Color SubtitleColor = Hex("#9EC8FF");

    /// <summary>商城 TopBar 内相对坐标（y=0）。</summary>
    public static readonly Vector2[] ShopBarPositions =
    {
        new Vector2(-360f, 0f),
        new Vector2(-120f, 0f),
        new Vector2(120f, 0f),
        new Vector2(360f, 0f),
    };

    /// <summary>主场景 SafeArea 内坐标（与商城 x 对齐，y 适配头像行）。</summary>
    public static readonly Vector2[] MainSceneBarPositions =
    {
        new Vector2(-360f, 760f),
        new Vector2(-120f, 760f),
        new Vector2(120f, 760f),
        new Vector2(360f, 760f),
    };

    private const string MainScenePath = "Assets/Scenes/MainScene.unity";

    /// <summary>菜单：同步主场景与商城 TopBar 四槽资源条布局。</summary>
    [MenuItem("Attack Barbarians/UI/Sync Top Resource Bars In MainScene")]
    public static void SyncTopResourceBarsInMainScene()
    {
        Scene scene = EditorSceneManager.OpenScene(MainScenePath, OpenSceneMode.Single);
        bool changed = false;

        changed |= RebuildBar(FindChildRecursive(Object.FindObjectOfType<MainSceneView>()?.transform, "TopResources"), MainSceneBarPositions);
        changed |= RebuildBar(FindChildRecursive(Object.FindObjectOfType<MainSceneView>()?.transform, "ShopTopBar"), ShopBarPositions);

        if (!changed)
        {
            Debug.LogWarning("[TopResourceBarEditorUtility] 未找到 TopResources 或 ShopTopBar，请先搭建主场景/商城 UI。");
            return;
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[TopResourceBarEditorUtility] 主场景与商城顶部资源条已同步为统一四槽布局。");
    }

    /// <summary>在指定根节点创建四槽资源条并绑定面板。</summary>
    /// <param name="root">TopBar 根节点。</param>
    /// <param name="panel">资源条面板。</param>
    /// <param name="positions">各槽位坐标数组。</param>
    public static void Build(RectTransform root, TopResourceBarPanel panel, Vector2[] positions)
    {
        if (root == null || panel == null || positions == null || positions.Length < 4)
        {
            return;
        }

        UI_ItemSlot diamond = CreateResourcePill(
            root,
            DiamondSlotName,
            positions[0],
            "0",
            NeonBlue,
            CurrencyType.Diamond);
        UI_ItemSlot gold = CreateResourcePill(
            root,
            GoldSlotName,
            positions[1],
            "0",
            NeonGold,
            CurrencyType.Gold);
        UI_ItemSlot adTicket = CreateResourcePill(
            root,
            AdTicketSlotName,
            positions[2],
            "0",
            AdTicketAccent,
            CurrencyType.AdTicket);
        UI_ItemSlot stamina = CreateStaminaResourcePill(
            root,
            StaminaSlotName,
            positions[3],
            $"{StaminaConstants.DefaultMaxStamina}/{StaminaConstants.DefaultMaxStamina}",
            StaminaAccent);

        BindPanel(panel, diamond, gold, adTicket, stamina);
    }

    /// <summary>将四个 UI_ItemSlot 写入 TopResourceBarPanel 字段。</summary>
    /// <param name="panel">资源条面板。</param>
    /// <param name="diamond">钻石槽。</param>
    /// <param name="gold">金币槽。</param>
    /// <param name="adTicket">广告券槽。</param>
    /// <param name="stamina">体力槽。</param>
    public static void BindPanel(
        TopResourceBarPanel panel,
        UI_ItemSlot diamond,
        UI_ItemSlot gold,
        UI_ItemSlot adTicket,
        UI_ItemSlot stamina)
    {
        SerializedObject so = new SerializedObject(panel);
        so.FindProperty("diamondSlot").objectReferenceValue = diamond;
        so.FindProperty("goldSlot").objectReferenceValue = gold;
        so.FindProperty("adTicketSlot").objectReferenceValue = adTicket;
        so.FindProperty("staminaSlot").objectReferenceValue = stamina;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    /// <summary>清空并重建指定 TopBar 的四槽布局。</summary>
    /// <param name="root">TopBar Transform。</param>
    /// <param name="positions">槽位坐标。</param>
    private static bool RebuildBar(Transform root, Vector2[] positions)
    {
        if (root == null)
        {
            return false;
        }

        TopResourceBarPanel panel = root.GetComponent<TopResourceBarPanel>();
        if (panel == null)
        {
            panel = root.name.Contains("Shop")
                ? root.gameObject.AddComponent<ShopResourcePanel>()
                : root.gameObject.AddComponent<MainSceneResourcePanel>();
        }

        for (int i = root.childCount - 1; i >= 0; i--)
        {
            Object.DestroyImmediate(root.GetChild(i).gameObject);
        }

        Build(root as RectTransform, panel, positions);
        return true;
    }

    /// <summary>递归按名称查找子节点。</summary>
    /// <param name="root">搜索根。</param>
    /// <param name="name">节点名称。</param>
    private static Transform FindChildRecursive(Transform root, string name)
    {
        if (root == null)
        {
            return null;
        }

        if (root.name == name)
        {
            return root;
        }

        for (int i = 0; i < root.childCount; i++)
        {
            Transform found = FindChildRecursive(root.GetChild(i), name);
            if (found != null)
            {
                return found;
            }
        }

        return null;
    }

    /// <summary>创建普通货币资源 Pill（图标+数值+加号）。</summary>
    /// <param name="parent">父级。</param>
    /// <param name="name">节点名称。</param>
    /// <param name="position">位置。</param>
    /// <param name="value">初始数值文本。</param>
    /// <param name="accent">图标强调色。</param>
    /// <param name="currency">货币类型。</param>
    private static UI_ItemSlot CreateResourcePill(
        RectTransform parent,
        string name,
        Vector2 position,
        string value,
        Color accent,
        CurrencyType currency)
    {
        Image panel = CreatePanel(parent, name, position, new Vector2(PillWidth, PillHeight), PanelDeep);
        Button add = panel.gameObject.AddComponent<Button>();
        CreatePanel(panel.rectTransform, "Icon", new Vector2(-78f, 0f), new Vector2(42f, 42f), accent);
        TMP_Text valueText = CreateText(
            panel.rectTransform,
            "ValueText",
            value,
            22,
            TextWhite,
            TextAlignmentOptions.Left,
            new Vector2(8f, 0f),
            new Vector2(120f, 42f));
        CreateText(
            panel.rectTransform,
            "AddText",
            "+",
            28,
            NeonBlue,
            TextAlignmentOptions.Center,
            new Vector2(88f, 0f),
            new Vector2(34f, 42f));

        UI_ItemSlot slot = panel.gameObject.AddComponent<UI_ItemSlot>();
        SerializedObject so = new SerializedObject(slot);
        so.FindProperty("currency").enumValueIndex = (int)currency;
        so.FindProperty("iconImage").objectReferenceValue = panel.transform.Find("Icon")?.GetComponent<Image>();
        so.FindProperty("valueText").objectReferenceValue = valueText;
        so.FindProperty("addButton").objectReferenceValue = add;
        so.ApplyModifiedPropertiesWithoutUndo();
        return slot;
    }

    /// <summary>创建体力资源 Pill（含副数值槽位）。</summary>
    /// <param name="parent">父级。</param>
    /// <param name="name">节点名称。</param>
    /// <param name="position">位置。</param>
    /// <param name="value">初始数值文本。</param>
    /// <param name="accent">图标强调色。</param>
    private static UI_ItemSlot CreateStaminaResourcePill(
        RectTransform parent,
        string name,
        Vector2 position,
        string value,
        Color accent)
    {
        Image panel = CreatePanel(parent, name, position, new Vector2(PillWidth, StaminaPillHeight), PanelDeep);
        Button add = panel.gameObject.AddComponent<Button>();
        CreatePanel(panel.rectTransform, "Icon", new Vector2(-78f, 8f), new Vector2(42f, 42f), accent);
        TMP_Text valueText = CreateText(
            panel.rectTransform,
            "ValueText",
            value,
            22,
            TextWhite,
            TextAlignmentOptions.Left,
            new Vector2(8f, 10f),
            new Vector2(120f, 28f));
        TMP_Text subValueText = CreateText(
            panel.rectTransform,
            "SubValueText",
            string.Empty,
            16,
            SubtitleColor,
            TextAlignmentOptions.Left,
            new Vector2(8f, -16f),
            new Vector2(150f, 22f));
        CreateText(
            panel.rectTransform,
            "AddText",
            "+",
            28,
            NeonBlue,
            TextAlignmentOptions.Center,
            new Vector2(88f, 0f),
            new Vector2(34f, 42f));

        UI_ItemSlot slot = panel.gameObject.AddComponent<UI_ItemSlot>();
        SerializedObject so = new SerializedObject(slot);
        so.FindProperty("currency").enumValueIndex = (int)CurrencyType.Stamina;
        so.FindProperty("iconImage").objectReferenceValue = panel.transform.Find("Icon")?.GetComponent<Image>();
        so.FindProperty("valueText").objectReferenceValue = valueText;
        so.FindProperty("subValueText").objectReferenceValue = subValueText;
        so.FindProperty("addButton").objectReferenceValue = add;
        so.ApplyModifiedPropertiesWithoutUndo();
        subValueText.gameObject.SetActive(false);
        return slot;
    }

    /// <summary>创建 Image 面板。</summary>
    /// <param name="parent">父级。</param>
    /// <param name="name">节点名称。</param>
    /// <param name="position">位置。</param>
    /// <param name="size">尺寸。</param>
    /// <param name="color">颜色。</param>
    private static Image CreatePanel(RectTransform parent, string name, Vector2 position, Vector2 size, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(parent, false);
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        Image image = go.GetComponent<Image>();
        image.color = color;
        return image;
    }

    /// <summary>创建 TMP 文本。</summary>
    /// <param name="parent">父级。</param>
    /// <param name="name">节点名称。</param>
    /// <param name="text">文本。</param>
    /// <param name="fontSize">字号。</param>
    /// <param name="color">颜色。</param>
    /// <param name="alignment">对齐。</param>
    /// <param name="position">位置。</param>
    /// <param name="size">尺寸。</param>
    private static TMP_Text CreateText(
        RectTransform parent,
        string name,
        string text,
        int fontSize,
        Color color,
        TextAlignmentOptions alignment,
        Vector2 position,
        Vector2 size)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        TMP_Text tmp = go.GetComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.color = color;
        tmp.alignment = alignment;
        tmp.raycastTarget = false;
        return tmp;
    }

    /// <summary>解析 HTML 颜色字符串。</summary>
    /// <param name="html">十六进制颜色值。</param>
    private static Color Hex(string html)
    {
        return ColorUtility.TryParseHtmlString(html, out Color color) ? color : Color.white;
    }
}
#endif
