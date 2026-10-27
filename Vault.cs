using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Security.Cryptography;
using System.Text;

namespace KeyCenter
{
    /// <summary>条目下的一条 Key（可有自定义备注名）。</summary>
    [DataContract]
    public class SubKey
    {
        [DataMember(Name = "id", Order = 0)]
        public string Id;

        [DataMember(Name = "label", Order = 1)]
        public string Label;

        [DataMember(Name = "value", Order = 2)]
        public string Value;
    }

    [DataContract]
    public class KeyItem
    {
        [DataMember(Name = "id", Order = 0)]
        public string Id;

        [DataMember(Name = "name", Order = 1)]
        public string Name;

        [DataMember(Name = "category", Order = 2)]
        public string Category;

        // 旧版单 Key 字段，仅用于迁移兼容
        [DataMember(Name = "value", Order = 3)]
        public string Value;

        [DataMember(Name = "note", Order = 4)]
        public string Note;

        [DataMember(Name = "created", Order = 5)]
        public long Created;

        [DataMember(Name = "updated", Order = 6)]
        public long Updated;

        [DataMember(Name = "keys", Order = 7)]
        public List<SubKey> Keys;

        [DataMember(Name = "defaultKeyId", Order = 8)]
        public string DefaultKeyId;

        public KeyItem()
        {
            Keys = new List<SubKey>();
            DefaultKeyId = "";
        }

        public SubKey GetDefaultKey()
        {
            if (Keys == null || Keys.Count == 0) return null;
            if (!string.IsNullOrEmpty(DefaultKeyId))
            {
                for (int i = 0; i < Keys.Count; i++)
                {
                    if (Keys[i] != null && Keys[i].Id == DefaultKeyId) return Keys[i];
                }
            }
            for (int i = 0; i < Keys.Count; i++)
            {
                if (Keys[i] != null) return Keys[i];
            }
            return null;
        }

        public SubKey FindKey(string keyId)
        {
            if (Keys == null || keyId == null) return null;
            for (int i = 0; i < Keys.Count; i++)
            {
                if (Keys[i] != null && Keys[i].Id == keyId) return Keys[i];
            }
            return null;
        }

        public void NormalizeKeys()
        {
            if (Keys == null) Keys = new List<SubKey>();
            // 旧数据迁移：把单值升级为第一条 Key
            if (Keys.Count == 0 && !string.IsNullOrEmpty(Value))
            {
                SubKey migrated = new SubKey();
                migrated.Id = Vault.NewId();
                migrated.Label = "";
                migrated.Value = Value;
                Keys.Add(migrated);
                DefaultKeyId = migrated.Id;
            }
            Value = "";
            for (int i = Keys.Count - 1; i >= 0; i--)
            {
                SubKey k = Keys[i];
                if (k == null)
                {
                    Keys.RemoveAt(i);
                    continue;
                }
                if (string.IsNullOrEmpty(k.Id)) k.Id = Vault.NewId();
                if (k.Label == null) k.Label = "";
                if (k.Value == null) k.Value = "";
            }
            if (Keys.Count > 0)
            {
                if (string.IsNullOrEmpty(DefaultKeyId) || FindKey(DefaultKeyId) == null)
                {
                    DefaultKeyId = Keys[0].Id;
                }
            }
            else
            {
                DefaultKeyId = "";
            }
        }

        public string DefaultValue
        {
            get
            {
                SubKey k = GetDefaultKey();
                return k == null ? "" : k.Value;
            }
        }
    }

    [DataContract]
    public class VaultData
    {
        [DataMember(Name = "version", Order = 0)]
        public int Version;

        [DataMember(Name = "categories", Order = 1)]
        public List<string> Categories;

        [DataMember(Name = "items", Order = 2)]
        public List<KeyItem> Items;

        [DataMember(Name = "autoLockMinutes", Order = 3)]
        public int AutoLockMinutes;

        public VaultData()
        {
            Version = 2;
            Categories = new List<string>();
            Items = new List<KeyItem>();
            AutoLockMinutes = 5;
        }

