using System.Threading.Tasks;
using TaleWorlds.Engine.Screens;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Diamond;
using TaleWorlds.MountAndBlade.ViewModelCollection.Multiplayer.Lobby.HostGame;

namespace CombatDevTest
{
    public class SPHostGameVM : MPHostGameVM
    {
        public SPHostGameVM(LobbyState lobbyState) : base(lobbyState)
        {
        }

        protected async void HandleServerStart()
        {
            ScreenManager.PopScreen();
            MBCommon.CurrentGameType = MBCommon.GameType.MultiClientServer;
            
            MBMultiplayerOptionsAccessor.InitializeAllOptionsFromCurrent();
            GameNetwork.PreStartMultiplayerOnServer();
            BannerlordNetwork.StartMultiplayerLobbyMission(LobbyMissionType.Custom);
            if (!Module.CurrentModule.StartMultiplayerGame(MBMultiplayerOptionsAccessor.GetGameType(),
                MBMultiplayerOptionsAccessor.GetMap()))
            {
            }    

            while (Mission.Current == null || Mission.Current.CurrentState != Mission.State.Continuing)
            {
                await Task.Delay(1);
            }

            GameNetwork.StartMultiplayerOnServer(9999);
            NetworkMain.GameClient.GetType().GetProperty("CurrentState")
                .SetValue(NetworkMain.GameClient, LobbyClient.State.HostingCustomGame);
            if (NetworkMain.GameClient.IsInGame)
            {
                BannerlordNetwork.CreateServerPeer();
                if (!GameNetwork.IsDedicatedServer)
                {
                    
                    GameNetwork.ClientFinishedLoading(GameNetwork.MyPeer);
                }
            }
        }

        public new void ExecuteStart()
        {
            HandleServerStart();
        }
    }
}