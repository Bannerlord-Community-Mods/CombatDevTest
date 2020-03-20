using System;
using System.Runtime.InteropServices;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
// using TaleWorlds.MountAndBlade.GauntletUI;
// using TaleWorlds.MountAndBlade.LegacyGUI.Missions;

namespace CombatDevTest
{
    

    public class CombatDevTestSubModule : MBSubModuleBase
    {

        [DllImport("Rgl.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "?toggle_imgui_console_visibility@rglCommand_line_manager@@QEAAXXZ")]
        public static extern void toggle_imgui_console_visibility(UIntPtr x);

        public override void OnMultiplayerGameStart(Game game, object starterObject)
        {
            base.OnMultiplayerGameStart(game, starterObject);
            game.AddGameHandler<OfflineMultiplayerGameHandler>();
        }

        protected override void OnSubModuleLoad()
        {
            toggle_imgui_console_visibility(UIntPtr.Zero);
            base.OnSubModuleLoad();
            
        }

        protected override void OnSubModuleUnloaded()
        {
            base.OnSubModuleUnloaded();
        }

        protected override void OnBeforeInitialModuleScreenSetAsRoot()
        {

        }

        protected override void OnApplicationTick(float dt)
        {
            // ModuleLogger.Writer.WriteLine("CaptureTheBannerLordSubModule::OnApplicationTick {0}", dt);
            base.OnApplicationTick(dt);
        }
    }
}