        public void Normalize()
        {
            if (Version < 2) Version = 2;
            if (Categories == null) Categories = new List<string>();
            if (Items == null) Items = new List<KeyItem>();
            List<string> cleanCats = new List<string>();
            for (int i = 0; i < Categories.Count; i++)
            {
                string c = Categories[i];
                if (c == null) continue;
                c = c.Trim();
                if (c.Length == 0) continue;
                if (!cleanCats.Contains(c)) cleanCats.Add(c);
            }
            Categories = cleanCats;
            for (int i = 0; i < Items.Count; i++)
            {
                KeyItem it = Items[i];
                if (it == null)
                {
                    Items.RemoveAt(i);
                    i--;
                    continue;
                }
                if (string.IsNullOrEmpty(it.Id)) it.Id = Vault.NewId();
                if (it.Name == null) it.Name = "";
                if (it.Category == null) it.Category = "";
                if (it.Value == null) it.Value = "";
                if (it.Note == null) it.Note = "";
                it.NormalizeKeys();
            }
            if (AutoLockMinutes < 0) AutoLockMinutes = 0;
        }
    }

    internal class WrongPasswordException : Exception
    {
        public WrongPasswordException() : base(L.T("密码错误或数据文件已损坏")) { }
    }

    /// <summary>会话内缓存的派生密钥：只在解锁/改密时做一次 PBKDF2，保存时直接复用。</summary>
    public sealed class KeyMaterial
    {
        public byte[] Salt;
        public byte[] EncKey;
        public byte[] MacKey;

        public void Clear()
        {
            if (EncKey != null) { Array.Clear(EncKey, 0, EncKey.Length); EncKey = null; }
            if (MacKey != null) { Array.Clear(MacKey, 0, MacKey.Length); MacKey = null; }
            if (Salt != null) { Salt = null; }
        }

        /// <summary>为后台保存任务复制一份独立密钥，避免任务期间锁定/改密清零影响写入。</summary>
        public KeyMaterial Clone()
        {
            if (EncKey == null || MacKey == null || Salt == null) throw new InvalidOperationException(L.T("会话密钥已失效"));
            KeyMaterial k = new KeyMaterial();
            k.Salt = (byte[])Salt.Clone();
            k.EncKey = (byte[])EncKey.Clone();
            k.MacKey = (byte[])MacKey.Clone();
            return k;
        }
    }

    public sealed class LoadResult
    {
        public VaultData Data;
        public KeyMaterial Keys;
    }

    internal static class Vault
    {
        private const int Iterations = 210000;
        private static readonly byte[] Magic = new byte[] { 0x4B, 0x43, 0x54, 0x31 }; // "KCT1"
        private const int SaltLen = 16;
        private const int IvLen = 16;
        private const int MacLen = 32;
        private const int HeaderLen = 4 + SaltLen + IvLen + 4;

        public static string NewId()
        {
            return Guid.NewGuid().ToString("N");
        }

        public static long NowMs()
        {
            DateTime epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            return (long)(DateTime.UtcNow - epoch).TotalMilliseconds;
        }

        private static byte[] RandomBytes(int len)
        {
            byte[] b = new byte[len];
            using (RandomNumberGenerator rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(b);
            }
            return b;
        }

        private static byte[] DeriveKey(string password, byte[] salt, int len)
        {
            // PBKDF2-HMAC-SHA256
            using (Rfc2898DeriveBytes kdf = new Rfc2898DeriveBytes(password, salt, Iterations, HashAlgorithmName.SHA256))
            {
                return kdf.GetBytes(len);
            }
        }

        public static KeyMaterial CreateMaterial(string password)
        {
            byte[] salt = RandomBytes(SaltLen);
            return DeriveMaterial(password, salt);
        }

        public static KeyMaterial DeriveMaterial(string password, byte[] salt)
        {
            byte[] material = DeriveKey(password, salt, 64);
            KeyMaterial km = new KeyMaterial();
            km.Salt = (byte[])salt.Clone();
            km.EncKey = new byte[32];
            km.MacKey = new byte[32];
            Buffer.BlockCopy(material, 0, km.EncKey, 0, 32);
            Buffer.BlockCopy(material, 32, km.MacKey, 0, 32);
            Array.Clear(material, 0, material.Length);
            return km;
        }

