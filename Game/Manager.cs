using J2i.Net.XInputWrapper;
using System;
using System.Runtime.InteropServices;
using System.Threading;
using UnityEngine;
namespace TAS {
    [Flags]
    public enum State {
        None = 0,
        Enable = 1,
        FrameStep = 2,
        Disable = 4
    }
    public class Manager {
        [DllImport("user32.dll", CharSet = CharSet.Auto, ExactSpelling = true)]
        public static extern short GetAsyncKeyState(Keys vkey);
        public static bool Running;
        private static InputController controller, replays;
        private static State state, nextState;
        private static XboxController xbox;
        public static Vector3 PlayerVelocity;
        public static Vector3 LastPlayerPosition;
        public static string CurrentStatus;
        private static int controllerIndex;
        static Manager() {
            controller = new InputController("Tinertia.tas");
            xbox = XboxController.RetrieveController(0);
            XboxController.UpdateFrequency = 60;
            XboxController.StartPolling();
        }
        private static bool IsKeyDown(Keys key) {
            return (GetAsyncKeyState(key) & 32768) == 32768;
        }
        public static bool IsLoading() {
            return CgGame.IsLoadingScene || (Tinertia.CurrentLevel != null && Tinertia.CurrentLevel.IsSectionComplete);
        }
        public static void UpdateInputs() {
            if (!xbox.IsConnected) {
                controllerIndex++;
                if (controllerIndex >= 4) {
                    controllerIndex = 0;
                }
                xbox = XboxController.RetrieveController(controllerIndex);
            }
            if (replays == null) {
                replays = new InputController("Replay.tas");
            }
            HandleFrameRates();
            CheckControls();
            FrameStepping();

            if (!Application.runInBackground) {
                Application.runInBackground = true;
            }

            CgActor player = CgGame.GetPlayer();
            if (player != null && player.transform != null) {
                Vector3 temp = player.transform.position;
                PlayerVelocity = (temp - LastPlayerPosition) * 60;
                LastPlayerPosition = temp;
            }

            if (HasFlag(state, State.Enable)) {
                Running = true;
                bool fastForward = controller.HasFastForward;
                controller.PlaybackPlayer();

                if (fastForward && (!controller.HasFastForward || controller.Current.ForceBreak && controller.CurrentInputFrame == controller.Current.Frames)) {
                    state |= State.FrameStep;
                }

                if (!controller.CanPlayback) {
                    DisableRun();
                } else {
                    InputRecord input = controller.Current;
                    if (input.HasActions(Actions.Cancel) && !Tinertia.PauseGame) {
                        switch (Tinertia.GameMode) {
                            case TinertiaGameMode.Campaign:
                            case TinertiaGameMode.Gauntlet:
                                Tinertia.instance.DoRestartLevel(false);
                                break;
                            case TinertiaGameMode.SpeedRun: {
                                    bool flag = Tinertia.CurrentLevel as World != null;
                                    if (flag && Tinertia.CurrentLevel.CurrentSectionIndex == 0) {
                                        Tinertia.instance.DoRestartWorld(false);
                                    } else {
                                        Tinertia.instance.DoRestartLevel(false);
                                    }
                                    break;
                                }
                        }
                    }
                }

                string status = $"{controller.Current.Line}[{controller}]";
                CurrentStatus = status;
            } else {
                Running = false;
                CurrentStatus = null;
            }
        }
        public static void RecordReplay(TinertiaFrameInputs frame) {
            if (Tinertia.ShowReplay) {
                replays.RecordPlayer(frame);
            } else if (replays.CurrentFrame > 0) {
                replays.WriteInputs();
                replays.InitializeRecording();
            }
        }
        public static TinertiaFrameInputs GetFrameInputs() {
            InputRecord input = controller.Current;
            return new TinertiaFrameInputs() {
                stickLeft = new Vector3(input.GetX(), input.GetY()),
                stickRight = new Vector3(input.GetShootX(), input.GetShootY()),
                buttonDash = input.HasActions(Actions.Dash)
            };
        }
        public static bool GetKeyDown(KeyCode key) {
            if (!controller.FirstInputFrame) { return false; }

            InputRecord input = controller.Current;
            switch (key) {
                case KeyCode.Joystick1Button1: return input.HasActions(Actions.Accept);
                case KeyCode.Joystick1Button2: return input.HasActions(Actions.Cancel);
                case KeyCode.Joystick1Button9: return input.HasActions(Actions.Start);
            }
            return false;
        }
        public static bool GetKeyUp(KeyCode key) {
            if (!controller.LastInputFrame) { return false; }

            InputRecord input = controller.Current;
            switch (key) {
                case KeyCode.Joystick1Button1: return input.HasActions(Actions.Accept);
                case KeyCode.Joystick1Button2: return input.HasActions(Actions.Cancel);
                case KeyCode.Joystick1Button9: return input.HasActions(Actions.Start);
            }
            return false;
        }
        public static float GetAxis(string axis) {
            InputRecord input = controller.Current;
            switch (axis) {
                case "UI_X":
                case "Horizontal": return input.GetX();
                case "UI_Y":
                case "Vertical": return input.GetY();
                case "Shoot_X": return input.GetShootX();
                case "Shoot_Y": return input.GetShootY();
            }
            return 0f;
        }
        private static void HandleFrameRates() {
            if ((HasFlag(state, State.Enable) || Tinertia.ShowReplay) && !HasFlag(state, State.FrameStep)) {
                float rightStickX = (float)xbox.RightThumbStickX / 32768f;
                if (IsKeyDown(Keys.LShiftKey) && IsKeyDown(Keys.LControlKey)) {
                    rightStickX = -0.65f;
                } else if (controller.HasFastForward || (IsKeyDown(Keys.RShiftKey) && IsKeyDown(Keys.RControlKey))) {
                    rightStickX = 1f;
                }

                if (rightStickX <= -0.9) {
                    SetFrameRate(3);
                } else if (rightStickX <= -0.8) {
                    SetFrameRate(10);
                } else if (rightStickX <= -0.7) {
                    SetFrameRate(20);
                } else if (rightStickX <= -0.6) {
                    SetFrameRate(30);
                } else if (rightStickX <= -0.5) {
                    SetFrameRate(40);
                } else if (rightStickX <= -0.4) {
                    SetFrameRate(45);
                } else if (rightStickX <= -0.3) {
                    SetFrameRate(50);
                } else if (rightStickX <= -0.2) {
                    SetFrameRate(55);
                } else if (rightStickX <= 0.2) {
                    SetFrameRate();
                } else if (rightStickX <= 0.3) {
                    SetFrameRate(80);
                } else if (rightStickX <= 0.4) {
                    SetFrameRate(100);
                } else if (rightStickX <= 0.5) {
                    SetFrameRate(120);
                } else if (rightStickX <= 0.6) {
                    SetFrameRate(140);
                } else if (rightStickX <= 0.7) {
                    SetFrameRate(160);
                } else if (rightStickX <= 0.8) {
                    SetFrameRate(180);
                } else if (rightStickX <= 0.9) {
                    SetFrameRate(200);
                } else {
                    SetFrameRate(6000);
                }
            } else {
                SetFrameRate();
            }
        }
        private static void SetFrameRate(int newFrameRate = 60) {
            //CgGame.TimeScale = Tinertia.ShowReplay && newFrameRate == 60 ? Time.timeScale : 1f;
            Time.fixedDeltaTime = 1f / 60f;
            CgCamera.Main.enabled = newFrameRate <= 200;
            CgCamera.UI.enabled = newFrameRate <= 200;
            Time.maximumDeltaTime = Time.fixedDeltaTime;
            Time.captureFramerate = Tinertia.ShowReplay ? 0 : 60;
            Application.targetFrameRate = newFrameRate;
            QualitySettings.vSyncCount = 0;// newFrameRate == 60 ? 1 : 0;
        }
        private static void FrameStepping() {
            bool dpadUp = xbox.IsDPadUpPressed || IsKeyDown(Keys.OemOpenBrackets);

            if ((HasFlag(state, State.Enable) || Tinertia.ShowReplay) && (HasFlag(state, State.FrameStep) || dpadUp)) {
                bool continueLoop = dpadUp;
                while (HasFlag(state, State.Enable) || Tinertia.ShowReplay) {
                    float rightStickX = (float)xbox.RightThumbStickX / 32768f;
                    if (IsKeyDown(Keys.RShiftKey) && IsKeyDown(Keys.RControlKey)) {
                        rightStickX = 0.65f;
                    }
                    dpadUp = xbox.IsDPadUpPressed || IsKeyDown(Keys.OemOpenBrackets);
                    bool dpadDown = xbox.IsDPadDownPressed || IsKeyDown(Keys.OemCloseBrackets);

                    CheckControls();
                    if (!continueLoop && dpadUp) {
                        state |= State.FrameStep;
                        break;
                    } else if (dpadDown) {
                        state &= ~State.FrameStep;
                        break;
                    } else if (rightStickX >= 0.2) {
                        state |= State.FrameStep;
                        int sleepTime = 0;
                        if (rightStickX <= 0.3) {
                            sleepTime = 200;
                        } else if (rightStickX <= 0.4) {
                            sleepTime = 100;
                        } else if (rightStickX <= 0.5) {
                            sleepTime = 80;
                        } else if (rightStickX <= 0.6) {
                            sleepTime = 64;
                        } else if (rightStickX <= 0.7) {
                            sleepTime = 48;
                        } else if (rightStickX <= 0.8) {
                            sleepTime = 32;
                        } else if (rightStickX <= 0.9) {
                            sleepTime = 16;
                        }
                        Thread.Sleep(sleepTime);
                        break;
                    }
                    continueLoop = dpadUp;
                    Thread.Sleep(1);
                }
                if (!Tinertia.ShowReplay) {
                    ReloadRun();
                }
            }
        }
        private static void CheckControls() {
            bool openBracket = IsKeyDown(Keys.ControlKey) && IsKeyDown(Keys.OemOpenBrackets);
            bool closeBrackets = IsKeyDown(Keys.ControlKey) && IsKeyDown(Keys.OemCloseBrackets);
            bool rightStick = xbox.IsRightStickPressed || openBracket || closeBrackets;

            if (!HasFlag(state, State.Enable) && !Tinertia.ShowReplay && rightStick) {
                nextState |= State.Enable;
            } else if (HasFlag(state, State.Enable) && rightStick) {
                nextState |= State.Disable;
            }

            if (!rightStick && HasFlag(nextState, State.Enable)) {
                EnableRun();
            } else if (!rightStick && HasFlag(nextState, State.Disable)) {
                DisableRun();
            }
        }
        private static void DisableRun() {
            Running = false;
            nextState &= ~State.Disable;
            state = State.None;
        }
        private static void EnableRun() {
            Tinertia.PauseGame = true;
            Tinertia.ShowLevelSelect = false;
            CgCamera.SetView(null, 0f, CgEaseType.EaseOutCubic);
            Tinertia.SessionStats.Reset();
            Tinertia.LevelTimer.Reset();
            CgGame.ReloadLevelSync(Tinertia.GameMode != TinertiaGameMode.Campaign && Tinertia.CurrentLevel as World != null ? 0 : Tinertia.CurrentLevel.CurrentSectionIndex);
            Tinertia.PauseGame = false;

            nextState &= ~State.Enable;
            UpdateVariables();
        }
        private static void ReloadRun() {
            controller.ReloadPlayback();
        }
        private static void UpdateVariables() {
            state |= State.Enable;
            state &= ~State.FrameStep;
            controller.InitializePlayback();
            Running = true;
        }
        private static bool HasFlag(State state, State flag) {
            return (state & flag) == flag;
        }
    }
}