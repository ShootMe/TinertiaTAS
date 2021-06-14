using System;
using System.Text;
namespace TinertiaStudio.Entities {
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
		public static char Delimiter = ',';
		public int Frames { get; set; }
		public Actions Actions { get; set; }
		public float Move { get; set; }
		public float Shoot { get; set; }
		public string Notes { get; set; }
		public int ZeroPadding { get; set; }
		public InputRecord(int frameCount, Actions actions, string notes = null) {
			Frames = frameCount;
			Actions = actions;
			Notes = notes;
		}
		public InputRecord(string line) {
			Notes = string.Empty;

			int index = 0;
			Frames = ReadFrames(line, ref index);
			if (Frames == 0) {
				Notes = line;
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

			while (start < line.Length) {
				char c = line[start];

				if (!foundFrames) {
					if (char.IsDigit(c)) {
						foundFrames = true;
						frames = c ^ 0x30;
						if (c == '0') { ZeroPadding = 1; }
					} else if (c != ' ') {
						return frames;
					}
				} else if (char.IsDigit(c)) {
					if (frames < 9999) {
						frames = frames * 10 + (c ^ 0x30);
						if (c == '0' && frames == 0) { ZeroPadding++; }
					} else {
						frames = 9999;
					}
				} else if (c != ' ') {
					return frames;
				}

				start++;
			}

			return frames;
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
					} else if (c != ',') {
						return 0f;
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
		public bool HasActions(Actions actions) {
			return (Actions & actions) != 0;
		}
		public override string ToString() {
			return Frames == 0 ? Notes : Frames.ToString().PadLeft(ZeroPadding, '0').PadLeft(4, ' ') + ActionsToString();
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
			if (HasActions(Actions.Move)) { sb.Append(",M,").Append(Move.ToString("0")); }
			if (HasActions(Actions.Shoot)) { sb.Append(",S,").Append(Shoot.ToString("0")); }
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
			return one.Frames == two.Frames && one.Actions == two.Actions && one.Move == two.Move && one.Shoot == two.Shoot;
		}
		public static bool operator !=(InputRecord one, InputRecord two) {
			bool oneNull = (object)one == null;
			bool twoNull = (object)two == null;
			if (oneNull != twoNull) {
				return true;
			} else if (oneNull && twoNull) {
				return false;
			}
			return one.Frames != two.Frames || one.Actions != two.Actions || one.Move != two.Move || one.Shoot != two.Shoot;
		}
		public int ActionPosition() {
			return Frames == 0 ? -1 : Math.Max(4, Frames.ToString().Length);
		}
	}
}