        public static byte[] Encrypt(VaultData data, KeyMaterial km)
        {
            if (data == null) throw new ArgumentNullException("data");
            if (km == null || km.EncKey == null || km.MacKey == null || km.Salt == null) throw new ArgumentNullException("km");

            byte[] plain = SerializeJson(data);
            if (plain.Length > MaxFileBytes)
            {
                Array.Clear(plain, 0, plain.Length);
                throw new InvalidDataException(L.T("数据过大，已取消保存"));
            }
            byte[] iv = RandomBytes(IvLen);
            byte[] cipher;
            using (Aes aes = Aes.Create())
            {
                aes.KeySize = 256;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;
                aes.Key = km.EncKey;
                aes.IV = iv;
                using (ICryptoTransform enc = aes.CreateEncryptor())
                {
                    cipher = enc.TransformFinalBlock(plain, 0, plain.Length);
                }
            }
            Array.Clear(plain, 0, plain.Length);

            using (MemoryStream ms = new MemoryStream())
            {
                using (BinaryWriter w = new BinaryWriter(ms, Encoding.UTF8, true))
                {
                    w.Write(Magic);
                    w.Write(km.Salt);
                    w.Write(iv);
                    w.Write(cipher.Length);
                    w.Write(cipher);
                }
                byte[] head = ms.ToArray();
                byte[] mac;
                using (HMACSHA256 h = new HMACSHA256(km.MacKey))
                {
                    mac = h.ComputeHash(head);
                }
                byte[] result = new byte[head.Length + MacLen];
                Buffer.BlockCopy(head, 0, result, 0, head.Length);
                Buffer.BlockCopy(mac, 0, result, head.Length, MacLen);
                return result;
            }
        }

        public static VaultData Decrypt(byte[] file, KeyMaterial km)
        {
            if (file == null || file.Length < HeaderLen + MacLen)
                throw new InvalidDataException(L.T("数据文件不完整或已损坏"));
            if (file.Length > MaxFileBytes) throw new InvalidDataException(L.T("数据文件过大，已拒绝读取"));
            for (int i = 0; i < Magic.Length; i++)
            {
                if (file[i] != Magic[i]) throw new InvalidDataException(L.T("不是 Key Center 数据文件"));
            }
            byte[] iv = new byte[IvLen];
            Buffer.BlockCopy(file, 4 + SaltLen, iv, 0, IvLen);
            int cipherLen = BitConverter.ToInt32(file, 4 + SaltLen + IvLen);
            if (cipherLen < 0 || HeaderLen + cipherLen + MacLen != file.Length)
                throw new InvalidDataException(L.T("数据文件不完整或已损坏"));

            byte[] head = new byte[HeaderLen + cipherLen];
            Buffer.BlockCopy(file, 0, head, 0, head.Length);
            byte[] mac = new byte[MacLen];
            Buffer.BlockCopy(file, head.Length, mac, 0, MacLen);
            byte[] calc;
            using (HMACSHA256 h = new HMACSHA256(km.MacKey))
            {
                calc = h.ComputeHash(head);
            }
            if (!FixedEquals(calc, mac)) throw new WrongPasswordException();

            byte[] cipher = new byte[cipherLen];
            Buffer.BlockCopy(file, HeaderLen, cipher, 0, cipherLen);
            byte[] plain;
            try
            {
                using (Aes aes = Aes.Create())
                {
                    aes.KeySize = 256;
                    aes.Mode = CipherMode.CBC;
                    aes.Padding = PaddingMode.PKCS7;
                    aes.Key = km.EncKey;
                    aes.IV = iv;
                    using (ICryptoTransform dec = aes.CreateDecryptor())
                    {
                        plain = dec.TransformFinalBlock(cipher, 0, cipher.Length);
                    }
                }
            }
            catch (CryptographicException)
            {
                throw new WrongPasswordException();
            }
            try { return DeserializeJson(plain); }
            finally { Array.Clear(plain, 0, plain.Length); }
        }

        public static LoadResult LoadFromFileWithKeys(string path, string password)
        {
            byte[] bytes = ReadFileChecked(path);
            if (bytes.Length < HeaderLen + MacLen) throw new InvalidDataException(L.T("数据文件不完整或已损坏"));
            byte[] salt = new byte[SaltLen];
            Buffer.BlockCopy(bytes, 4, salt, 0, SaltLen);
            KeyMaterial km = DeriveMaterial(password, salt);
            LoadResult r = new LoadResult();
            r.Keys = km;
            try
            {
                r.Data = Decrypt(bytes, km);
            }
            catch (Exception)
            {
                km.Clear();
                throw;
            }
            return r;
        }

