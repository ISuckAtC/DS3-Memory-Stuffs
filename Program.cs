using System;
using System.IO;
using System.Text;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace DS3_Mem
{
    class Program
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool ReadProcessMemory(
        IntPtr hProcess,
        IntPtr lpBaseAddress,
        [Out] byte[] lpBuffer,
        int dwSize,
        out IntPtr lpNumberOfBytesRead);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool WriteProcessMemory(
        IntPtr hProcess,
        IntPtr lpBaseAddress,
        byte[] lpBuffer,
        Int32 nSize,
        out IntPtr lpNumberOfBytesWritten);

        static byte[] ReadMem(IntPtr baseAdd, int size)
        {
            byte[] buf = new byte[size];
            IntPtr bRead = new IntPtr();
            ReadProcessMemory(Handle, baseAdd, buf, size, out bRead);
            return buf;
        }

        static bool WriteMem(IntPtr baseAdd, byte[] bytes)
        {
            IntPtr bWrite = new IntPtr();
            return WriteProcessMemory(Handle, baseAdd, bytes, bytes.Length, out bWrite);
        }

        [StructLayout(LayoutKind.Sequential)]
        struct KEYBDINPUT
        {
            public ushort wVk;
            public ushort wScan;
            public uint dwFlags;
            public uint time;
            public long dwExtraInfo;
        }
        [StructLayout(LayoutKind.Sequential)]
        struct MOUSEINPUT
        {
            public int dx, dy;
            public uint mouseData, dwFlags, time;
            public long dwExtraInfo;
        }
        [StructLayout(LayoutKind.Explicit)]
        struct INPUTTYPES
        {
            [FieldOffset(0)]
            public MOUSEINPUT Mouse;
            [FieldOffset(0)]
            public KEYBDINPUT Keyboard;
        }
        [StructLayout(LayoutKind.Sequential)]
        struct INPUT
        {
            public uint type;
            public INPUTTYPES inputData;
        }

        [DllImport("User32.dll", SetLastError = true)]
        static extern uint SendInput(int inputCount, INPUT[] inputs, int size);

        [DllImport("User32.dll", SetLastError = true)]
        static extern short GetKeyState(int nVirtKey);
        [DllImport("User32.dll", SetLastError = true)]
        static extern bool GetCursorPos(long lpPoint);

        public struct POINT
        {
            public int x, y;
        }

        public struct Vector3
        {
            public float X, Z, Y;
            public static float Distance(Vector3 A, Vector3 B)
            {
                float diffX = B.X - A.X;
                float diffZ = B.Z - A.Z;
                float diffY = B.Y - A.Y;
                return MathF.Sqrt((diffX * diffX) + (diffZ * diffZ) + (diffY * diffY));
            }

            public static float Distance2D(Vector3 a, Vector3 b)
            {
                float diffX = b.X - a.X;
                float diffY = b.Y - a.Y;
                float diffZ = 0;
                return MathF.Sqrt((diffX * diffX) + (diffZ * diffZ) + (diffY * diffY));
            }

            public static Vector3 operator -(Vector3 a, Vector3 b) => new Vector3() { X = a.X - b.X, Y = a.Y - b.Y, Z = a.Z - b.Z };
        }

        static void SendKey(ushort key, bool up = false)
        {
            KEYBDINPUT kbi = new KEYBDINPUT();
            kbi.wScan = key;
            kbi.dwFlags = 1 << 3 | (uint)(up ? 1 << 1 : 0);
            INPUT input = new INPUT();
            input.inputData.Keyboard = kbi;
            input.type = 1;
            unsafe
            {
                SendInput(1, new INPUT[] { input }, sizeof(INPUT));
                lastErr = Marshal.GetLastWin32Error();
            }
            //if (lastErr > 0) Console.WriteLine("ERROR: " + lastErr + " | key: " + key);
        }
        static void MoveMouse(int x, int y)
        {
            MOUSEINPUT mi = new MOUSEINPUT();
            mi.dx = x;
            mi.dy = y;
            mi.dwFlags = 1;
            INPUT input = new INPUT();
            input.type = 0;
            input.inputData.Mouse = mi;

            unsafe
            {
                SendInput(1, new INPUT[] { input }, sizeof(INPUT));
                lastErr = Marshal.GetLastWin32Error();
            }
            if (lastErr > 0) Console.WriteLine("ERROR: " + lastErr);
        }

        static public IntPtr BaseDS3;
        static public IntPtr Handle;
        static public IntPtr BaseA;
        static public IntPtr BaseB;
        static public IntPtr BaseC;
        static public IntPtr BaseD;
        static public IntPtr BaseE;
        static public IntPtr BaseF;
        static public IntPtr BaseZ;
        static public IntPtr Param;
        static public IntPtr GameFlagData;
        static public IntPtr LockBonus_ptr;
        static public IntPtr DrawNearOnly_ptr;
        static public IntPtr debug_flags;


        static public int lastErr;

        static IntPtr PointerOffset(IntPtr ptr, long[] offsets)
        {

            foreach (long offset in offsets)
            {
                ptr = new IntPtr(BitConverter.ToInt64(ReadMem(ptr, 8)) + offset);
            }
            return ptr;
        }

        static byte[] Snap(byte[] b)
        {
            byte[] o = new byte[b.Length / 2];
            for (int i = 0; i < b.Length; i += 2) o[i / 2] = b[i];
            return o;
        }

        static IntPtr AnimPtr;
        static IntPtr otherPosPtr;
        static IntPtr myPosPtr;
        static IntPtr myRotationPtr;
        static int Anim;
        static int lastAnim;
        static Vector3 otherPosition;
        static Vector3 myPosition;

        static POINT mousePos;
        static bool holding;

        static bool attacking;
        static bool stun;

        static float distance;

        static bool holdFrayedWolf;

        static int wayPointCounter;


        static float desiredDistance = 3.2f;
        static float leeway = 0.5f;
        static float desiredRotation = 0f;
        static float rotationLeeway = 0.1f;

        static void Main(string[] args)
        {
            Process ds3 = Process.GetProcessesByName("DarkSoulsIII")[0];
            SetBases(ds3);

            AnimPtr = PointerOffset(BaseB, new long[] { 0x40, 0x38, 0x1F90, 0x80, 0xc8 });
            otherPosPtr = PointerOffset(BaseB, new long[] { 0x40, 0x38, 0x18, 0x28, 0x80 });
            myPosPtr = PointerOffset(BaseB, new long[] { 0x40, 0x28, 0x80 });
            myRotationPtr = PointerOffset(BaseB, new long[] { 0x40, 0x28, 0x74 });

            /*
            GoToFuckingPontiff(new Vector3[] { 
                new Vector3() { X = 486f, Y = -1160f, Z = 0f },
                new Vector3() { X = 483f, Y = -1160f, Z = 0f },
                new Vector3() { X = 452f, Y = -1189f, Z = 0f },
                new Vector3() { X = 452f, Y = -1203f, Z = 0f },
                new Vector3() { X = 436f, Y = -1203f, Z = 0f },
                new Vector3() { X = 438f, Y = -1219f, Z = 0f },
                new Vector3() { X = 418f, Y = -1234f, Z = 0f },
                new Vector3() { X = 400f, Y = -1260f, Z = 0f }
                });
            */

            int loops = 0;
            Thread d = new Thread(() => WatchMemory(ref loops));
            //d.Start();


            //Thread b = new Thread(PKDitto);
            //b.Start();

            Thread a = new Thread(AnimationWatcher);
            a.Start();
            //Task.Run(() => Ticker(16));
            //Task.Run(() => UpdateValues());

            Console.WriteLine("Going to sleep");

            while(GetKeyState(0x52) >= 0);
            
            Console.WriteLine(loops);

            //Thread.Sleep(-1);
        }

        public static void WatchMemory(ref int loops)
        {
            while(true)
            {
                loops++;
                Anim = BitConverter.ToInt32(ReadMem(AnimPtr, 4));
                otherPosition.X = BitConverter.ToSingle(ReadMem(otherPosPtr, 4));
                otherPosition.Z = BitConverter.ToSingle(ReadMem(otherPosPtr + 4, 4));
                otherPosition.Y = BitConverter.ToSingle(ReadMem(otherPosPtr + 8, 4));
                myPosition.X = BitConverter.ToSingle(ReadMem(myPosPtr, 4));
                myPosition.Z = BitConverter.ToSingle(ReadMem(myPosPtr + 4, 4));
                myPosition.Y = BitConverter.ToSingle(ReadMem(myPosPtr + 8, 4));
            }
        }

        public static async void PKDitto()
        {
            unsafe
            {
                Thread animationWatcher = new Thread(() =>
                {
                    while (true)
                    {
                        Anim = BitConverter.ToInt32(ReadMem(AnimPtr, 4));
                        if (Anim == 1)
                        {
                            stun = true;
                            continue;
                        }
                        if (Anim == 27115)
                        {
                            if (distance > desiredDistance) break;
                            Thread.Sleep(20);
                            SendKey(0x2a);
                            Thread.Sleep(50);
                            SendKey(0x2a, true);
                            Thread.Sleep(1000);
                        }
                        if (Anim == 164034000)
                        {
                            // PK R1
                        }
                        stun = false;
                    }
                });
                Thread signer = new Thread(() =>
                {
                    while (true)
                    {
                        SendKey(0x40);
                        Thread.Sleep(500);
                        SendKey(0x40, true);
                        Thread.Sleep(29500);
                        AnimPtr = PointerOffset(BaseB, new long[] { 0x40, 0x38, 0x1F90, 0x80, 0xc8 });
                        otherPosPtr = PointerOffset(BaseB, new long[] { 0x40, 0x38, 0x18, 0x28, 0x80 });
                    }
                });
                Thread positionWatcher = new Thread(() =>
                {
                    while (true)
                    {
                        otherPosition.X = BitConverter.ToSingle(ReadMem(otherPosPtr, 4));
                        otherPosition.Z = BitConverter.ToSingle(ReadMem(otherPosPtr + 4, 4));
                        otherPosition.Y = BitConverter.ToSingle(ReadMem(otherPosPtr + 8, 4));
                        myPosition.X = BitConverter.ToSingle(ReadMem(myPosPtr, 4));
                        myPosition.Z = BitConverter.ToSingle(ReadMem(myPosPtr + 4, 4));
                        myPosition.Y = BitConverter.ToSingle(ReadMem(myPosPtr + 8, 4));

                        if (otherPosition.X == 0 && otherPosition.Y == 0)
                        {
                            SendKey(0x1f, true);
                            SendKey(0x11, true);
                            Thread.Sleep(50);
                            continue;
                        }

                        distance = Vector3.Distance(otherPosition, myPosition);
                        if (distance > desiredDistance + 4)
                        {
                            SendKey(0x18, true);
                        }
                        else
                        {
                            SendKey(0x18);

                            if (stun)
                            {
                                SendKey(0x11);
                                SendKey(0x1f, true);
                                continue;
                            }
                        }

                        if (!stun && distance < desiredDistance)
                        {
                            R1R1PKCS();
                            continue;
                        }

                        if (distance < desiredDistance - leeway)
                        {
                            SendKey(0x1f);
                            SendKey(0x11, true);
                        }
                        else if (distance > desiredDistance + leeway)
                        {
                            SendKey(0x11);
                            SendKey(0x1f, true);
                        }
                        else
                        {
                            SendKey(0x1f, true);
                            SendKey(0x11, true);
                        }
                        Thread.Sleep(50);
                    }
                });

                animationWatcher.Start();
                positionWatcher.Start();
                signer.Start();
            }
        }

        public static void GoToFuckingPontiff(Vector3[] waypoints)
        {
            unsafe
            {
                bool goPontiff = true;
                POINT pos = new POINT();
                GetCursorPos((long)(&pos));
                mousePos = pos;
                int posX = mousePos.x;
                float rotation = 0;
                Thread checkDesiredRotation = new Thread(() =>
                {
                    while (goPontiff && GetKeyState(0x52) >= 0)
                    {
                        Vector3 vectorDir = myPosition - waypoints[wayPointCounter];
                        distance = Vector3.Distance2D(myPosition, waypoints[wayPointCounter]);
                        desiredRotation = MathF.Atan2(vectorDir.X, vectorDir.Y);
                        Console.WriteLine(vectorDir.X + ", " + vectorDir.Y + " | " + rotation + "/" + desiredRotation + " | " + distance);
                        Thread.Sleep(300);
                    }
                });
                Thread rotate = new Thread(() =>
                {
                    while (goPontiff && GetKeyState(0x52) >= 0)
                    {
                        bool reverse = Math.Abs(rotation - desiredRotation) > MathF.PI;
                        rotation = BitConverter.ToSingle(ReadMem(myRotationPtr, 4));
                        if (rotation > desiredRotation + rotationLeeway)
                        {
                            MoveMouse(reverse ? 30 : -30, 0);
                        }
                        else if (rotation < desiredRotation - rotationLeeway)
                        {
                            MoveMouse(reverse ? -30 : 30, 0);
                        }
                        Thread.Sleep(20);
                    }
                });
                Thread positionWatcher = new Thread(() =>
                {
                    while (goPontiff && true)
                    {
                        //otherPosition.X = BitConverter.ToSingle(ReadMem(otherPosPtr, 4));
                        //otherPosition.Z = BitConverter.ToSingle(ReadMem(otherPosPtr + 4, 4));
                        //otherPosition.Y = BitConverter.ToSingle(ReadMem(otherPosPtr + 8, 4));
                        myPosition.X = BitConverter.ToSingle(ReadMem(myPosPtr, 4));
                        myPosition.Z = BitConverter.ToSingle(ReadMem(myPosPtr + 4, 4));
                        myPosition.Y = BitConverter.ToSingle(ReadMem(myPosPtr + 8, 4));

                        Thread.Sleep(50);
                    }
                });
                checkDesiredRotation.Start();
                rotate.Start();
                positionWatcher.Start();
                SendKey(0x11);
                Thread.Sleep(500);
                while (true)
                {
                    if (distance < desiredDistance)
                    {
                        if (wayPointCounter + 1 < waypoints.Length)
                        {
                            Console.WriteLine("Going to next waypoint");
                            Vector3 vectorDir = myPosition - waypoints[wayPointCounter];
                            distance = Vector3.Distance2D(myPosition, waypoints[wayPointCounter]);
                            desiredRotation = MathF.Atan2(vectorDir.X, vectorDir.Y);
                            wayPointCounter++;
                            Thread.Sleep(2500);
                        } 
                        else 
                        {
                            Console.WriteLine("I am at pontiff");
                            break;
                        }
                    }
                }

                SendKey(0x11, true);
                goPontiff = false;
            }
        }

        public static void R1R1PKCS() // Assumes blocking
        {
            SendKey(0x2a);
            Thread.Sleep(500);
            SendKey(0x2a, true);
            if (!stun) return;
            Thread.Sleep(100);
            SendKey(0x2a);
        }

        public static void AnimationWatcher()
        {
            while (true)
            {
                Anim = BitConverter.ToInt32(ReadMem(AnimPtr, 4));
                if (Anim == 6000)
                {
                    SendKey(0x2a);
                    Thread.Sleep(50);
                    SendKey(0x2a, true);

                }
                if (!holding && (
                    Anim == 20034000 ||
                    Anim == 151034000 ||
                    Anim == 23030000 ||
                    Anim == 29034500 ||
                    Anim == 29034000) ||
                    Anim == 29030000)
                {
                    holding = true;
                    Console.WriteLine("REEE");
                    SendKey(0x19);
                    /*
                    SendKey(0x2e); // C (menu)

                    await Task.Delay(17);

                    SendKey(0x2e, true);

                    SendKey(0x12); // E (select)

                    await Task.Delay(17);

                    SendKey(0x13);

                    await Task.Delay(17);
                    SendKey(0x19);
                    SendKey(0x2e); // C (menu)
                    */

                    Thread.Sleep(20);

                    SendKey(0x19, true);

                    //Anim = BitConverter.ToInt32(ReadMem(AnimPtr, 4));

                    SendKey(0x13, true);
                    SendKey(0x2e, true);
                    SendKey(0x12, true);
                    SendKey(0x05, true);
                    SendKey(0x19, true);
                    SendKey(0x10, true);
                }
            }
        }

        public static async void Ticker(int interval)
        {
            Console.WriteLine("Watching...");
            while (true)
            {
                Tick();
                await Task.Delay(interval);
            }
        }

        public static async void Tick()
        {
            Anim = BitConverter.ToInt32(ReadMem(AnimPtr, 4));
            /*float distance = Vector3.Distance(myPosition, otherPosition);
            //Console.WriteLine("\n(" + myPosition.X + ", " + myPosition.Z + ", " + myPosition.Y + ")");
            //Console.WriteLine("(" + otherPosition.X + ", " + otherPosition.Z + ", " + otherPosition.Y + ")");
            
            if (distance < desiredDistance - leeway)
            {
                SendKey(0x1f);
                SendKey(0x11, true);
            } 
            else if (distance > desiredDistance + leeway)
            {
                SendKey(0x11);
                SendKey(0x1f, true);
            }
            else
            {
                SendKey(0x1f, true);
                SendKey(0x11, true);
            }


            return;
            */

            if (GetKeyState(0x52) < 0)
            {
                holding = false;
            }
            else holding = true;
            if (GetKeyState(0x4D) < 0)
            {
                holdFrayedWolf = true;
                (new Thread(FrayedWolf)).Start();
            }

            if (lastAnim != Anim)
            {
                Console.WriteLine(Anim);
            }
            lastAnim = Anim;

            // Friede R1 - 232034000
            // SS R1 - 23030000
            // Warpick R1 - 33034000
            // Gael R1 - 25034000
            // Demon scar R1 - 257034000
            // dagger 2hR1 - 20034000
            // cs 2hR1 - 151034000
        }

        public static void FrayedWolf()
        {
            SendKey(0x19); // P
            Thread.Sleep(60);
            SendKey(0x1D); // LCtrl
            SendKey(0x19, true);
            Thread.Sleep(1900);

            SendKey(0x2e); // C (menu)

            Thread.Sleep(50);
            SendKey(0x1D, true); // LCtrl

            SendKey(0x2e, true); // C (menu)

            SendKey(0x12); // E (select)

            Thread.Sleep(50);

            SendKey(0x12, true); // E (select)

            Thread.Sleep(40);

            SendKey(0x12); // E (select)

            Thread.Sleep(50);
            SendKey(0x19);

            SendKey(0x05); // 4 (up)
            SendKey(0x12, true); // E (select)
            Thread.Sleep(50);
            SendKey(0x12); // E (select)
            Thread.Sleep(50);
            SendKey(0x05, true); // 4 (up)
            SendKey(0x2e); // C (menu)

            Thread.Sleep(100);
            SendKey(0x1D); // LCtrl
            Thread.Sleep(100);

            SendKey(0x1D, true);
            SendKey(0x2e, true);
            SendKey(0x12, true);
            SendKey(0x19, true);

            holdFrayedWolf = false;
            Thread.Yield();
        }

        static void SetBases(Process ds3)
        {
            Handle = ds3.Handle;
            BaseDS3 = ds3.MainModule.BaseAddress;
            BaseA = new IntPtr(BaseDS3.ToInt64() + long.Parse("4740178", System.Globalization.NumberStyles.HexNumber));
            BaseB = new IntPtr(BaseDS3.ToInt64() + long.Parse("4768E78", System.Globalization.NumberStyles.HexNumber));
            BaseC = new IntPtr(BaseDS3.ToInt64() + long.Parse("4743AB0", System.Globalization.NumberStyles.HexNumber));
            BaseD = new IntPtr(BaseDS3.ToInt64() + long.Parse("4743A80", System.Globalization.NumberStyles.HexNumber));
            BaseE = new IntPtr(BaseDS3.ToInt64() + long.Parse("473FD08", System.Globalization.NumberStyles.HexNumber));
            BaseF = new IntPtr(BaseDS3.ToInt64() + long.Parse("473AD78", System.Globalization.NumberStyles.HexNumber));
            BaseZ = new IntPtr(BaseDS3.ToInt64() + long.Parse("4768F98", System.Globalization.NumberStyles.HexNumber));
            Param = new IntPtr(BaseDS3.ToInt64() + long.Parse("4782838", System.Globalization.NumberStyles.HexNumber));
            GameFlagData = new IntPtr(BaseDS3.ToInt64() + long.Parse("473BE28", System.Globalization.NumberStyles.HexNumber));
            LockBonus_ptr = new IntPtr(BaseDS3.ToInt64() + long.Parse("4766CA0", System.Globalization.NumberStyles.HexNumber));
            DrawNearOnly_ptr = new IntPtr(BaseDS3.ToInt64() + long.Parse("4766555", System.Globalization.NumberStyles.HexNumber));
            debug_flags = new IntPtr(BaseDS3.ToInt64() + long.Parse("4768F68", System.Globalization.NumberStyles.HexNumber));
        }

    }
}
