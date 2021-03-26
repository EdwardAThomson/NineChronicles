using Nekoyume.EnumType;
using mixpanel;

namespace Nekoyume.UI
{
    public class SystemPopup : Alert
    {
        public override WidgetType WidgetType => WidgetType.SystemInfo;

        public override void Show(string title, string content, string labelOK = "UI_OK", bool localize = true)
        {
            Mixpanel.Track("Unity/SystemPopupImpression");
            if (Game.Game.instance.Stage.IsInStage)
            {
                var props = new Value
                {
                    ["StageId"] = Game.Game.instance.Stage.stageId,
                };
                Mixpanel.Track("Unity/Stage Exit Crash", props);
            }

            base.Show(title, content, labelOK, localize);
        }

        public void ShowAndQuit(string title, string content, string labelOK = "UI_OK", bool localize = true)
        {
#if UNITY_EDITOR
            CloseCallback = UnityEditor.EditorApplication.ExitPlaymode;
#else
            CloseCallback = UnityEngine.Application.Quit;
#endif
            Show(title, content, labelOK, localize);
        }
    }
}
