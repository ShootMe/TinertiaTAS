using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
namespace TAS {
    public class InputController {
        private List<InputRecord> inputs = new List<InputRecord>();
        private int inputIndex, frameToNext;
        private string filePath;
        private List<InputRecord> fastForwards = new List<InputRecord>();

        public InputController(string filePath) {
            this.filePath = filePath;
            Current = new InputRecord();
        }

        public bool CanPlayback { get { return inputIndex < inputs.Count; } }
        public bool HasFastForward { get { return fastForwards.Count > 0; } }
        public int FastForwardSpeed { get { return fastForwards.Count == 0 ? 1 : fastForwards[0].Frames == 0 ? 6000 : fastForwards[0].Frames; } }
        public int CurrentFrame { get; set; }
        public int CurrentInputFrame { get { return CurrentFrame - frameToNext + Current.Frames; } }
        public bool FirstInputFrame { get { return CurrentFrame - frameToNext + Current.Frames == 1; } }
        public bool LastInputFrame { get { return CurrentFrame - frameToNext == 0; } }
        public InputRecord Current { get; set; }
        public InputRecord Previous {
            get {
                if (frameToNext != 0) {
                    if (CurrentInputFrame == 1) {
                        if (inputIndex - 1 >= 0 && inputs.Count > 0) {
                            return inputs[inputIndex - 1];
                        }
                        return null;
                    }
                    return Current;
                }
                return null;
            }
        }
        public InputRecord Next {
            get {
                if (frameToNext != 0 && inputIndex + 1 < inputs.Count) {
                    return inputs[inputIndex + 1];
                }
                return null;
            }
        }
        public override string ToString() {
            if (frameToNext == 0 && Current != null) {
                return $"{Current}({CurrentFrame})";
            } else if (inputIndex < inputs.Count && Current != null) {
                int inputFrames = Current.Frames;
                int startFrame = frameToNext - inputFrames;
                return $"{Current}({(CurrentFrame - startFrame)} / {inputFrames} : {CurrentFrame})";
            }
            return string.Empty;
        }
        public string NextInput() {
            if (frameToNext != 0 && inputIndex + 1 < inputs.Count) {
                return inputs[inputIndex + 1].ToString();
            }
            return string.Empty;
        }
        public void InitializePlayback() {
            int trycount = 5;
            while (!ReadFile() && trycount >= 0) {
                System.Threading.Thread.Sleep(50);
                trycount--;
            }

            CurrentFrame = 0;
            inputIndex = 0;
            if (inputs.Count > 0) {
                Current = inputs[0];
                frameToNext = Current.Frames;
            } else {
                Current = new InputRecord();
                frameToNext = 1;
            }
        }
        public void ReloadPlayback() {
            int playedBackFrames = CurrentFrame;
            InitializePlayback();
            CurrentFrame = playedBackFrames;

            while (CurrentFrame >= frameToNext) {
                if (inputIndex + 1 >= inputs.Count) {
                    inputIndex++;
                    return;
                }
                if (Current.FastForward) {
                    fastForwards.RemoveAt(0);
                }
                Current = inputs[++inputIndex];
                frameToNext += Current.Frames;
            }
        }
        public void InitializeRecording() {
            CurrentFrame = 0;
            inputIndex = 0;
            Current = new InputRecord();
            frameToNext = 0;
            inputs.Clear();
        }
        public void PlaybackPlayer() {
            if (inputIndex < inputs.Count && !Manager.IsLoading()) {
                if (CurrentFrame >= frameToNext) {
                    if (inputIndex + 1 >= inputs.Count) {
                        inputIndex++;
                        return;
                    }
                    if (Current.FastForward) {
                        fastForwards.RemoveAt(0);
                    }
                    Current = inputs[++inputIndex];
                    frameToNext += Current.Frames;
                }

                CurrentFrame++;
            }
        }
        public void RecordPlayer(TinertiaFrameInputs frame) {
            InputRecord input = new InputRecord() { Line = inputIndex + 1, Frames = CurrentFrame };
            SetInputs(frame, input);

            if (CurrentFrame == 0 && input == Current) {
                return;
            } else if (input != Current && !Manager.IsLoading()) {
                Current.Frames = CurrentFrame - Current.Frames;
                inputIndex++;
                if (Current.Frames != 0) {
                    inputs.Add(Current);
                }
                Current = input;
            }
            CurrentFrame++;
        }
        private void SetInputs(TinertiaFrameInputs frame, InputRecord input) {
            float xMax = frame.stickLeft.x;
            float yMax = frame.stickLeft.y;

            if (xMax != 0 && yMax != 0) {
                input.Actions |= Actions.Move;
                float angle = (float)(Math.Atan2(xMax, yMax) * 180 / Math.PI);
                if (angle < 0) { angle += 360; }
                input.Move = angle;
            } else if (xMax < 0) {
                input.Actions |= Actions.Left;
            } else if (xMax > 0) {
                input.Actions |= Actions.Right;
            } else if (yMax < 0) {
                input.Actions |= Actions.Down;
            } else if (yMax > 0) {
                input.Actions |= Actions.Up;
            }

            xMax = frame.stickRight.x;
            yMax = frame.stickRight.y;

            if (xMax != 0 && yMax != 0) {
                input.Actions |= Actions.Shoot;
                float angle = (float)(Math.Atan2(xMax, yMax) * 180 / Math.PI);
                if (angle < 0) { angle += 360; }
                input.Shoot = angle;
            } else if (xMax < 0) {
                input.Actions |= Actions.Shoot;
                input.Shoot = 270;
            } else if (xMax > 0) {
                input.Actions |= Actions.Shoot;
                input.Shoot = 90;
            } else if (yMax < 0) {
                input.Actions |= Actions.Shoot;
                input.Shoot = 180;
            } else if (yMax > 0) {
                input.Actions |= Actions.Shoot;
                input.Shoot = 0;
            }

            if (frame.buttonDash) {
                input.Actions |= Actions.Dash;
            }
        }
        public void WriteInputs() {
            using (FileStream fs = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.ReadWrite)) {
                for (int i = 0; i < inputs.Count; i++) {
                    InputRecord record = inputs[i];
                    byte[] data = Encoding.ASCII.GetBytes(record.ToString() + "\r\n");
                    fs.Write(data, 0, data.Length);
                }
                fs.Close();
            }
        }
        private bool ReadFile() {
            try {
                inputs.Clear();
                fastForwards.Clear();
                if (!File.Exists(filePath)) { return false; }

                int lines = 0;
                using (StreamReader sr = new StreamReader(filePath)) {
                    while (!sr.EndOfStream) {
                        string line = sr.ReadLine();

                        if (line.IndexOf("Read", StringComparison.OrdinalIgnoreCase) == 0 && line.Length > 5) {
                            lines++;
                            ReadFile(line.Substring(5), lines);
                            lines--;
                        }

                        InputRecord input = new InputRecord(++lines, line);
                        if (input.FastForward) {
                            fastForwards.Add(input);

                            if (inputs.Count > 0) {
                                inputs[inputs.Count - 1].ForceBreak = input.ForceBreak;
                                inputs[inputs.Count - 1].FastForward = true;
                            }
                        } else if (input.Frames != 0) {
                            inputs.Add(input);
                        }
                    }
                }
                return true;
            } catch {
                return false;
            }
        }
        private void ReadFile(string extraFile, int lines) {
            int index = extraFile.IndexOf(',');
            string filePath = index > 0 ? extraFile.Substring(0, index) : extraFile;
            if (!File.Exists(filePath)) {
                string[] files = Directory.GetFiles(Directory.GetCurrentDirectory(), $"{filePath}*.tas");
                filePath = files.Length > 0 ? files[0] : null;
                if (!File.Exists(filePath)) { return; }
            }
            int skipLines = 0;
            int lineLen = int.MaxValue;
            if (index > 0) {
                int indexLen = extraFile.IndexOf(',', index + 1);
                if (indexLen > 0) {
                    string startLine = extraFile.Substring(index + 1, indexLen - index - 1);
                    string endLine = extraFile.Substring(indexLen + 1);
                    if (!int.TryParse(startLine, out skipLines)) {
                        skipLines = GetLine(startLine, filePath);
                    }
                    if (!int.TryParse(endLine, out lineLen)) {
                        lineLen = GetLine(endLine, filePath);
                    }
                } else {
                    string startLine = extraFile.Substring(index + 1);
                    if (!int.TryParse(startLine, out skipLines)) {
                        skipLines = GetLine(startLine, filePath);
                    }
                }
            }

            int subLine = 0;
            using (StreamReader sr = new StreamReader(filePath)) {
                while (!sr.EndOfStream) {
                    string line = sr.ReadLine();

                    subLine++;
                    if (subLine <= skipLines) { continue; }
                    if (subLine > lineLen) { break; }

                    if (line.IndexOf("Read", StringComparison.OrdinalIgnoreCase) == 0 && line.Length > 5) {
                        ReadFile(line.Substring(5), lines);
                    }

                    InputRecord input = new InputRecord(lines, line);
                    if (input.FastForward) {
                        fastForwards.Add(input);

                        if (inputs.Count > 0) {
                            inputs[inputs.Count - 1].ForceBreak = input.ForceBreak;
                            inputs[inputs.Count - 1].FastForward = true;
                        }
                    } else if (input.Frames != 0) {
                        inputs.Add(input);
                    }
                }
            }
        }
        private int GetLine(string label, string path) {
            int curLine = 0;
            using (StreamReader sr = new StreamReader(path)) {
                while (!sr.EndOfStream) {
                    curLine++;
                    string line = sr.ReadLine();
                    if (line.StartsWith("#" + label)) {
                        return curLine;
                    }
                }
                return int.MaxValue;
            }
        }
    }
}