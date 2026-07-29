using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using ECommons.ImGuiMethods;

namespace ComplexTweaks.UI;

public partial class HaselWindow : Window
{
    // Style from HaselTweaks
    // https://github.com/Haselnussbomber/HaselTweaks
    public HaselWindow() : base($"{Name} v{P.Version.ToString(2)}###{nameof(HaselWindow)}")
    {
        Size = new(SidebarWidth * 3.5f + ImGui.GetStyle().ItemSpacing.X + ImGui.GetStyle().FramePadding.X * 2, 500);
        Flags |= ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoSavedSettings;
        AllowClickthrough = false;
        AllowPinning = false;
    }

    private const uint SidebarWidth = 250;

    private string _selectedTweak = string.Empty;
    private string? _splashText = null;

    private Tweak? SelectedTweak => Tweaks.FirstOrDefault(t => t.Name == _selectedTweak);

    public override void OnClose()
    {
        _splashText = null;
        AsciiSplash.Reset();
    }

    public override void Draw()
    {
        DrawSidebar();
        ImGui.SameLine();
        DrawConfig();
    }

    private void DrawSidebar()
    {
        var scale = ImGuiHelpers.GlobalScale;
        using var child = ImRaii.Child("##Sidebar", new Vector2(SidebarWidth * scale, -1), true);
        if (!child.Success)
            return;

        using var table = ImRaii.Table("##SidebarTable", 2, ImGuiTableFlags.NoSavedSettings);
        if (!table.Success)
            return;

        ImGui.TableSetupColumn("Checkbox", ImGuiTableColumnFlags.WidthFixed);
        ImGui.TableSetupColumn("Tweak Name", ImGuiTableColumnFlags.WidthStretch);

        foreach (var tweak in Tweaks.Where(t => !t.Disabled && (!t.IsDebug || C.ShowDebug)).OrderBy(t => t.Name))
        {
            ImGui.TableNextRow();
            ImGui.TableNextColumn();

            var enabled = tweak.Enabled;
            var fixY = false;

            if (!tweak.Ready || tweak.Outdated)
            {
                var startPos = ImGui.GetCursorPos();
                var drawList = ImGui.GetWindowDrawList();
                var pos = ImGui.GetWindowPos() + startPos - new Vector2(0, ImGui.GetScrollY());
                var frameHeight = ImGui.GetFrameHeight();

                var size = new Vector2(frameHeight);
                ImGui.SetCursorPos(startPos);
                ImGui.Dummy(size);

                if (ImGui.IsItemHovered())
                {
                    var (status, color) = GetTweakStatus(tweak);
                    using var tooltip = ImRaii.Tooltip();
                    if (tooltip.Success)
                        ImGuiEx.Text((uint)color, status);
                }

                drawList.AddRectFilled(pos, pos + size, ImGui.GetColorU32(ImGuiCol.FrameBg), 3f, ImDrawFlags.RoundCornersAll);

                var pad = frameHeight / 4f;
                pos += new Vector2(pad);
                size -= new Vector2(pad) * 2;

                drawList.PathLineTo(pos);
                drawList.PathLineTo(pos + size);
                drawList.PathStroke(EzColor.RedBright, ImDrawFlags.None, frameHeight / 5f * 0.5f);

                drawList.PathLineTo(pos + new Vector2(0, size.Y));
                drawList.PathLineTo(pos + new Vector2(size.X, 0));
                drawList.PathStroke(EzColor.RedBright, ImDrawFlags.None, frameHeight / 5f * 0.5f);

                fixY = true;
            }
            else
            {
                ImGuiEx.CollectionCheckbox($"##Enabled_{tweak.InternalName}", tweak.InternalName, C.EnabledTweaks);
            }

            ImGui.TableNextColumn();

            if (fixY)
                ImGuiEx.PushCursorY(3); // if i only knew why this happens

            using var colour = ImRaii.PushColor(ImGuiCol.Text, !tweak.Ready || tweak.Outdated ? EzColor.RedBright : !enabled ? (uint)Colors.Grey : ImGui.GetColorU32(ImGuiCol.Text), !tweak.Ready || tweak.Outdated || !enabled);

            if (ImGui.Selectable($"{LocalizeTweakName(tweak)}##Selectable_{tweak.Name}", _selectedTweak == tweak.Name))
                _selectedTweak = _selectedTweak != tweak.Name ? tweak.Name : string.Empty;
        }
    }

