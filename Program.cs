using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Windows.Forms;

[assembly: AssemblyTitle("Key Center")]
[assembly: AssemblyDescription("便携式 API Key 保险箱")]
[assembly: AssemblyProduct("Key Center")]
[assembly: AssemblyCopyright("Key Center")]
[assembly: AssemblyVersion("1.8.8.0")]
[assembly: AssemblyFileVersion("1.8.8.0")]

namespace KeyCenter
{
    internal static class Program
    {
        private static Mutex keepAliveMutex;

        [STAThread]
        private static void Main(string[] args)
        {
            string dataPath = null;
            string selfTestLog = null;
            int restartPid = 0;
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == "--data" && i + 1 < args.Length)
                {
                    dataPath = args[++i];
                }
                else if (args[i] == "--restart-helper" && i + 1 < args.Length)
                {
                    int pid;
                    if (int.TryParse(args[++i], out pid)) restartPid = pid;
                }
                else if (args[i] == "--no-glass")
                {
                    AppConfig.GlassDisabled = true;
                }
                else if (args[i] == "--selftest" && i + 1 < args.Length)
                {
                    selfTestLog = args[++i];
                }
            }

            if (selfTestLog != null)
            {
                Environment.ExitCode = SelfTest.Run(selfTestLog);
                return;
            }

            if (dataPath == null || dataPath.Trim().Length == 0)
            {
                dataPath = Path.Combine(Application.StartupPath, "keycenter.dat");
            }
            else if (!Path.IsPathRooted(dataPath))
            {
                dataPath = Path.Combine(Application.StartupPath, dataPath);
            }
            dataPath = Path.GetFullPath(dataPath);

            if (restartPid > 0)
            {
                RestartHelper.WaitAndLaunch(restartPid, dataPath);
                return;
            }

            string mutexName = "KeyCenter_" + HashPath(dataPath);
            bool createdNew = true;
            keepAliveMutex = new Mutex(true, mutexName, out createdNew);
            if (!createdNew)
            {
                ActivateExisting();
                return;
            }

