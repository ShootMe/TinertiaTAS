using System;
using System.Text;
namespace TAS {
	[Flags]
	public enum Actions {
		None,
		Left = 0x1,
		Right = 0x2,
		Up = 0x4,
		Down = 0x8,
		Move = 0x10,
		Dash = 0x20,
		Shoot = 0x40,
		Start = 0x80,
		Accept = 0x100,
		Cancel = 0x200
	}
    public class InputRecord {
        public int Line { get; set; }
        public int Frames { get; set; }
        public Actions Actions { get; set; }
        public float Move { get; set; }
        public float Shoot { get; set; }
        public bool FastForward { get; set; }
        public bool ForceBreak { get; set; }
        public InputRecord() { }
        public InputRecord(int number, string line) {
            Line = number;

            int index = 0;
            Frames = ReadFrames(line, ref index);
            if (Frames == 0) {
                line = line.Trim();
                if (line.StartsWith("***")) {
                    FastForward = true;
                    index = 3;

                    if (line.Length >= 4 && line[3] == '!') {
                        ForceBreak = true;
                        index = 4;
                    }

                    Frames = ReadFrames(line, ref index);
                }
                return;
            }

            while (index < line.Length) {
                char c = line[index];

                switch (char.ToUpper(c)) {
                    case 'L': Actions ^= Actions.Left; break;
                    case 'R': Actions ^= Actions.Right; break;
                    case 'U': Actions ^= Actions.Up; break;
                    case 'D': Actions ^= Actions.Down; break;
                    case 'M':
                        Actions ^= Actions.Move;
                        index++;
                        Move = ReadAngle(line, ref index);
                        continue;
                    case 'B': Actions ^= Actions.Dash; break;
                    case 'S':
                        Actions ^= Actions.Shoot;
                        index++;
                        Shoot = ReadAngle(line, ref index);
                        continue;
                    case 'X': Actions ^= Actions.Start; break;
                    case 'A': Actions ^= Actions.Accept; break;
                    case 'C': Actions ^= Actions.Cancel; break;
                }

                index++;
            }
        }
        private int ReadFrames(string line, ref int start) {
            bool foundFrames = false;
            int frames = 0;
            bool negative = false;
            while (start < line.Length) {
                char c = line[start];

                if (!foundFrames) {
                    if (char.IsDigit(c)) {
                        foundFrames = true;
                        frames = c ^ 0x30;
                    } else if (c == '-') {
                        negative = true;
                    } else if (c != ' ') {
                        return negative ? -frames : frames;
                    }
                } else if (char.IsDigit(c)) {
                    if (frames < 999999) {
                        frames = frames * 10 + (c ^ 0x30);
                    } else {
                        frames = 999999;
                    }
                } else if (c != ' ') {
                    return negative ? -frames : frames;
                }

                start++;
            }

            return negative ? -frames : frames;
        }
        private float ReadAngle(string line, ref int start) {
            bool foundAngle = false;
            bool foundDecimal = false;
            int decimalPlaces = 1;
            int angle = 0;
            bool negative = false;

            while (start < line.Length) {
                char c = line[start];

                if (!foundAngle) {
                    if (char.IsDigit(c)) {
                        foundAngle = true;
                        angle = c ^ 0x30;
                    } else if (c == '.') {
                        foundAngle = true;
                        foundDecimal = true;
                    } else if (c == '-') {
                        negative = true;
                    }
                } else if (char.IsDigit(c)) {
                    angle = angle * 10 + (c ^ 0x30);
                    if (foundDecimal) {
                        decimalPlaces *= 10;
                    }
                } else if (c == '.') {
                    foundDecimal = true;
                } else if (c != ' ') {
                    return (negative ? (float)-angle : (float)angle) / (float)decimalPlaces;
                }

                start++;
            }

            return (negative ? (float)-angle : (float)angle) / (float)decimalPlaces;
        }
        public float GetX() {
            if (HasActions(Actions.Right)) {
                return 1f;
            } else if (HasActions(Actions.Left)) {
                return -1f;
            } else if (!HasActions(Actions.Move)) {
                return 0f;
            }
            return (float)Math.Sin(Move * Math.PI / 180.0);
        }
        public float GetY() {
            if (HasActions(Actions.Up)) {
                return 1f;
            } else if (HasActions(Actions.Down)) {
                return -1f;
            } else if (!HasActions(Actions.Move)) {
                return 0f;
            }
            return (float)Math.Cos(Move * Math.PI / 180.0);
        }
        public float GetShootX() {
            if (!HasActions(Actions.Shoot)) {
                return 0f;
            }
            return (float)Math.Sin(Shoot * Math.PI / 180.0);
        }
        public float GetShootY() {
            if (!HasActions(Actions.Shoot)) {
                return 0f;
            }
            return (float)Math.Cos(Shoot * Math.PI / 180.0);
        }
        public bool HasActions(Actions actions) {
            return (Actions & actions) != 0;
        }
        public override string ToString() {
            return Frames == 0 ? string.Empty : Frames.ToString().PadLeft(4, ' ') + ActionsToString();
        }
        public string ActionsToString() {
            StringBuilder sb = new StringBuilder();
            if (HasActions(Actions.Left)) { sb.Append(",L"); }
            if (HasActions(Actions.Right)) { sb.Append(",R"); }
            if (HasActions(Actions.Up)) { sb.Append(",U"); }
            if (HasActions(Actions.Down)) { sb.Append(",D"); }
            if (HasActions(Actions.Dash)) { sb.Append(",B"); }
            if (HasActions(Actions.Start)) { sb.Append(",X"); }
            if (HasActions(Actions.Accept)) { sb.Append(",A"); }
            if (HasActions(Actions.Cancel)) { sb.Append(",C"); }
            if (HasActions(Actions.Move)) { sb.Append(",M,").Append(Move.ToString("0.0")); }
            if (HasActions(Actions.Shoot)) { sb.Append(",S,").Append(Shoot.ToString("0.0")); }
            return sb.ToString();
        }
        public override bool Equals(object obj) {
            return obj is InputRecord && ((InputRecord)obj) == this;
        }
        public override int GetHashCode() {
            return Frames ^ (int)Actions;
        }
        public static bool operator ==(InputRecord one, InputRecord two) {
            bool oneNull = (object)one == null;
            bool twoNull = (object)two == null;
            if (oneNull != twoNull) {
                return false;
            } else if (oneNull && twoNull) {
                return true;
            }
            return one.Actions == two.Actions && one.Move == two.Move && one.Shoot == two.Shoot;
        }
        public static bool operator !=(InputRecord one, InputRecord two) {
            bool oneNull = (object)one == null;
            bool twoNull = (object)two == null;
            if (oneNull != twoNull) {
                return true;
            } else if (oneNull && twoNull) {
                return false;
            }
            return one.Actions != two.Actions || one.Move != two.Move || one.Shoot != two.Shoot;
        }
    }
}
