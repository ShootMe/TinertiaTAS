namespace TinertiaStudio.Entities {
	public class Vector3 {
		public float X { get; set; }
		public float Z { get; set; }
		public float Y { get; set; }
		public Vector3(float x, float z, float y) {
			X = x;
			Z = z;
			Y = y;
		}
		public override string ToString() {
			return ToString(2);
		}
		public string ToString(int decimalPoints = 2) {
			return "(" + X.ToString("0.".PadRight(decimalPoints + 2, '0')) + "," + Z.ToString("0.".PadRight(decimalPoints + 2, '0')) + "," + Y.ToString("0.".PadRight(decimalPoints + 2, '0')) + ")";
		}
	}
}
