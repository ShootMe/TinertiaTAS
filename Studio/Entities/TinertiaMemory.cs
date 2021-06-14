using System;
using System.Diagnostics;
namespace TinertiaStudio.Entities {
    public class TinertiaMemory {
        private static ProgramPointer CgGame = new ProgramPointer(new FindPointerSignature(PointerVersion.All, AutoDeref.Single, "558BEC83EC288B05????????83EC086A0050E8????????83C41085C0741D8B05", 8));
        private static ProgramPointer Tinertia = new ProgramPointer(new FindPointerSignature(PointerVersion.All, AutoDeref.Single, "68????????57E8????????83C41085FF0F84????????83EC0C68????????E8????????83C410897810C74014????????C7401C????????C7400C????????83EC0C50E8????????83C41083EC0C6AFF", 1));
        private static ProgramPointer TAS = new ProgramPointer(new FindPointerSignature(PointerVersion.All, AutoDeref.Single, "83C4108B4DFCB8????????890883EC0C6A00E8????????83C4108BC8B8", 7));
        public Process Program { get; set; }
        public bool IsHooked { get; set; } = false;
        private DateTime lastHooked;

        public TinertiaMemory() {
            lastHooked = DateTime.MinValue;
        }

        public WorldId World() {
            CgLevelId id = Level();
            switch (id) {
                case CgLevelId.StartScreen:
                case CgLevelId.MainMenu:
                    return WorldId.Menu;
                case CgLevelId.Core_01:
                case CgLevelId.Core_02:
                case CgLevelId.Core_03:
                case CgLevelId.Core_04:
                case CgLevelId.Core_05:
                case CgLevelId.Core_06:
                case CgLevelId.Core_07:
                case CgLevelId.Core_08:
                case CgLevelId.Core_09:
                case CgLevelId.Core_Boss:
                    return WorldId.Core;
                case CgLevelId.Mines_01:
                case CgLevelId.Mines_02:
                case CgLevelId.Mines_03:
                case CgLevelId.Mines_04:
                case CgLevelId.Mines_05:
                case CgLevelId.Mines_06:
                case CgLevelId.Mines_07:
                case CgLevelId.Mines_08:
                case CgLevelId.Mines_09:
                case CgLevelId.Mines_Boss:
                    return WorldId.Mines;
                case CgLevelId.Sewers_01:
                case CgLevelId.Sewers_02:
                case CgLevelId.Sewers_03:
                case CgLevelId.Sewers_04:
                case CgLevelId.Sewers_05:
                case CgLevelId.Sewers_06:
                case CgLevelId.Sewers_07:
                case CgLevelId.Sewers_08:
                case CgLevelId.Sewers_09:
                case CgLevelId.Sewers_Boss:
                    return WorldId.Sewers;
                case CgLevelId.Slums_01:
                case CgLevelId.Slums_02:
                case CgLevelId.Slums_03:
                case CgLevelId.Slums_04:
                case CgLevelId.Slums_05:
                case CgLevelId.Slums_06:
                case CgLevelId.Slums_07:
                case CgLevelId.Slums_08:
                case CgLevelId.Slums_09:
                case CgLevelId.Slums_Boss:
                    return WorldId.Slums;
                case CgLevelId.Factory_01:
                case CgLevelId.Factory_02:
                case CgLevelId.Factory_03:
                case CgLevelId.Factory_04:
                case CgLevelId.Factory_05:
                case CgLevelId.Factory_06:
                case CgLevelId.Factory_07:
                case CgLevelId.Factory_08:
                case CgLevelId.Factory_09:
                case CgLevelId.Factory_Boss:
                    return WorldId.Factory;
                case CgLevelId.Buildings_01:
                case CgLevelId.Buildings_02:
                case CgLevelId.Buildings_03:
                case CgLevelId.Buildings_04:
                case CgLevelId.Buildings_05:
                case CgLevelId.Buildings_06:
                case CgLevelId.Buildings_07:
                case CgLevelId.Buildings_08:
                case CgLevelId.Buildings_09:
                case CgLevelId.Buildings_Boss:
                    return WorldId.Buildings;
                case CgLevelId.Command_01:
                case CgLevelId.Command_02:
                case CgLevelId.Command_03:
                case CgLevelId.Command_04:
                case CgLevelId.Command_05:
                case CgLevelId.Command_Boss:
                    return WorldId.Command;
                case CgLevelId.Gauntlet:
                    return WorldId.Guantlet;
            }
            return WorldId.NULL;
        }
        public CgLevelId Level() {
            //Tinertia.instance.currentLevel.isWorld
            bool isWorld = Tinertia.Read<bool>(Program, 0x0, 0x2c, 0xa4);
            //Tinertia.instance.currentLevel.currentSectionIndex
            int currentSection = Tinertia.Read<int>(Program, 0x0, 0x2c, 0x8c);
            if (isWorld) {
                //Tinertia.instance.currentLevel.worldLevels[currentSection].id
                CgLevelId worldid = (CgLevelId)Tinertia.Read<int>(Program, 0x0, 0x2c, 0xc0, 0x10 + (currentSection * 4), 0x6c);
                if (worldid == CgLevelId.NULL) {
                    if (Enum.TryParse<CgLevelId>(Tinertia.Read(Program, 0x0, 0x2c, 0xc8, 0x10 + (currentSection * 4), 0x8), out worldid)) {
                        return worldid;
                    }
                }
                return worldid;
            }
            return (CgLevelId)Tinertia.Read<int>(Program, 0x0, 0x2c, 0xd4);
        }
        public bool IsLoading() {
            //CgGame.instance != 0 && CgGame.instance.sceneLoadingState
            return CgGame.Read<int>(Program, 0x0) != 0 && CgGame.Read<int>(Program, 0x0, 0x9c) != 0;
        }
        public int PendingActions() {
            //CgGame.instance.players[0].actionQueue.Count
            return CgGame.Read<int>(Program, 0x0, 0x48, 0x10, 0x54, 0xc);
        }
        public TinertiaGameMode Mode() {
            //Tinertia.instance.mode
            return (TinertiaGameMode)Tinertia.Read<int>(Program, 0x0, 0xa4);
        }
        public bool IsPaused() {
            //Tinertia.instance.isGamePaused
            return Tinertia.Read<bool>(Program, 0x0, 0x6d);
        }
        public bool SectionFinished() {
            //Tinertia.instance.isSectionComplete
            return Tinertia.Read<bool>(Program, 0x0, 0x2c, 0xa3);
        }
        public bool SceneFinished() {
            //Tinertia.instance.isSceneComplete
            return Tinertia.Read<bool>(Program, 0x0, 0x2c, 0xa2);
        }
        public CgTimerState TimerState() {
            //Tinertia.instance.levelTimer.state
            return (CgTimerState)Tinertia.Read<int>(Program, 0x0, 0x40, 0xc);
        }
        public bool ShowLevelSelect() {
            //Tinertia.instance.showLevelSelect
            return Tinertia.Read<bool>(Program, 0x0, 0x98);
        }
        public string TASStatus() {
            return TAS.Read(Program, 0x2c, 0x0);
        }
        public Vector3 TASPosition() {
            float x = TAS.Read<float>(Program, 0x20);
            float y = TAS.Read<float>(Program, 0x24);
            float z = TAS.Read<float>(Program, 0x28);
            return new Vector3(x, z, y);
        }
        public Vector3 TASVelocity() {
            float x = TAS.Read<float>(Program, 0x14);
            float y = TAS.Read<float>(Program, 0x18);
            float z = TAS.Read<float>(Program, 0x1c);
            return new Vector3(x, z, y);
        }

