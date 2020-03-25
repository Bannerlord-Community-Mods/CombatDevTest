using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using NetworkMessages.FromServer;
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
using TaleWorlds.MountAndBlade.View.Screen;
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



            if (NetworkMain.GameClient.IsInGame && (NetworkMain.GameClient.IsHostingCustomGame || Input.IsKeyDown(InputKey.F12)) &&
                Mission.Current != null && Mission.Current.IsLoadingFinished &&
                !Mission.Current.HasMissionBehaviour<CombatTestMissionController>())
            {
                Mission.Current.AddMissionBehaviour(new CombatTestMissionController());
            }
        
        }
    }

    internal class CombatTestMissionController : MissionView    
    {
        private static List<string> consoleCommands;

        private bool _allInvulnerable = false;
        private bool enableAIChanges = false;
        private bool everyonePassive = false;

        private bool playerInvulnerable = false;
        private AgentDrivenProperties StatsBackup = new AgentDrivenProperties();
        private AgentDrivenProperties StatsToSet = new AgentDrivenProperties();
        private bool wasEnableAIChanges = false;

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
            }

            ;
            DrawDevCheats();
            Imgui.End();
            /*Imgui.Begin(("Console"));
            DrawConsole();
            Imgui.End();*/
            Imgui.EndMainThreadScope();
        }

      

      

        private void DrawDevCheats()
        {
            wasEnableAIChanges = enableAIChanges;
            Imgui.Checkbox("Player Invulnerable", ref playerInvulnerable);

            var player = Mission.Current.MainAgent;
            Imgui.Checkbox($"Everyone Invulnerable?: {_allInvulnerable}", ref _allInvulnerable);

            Imgui.Checkbox($"Everyone Passive?: {everyonePassive}", ref everyonePassive);

            Imgui.Checkbox("Enable AI Changes", ref enableAIChanges);


            foreach (var agent in Mission.Current.AllAgents)
            {
                if (agent == null)
                {
                    continue;
                }
                agent?.SetInvulnerable(_allInvulnerable);
                
                var component = agent?.GetComponent<AgentAIStateFlagComponent>();
                if (component != null) component.IsPaused = everyonePassive;
                if (agent == player)
                {
                    continue;
                }
            }

            if (enableAIChanges)
            {
                if (wasEnableAIChanges)
                {
                    SliderUpdate();
                    AskForApply();
                }
                else
                {
                    BackupStats();
                }
            }
            else if (wasEnableAIChanges)
            {
                ResetStats();
            }


            if (Imgui.Button(" Gib Player 100 Money"))
            {
                var _gameModeServer = Mission.Current.GetMissionBehaviour<MissionMultiplayerGameModeBase>();
                _gameModeServer.ChangeCurrentGoldForPeer(GameNetwork.MyPeer.GetComponent<MissionPeer>(),
                    _gameModeServer.GetCurrentGoldForPeer(GameNetwork.MyPeer.GetComponent<MissionPeer>()) + 100);
            }

            player?.SetInvulnerable(playerInvulnerable);
        }

        private void ResetStats()
        {
            var player = Mission.Current.MainAgent;
            foreach (var agent in Mission.Current.AllAgents)
            {
                if (agent == null || agent == player)
                {
                    continue;
                }

                var component = agent?.GetComponent<AgentAIStateFlagComponent>();
                if (component != null) component.IsPaused = everyonePassive;
                var agentDrivenProperties = (AgentDrivenProperties) agent.GetType()
                    .GetProperty("AgentDrivenProperties", BindingFlags.NonPublic | BindingFlags.Instance)
                    ?.GetValue(agent);


                foreach (DrivenProperty drivenProperty in (DrivenProperty[]) Enum.GetValues(typeof(DrivenProperty)))
                {
                    if (drivenProperty >= DrivenProperty.MountManeuver ||
                        drivenProperty <= DrivenProperty.None) continue;
                    var val = StatsBackup.GetStat(drivenProperty);

                    agentDrivenProperties?.SetStat(drivenProperty, val);
                }

                agent.UpdateAgentProperties();
            }
        }

        private void BackupStats()
        {
            var player = Mission.Current.MainAgent;
            foreach (var agent in Mission.Current.AllAgents)
            {
                if (agent == null || agent == player)
                {
                    continue;
                }

                var component = agent?.GetComponent<AgentAIStateFlagComponent>();
                if (component != null) component.IsPaused = everyonePassive;
                var agentDrivenProperties = (AgentDrivenProperties) agent.GetType()
                    .GetProperty("AgentDrivenProperties", BindingFlags.NonPublic | BindingFlags.Instance)
                    ?.GetValue(agent);

                if (agentDrivenProperties == null) continue;
                foreach (DrivenProperty drivenProperty in (DrivenProperty[]) Enum.GetValues(
                    typeof(DrivenProperty)))
                {
                    if (drivenProperty >= DrivenProperty.MountManeuver ||
                        drivenProperty <= DrivenProperty.None) continue;
                    var val = agentDrivenProperties.GetStat(drivenProperty);
                    StatsBackup.SetStat(drivenProperty, val);
                    StatsToSet.SetStat(drivenProperty, val);
                }
            }
        }

        private void AskForApply()
        {
            var player = Mission.Current.MainAgent;
            if (Imgui.Button("UPDATE AI"))
            {
                foreach (var agent in Mission.Current.AllAgents)
                {
                    if (agent == null || agent == player)
                    {
                        continue;
                    }

                    var component = agent?.GetComponent<AgentAIStateFlagComponent>();
                    if (component != null) component.IsPaused = everyonePassive;
                    var agentDrivenProperties = (AgentDrivenProperties) agent.GetType()
                        .GetProperty("AgentDrivenProperties", BindingFlags.NonPublic | BindingFlags.Instance)
                        ?.GetValue(agent);

                    foreach (DrivenProperty drivenProperty in (DrivenProperty[]) Enum.GetValues(typeof(DrivenProperty)))
                    {
                        if (drivenProperty < DrivenProperty.MountManeuver && drivenProperty > DrivenProperty.None)
                        {
                            float val = StatsToSet.GetStat(drivenProperty);

                            agentDrivenProperties?.SetStat(drivenProperty, val);
                        }

                        agent.UpdateAgentProperties();
                    }
                }
            }
        }

        private void SliderUpdate()
        {
            foreach (DrivenProperty drivenProperty in (DrivenProperty[]) Enum.GetValues(typeof(DrivenProperty)))
            {
                if (drivenProperty < DrivenProperty.MountManeuver && drivenProperty > DrivenProperty.None)
                {
                    float val = StatsToSet.GetStat(drivenProperty);

                    Imgui.SliderFloat(Enum.GetName(typeof(DrivenProperty), drivenProperty), ref val, -10, 10);
                    StatsToSet.SetStat(drivenProperty, val);
                }
            }
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