using System.Drawing;
using System.Windows.Forms;

namespace BunkerMoney {
	public partial class PriceDialog : Form {

		public int price = 0;
		private bool mouseDown;
		private Point lastLocation;

		public PriceDialog() {
			InitializeComponent();
		}

		private void NextClick(object sender, System.EventArgs e) {
			if(int.TryParse(textBox1.Text, out price)) {
				DialogResult = DialogResult.OK;
				Close();
			} else {
				TextBox1SetText("Try Again!");
			}
		}

		private void Panel_MouseDown(object sender, MouseEventArgs e) {
			mouseDown = true;
			lastLocation = e.Location;
		}

		private void Panel_MouseMove(object sender, MouseEventArgs e) {
			if(mouseDown) {
				Location = new Point(Location.X - lastLocation.X + e.X, Location.Y - lastLocation.Y + e.Y);
				Update();
			}
		}

		private void Panel_MouseUp(object sender, MouseEventArgs e) => mouseDown = false;

		private void CloseButton(object sender, System.EventArgs e) => Close();

		private void TextBox1_Enter(object sender, System.EventArgs e) {
			if(textBox1.ForeColor == Color.White) return;
			textBox1.Text = "";
			textBox1.ForeColor = Color.White;
		}

		private void TextBox1_Leave(object sender, System.EventArgs e) {
			if(textBox1.Text.Trim() == "") TextBox1SetText("Enter Number...");
		}

		private void TextBox1SetText(string text) {
			textBox1.Text = text;
			textBox1.ForeColor = Color.DimGray;
		}
	}
}