        private const long MaxFileBytes = 64L * 1024 * 1024;

        private static byte[] ReadFileChecked(string path)
        {
            FileInfo fi = new FileInfo(path);
            if (!fi.Exists) throw new FileNotFoundException(L.T("数据文件不存在"), path);
            if (fi.Length > MaxFileBytes) throw new InvalidDataException(L.T("数据文件过大，已拒绝读取"));
            return File.ReadAllBytes(path);
        }

        public static void SaveToFile(string path, VaultData data, KeyMaterial km)
        {
            WriteAtomic(path, Encrypt(data, km));
        }

        private static void WriteAtomic(string path, byte[] bytes)
        {
            if (bytes == null || bytes.Length == 0) throw new InvalidDataException(L.T("待写入数据为空"));
            if (bytes.Length > MaxFileBytes) throw new InvalidDataException(L.T("数据文件过大，已取消保存"));
            string dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir)) Directory.CreateDirectory(dir);
            if (Directory.Exists(path)) throw new IOException(L.T("数据文件路径被目录占用，无法保存"));
            string tmp = path + ".tmp";
            string bak = path + ".bak";
            string bakNew = path + ".bak.new";
            string safety = path + ".safety";
            bool committed = false;
            bool bakRotated = false;
            try
            {
                using (FileStream fs = new FileStream(tmp, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    fs.Write(bytes, 0, bytes.Length);
                    fs.Flush(true);
                }
                if (!File.Exists(path))
                {
                    File.Move(tmp, path);
                    committed = true;
                    return;
                }
                bool replaced = false;
                string replaceError = null;
                try
                {
                    if (File.Exists(bakNew)) File.Delete(bakNew);
                    File.Replace(tmp, path, bakNew);
                    replaced = true;
                }
                catch (Exception ex) { replaceError = ex.Message; }
                if (replaced)
                {
                    string bakPrev = bak + ".prev";
                    try
                    {
                        if (File.Exists(bak))
                        {
                            try { if (File.Exists(bakPrev)) File.Delete(bakPrev); }
                            catch (Exception) { }
                            File.Move(bak, bakPrev);
                        }
                        File.Move(bakNew, bak);
                        bakRotated = true;
                        try { if (File.Exists(bakPrev)) File.Delete(bakPrev); }
                        catch (Exception) { }
                    }
                    catch (Exception)
                    {
                        // 轮换失败时恢复旧 bak，并保留 bak.new 作为可恢复副本
                        try
                        {
                            if (!File.Exists(bak) && File.Exists(bakPrev)) File.Move(bakPrev, bak);
                        }
                        catch (Exception) { }
                    }
                    committed = true;
                    return;
                }
                // Replace 不可用/失败：改用“重命名旧文件→重命名新文件”的安全 fallback。
                // 不覆盖写正式文件；失败时原文件仍在 .safety，可恢复。
                try { if (File.Exists(safety)) File.Delete(safety); }
                catch (Exception) { }
                bool oldMoved = false;
                try
                {
                    File.Move(path, safety);
                    oldMoved = true;
                    File.Move(tmp, path);
                    committed = true;
                }
                catch (Exception ex)
                {
                    string restoreError = null;
                    if (oldMoved)
                    {
                        try
                        {
                            if (!File.Exists(path)) File.Move(safety, path);
                        }
                        catch (Exception rex) { restoreError = rex.Message; }
                    }
                    if (restoreError != null)
                        throw new IOException(L.T("保存失败且原文件恢复失败；原文件保留在 ") + safety + L.T("。Replace 错误：") + (replaceError ?? "-") + L.T("；原因：") + ex.Message + L.T("；恢复错误：") + restoreError, ex);
                    throw new IOException(L.T("保存失败，已保留/恢复原文件。Replace 错误：") + (replaceError ?? "-") + L.T("；原因：") + ex.Message, ex);
                }
                try
                {
                    if (File.Exists(bak)) File.Delete(bak);
                    File.Move(safety, bak);
                    bakRotated = true;
                }
                catch (Exception)
                {
                    // 保留 safety 作为可恢复副本
                }
            }
            finally
            {
                try { if (File.Exists(tmp)) File.Delete(tmp); }
                catch (Exception) { }
                if (committed && bakRotated)
                {
                    try { if (File.Exists(bakNew)) File.Delete(bakNew); }
                    catch (Exception) { }
                }
            }
        }