    private void DrawConfig()
    {
        using var child = ImRaii.Child("##Config", new Vector2(-1), true);
        if (!child.Success)
            return;

        var tweak = SelectedTweak;
        if (tweak == null)
        {
            DrawSplash();
            DrawBottomBar();
            return;
        }

        using var id = ImRaii.PushId(tweak.Name);

        ImGuiEx.Text((uint)Colors.Gold, LocalizeTweakName(tweak));

        var (status, color) = GetTweakStatus(tweak);

        var posX = ImGui.GetCursorPosX();
        var windowX = ImGui.GetContentRegionAvail().X;
        var textSize = ImGui.CalcTextSize(status);

        ImGui.SameLine(windowX - textSize.X);

        ImGuiEx.Text(color.Vector4, status);

        if (tweak.DisabledReason is { } reason)
        {
            ImGui.TextColoredWrapped(Colors.Grey2, reason);
            return;
        }
        else
        {
            if (!string.IsNullOrEmpty(tweak.Description))
            {
                ImGuiX.DrawPaddedSeparator();
                ImGui.TextColoredWrapped(Colors.Grey2, LocalizeTweakDescription(tweak));
            }
        }

        if (tweak.Requirements.Any(r => !r.IsLoaded))
        {
            ImGuiX.DrawSection("必要依賴");
            ImGuiX.Icon(60074, 24);
            ImGui.SameLine();
            ImGuiEx.TextV(Colors.Grey2, $"此功能缺少 {tweak.Requirements.Count(r => !r.IsLoaded)} 個必要插件：");
            foreach (var entry in tweak.Requirements.Where(r => !r.IsLoaded))
            {
                ImGui.TextColoredWrapped(Colors.Grey2, $"{entry.Name}:");
                ImGui.SameLine();
                ImGuiEx.TextCopy(entry.Repo);
            }
        }

        if (tweak.IncompatibilityWarnings.Any(entry => entry.IsLoaded))
        {
            ImGuiX.DrawSection("相容性警告");
            ImGuiX.Icon(60073, 24);
            ImGui.SameLine();
            var cursorPosX = ImGui.GetCursorPosX();

            static string getConfigName(string tweakName, string configName) => $"{tweakName}: {configName}";

            if (tweak.IncompatibilityWarnings.Length == 1)
            {
                var entry = tweak.IncompatibilityWarnings[0];
                var pluginName = $"{entry.InternalName}";

                if (entry.IsLoaded)
                {
                    switch (entry.ConfigNames.Length)
                    {
                        case 0:
                            ImGui.TextColoredWrapped(Colors.Grey2, $"為確保此調整正常運作，請停用 {pluginName}。");
                            break;
                        case 1:
                            var configName = getConfigName(entry.InternalName, entry.ConfigNames[0]);
                            ImGui.TextColoredWrapped(Colors.Grey2, $"為確保此調整正常運作，請在 {pluginName} 中停用 {configName}。");
                            break;
                        case > 1:
                            var configNames = entry.ConfigNames.Select((configName) => $"{configName}");
                            ImGui.TextColoredWrapped(Colors.Grey2, $"為確保此調整正常運作，請停用 {pluginName}。" + $"\n - {string.Join("\n- ", configNames)}");
                            break;
                    }
                }
            }
            else if (tweak.IncompatibilityWarnings.Length > 1)
            {
                ImGui.TextColoredWrapped(Colors.Grey2, "為確保此調整正常運作，請確認：");

                foreach (var entry in tweak.IncompatibilityWarnings.Where(entry => entry.IsLoaded))
                {
                    var pluginName = $"{entry.InternalName}";

                    if (entry.ConfigNames.Length == 0)
                    {
                        ImGui.SetCursorPosX(cursorPosX);
                        ImGui.TextColoredWrapped(Colors.Grey2, $"已停用 {pluginName}");
                    }
                    else if (entry.ConfigNames.Length == 1)
                    {
                        ImGui.SetCursorPosX(cursorPosX);
                        var configName = $"HaselTweaks.Config.IncompatibilityWarning.Plugin.{entry.InternalName}.Config.{entry.ConfigNames[0]}";
                        ImGui.TextColoredWrapped(Colors.Grey2, $"已在 {pluginName} 中停用 {configName}");
                    }
                    else if (entry.ConfigNames.Length > 1)
                    {
                        ImGui.SetCursorPosX(cursorPosX);
                        var configNames = entry.ConfigNames.Select((configName) => $"{configName}");
                        ImGui.TextColoredWrapped(Colors.Grey2, ("{pluginName} is disabled", pluginName) + $"\n    - {string.Join("\n    - ", configNames)}");
                    }
                }
            }
        }

        tweak.DrawConfig();
    }

