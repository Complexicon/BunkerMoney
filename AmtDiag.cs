using System.Drawing;
using System.Windows.Forms;

namespace BunkerMoney {
	public partial class AmtDialog : Form {

		public int amt = 0;
		private bool mouseDown;
		private Point lastLocation;

		public AmtDialog() {
			InitializeComponent();
		}

		private void OneDeliveryClick(object sender, System.EventArgs e) {
			amt = 1;
			DialogResult = DialogResult.OK;
		}

		private void FiveDeliveryClick(object sender, System.EventArgs e) {
			amt = 5;
			DialogResult = DialogResult.OK;
		}

		private void Panel_MouseDown(object sender, MouseEventArgs e) {
			mouseDown = true;
			lastLocation = e.Location;
		}

		private void Panel_MouseMove(object sender, MouseEventArgs e) {
			if(mouseDown) {
				Location = new Point(Location.X - lastLocation.X + e.X, Location.Y - lastLocation.Y + e.Y);
				this.Update();
			}
		}

		private void Panel_MouseUp(object sender, MouseEventArgs e) => mouseDown = false;

		private void CloseButton(object sender, System.EventArgs e) {
			this.Close();
		}

		private void Button4_Click(object sender, System.EventArgs e) {
			amt = 10;
			DialogResult = DialogResult.OK;
		}
	}
}