        private static byte[] SerializeJson(VaultData data)
        {
            DataContractJsonSerializer ser = new DataContractJsonSerializer(typeof(VaultData));
            using (MemoryStream ms = new MemoryStream())
            {
                ser.WriteObject(ms, data);
                return ms.ToArray();
            }
        }

        private static VaultData DeserializeJson(byte[] json)
        {
            try
            {
                DataContractJsonSerializer ser = new DataContractJsonSerializer(typeof(VaultData));
                using (MemoryStream ms = new MemoryStream(json))
                {
                    VaultData d = (VaultData)ser.ReadObject(ms);
                    if (d == null) throw new InvalidDataException(L.T("数据内容为空"));
                    if (d.Version > 2) throw new InvalidDataException(L.T("数据版本比当前程序新，请升级 Key Center 后再打开"));
                    d.Normalize();
                    return d;
                }
            }
            catch (InvalidDataException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new InvalidDataException(L.T("数据内容无法解析：") + ex.Message, ex);
            }
        }

        public static byte[] Encrypt(VaultData data, string password)
        {
            if (data == null) throw new ArgumentNullException("data");
            if (string.IsNullOrEmpty(password)) throw new ArgumentException(L.T("密码不能为空"), "password");

            byte[] plain = SerializeJson(data);
            byte[] salt = RandomBytes(SaltLen);
            byte[] iv = RandomBytes(IvLen);
            byte[] material = DeriveKey(password, salt, 64);
            byte[] encKey = new byte[32];
            byte[] macKey = new byte[32];
            Buffer.BlockCopy(material, 0, encKey, 0, 32);
            Buffer.BlockCopy(material, 32, macKey, 0, 32);
            Array.Clear(material, 0, material.Length);

            byte[] cipher;
            using (Aes aes = Aes.Create())
            {
                aes.KeySize = 256;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;
                aes.Key = encKey;
                aes.IV = iv;
                using (ICryptoTransform enc = aes.CreateEncryptor())
                {
                    cipher = enc.TransformFinalBlock(plain, 0, plain.Length);
                }
            }
            Array.Clear(plain, 0, plain.Length);
            Array.Clear(encKey, 0, encKey.Length);

            using (MemoryStream ms = new MemoryStream())
            {
                using (BinaryWriter w = new BinaryWriter(ms, Encoding.UTF8, true))
                {
                    w.Write(Magic);
                    w.Write(salt);
                    w.Write(iv);
                    w.Write(cipher.Length);
                    w.Write(cipher);
                }
                byte[] head = ms.ToArray();
                byte[] mac;
                using (HMACSHA256 h = new HMACSHA256(macKey))
                {
                    mac = h.ComputeHash(head);
                }
                byte[] result = new byte[head.Length + MacLen];
                Buffer.BlockCopy(head, 0, result, 0, head.Length);
                Buffer.BlockCopy(mac, 0, result, head.Length, MacLen);
                Array.Clear(macKey, 0, macKey.Length);
                return result;
            }
        }