            try
            {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                WheelRouter.Install();
                Application.ThreadException += delegate(object sender, ThreadExceptionEventArgs e)
                {

                    try
                    {
                        InfoDialog.Show(null, L.T("出错了"), e.Exception.Message);
                    }
                    catch (Exception) { }
                };
                Application.Run(new MainForm(dataPath));
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Key Center", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                GC.KeepAlive(keepAliveMutex);
            }
        }

        private static string HashPath(string path)
        {
            try
            {
                using (SHA1 sha = SHA1.Create())
                {
                    byte[] h = sha.ComputeHash(Encoding.UTF8.GetBytes(path.ToLowerInvariant()));
                    StringBuilder sb = new StringBuilder();
                    for (int i = 0; i < h.Length; i++) sb.Append(h[i].ToString("x2"));
                    return sb.ToString();
                }
            }
            catch (Exception)
            {
                return "default";
            }
        }

        private static void ActivateExisting()
        {
            try
            {
                string exeName = Path.GetFileNameWithoutExtension(Application.ExecutablePath);
                Process[] ps = Process.GetProcessesByName(exeName);
                for (int i = 0; i < ps.Length; i++)
                {
                    if (ps[i].MainWindowHandle != IntPtr.Zero)
                    {
                        Native.SetForegroundWindow(ps[i].MainWindowHandle);
                        return;
                    }
                }
            }
            catch (Exception) { }
            MessageBox.Show(L.T("Key Center 已经在运行中。"), "Key Center", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }

    /// <summary>语言切换重启时等待旧实例退出，再直接启动新实例（不经过 cmd.exe）。</summary>
    internal static class RestartHelper
    {
        public static void WaitAndLaunch(int processId, string dataPath)
        {
            try
            {
                Process old = Process.GetProcessById(processId);
                old.WaitForExit(5000);
            }
            catch (Exception) { }

            try
            {
                ProcessStartInfo psi = new ProcessStartInfo(Application.ExecutablePath);
                psi.UseShellExecute = false;
                psi.CreateNoWindow = true;
                psi.WindowStyle = ProcessWindowStyle.Hidden;
                psi.Arguments = "--data \"" + (dataPath ?? "") + "\"";
                Process.Start(psi);
            }
            catch (Exception) { }
        }
    }

    /// <summary>命令行自检：验证加密、篡改检测、文件读写。</summary>
    internal static class SelfTest
    {
        public static int Run(string logPath)
        {
            StringBuilder sb = new StringBuilder();
            string tempDir = null;
            try
            {
                VaultData d = new VaultData();
                d.Categories.Add(L.T("工作"));
                d.Categories.Add(L.T("个人"));
                KeyItem it = new KeyItem();
                it.Id = Vault.NewId();
                it.Name = L.T("测试 Key \"quoted\"");
                it.Category = L.T("工作");
                SubKey sk = new SubKey(); sk.Id = Vault.NewId(); sk.Value = "demo-value-not-a-real-key"; it.Keys.Add(sk); it.DefaultKeyId = sk.Id;
                it.Note = L.T("备注\n第二行 中文");
                it.Created = Vault.NowMs();
                it.Updated = it.Created;
                d.Items.Add(it);

                string pw = L.T("Test#2026 密码");
                byte[] enc = Vault.Encrypt(d, pw);
                VaultData back = Vault.Decrypt(enc, pw);
                Assert(back.Categories.Count == 2, "categories count");
                Assert(back.Items.Count == 1, "items count");
                Assert(back.Items[0].Name == it.Name, "name roundtrip");
                Assert(back.Items[0].DefaultValue == "demo-value-not-a-real-key", "value roundtrip");
                Assert(back.Items[0].Note == it.Note, "note roundtrip");
                Assert(back.Items[0].Category == it.Category, "category roundtrip");
                Assert(back.AutoLockMinutes == 5, "auto lock roundtrip");

                bool wrongRejected = false;
                try { Vault.Decrypt(enc, "bad-password"); }
                catch (WrongPasswordException) { wrongRejected = true; }
                Assert(wrongRejected, "wrong password rejected");

                byte[] tampered = (byte[])enc.Clone();
                tampered[tampered.Length - 40] ^= 0x5A;
                bool tamperRejected = false;
                try { Vault.Decrypt(tampered, pw); }
                catch (WrongPasswordException) { tamperRejected = true; }
                Assert(tamperRejected, "tamper rejected");

                tempDir = Path.Combine(Path.GetTempPath(), "kc-selftest-" + Guid.NewGuid().ToString("N"));
                Directory.CreateDirectory(tempDir);
                string file = Path.Combine(tempDir, "selftest.dat");
                Vault.SaveToFile(file, d, pw);
                VaultData fromFile = Vault.LoadFromFile(file, pw);
                Assert(fromFile.Items[0].DefaultValue == "demo-value-not-a-real-key", "file roundtrip");

                // 模拟保存 fallback 在主文件与临时文件之间被强杀：只剩 .safety。
                string recoverFile = Path.Combine(tempDir, "recover.dat");
                Vault.SaveToFile(recoverFile, d, pw);
                File.Move(recoverFile, recoverFile + ".safety");
                LoadResult recovered = Vault.LoadFromFileWithKeys(recoverFile, pw);
                Assert(recovered.RecoveredFrom == recoverFile + ".safety", "recovery source");
                Assert(File.Exists(recoverFile), "recovery restored primary");
                Assert(recovered.Data.Items.Count == 1, "recovery data");
                recovered.Keys.Clear();

                sb.AppendLine("SELFTEST OK");
                sb.AppendLine("clr=" + Environment.Version.ToString());
                return 0;
            }
            catch (Exception ex)
            {
                sb.AppendLine("SELFTEST FAIL");
                sb.AppendLine(ex.ToString());
                return 1;
            }
            finally
            {
                try
                {
                    if (tempDir != null && Directory.Exists(tempDir)) Directory.Delete(tempDir, true);
                }
                catch (Exception) { }
                try { File.WriteAllText(logPath, sb.ToString(), Encoding.UTF8); }
                catch (Exception) { }
            }
        }

        private static void Assert(bool cond, string name)
        {
            if (!cond) throw new Exception("assert failed: " + name);
        }
    }
}