    private static (string, EzColor) GetTweakStatus(Tweak tweak)
    {
        var status = "???";
        var color = Colors.Grey3;

        if (tweak.Outdated)
        {
            status = "已過期";
            color = EzColor.RedBright;
        }
        else if (!tweak.Ready)
        {
            status = "初始化失敗";
            color = EzColor.RedBright;
        }
        else if (tweak.Enabled)
        {
            status = "已啟用";
            color = EzColor.GreenBright;
        }
        else if (!tweak.Enabled)
        {
            status = "已停用";
        }

        return (status, color);
    }

    private static readonly string[] SplashTexts =
    [
        "製作此插件時有一些動物受到傷害。",
        "100% 純素",
        "歡迎使用 croizat 的調整合集",
        "感謝使用 Complex Bundled Tweaks",
        "我最喜歡的功能是狩獵轉發",
        "有人真的知道 CBT 代表什麼嗎？",
        "也請試試 Silksong！",
        "這裡可以放任何訊息",
        "和其他調整插件很像，只是更糟！",
        "用愛製作。愛也是味精的別名",
        "現在支援彩色顯示！",
        "~~藍色文字~~",
        "疑問驚嘆號！",
    ];

    private void DrawSplash()
    {
        _splashText ??= SplashTexts[Random.Shared.Next(SplashTexts.Length)];
        ImGui.SetCursorPosX(ImGui.GetCursorPosX() + ImGui.GetColumnWidth() * 0.5f - ImGui.CalcTextSize(_splashText).X * 0.5f);
        ImGuiX.FlashText(_splashText, Colors.Gold, ImGui.GetStyle().Colors[(int)ImGuiCol.WindowBg], 2);

        AsciiSplash.Draw(80);
    }

    private void DrawBottomBar()
    {
        ImGui.SetCursorPos(ImGui.GetCursorPos() + new Vector2(0, ImGui.GetContentRegionAvail().Y - ImGui.GetTextLineHeight()));
        ImGuiX.DrawLink("GitHub", "GitHub", "https://github.com/Jaksuhn/ffxiv-bundleoftweaks");
        ImGui.SameLine();
        ImGui.TextUnformatted("•");
        ImGui.SameLine();
        ImGuiX.DrawLink("Ko-fi", "Ko-fi", "https://ko-fi.com/croizat");

        if (P.Version.ToString(2).Length > 1)
        {
            ImGui.SetCursorPos(ImGui.GetCursorPos() + ImGui.GetContentRegionAvail() - ImGui.CalcTextSize($"v{P.Version.ToString(2)}"));
            ImGui.TextUnformatted($"v{P.Version.ToString(2)}");
        }
    }

