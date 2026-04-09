using Godot;

namespace Kuros.Actors.Heroes
{
    /// <summary>
    /// 单个构筑层级效果配置条目，可用于数据驱动多构筑。
    /// </summary>
    [GlobalClass]
    public partial class BuildLevelEffectEntry : Resource
    {
        [Export] public string BuildId { get; set; } = string.Empty;
        [Export] public string BuildClass { get; set; } = string.Empty;
        [Export] public string BuildName { get; set; } = string.Empty;
        [Export(PropertyHint.Range, "1,99,1")] public int Level { get; set; } = 1;
        [Export(PropertyHint.Range, "0,999,1")] public int RequiredPoints { get; set; } = 1;

        [Export] public string EffectId { get; set; } = string.Empty;
        [Export] public string EffectScript { get; set; } = string.Empty;
        [Export(PropertyHint.MultilineText)] public string Description { get; set; } = string.Empty;
        [Export(PropertyHint.File, "*.svg,*.png,*.webp,*.jpg,*.jpeg")] public string IconPath { get; set; } = string.Empty;

        [Export] public PackedScene? EffectScene { get; set; }
    }
}
