using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using static System.Windows.Forms.DialogResult;

namespace BunkerMoney {
	static class Program {

		static int[] bunkerOff = new int[] { 0x1180, 0x4128 };
		static long GlobalPTR;

		static Mem Mem;

		static string url = "https://raw.githubusercontent.com/Complexicon/BunkerMoney/master/offsets.ini";

		[STAThread]
		static void Main() {
			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault(false);

			using(WebClient client = new WebClient()) {
				try {
					var cfgDict = new Dictionary<string, int>();
					string s = client.DownloadString(url);
					string[] lines = s.Split(Environment.NewLine.ToCharArray());
					Console.WriteLine(lines);
					foreach(string i in lines) {
						if(i == "") continue;
						var splitted = i.Split('=');
						cfgDict[splitted[0]] = Convert.ToInt32(splitted[1], 16);
					}
					bunkerOff[0] = cfgDict["offset1"];
					bunkerOff[1] = cfgDict["offset2"];
				} catch {
					MessageBox.Show("Couldn't load Remote Offsets! Using local Offsets (GTA 1.46)!", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
				}
			}

			try {
				Mem = new Mem("GTA5");
				var addr = Mem.FindPattern(new byte[] { 0x4C, 0x8D, 0x05, 0x0, 0x0, 0x0, 0x0, 0x4D, 0x8B, 0x08, 0x4D, 0x85, 0xC9, 0x74, 0x11 }, "xxx????xxxxxxxx");
				GlobalPTR = addr + Mem.ReadInt(addr + 3, null) + 7;
			} catch {
				MessageBox.Show("GTA is not Running!", "Serious Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
				return;
			}

			var priceDiag = new PriceDialog();
			var amtDiag = new AmtDialog();

			if(priceDiag.ShowDialog() == OK && amtDiag.ShowDialog() == OK) {
				try {
					Mem.WriteInt(GlobalPTR - 0x128, bunkerOff, Convert.ToInt32((1000000 / (priceDiag.price / amtDiag.amt)) * 4.5));
					MessageBox.Show("Done! Deliver Vehlicle(s) and Enjoy :) Program will now close!", "Yay!", MessageBoxButtons.OK, MessageBoxIcon.Information);

				} catch { MessageBox.Show("There was an Error during the Procedure!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error); }
			} else { MessageBox.Show("Please make sure everything is correct!", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Exclamation); }

		}
	}

	class Mem {

		[DllImport("kernel32.dll")]
		public static extern int WriteProcessMemory(IntPtr Handle, long Address, byte[] buffer, int Size, int BytesWritten = 0);
		[DllImport("kernel32.dll")]
		public static extern int ReadProcessMemory(IntPtr Handle, long Address, byte[] buffer, int Size, int BytesRead = 0);

		public Process Proc;
		public long BaseAddress;

		public Mem(string process) {
			try {
				Proc = Process.GetProcessesByName(process)[0];
				BaseAddress = Proc.MainModule.BaseAddress.ToInt64();
			} catch { throw new Exception(); }
		}

		public IntPtr GetProcHandle() {
			try { return Proc.Handle; } 
			catch { return IntPtr.Zero; }
		}

		public long GetPtrAddr(long Pointer, int[] Offset = null) {
			byte[] Buffer = new byte[8];

			ReadProcessMemory(GetProcHandle(), Pointer, Buffer, Buffer.Length);

			if(Offset != null) {
				for(int x = 0; x < (Offset.Length - 1); x++) {
					Pointer = BitConverter.ToInt64(Buffer, 0) + Offset[x];
					ReadProcessMemory(GetProcHandle(), Pointer, Buffer, Buffer.Length);
				}

				Pointer = BitConverter.ToInt64(Buffer, 0) + Offset[Offset.Length - 1];
			}

			return Pointer;
		}

		public long FindPattern(byte[] pattern, string mask) {
			int moduleSize = Proc.MainModule.ModuleMemorySize;

			if(moduleSize == 0)
				throw new Exception($"Size of module {Proc.MainModule.ModuleName} is INVALID.");

			byte[] moduleBytes = new byte[moduleSize];
			ReadProcessMemory(GetProcHandle(), BaseAddress, moduleBytes, moduleSize);

			for(long i = 0; i < moduleSize; i++) {
				bool found = true;

				for(int l = 0; l < mask.Length; l++) {
					found = mask[l] == '?' || moduleBytes[l + i] == pattern[l];

					if(!found)
						break;
				}

				if(found) {
					moduleBytes = null;
					return i;
				}

			}
			return 0;
		}

		public void WriteInt(long BasePTR, int[] offset, int Value) => WriteProcessMemory(GetProcHandle(), GetPtrAddr(BaseAddress + BasePTR, offset), BitConverter.GetBytes(Value), BitConverter.GetBytes(Value).Length);
		public int ReadInt(long BasePTR, int[] offset) {
			byte[] Buffer = new byte[4];
			ReadProcessMemory(GetProcHandle(), GetPtrAddr(BaseAddress + BasePTR, offset), Buffer, 4);
			return BitConverter.ToInt32(Buffer, 0);
		}

	}

}