        public bool HookProcess() {
            if ((Program == null || Program.HasExited) && DateTime.Now > lastHooked.AddSeconds(1)) {
                lastHooked = DateTime.Now;
                Process[] processes = Process.GetProcessesByName("Tinertia-Windows");
                Program = processes.Length == 0 ? null : processes[0];
                IsHooked = true;
            }

            if (Program == null || Program.HasExited) {
                IsHooked = false;
            }

            return IsHooked;
        }
        public void Dispose() {
            if (Program != null) {
                Program.Dispose();
            }
        }
    }
    public enum CgTimerState {
        Stopped,
        Running
    }
    public enum TinertiaGameMode {
        Campaign,
        SpeedRun,
        YoLo,
        Gauntlet
    }
    public enum WorldId {
        NULL,
        Menu,
        Core,
        Mines,
        Sewers,
        Slums,
        Factory,
        Buildings,
        Command,
        Guantlet
    }
    public enum CgLevelId {
        NULL,
        StartScreen = 89473634,
        MainMenu = 84377569,
        Core_01 = 2860319,
        Core_02 = 21930271,
        Core_03 = 88811081,
        Core_04 = 70535902,
        Core_05 = 32950342,
        Core_06 = 510956,
        Core_07 = 62934167,
        Core_08 = 28928813,
        Core_09 = 9673724,
        Core_Boss = 23622748,
        Mines_01 = 37954516,
        Mines_02 = 45676537,
        Mines_03 = 64588823,
        Mines_04 = 73949071,
        Mines_05 = 75501516,
        Mines_06 = 82152109,
        Mines_07 = 94386691,
        Mines_08 = 11377755,
        Mines_09 = 12918559,
        Mines_Boss = 39996502,
        Sewers_01 = 94669146,
        Sewers_02 = 14460096,
        Sewers_03 = 33717318,
        Sewers_04 = 47149626,
        Sewers_05 = 63796241,
        Sewers_06 = 47599339,
        Sewers_07 = 38095357,
        Sewers_08 = 13029211,
        Sewers_09 = 21045811,
        Sewers_Boss = 58393848,
        Slums_01 = 75617109,
        Slums_02 = 67806756,
        Slums_03 = 58127701,
        Slums_04 = 70590868,
        Slums_05 = 48547589,
        Slums_06 = 24438980,
        Slums_07 = 24923819,
        Slums_08 = 64200828,
        Slums_09 = 54328219,
        Slums_Boss = 88334895,
        Factory_01 = 19289283,
        Factory_02 = 82029867,
        Factory_03 = 61625014,
        Factory_04 = 94132227,
        Factory_05 = 78367467,
        Factory_06 = 54532556,
        Factory_07 = 8471836,
        Factory_08 = 12079557,
        Factory_09 = 77599601,
        Factory_Boss = 31164388,
        Buildings_01 = 34339745,
        Buildings_02 = 741544,
        Buildings_03 = 4002833,
        Buildings_04 = 89779951,
        Buildings_05 = 83815763,
        Buildings_06 = 51811090,
        Buildings_07 = 24724165,
        Buildings_08 = 68764697,
        Buildings_09 = 51529831,
        Buildings_Boss = 96318608,
        Command_01 = 37170220,
        Command_02 = 23778912,
        Command_03 = 22664878,
        Command_04 = 2217964,
        Command_05 = 7598833,
        Command_Boss = 39847359,
        Gauntlet = 69830922
    }
}
