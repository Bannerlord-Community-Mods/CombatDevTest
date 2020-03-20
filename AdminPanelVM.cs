using System.Collections.Generic;
using TaleWorlds.Engine.Screens;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;
using TaleWorlds.MountAndBlade.ViewModelCollection.Multiplayer.Lobby.HostGame;

namespace CombatDevTest
{
    public class AdminPanelVM : ViewModel
    {
        private MPHostGameOptionsVM _hostGameOptions;
        private string _createText;

        public AdminPanelVM()
        {
            this.HostGameOptions = new MPHostGameOptionsVM(true);
            this.RefreshValues();
        }

        public override void RefreshValues()
        {
            base.RefreshValues();
            this.CreateText = new TextObject("{=aRzlp5XH}Dont press", (Dictionary<string, TextObject>) null).ToString();
            this.HostGameOptions.RefreshValues();
        }

        public void ExecuteStart()
        {
            var admin = Mission.Current.GetMissionBehaviour<MultiplayerAdminComponent>();
admin.OnApplySettings();
        }
        [CommandLineFunctionality.CommandLineArgumentFunction("admnin", "mp_host")]
        public static string MPHostHelp(List<string> strings)
        {
          
            if (!GameNetwork.IsServerOrRecorder)
            {
                return "Failed: Only the host can use mp_host commands.";
            }
            
            var admin = Mission.Current.GetMissionBehaviour<MultiplayerAdminComponent>();
            admin.ShowAdminMenu();
            return "" + "mp_host.restart_game : Restarts the game.\n" + "mp_host.kick_player : Kicks the given player.\n";
        }

        [DataSourceProperty]
        public MPHostGameOptionsVM HostGameOptions
        {
            get
            {
                return this._hostGameOptions;
            }
            set
            {
                if (value == this._hostGameOptions)
                    return;
                this._hostGameOptions = value;
                this.OnPropertyChanged(nameof (HostGameOptions));
            }
        }

        [DataSourceProperty]
        public string CreateText
        {
            get
            {
                return this._createText;
            }
            set
            {
                if (!(value != this._createText))
                    return;
                this._createText = value;
                this.OnPropertyChanged(nameof (CreateText));
            }
        }
    }


}