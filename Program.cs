using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace BunkerMoney {
	static class Program {

		static long WorldPTR;
		static long GlobalPTR;
		static long BlipPTR;

		/*
		THIS IS IMPORTANT! limit is the hardcap 25 means 2.5mil (100k * 25)
		*/
		static int limit = 25;
		//bonus compensation is hardcoded for now
		//i dont have the offset to get player count and i wont fuck around to find it
		static double bonusCompensation = 0.25;
		//stores last known value in bunker
		static int lastMoneyInBunker = 0;

		static Mem Mem;
		static MenuItem debug;
		static NotifyIcon trayIcon;

		//ASYNCKEYSTATE
		[DllImport("user32.dll")]
		public static extern int GetAsyncKeyState(Keys i);

		[STAThread]
		static void Main() {
			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault(false);

			ContextMenu trayMenu = new ContextMenu();

			//why did i add a tray icon?
			trayIcon = new NotifyIcon {
				Text = "BunkerMoney",
				Icon = BunkerMoney.Properties.Resources.icon,
				ContextMenu = trayMenu,
				Visible = true
			};
			debug = trayMenu.MenuItems.Add("lastMoneyInBunker: 0");
			trayMenu.MenuItems.Add("-");
			trayMenu.MenuItems.Add("Exit", Exit);

			try {
				Mem = new Mem("GTA5");
				long addr = Mem.FindPattern(new byte[] { 0x48, 0x8B, 0x05, 0x0, 0x0, 0x0, 0x0, 0x45, 0x0, 0x0, 0x0, 0x0, 0x48, 0x8B, 0x48, 0x08, 0x48, 0x85, 0xC9, 0x74, 0x07 }, "xxx????x????xxxxxxxxx");
				WorldPTR = addr + Mem.ReadInt(addr + 3, null) + 7;
				long addr2 = Mem.FindPattern(new byte[] { 0x4C, 0x8D, 0x05, 0x0, 0x0, 0x0, 0x0, 0x0F, 0xB7, 0xC1 }, "xxx????xxx");
				BlipPTR = addr2 + Mem.ReadInt(addr2 + 3, null) + 7;
				var addr3 = Mem.FindPattern(new byte[] { 0x4C, 0x8D, 0x05, 0x0, 0x0, 0x0, 0x0, 0x4D, 0x8B, 0x08, 0x4D, 0x85, 0xC9, 0x74, 0x11 }, "xxx????xxxxxxxx");
				GlobalPTR = addr3 + Mem.ReadInt(addr3 + 3, null) + 7;
			} catch {
				MessageBox.Show("GTA is not Running!", "Serious Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
				return;
			}

			new Thread(Thr).Start();
			Application.Run();

		}

		private static void Thr() {
			//god i hate this shit
			while(true) {

				if(GetAsyncKeyState(Keys.B) != 0) {

					//READ last money
					if(PressedOnce(Keys.R)) {
						lastMoneyInBunker = Mem.ReadInt(GlobalPTR - 0x128, Offset.moneyInBunker);
						if(lastMoneyInBunker > 5000000 || lastMoneyInBunker < 1000) 
							MessageBox.Show("Values are Probably not Correct.\nMaybe try again? if it persists restart GTA!", "WARN", MessageBoxButtons.OK, MessageBoxIcon.Warning);
						Toggle();
						debug.Text = "lastMoneyInBunker: " + lastMoneyInBunker + "$";
					}

					//PATCH packages
					if(PressedOnce(Keys.P)) {
						uint packetMax = Mem.ReadUInt(GlobalPTR - 0x128, Offset.packetMax);
						if(packetMax > 0xff) {
							MessageBox.Show("Couldn't read correct values!\nTry restarting GTA. Closing!", "WARN", MessageBoxButtons.OK, MessageBoxIcon.Warning);
							Environment.Exit(0);
						}
						int packetAmtForMil = Convert.ToInt32(100000 / ((lastMoneyInBunker + (lastMoneyInBunker * bonusCompensation)) / packetMax));
						Mem.Write(GlobalPTR - 0x128, Offset.packet, packetAmtForMil * limit);

						Activate();
					}



				}

				//retarded ass "helper" functions
				if(GetAsyncKeyState(Keys.X) != 0) {
					if(PressedOnce(Keys.D1)) Teleport(ObjectiveCoords);
					if(PressedOnce(Keys.D2)) Teleport(WaypointCoords);
					if(PressedOnce(Keys.G)) {
						if(Godmode) Deactivate();
						else Activate();
						Godmode = !Godmode;
					}
					if(PressedOnce(Keys.E)) {
						if(WeaponBulletType == 3) {
							WeaponBulletType = 5;
							WeaponExplosionType = 18;
							Activate();
						} else {
							WeaponBulletType = 3;
							WeaponExplosionType = -1;
							Deactivate();
						}

					}
					if(PressedOnce(Keys.W)) {
						WantedLevel = 0;
						Activate();
					}
				}

				ClearStates();
				Thread.Sleep(10);
			}
		}

		//audio response to actions
		public static void Activate() {
			Console.Beep(523, 75);
			Console.Beep(587, 75);
			Console.Beep(700, 75);
		}

		public static void Deactivate() {
			Console.Beep(700, 75);
			Console.Beep(587, 75);
			Console.Beep(523, 75);
		}

		public static void Toggle() {
			Console.Beep(523, 75);
			Console.Beep(523, 75);
			Console.Beep(523, 75);
		}

		private static void Exit(object sender, EventArgs e) {
			trayIcon.Visible = false;
			Environment.Exit(0);
		}

		//KEYBOARD HELPER

		static List<Keys> pressedKeys = new List<Keys>();

		static bool PressedOnce(Keys k) {
			bool pressed = GetAsyncKeyState(k) > 0;
			if(pressed && !pressedKeys.Contains(k)) {
				pressedKeys.Add(k);
				return true;
			} else if(!pressed && pressedKeys.Contains(k)) pressedKeys.RemoveAll(item => item == k);
			return false;
		}

		//flush keyboard state
		//fucking slimmed down keyboard lib and mem lib coz vs19 cant link projects properly
		static void ClearStates() {
			for(int i=0; i < 0xFF; i++) GetAsyncKeyState((Keys)i);
		}

		//HAX VARS
		public static int WeaponExplosionType {
			get => Mem.ReadInt(WorldPTR, Offset.WeaponExplosionType);
			set => Mem.Write(WorldPTR, Offset.WeaponExplosionType, (int)value);
		}

		public static int WeaponBulletType {
			get => Mem.ReadInt(WorldPTR, Offset.WeaponDamageType);
			set => Mem.Write(WorldPTR, Offset.WeaponDamageType, value);
		}
		public static int WantedLevel {
			get => Mem.ReadInt(WorldPTR, Offset.WantedLevel);
			set => Mem.Write(WorldPTR, Offset.WantedLevel, value);
		}
		public static bool Godmode {
			get => Mem.ReadBool(WorldPTR, Offset.Godmode);
			set => Mem.Write(WorldPTR, Offset.Godmode, value);
		}
		public static float PlayerX {
			get => Mem.ReadFloat(WorldPTR, Offset.PlayerVecX);
			set {
				Mem.Write(WorldPTR, Offset.PlayerVecX, value);
				Mem.Write(WorldPTR, Offset.PlayerX, value);
			}
		}

		public static float PlayerY {
			get => Mem.ReadFloat(WorldPTR, Offset.PlayerVecY);
			set {
				Mem.Write(WorldPTR, Offset.PlayerVecY, value);
				Mem.Write(WorldPTR, Offset.PlayerY, value);
			}
		}

		public static float PlayerZ {
			get => Mem.ReadFloat(WorldPTR, Offset.PlayerVecZ);
			set {
				Mem.Write(WorldPTR, Offset.PlayerVecZ, value);
				Mem.Write(WorldPTR, Offset.PlayerZ, value);
			}
		}

		public static float CarX {
			get => Mem.ReadFloat(WorldPTR, Offset.CarVecX);
			set {
				Mem.Write(WorldPTR, Offset.CarVecX, value);
				Mem.Write(WorldPTR, Offset.CarX, value);
			}
		}

		public static float CarY {
			get => Mem.ReadFloat(WorldPTR, Offset.CarVecY);
			set {
				Mem.Write(WorldPTR, Offset.CarVecY, value);
				Mem.Write(WorldPTR, Offset.CarY, value);
			}
		}

		public static float CarZ {
			get => Mem.ReadFloat(WorldPTR, Offset.CarVecZ);
			set {
				Mem.Write(WorldPTR, Offset.CarVecZ, value);
				Mem.Write(WorldPTR, Offset.CarZ, value);
			}
		}

		public static void Teleport(Location l) {
			if(Mem.ReadInt(WorldPTR, Offset.PlayerInCar) == 256) {
				CarX = l.x;
				CarY = l.y;
				CarZ = l.z;
			} else {
				PlayerX = l.x;
				PlayerY = l.y;
				PlayerZ = l.z;
			}
		}

		//this coord shit is so hacky.

		public static Location WaypointCoords {
			get {
				for(int i = 2000; i > 1; i--) {
					if(Mem.ReadInt(BlipPTR + (i * 8), new int[] { 0x48 }) == 84 && Mem.ReadInt(BlipPTR + (i * 8), new int[] { 0x40 }) == 8) {
						return new Location {
							x = Mem.ReadFloat(BlipPTR + (i * 8), new int[] { 0x10 }),
							y =Mem.ReadFloat(BlipPTR + (i * 8), new int[] { 0x14 }),
							z = -210F
						};
					}
				}
				return new Location { x = PlayerX, y = PlayerY, z = PlayerZ };
			}
		}

		public static Location ObjectiveCoords {
			get {
				for(int i = 2000; i > 1; i--) {
					int objDetect = Mem.ReadInt(BlipPTR + (i * 8), new int[] { 0x48 });
					if(Mem.ReadInt(BlipPTR + (i * 8), new int[] { 0x40 }) == 1 && ((objDetect == 1) || (objDetect == 66) || (objDetect == 60))) {
						return new Location {
							x = Mem.ReadFloat(BlipPTR + (i * 8), new int[] { 0x10 }),
							y = Mem.ReadFloat(BlipPTR + (i * 8), new int[] { 0x14 }),
							z = Mem.ReadFloat(BlipPTR + (i * 8), new int[] { 0x18 }) + 10F
						};
					}
				}
				return new Location { x = PlayerX, y = PlayerY, z = PlayerZ };
			}
		}


	}

	//VEC3 effectively
	struct Location { public float x, y, z; }

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
			try { return Proc.Handle; } catch { return IntPtr.Zero; }
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

			if(moduleSize == 0) throw new Exception($"Size of module {Proc.MainModule.ModuleName} is INVALID.");

			byte[] moduleBytes = new byte[moduleSize];
			ReadProcessMemory(GetProcHandle(), BaseAddress, moduleBytes, moduleSize);

			for(long i = 0; i < moduleSize; i++) {
				for(int l = 0; l < mask.Length; l++)
					//dirty hack heh
					if(!(mask[l] == '?' || moduleBytes[l + i] == pattern[l])) goto SKIP;

				return i;
				SKIP:;
			}
			return 0;
		}


		public byte[] ReadBytes(long BasePTR, int[] offset, int Length) {
			byte[] Buffer = new byte[Length];
			ReadProcessMemory(GetProcHandle(), GetPtrAddr(BaseAddress + BasePTR, offset), Buffer, Length);
			return Buffer;
		}

		public void Write(long BasePTR, int[] offset, byte[] Bytes) => WriteProcessMemory(GetProcHandle(), GetPtrAddr(BaseAddress + BasePTR, offset), Bytes, Bytes.Length);

		public void Write(long BasePTR, int[] offset, bool b) => Write(BasePTR, offset, b ? new byte[] { 0x01 } : new byte[] { 0x00 });
		public void Write(long BasePTR, int[] offset, float Value) => Write(BasePTR, offset, BitConverter.GetBytes(Value));
		public void Write(long BasePTR, int[] offset, double Value) => Write(BasePTR, offset, BitConverter.GetBytes(Value));
		public void Write(long BasePTR, int[] offset, int Value) => Write(BasePTR, offset, BitConverter.GetBytes(Value));
		public void Write(long BasePTR, int[] offset, string String) => Write(BasePTR, offset, new ASCIIEncoding().GetBytes(String));
		public void Write(long BasePTR, int[] offset, long Value) => Write(BasePTR, offset, BitConverter.GetBytes(Value));
		public void Write(long BasePTR, int[] offset, uint Value) => Write(BasePTR, offset, BitConverter.GetBytes(Value));
		public void Write(long BasePTR, int[] offset, byte Value) => Write(BasePTR, offset, new byte[] { Value });
		public bool ReadBool(long BasePTR, int[] offset) => ReadByte(BasePTR, offset) != 0x00;


		public float ReadFloat(long BasePTR, int[] offset) => BitConverter.ToSingle(ReadBytes(BasePTR, offset, 4), 0);
		public double ReadDouble(long BasePTR, int[] offset) => BitConverter.ToDouble(ReadBytes(BasePTR, offset, 8), 0);
		public int ReadInt(long BasePTR, int[] offset) => BitConverter.ToInt32(ReadBytes(BasePTR, offset, 4), 0);
		public uint ReadUInt(long BasePTR, int[] offset) => BitConverter.ToUInt32(ReadBytes(BasePTR, offset, 4), 0);
		public string ReadString(long BasePTR, int[] offset, int size) => new ASCIIEncoding().GetString(ReadBytes(BasePTR, offset, size));
		public long ReadPointer(long BasePTR, int[] offset) => BitConverter.ToInt64(ReadBytes(BasePTR, offset, 8), 0);
		public byte ReadByte(long BasePTR, int[] offset) => ReadBytes(BasePTR, offset, 1)[0];

	}

	public static class Offset {

		public static int[] packet = new int[] { 0x1180, 0x41B8 };
		public static int[] packetMax = new int[] { 0x1180, 0x3A10 };
		public static int[] moneyInBunker = new int[] { 0x1180, -0x3B770 };

		public static int[] Godmode = new int[] { 0x08, 0x189 };

		public static int[] WantedLevel = new int[] { 0x08, 0x10b8, 0x848 };

		public static int[] PlayerX = new int[] { 0x08, 0x90 };
		public static int[] PlayerY = new int[] { 0x08, 0x94 };
		public static int[] PlayerZ = new int[] { 0x08, 0x98 };
		public static int[] PlayerVecX = new int[] { 0x08, 0x30, 0x50 };
		public static int[] PlayerVecY = new int[] { 0x08, 0x30, 0x54 };
		public static int[] PlayerVecZ = new int[] { 0x08, 0x30, 0x58 };

		public static int[] PlayerInCar = new int[] { 0x08, 0xE44 };

		public static int[] CarX = new int[] { 0x08, 0xd28, 0x90 };
		public static int[] CarY = new int[] { 0x08, 0xd28, 0x94 };
		public static int[] CarZ = new int[] { 0x08, 0xd28, 0x98 };
		public static int[] CarVecX = new int[] { 0x08, 0xd28, 0x30, 0x50 };
		public static int[] CarVecY = new int[] { 0x08, 0xd28, 0x30, 0x54 };
		public static int[] CarVecZ = new int[] { 0x08, 0xd28, 0x30, 0x58 };

		public static int[] WeaponDamageType = new int[] { 0x08, 0x10C8, 0x20, 0x20 };
		public static int[] WeaponExplosionType = new int[] { 0x08, 0x10C8, 0x20, 0x24 };

	}

}