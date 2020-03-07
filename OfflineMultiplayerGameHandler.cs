using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection;
using TaleWorlds.Engine;
using TaleWorlds.Engine.Screens;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Diamond;
using TaleWorlds.MountAndBlade.GauntletUI;
using TaleWorlds.MountAndBlade.Test;
using TaleWorlds.MountAndBlade.View.Missions;
using Debug = System.Diagnostics.Debug;
using Module = TaleWorlds.MountAndBlade.Module;

namespace CombatDevTest
{
    class OfflineMultiplayerGameHandler : GameHandler
    {
        public override void OnBeforeSave()
        {
        }

        public override void OnAfterSave()
        {
        }


        protected override void OnTick()
        {
            base.OnTick();



            if (NetworkMain.GameClient.AtLobby && NetworkMain.GameClient.LoggedIn && Input.IsKeyDown(InputKey.F5))
            {
                var x = ScreenManager.TopScreen;
                if (ScreenManager.TopScreen is MultiplayerLobbyGauntletScreen)
                {
                    MultiplayerLobbyGauntletScreen lobby = ScreenManager.TopScreen as MultiplayerLobbyGauntletScreen;
                    var lobbystate = (LobbyState) lobby.GetType()
                        .GetField("_lobbyState", BindingFlags.NonPublic | BindingFlags.Instance)
                        .GetValue(lobby);
                    var sView = ViewCreatorManager.CreateScreenView<HostOptionsScreen>(new SPHostGameVM(lobbystate));
                    ScreenManager.PushScreen(sView);
                }
            }

            if (NetworkMain.GameClient.IsInGame && NetworkMain.GameClient.IsHostingCustomGame &&
                Mission.Current != null && Mission.Current.IsLoadingFinished &&
                !Mission.Current.HasMissionBehaviour<CombatTestMissionController>())
            {
                Mission.Current.AddMissionBehaviour(new CombatTestMissionController());
            }
        }
    }

    internal class CombatTestMissionController : MissionView
    {
        public override void OnAfterMissionCreated()
        {
            base.OnAfterMissionCreated();
        }


        static void DisplayMessage(string msg)
        {
            InformationManager.DisplayMessage(
                new InformationMessage(new TaleWorlds.Localization.TextObject(msg, null).ToString()));
        }

        public override void OnMissionTick(float dt)
        {
            
            base.OnMissionTick(dt);
            RefreshGUI(dt);
        }

        private void RefreshGUI(float dt)
        {
            Imgui.BeginMainThreadScope();
            Imgui.Begin("Combat Dev Feedback Reload");
            if (Imgui.Button("Reload Managed Core Params"))
            {
                ManagedParameters.Instance.Initialize(ModuleInfo.GetXmlPath("CombatDevTest",
                    "managed_core_parameters"));

                DisplayMessage("Reloaded managed Core Params");
            }
            DrawReloadXMLs();
            Imgui.End();
            Imgui.Begin("Combat Dev Feedback Cheats");
            if (Imgui.Button(" Quit"))
            {
                
                GameStateManager gameStateManager = Game.Current.GameStateManager;
                if (!(gameStateManager.ActiveState is LobbyState))
                {
                    if (gameStateManager.ActiveState is MissionState)
                    {
                        Imgui.End();
                        Imgui.EndMainThreadScope();
                        NetworkMain.GameClient.Logout();
                       
                        return;
                    }
                    gameStateManager.PopState(0);
                }
                
            };
            DrawDevCheats();    
            Imgui.End();
            /*Imgui.Begin(("Console"));
            DrawConsole();
            Imgui.End();*/
            Imgui.EndMainThreadScope();
        }
        private static bool CheckAssemblyReferencesThis(Assembly assembly)
        {
            Assembly assembly1 = typeof (CommandLineFunctionality).Assembly;
            if (assembly1.GetName().Name == assembly.GetName().Name)
                return true;
            foreach (AssemblyName referencedAssembly in assembly.GetReferencedAssemblies())
            {
                if (referencedAssembly.Name == assembly1.GetName().Name)
                    return true;
            }
            return false;
        }

