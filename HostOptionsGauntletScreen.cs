using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.Engine.Screens;
using TaleWorlds.GauntletUI.Data;
using TaleWorlds.InputSystem;
using TaleWorlds.MountAndBlade.View.Missions;

namespace CombatDevTest
{
    [OverrideView(typeof(HostOptionsScreen))]
    public class HostOptionsGauntletScreen : ScreenBase
    {
        private GauntletLayer _gauntletLayer;

        private AdminPanelVM _dataSource;

        private GauntletMovie _gauntletMovie;


        public HostOptionsGauntletScreen(AdminPanelVM dataSource)
        {
            _dataSource = dataSource;
        }

        protected override void OnInitialize()
        {
            base.OnInitialize();

            this._gauntletLayer = new GauntletLayer(4000, "GauntletLayer");
            this._gauntletLayer.Input.RegisterHotKeyCategory(HotKeyManager.GetCategory("GenericPanelGameKeyCategory"));
            this._gauntletLayer.IsFocusLayer = true;
            base.AddLayer(this._gauntletLayer);
            this._gauntletLayer.InputRestrictions.SetInputRestrictions(true, TaleWorlds.Library.InputUsageMask.All);
            this._gauntletMovie = this._gauntletLayer.LoadMovie("CustomHostGame", this._dataSource);
            
            
        }

        protected override void OnActivate()
        {
            base.OnActivate();
            ScreenManager.TrySetFocus(_gauntletLayer);
        }

        protected override void OnFinalize()
        {
            _gauntletLayer.IsFocusLayer = false;
            ScreenManager.TryLoseFocus(_gauntletLayer);
            RemoveLayer(_gauntletLayer);
            _dataSource = null;
            _gauntletLayer = null;
        }

        protected override void OnDeactivate()
        {
        }

        protected override void OnFrameTick(float dt)
        {
            base.OnFrameTick(dt);
            if (this._gauntletLayer.Input.IsHotKeyReleased("Exit"))
            {
                ScreenManager.TrySetFocus(this._gauntletLayer);

                ScreenManager.PopScreen();
            }
        }
    }
}