        public static VaultData Decrypt(byte[] file, string password)
        {
            if (file == null || file.Length < HeaderLen + MacLen)
                throw new InvalidDataException(L.T("数据文件不完整或已损坏"));
            for (int i = 0; i < Magic.Length; i++)
            {
                if (file[i] != Magic[i]) throw new InvalidDataException(L.T("不是 Key Center 数据文件"));
            }
            byte[] salt = new byte[SaltLen];
            byte[] iv = new byte[IvLen];
            Buffer.BlockCopy(file, 4, salt, 0, SaltLen);
            Buffer.BlockCopy(file, 4 + SaltLen, iv, 0, IvLen);
            int cipherLen = BitConverter.ToInt32(file, 4 + SaltLen + IvLen);
            if (cipherLen < 0 || HeaderLen + cipherLen + MacLen != file.Length)
                throw new InvalidDataException(L.T("数据文件不完整或已损坏"));

            byte[] material = DeriveKey(password, salt, 64);
            byte[] encKey = new byte[32];
            byte[] macKey = new byte[32];
            Buffer.BlockCopy(material, 0, encKey, 0, 32);
            Buffer.BlockCopy(material, 32, macKey, 0, 32);
            Array.Clear(material, 0, material.Length);

            byte[] head = new byte[HeaderLen + cipherLen];
            Buffer.BlockCopy(file, 0, head, 0, head.Length);
            byte[] mac = new byte[MacLen];
            Buffer.BlockCopy(file, head.Length, mac, 0, MacLen);

            byte[] calc;
            using (HMACSHA256 h = new HMACSHA256(macKey))
            {
                calc = h.ComputeHash(head);
            }
            Array.Clear(macKey, 0, macKey.Length);
            if (!FixedEquals(calc, mac))
                throw new WrongPasswordException();

            byte[] cipher = new byte[cipherLen];
            Buffer.BlockCopy(file, HeaderLen, cipher, 0, cipherLen);
            byte[] plain;
            try
            {
                using (Aes aes = Aes.Create())
                {
                    aes.KeySize = 256;
                    aes.Mode = CipherMode.CBC;
                    aes.Padding = PaddingMode.PKCS7;
                    aes.Key = encKey;
                    aes.IV = iv;
                    using (ICryptoTransform dec = aes.CreateDecryptor())
                    {
                        plain = dec.TransformFinalBlock(cipher, 0, cipher.Length);
                    }
                }
            }
            catch (CryptographicException)
            {
                throw new WrongPasswordException();
            }
            finally
            {
                Array.Clear(encKey, 0, encKey.Length);
            }
            try
            {
                return DeserializeJson(plain);
            }
            finally
            {
                Array.Clear(plain, 0, plain.Length);
            }
        }

        private static bool FixedEquals(byte[] a, byte[] b)
        {
            if (a == null || b == null) return false;
            if (a.Length != b.Length) return false;
            int diff = 0;
            for (int i = 0; i < a.Length; i++) diff |= a[i] ^ b[i];
            return diff == 0;
        }

        public static void SaveToFile(string path, VaultData data, string password)
        {
            // 兼容旧调用：统一走 WriteAtomic，避免两套不同的提交/备份策略
            WriteAtomic(path, Encrypt(data, password));
        }

        public static VaultData LoadFromFile(string path, string password)
        {
            byte[] bytes = ReadFileChecked(path);
            return Decrypt(bytes, password);
        }

        /// <summary>生成用于后台保存的不可变快照（轻量深拷贝，避免 JSON 往返的 UI 停顿）。</summary>
        public static VaultData Clone(VaultData src)
        {
            if (src == null) return null;
            VaultData d = new VaultData();
            d.Version = src.Version;
            d.AutoLockMinutes = src.AutoLockMinutes;
            d.Categories = src.Categories == null ? new List<string>() : new List<string>(src.Categories);
            d.Items = new List<KeyItem>();
            if (src.Items != null)
            {
                for (int i = 0; i < src.Items.Count; i++)
                {
                    KeyItem it = src.Items[i];
                    if (it == null) { d.Items.Add(null); continue; }
                    KeyItem n = new KeyItem();
                    n.Id = it.Id;
                    n.Name = it.Name;
                    n.Category = it.Category;
                    n.Value = it.Value;
                    n.Note = it.Note;
                    n.Created = it.Created;
                    n.Updated = it.Updated;
                    n.DefaultKeyId = it.DefaultKeyId;
                    n.Keys = new List<SubKey>();
                    if (it.Keys != null)
                    {
                        for (int k = 0; k < it.Keys.Count; k++)
                        {
                            SubKey sk = it.Keys[k];
                            if (sk == null) { n.Keys.Add(null); continue; }
                            SubKey ns = new SubKey();
                            ns.Id = sk.Id;
                            ns.Label = sk.Label;
                            ns.Value = sk.Value;
                            n.Keys.Add(ns);
                        }
                    }
                    d.Items.Add(n);
                }
            }
            return d;
        }
    }
}
