/// <summary>
/// 主场景顶部资源条 Panel：继承 <see cref="TopResourceBarPanel"/>，展示水晶、金币、广告券与体力。
/// </summary>
/// <remarks>
/// <para><b>挂载区域：</b>MainScene 战斗页 SafeAreaRoot 顶部资源区。</para>
/// <para><b>职责：</b>调用基类 <see cref="TopResourceBarPanel.Refresh"/> 刷新数值；加号点击由 <see cref="MainSceneBattlePageView"/> 处理。</para>
/// </remarks>
public class MainSceneResourcePanel : TopResourceBarPanel
{
}