    private static string LocalizeTweakName(Tweak tweak) => tweak.InternalName switch
    {
        "AchievementTracker" => "成就追蹤器",
        "AddressBookDebug" => "傳送收藏地點",
        "AntiAFK" => "防止閒置踢出",
        "ARCeruleum" => "AutoRetainer × 青燐水",
        "ARQuesting" => "AutoRetainer × Questionable",
        "ARSwitcher" => "AutoRetainer 角色切換",
        "AutoEquipXPBoosts" => "自動裝備經驗值加成耳飾",
        "AutoFollow" => "自動跟隨",
        "AutoInvite" => "自動邀請",
        "AutoMerge" => "自動合併物品",
        "AutoPillion" => "自動搭乘坐騎",
        "AutoQueue" => "自動排本",
        "AutoSnipeQuests" => "自動完成狙擊任務",
        "ClickToMove" => "點擊移動",
        "Commands" => "指令",
        "DateWithDestiny" => "危命任務助手",
        "EnhancedDutyStartEnd" => "副本開始／結束增強",
        "EnhancedLoginLogout" => "登入增強",
        "GettingTooAttached" => "魔晶石鑲嵌成就助手",
        "GlamourSets" => "投影套裝追蹤器",
        "GMAlert" => "GM 警報",
        "HuntRelayHelper" => "狩獵訊息轉發助手",
        "InstantLogout" => "立即登出",
        "InstantReturn" => "快速返回",
        "LalaLookup" => "Lalachievements 查詢",
        "MaxGCRank" => "強制顯示稀有品籌備",
        "NoFailFollowQuests" => "跟隨任務防失敗",
        "RetrieveMateria" => "取出所有魔晶石",
        "SimpleCurrencyAlert" => "簡易貨幣提醒",
        "TimezoneTranslator" => "時區轉換",
        "Tools" => "工具",
        "WondrousTailsClickToOpen" => "點擊開啟天書奇談任務",
        _ => tweak.Name,
    };

    private static string LocalizeTweakDescription(Tweak tweak) => tweak.InternalName switch
    {
        "AchievementTracker" => "新增成就追蹤視窗。",
        "AddressBookDebug" => "傳送至已收藏的地點。",
        "AntiAFK" => "防止因閒置過久而被踢出遊戲。",
        "ARCeruleum" => "AutoRetainer 完成角色處理後，自動補充青燐水缸；持有量低於 200 時觸發。",
        "ARQuesting" => "AutoRetainer 完成角色處理後，自動進行可用的季節活動任務。",
        "ARSwitcher" => "新增伺服器資訊列項目與指令，用於切換 AutoRetainer 的前一個／下一個角色。",
        "AutoEquipXPBoosts" => "等級同步至 90 級以下時，自動裝備黃金篇經驗值加成耳飾。",
        "AutoInvite" => "依聊天訊息自動邀請玩家加入小隊。",
        "AutoMerge" => "開啟物品欄時，自動合併未滿的物品堆疊。",
        "AutoPillion" => "靠近其他玩家時，自動搭乘對方的多人坐騎。",
        "AutoQueue" => "區域切換後，自動申請已預先勾選的副本。",
        "AutoSnipeQuests" => "自動完成狙擊類任務。",
        "ClickToMove" => "提供點擊地面移動功能。",
        "Commands" => "提供多種實用指令。",
        "DateWithDestiny" => "追蹤危命任務並協助移動；不處理戰鬥。",
        "EnhancedDutyStartEnd" => "副本開始或結束時，自動執行指定操作。",
        "EnhancedLoginLogout" => "提供登入時的額外選項。",
        "GettingTooAttached" => "重複鑲嵌與取出魔晶石，以協助完成相關成就。",
        "GlamourSets" => "追蹤投影套裝收集狀態。",
        "GMAlert" => "偵測附近 GM 並提供多種警示。",
        "HuntRelayHelper" => "在含地圖連結的訊息後加入可點擊圖示，以轉發至其他頻道。",
        "InstantLogout" => "在非休息區登出時略過 20 秒倒數。",
        "InstantReturn" => "直接呼叫返回功能。",
        "LalaLookup" => "在選單新增前往 Lalachievements 查詢角色的項目。",
        "MaxGCRank" => "尚未解鎖稀有品籌備時，強制顯示對應視窗；不會繞過其他軍階限制。",
        "NoFailFollowQuests" => "防止跟隨任務中被發現；距離過遠仍可能失敗。",
        "RetrieveMateria" => "在物品選單新增取出全部魔晶石的項目。",
        "SimpleCurrencyAlert" => "切換區域時檢查並提醒貨幣數量。",
        "TimezoneTranslator" => "將聊天中的系統訊息時間轉換為你的時區。",
        "Tools" => "多種實用工具。",
        "WondrousTailsClickToOpen" => "點擊天書奇談項目即可開啟對應任務搜尋器；Ctrl＋點擊可半自動申請。",
        _ => tweak.Description,
    };
}