        private static List<string> consoleCommands;
        private void DrawConsole()
        {
            if (consoleCommands == null)
            {
                consoleCommands = new List<string>();
                foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
                {
                    if (CheckAssemblyReferencesThis(assembly))
                    {
                        foreach (Type type in assembly.GetTypes())
                        {
                            foreach (MethodInfo method in type.GetMethods(
                                BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
                            {
                                object[] customAttributes =
                                    method.GetCustomAttributes(typeof(CommandLineFunctionality.CommandLineArgumentFunction),
                                        false);
                                if (customAttributes != null && customAttributes.Length != 0 &&
                                    (customAttributes[0] is CommandLineFunctionality.CommandLineArgumentFunction
                                        argumentFunction && !(method.ReturnType != typeof(string))))
                                {
                                    string name = argumentFunction.Name;
                                    string key = argumentFunction.GroupName + "." + name;
                                   
                                }
                            }
                        }
                    }
                }
                consoleCommands.Add("game.reload_native_params");
            }
            else
            {
                consoleCommands.ForEach(command =>
                {
                    if (Imgui.Button(command))
                    {
                        DisplayMessage(CommandLineFunctionality.CallFunction(command, ""));
                    }
                });
                
            }
            

        }

        private bool playerInvulnerable = false;

        private bool _allInvulnerable = false;
        private bool everyonePassive = false;
        private float agentSkill = 0f;
        private AgentDrivenProperties StatsToSet = new AgentDrivenProperties();
        private bool enableAIChanges = false;
        private void DrawDevCheats()
        {
            Imgui.Checkbox("Player Invulnerable", ref playerInvulnerable);

            var player = Mission.Current.MainAgent;
            Imgui.Checkbox($"Everyone Invulnerable?: {_allInvulnerable}", ref _allInvulnerable);
            
            Imgui.Checkbox($"Everyone Passive?: {everyonePassive}", ref everyonePassive);
            Imgui.SliderFloat("AI SKill", ref agentSkill, 0, 1);
            foreach (DrivenProperty drivenProperty in (DrivenProperty[]) Enum.GetValues(typeof(DrivenProperty)))
            {
                if (drivenProperty < DrivenProperty.MountManeuver &&  drivenProperty > DrivenProperty.None )
                {
                    float val = StatsToSet.GetStat(drivenProperty);
                        
                    Imgui.SliderFloat(Enum.GetName(typeof(DrivenProperty), drivenProperty), ref val, 0, 1);
                    StatsToSet.SetStat(drivenProperty,val);
                }
            }
            foreach (var agent in Mission.Current.AllAgents)
            {
                if (agent == null)
                {
                    continue;
                };
                agent?.SetInvulnerable(_allInvulnerable);
                if (agent == player)
                {
                    continue;
                }
                var component = agent?.GetComponent<AgentAIStateFlagComponent>();
                if (component != null) component.IsPaused = everyonePassive;
                if (enableAIChanges)
                {
                    var agentDrivenProperties = (AgentDrivenProperties) agent.GetType()
                        .GetProperty("AgentDrivenProperties", BindingFlags.NonPublic | BindingFlags.Instance)
                        ?.GetValue(agent);
                

                
                    foreach (DrivenProperty drivenProperty in (DrivenProperty[]) Enum.GetValues(typeof(DrivenProperty)))
                    {
                        if (drivenProperty < DrivenProperty.MountManeuver &&  drivenProperty > DrivenProperty.None )
                        {
                            float val = StatsToSet.GetStat(drivenProperty);
                        
                            agentDrivenProperties?.SetStat(drivenProperty,val);
                        }
                    }
                    
                }
               
                agent.UpdateAgentProperties();
            }

          
            if (Imgui.Checkbox("Enable AI CHanges", ref enableAIChanges))
            {
            }
            if (Imgui.Button(" Gib Player 100 Money"))
            {
                
                    var _gameModeServer = Mission.Current.GetMissionBehaviour<MissionMultiplayerGameModeBase>();
                    _gameModeServer.ChangeCurrentGoldForPeer(GameNetwork.MyPeer.GetComponent<MissionPeer>(),_gameModeServer.GetCurrentGoldForPeer(GameNetwork.MyPeer.GetComponent<MissionPeer>())+100);
                
            };
            
           
            player?.SetInvulnerable(playerInvulnerable);
            
        }


        private void DrawReloadXMLs()
        {
            foreach (var xml in new[]
            {
                "BasicCultures", "MPCharacters", "MPClassDivisions", "Monsters", "SkeletonScales", "ItemModifiers",
                "ItemModifierGroups", "CraftingPieces", "CraftingTemplates", "Items"
            })
            {
                drawReloadXML(xml);
                Imgui.Separator();
            }
        }

        private void drawReloadXML(string xmlFile)
        {
            if (Imgui.Button($"Reload {xmlFile}"))
            {
                MBObjectManager.Instance.LoadXML(xmlFile, null);
            }
        }
    }
}