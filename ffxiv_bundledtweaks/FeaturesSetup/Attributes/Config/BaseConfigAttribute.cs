using Dalamud.Interface;
using ECommons.ImGuiMethods;
using Dalamud.Bindings.ImGui;
using System.Reflection;

namespace ComplexTweaks.FeaturesSetup.Attributes;

public abstract class BaseConfigAttribute : Attribute
{
    public string Label = string.Empty;
    public string Description = string.Empty;
    public string DependsOn = string.Empty;

    public abstract void Draw(Tweak tweak, object config, FieldInfo fieldInfo);

    protected void OnChangeInternal(Tweak tweak, FieldInfo fieldInfo)
    {
        //C.SaveConfiguration($"ez{tweak.Name}.json");
        tweak.CachedType.GetMethod(nameof(Tweak.OnConfigChangeInternal), BindingFlags.Instance | BindingFlags.NonPublic)?.Invoke(tweak, [fieldInfo.Name]);
    }

    protected static void DrawConfigInfos(FieldInfo fieldInfo)
    {
        var attributes = fieldInfo.GetCustomAttributes<ConfigInfoAttribute>();
        if (!attributes.Any())
            return;

        foreach (var attribute in attributes)
        {
            ImGui.SameLine();
            ImGuiX.Icon(attribute.Icon, attribute.Color);
            if (ImGui.IsItemHovered())
                ImGui.SetTooltip(attribute.Description);
        }
    }

    protected static bool DrawResetButton(string defaultValueString)
    {
        if (string.IsNullOrEmpty(defaultValueString))
            return false;

        ImGui.SameLine();
        return ImGuiEx.IconButton(FontAwesomeIcon.Undo, "##Reset");
    }

    protected static string LocalizeFieldLabel(FieldInfo fieldInfo) => fieldInfo.Name switch
    {
        "UpdateFrequency" => "更新頻率",
        "AutoRemoveCompleted" => "自動移除已完成項目",
        "ReturnHome" => "完成後返回所屬世界",
        "DistanceToKeep" => "保持距離",
        "DisableIfFurtherThan" => "超過此距離時停用",
        "OnlyInDuty" => "僅限副本內",
        "MountAndFly" => "騎乘並飛行",
        "ExcludeCombat" => "戰鬥中停用",
        "AutoFollowName" => "自動跟隨對象名稱",
        "Pattern" => "觸發文字",
        "IsRegex" => "使用正規表示式",
        "TurnOffOnceFull" => "小隊滿員後停用",
        "DelayMs" => "延遲（毫秒）",
        "YokaiMode" => "妖怪手錶模式",
        "StayInMeleeRange" => "保持近戰距離",
        "PrioritizeForlorns" => "優先鎖定迷失者",
        "PrioritizeBonusFates" => "優先有經驗值加成的危命任務",
        "PrioritizeStartedFates" => "優先已有進度的危命任務",
        "BonusWhenTwist" => "僅在命運之扭轉狀態下優先",
        "EquipWatch" => "裝備妖怪手錶",
        "SwapMinions" => "自動更換寵物",
        "SwapZones" => "自動切換區域",
        "FullAuto" => "全自動模式",
        "AutoMount" => "自動騎乘",
        "AutoFly" => "自動飛行",
        "PathToFate" => "前往下一個危命任務",
        "AutoSync" => "自動等級同步",
        "AutoTarget" => "自動鎖定敵人",
        "AutoMoveToMobs" => "自動移動至敵人",
        "MaxDuration" => "最長持續時間",
        "MinTimeRemaining" => "最少剩餘時間",
        "MaxProgress" => "最高進度",
        "ShowFateTimeRemaining" => "顯示危命任務剩餘時間",
        "ShowFateBonusIndicator" => "顯示危命任務加成標記",
        "CheckForAll" => "確認所有指定玩家",
        "CheckForAny" => "確認任一指定玩家",
        "AutoLeaveOnEnd" => "結束時自動離開",
        "TimeToWait" => "等待時間",
        "ShowOnlyMissing" => "僅顯示未取得項目",
        "Toast" => "顯示彈出通知",
        "ChatMessage" => "顯示聊天通知",
        "Sound" => "播放音效通知",
        "BeepCount" => "提示音次數",
        "BeepDuration" => "提示音長度",
        "BeepFrequency" => "提示音頻率",
        "KillGame" => "關閉遊戲",
        "OnlySendLocalHuntsToLocalChannels" => "僅將本地狩獵轉發至本地頻道",
        "AssumeBlankWorldsAreLocal" => "將未標示世界視為本地",
        "DontRepeatRelays" => "不要重複轉發",
        "AllowPartialWorldMatches" => "允許部分比對世界名稱",
        "RemoveWorldFromNNCallouts" => "轉發至新人頻道時移除世界標籤",
        "DryRun" => "測試模式",
        "ChatMessagePattern" => "聊天訊息格式",
        _ => fieldInfo.Name.SplitWords(),
    };
}
