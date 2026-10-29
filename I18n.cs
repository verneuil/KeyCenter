using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Windows.Forms;

namespace KeyCenter
{
    /// <summary>应用级常量与外部链接。</summary>
    internal static class AppInfo
    {
        public const string RepoUrl = "https://github.com/verneuil/KeyCenter";
        public const string ReleasesUrl = "https://github.com/verneuil/KeyCenter/releases";

        public static string Version
        {
            get
            {
                try
                {
                    Version v = Assembly.GetExecutingAssembly().GetName().Version;
                    return "v" + v.Major + "." + v.Minor + "." + v.Build;
                }
                catch (Exception) { return "v1.8.8"; }
            }
        }

        public static void OpenRepo()
        {
            OpenUrl(RepoUrl);
        }

        public static void OpenReleases()
        {
            OpenUrl(ReleasesUrl);
        }

        private static void OpenUrl(string url)
        {
            try
            {
                Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
            }
            catch (Exception) { }
        }
    }

    /// <summary>中英文本地化。字符串键使用源码中的原始中文，未收录时回退中文。</summary>
    internal static class L
    {
        public static bool IsEn
        {
            get { return string.Equals(AppConfig.Language, "en", StringComparison.OrdinalIgnoreCase); }
        }

        public static string T(string zh)
        {
            if (zh == null) return "";
            if (!IsEn) return zh;
            string en;
            return En.TryGetValue(zh, out en) ? en : zh;
        }

        public static string F(string zhFormat, params object[] args)
        {
            return string.Format(T(zhFormat), args);
        }

        /// <summary>“1 Key / 2 Keys / 1 个 Key”的计数文案。</summary>
        public static string KeyCount(int n)
        {
            if (!IsEn) return n + " 个 Key";
            return n + (n == 1 ? " Key" : " Keys");
        }

        private static readonly Dictionary<string, string> En = new Dictionary<string, string>(StringComparer.Ordinal)
        {
        { "    修改：", "    Modified: " },
        { " · 保存失败，Ctrl+S 重试（点此重试）", " · Save failed. Press Ctrl+S to retry (click here)" },
        { " · 保存中…", " · Saving…" },
        { "　·　关键词：", "　·　Keyword: " },
        { " 个 Key", " Keys" },
        { " 个 Key · 点击 ☆ 设为默认", " Keys · Click ☆ to set as default" },
        { " · 点击 ☆ 设为默认", " · Click ☆ to set as default" },
        { " 个 Key 会变为「未分类」，Key 本身不会被删除。", " Keys will be moved to «Uncategorized». The Keys themselves are not deleted." },
        { " 会变为「未分类」，Key 本身不会被删除。", " will be moved to «Uncategorized». The Keys themselves are not deleted." },
        { " 中。\r\n密码不会以任何形式保存，忘记后无法找回。", ".\r\nYour password is never saved in any form, and it cannot be recovered if you forget it." },
        { "　Key 总数：", "　Key count: " },
        { "；恢复错误：", "; restore error: " },
        { "；列表卡片显示带 ☆ 的默认 Key", "; list cards show the default Key marked with ☆" },
        { "；原因：", "; reason: " },
        { "。Replace 错误：", ". Replace error: " },
        { "（当前跟随系统：", "(currently following system: " },
        { "（Ctrl+S 重试）", "(press Ctrl+S to retry)" },
        { "」，正在保存", "», saving" },
        { "」吗？", "»?" },
        { "」吗？\r\n\r\n此操作不可撤销。", "»?\r\n\r\nThis action cannot be undone." },
        { "」设为默认 Key，正在保存", "» as the default Key, saving" },
        { "【默认】", "[Default]" },
        { "【未分类】", "[Uncategorized]" },
        { "# Key Center 配置（不含任何密钥）", "# Key Center configuration (no keys included)" },
        { "＋ 添加", "＋ Add" },
        { "＋ 添加一条 Key", "＋ Add a Key" },
        { "＋ 新建分类", "＋ New category" },
        { "========== 分类 ==========", "========== Categories ==========" },
        { "0 个 Key", "0 Keys" },
        { "1 分钟", "1 minute" },
        { "15 分钟", "15 minutes" },
        { "30 分钟", "30 minutes" },
        { "5 分钟", "5 minutes" },
        { "安全", "Security" },
        { "保存", "Save" },
        { "保存失败，已保留/恢复原文件。Replace 错误：", "Save failed; the original file was kept/restored. Replace error: " },
        { "保存失败，已取消锁定；可 Ctrl+S 重试后再次锁定", "Save failed; locking was cancelled. Press Ctrl+S to retry, then lock again." },
        { "保存失败：", "Save failed: " },
        { "保存失败：会话密钥不可用，请锁定后重新解锁", "Save failed: the session key is unavailable. Lock and unlock again." },
        { "保存失败：生成快照失败", "Save failed: could not create a snapshot." },
        { "保存失败：生成快照失败（Ctrl+S 重试）", "Save failed: could not create a snapshot (press Ctrl+S to retry)." },
        { "保存失败且原文件恢复失败；原文件保留在 ", "Save failed and the original file could not be restored; the original file remains at " },
        { "保险箱已创建，开始添加你的第一个 Key 吧", "Vault created. Add your first Key to get started." },
        { "备注", "Notes" },
        { "备注：", "Notes: " },
        { "备注（可选）", "Notes (optional)" },
        { "备注\n第二行 中文", "Notes\nSecond line Chinese" },
        { "备注名（可选）", "Note name (optional)" },
        { "本地加密 · API Key 保险箱", "Locally encrypted · API Key vault" },
        { "本地加密 · API Key 保险箱 · 已保存", "Locally encrypted · API Key vault · Saved" },
        { "编辑", "Edit" },
        { "编辑 ↑", "Edit ↑" },
        { "编辑 ↓", "Edit ↓" },
        { "编辑 Key", "Edit Key" },
        { "编辑时间：旧 → 新", "Edited: old → new" },
        { "编辑时间：新 → 旧", "Edited: new → old" },
        { "编辑条目", "Edit entry" },
        { "不是 Key Center 数据文件", "Not a Key Center data file" },
        { "测试 Key \"quoted\"", "Test Key \"quoted\"" },
        { "出错了", "Something went wrong" },
        { "窗口大小已恢复默认", "Window size restored to default" },
        { "创建 ↑", "Create ↑" },
        { "创建 ↓", "Create ↓" },
        { "创建：", "Created: " },
        { "创建并进入", "Create and open" },
        { "创建时间：旧 → 新", "Created: old → new" },
        { "创建时间：新 → 旧", "Created: new → old" },
        { "创建主密码", "Create master password" },
        { "打开数据文件夹", "Open data folder" },
        { "待写入数据为空", "No data to write" },
        { "当前默认 Key", "Current default Key" },
        { "当前修改无法保存，已取消锁定；请检查磁盘后重试", "Changes cannot be saved, so locking was cancelled; check the disk and try again." },
        { "导出期间会话已锁定，未生成明文文件", "The session was locked during export; no plaintext file was created." },
        { "导出失败", "Export failed" },
        { "导出临时文件不存在", "The export temporary file does not exist." },
        { "导出提交失败，旧文件备份保留在：", "Failed to commit the export; the previous file backup was kept at: " },
        { "导出文件写入不完整，旧文件备份保留在：", "The export was incomplete; the previous file backup was kept at: " },
        { "导出时间：", "Exported: " },
        { "导出所有 Key", "Export all Keys" },
        { "导出所有 Key（需验证主密码）", "Export all Keys (master password required)" },
        { "点击 ↑ ↓ 调整顺序", "Click ↑ ↓ to reorder" },
        { "点击右上角「添加」，保存你的第一个 API Key", "Click «Add» in the top-right corner to save your first API Key" },
        { "点击右上角「添加」把 Key 放进来", "Click «Add» in the top-right corner to add a Key" },
        { "读取失败：", "Read failed: " },
        { "分类", "Category" },
        { "分类、安全与数据存储", "Categories, Security & Data Storage" },
        { "分类管理", "Manage categories" },
        { "分类名称", "Category name" },
        { "分类已创建，但保存失败，请检查磁盘后重试", "Category created, but saving failed. Check the disk and try again." },
        { "复制", "Copy" },
        { "复制失败：剪贴板被其他程序占用", "Copy failed: the clipboard is in use by another program." },
        { "复制这条 Key", "Copy this Key" },
        { "该分类下的 ", "In this category: " },
        { "个人", "Personal" },
        { "跟随系统", "Follow system" },
        { "更新失败", "Update failed" },
        { "工作", "Work" },
        { "共 ", "Total: " },
        { "关闭", "Close" },
        { "还没有保存任何 Key", "No Keys saved yet" },
        { "还有修改没有成功保存到磁盘，退出后会丢失。\r\n仍要退出吗？", "Some changes were not saved to disk and will be lost if you exit.\r\nExit anyway?" },
        { "换个关键词，或者清空搜索框", "Try another keyword, or clear the search box" },
        { "恢复默认窗口大小", "Restore default window size" },
        { "会话密钥不可用，请锁定后重新解锁", "The session key is unavailable. Lock and unlock again." },
        { "会话密钥已失效", "The session key has expired." },
        { "继续导出", "Continue export" },
        { "解锁", "Unlock" },
        { "解锁 Key Center", "Unlock Key Center" },
        { "可添加多条；点 ☆ 设为默认，列表只显示默认 Key", "You can add several Keys; click ☆ to set as default. The list shows only the default Key." },
        { "例如：OpenAI API", "e.g. OpenAI API" },
        { "两次输入的密码不一致", "The two passwords do not match." },
        { "没有匹配的 Key", "No matching Keys" },
        { "密码不能为空", "Password cannot be empty." },
        { "密码错误，请重试", "Incorrect password. Try again." },
        { "密码错误或数据文件已损坏", "Incorrect password or the data file is corrupted." },
        { "密码至少 6 位", "Password must be at least 6 characters." },
        { "名称", "Name" },
        { "名称 A→Z", "Name A→Z" },
        { "名称 Z→A", "Name Z→A" },
        { "名称：", "Name: " },
        { "名称：A → Z", "Name: A → Z" },
        { "名称：Z → A", "Name: Z → A" },
        { "默认", "Default" },
        { "浅色", "Light" },
        { "切换为浅色模式", "Switch to light mode" },
        { "切换为深色模式", "Switch to dark mode" },
        { "请输入名称", "Enter a name" },
        { "请输入内容", "Enter content" },
        { "请输入主密码", "Enter your master password" },
        { "请先在左侧选择一个分类。", "Select a category on the left first." },
        { "取消", "Cancel" },
        { "全部", "All" },
        { "全部 Key", "All Keys" },
        { "全部隐藏", "Hide all" },
        { "确定", "OK" },
        { "确定删除分类「", "Delete category «" },
        { "确定修改", "Confirm changes" },
        { "确定要删除「", "Are you sure you want to delete «" },
        { "确认新密码", "Confirm new password" },
        { "确认主密码", "Confirm master password" },
        { "删除", "Delete" },
        { "删除 Key", "Delete Key" },
        { "删除分类", "Delete category" },
        { "删除这条 Key", "Delete this Key" },
        { "上次保存失败：", "Last save failed: " },
        { "设为默认 Key", "Set as default Key" },
        { "设置", "Settings" },
        { "设置主密码（至少 6 位）", "Set master password (at least 6 characters)" },
        { "深色", "Dark" },
        { "生成保存快照失败：", "Failed to create save snapshot: " },
        { "生成导出快照失败：\r\n", "Failed to create export snapshot:\r\n" },
        { "输入", "Input" },
        { "输入主密码，解密你的本地保险箱。", "Enter your master password to decrypt your local vault." },
        { "数据版本比当前程序新，请升级 Key Center 后再打开", "The data version is newer than this app. Upgrade Key Center before opening it." },
        { "数据存储", "Data storage" },
        { "数据过大，已取消保存", "Data too large; the save was cancelled." },
        { "数据内容为空", "Data content is empty." },
        { "数据内容无法解析：", "Cannot parse data content: " },
        { "数据文件：", "Data file: " },
        { "数据文件不存在", "Data file not found" },
        { "数据文件已从备份恢复：", "Data file recovered from backup: " },
        { "数据文件不完整或已损坏", "The data file is incomplete or corrupted." },
        { "数据文件过大，已拒绝读取", "The data file is too large; reading was refused." },
        { "数据文件过大，已取消保存", "The data file is too large; the save was cancelled." },
        { "数据文件路径被目录占用，无法保存", "The data file path is occupied by a folder; cannot save." },
        { "说明：此文件包含明文 Key，请勿上传网盘或发给他人。", "Note: this file contains plaintext Keys. Do not upload it to cloud drives or send it to others." },
        { "搜索", "Search" },
        { "搜索结果", "Search results" },
        { "搜索名称、分类或备注…", "Search name, category, or notes…" },
        { "所有 Key 都会用这个密码加密保存在 ", "All Keys are encrypted with this password and stored in " },
        { "锁定（Ctrl+L）", "Lock (Ctrl+L)" },
        { "提示", "Notice" },
        { "添加 Key", "Add Key" },
        { "条目数：", "Entries: " },
        { "退出", "Exit" },
        { "外观", "Appearance" },
        { "完成", "Done" },
        { "为了确认是本人操作，请输入一次主密码。\r\n导出文件是明文，请妥善保管。", "To confirm this is really you, enter your master password once.\r\nThe export file is plaintext, so keep it safe." },
        { "未分类", "Uncategorized" },
        { "文本文件 (*.txt)|*.txt", "Text files (*.txt)|*.txt" },
        { "无法生成导出内容：\r\n", "Failed to generate export content:\r\n" },
        { "无法写入数据文件：", "Failed to write data file: " },
        { "无法写入文件：\r\n", "Failed to write file:\r\n" },
        { "显示 / 隐藏", "Show / Hide" },
        { "显示全部", "Show all" },
        { "新的分类名称", "New category name" },
        { "新建分类", "New category" },
        { "新密码", "New password" },
        { "新密码已设置，点「完成」后生效", "New password set. Click «Done» to apply it." },
        { "新密码至少 6 位", "New password must be at least 6 characters." },
        { "新密码至少 6 位。修改后用新密码才能解锁，请务必牢记。", "New password must be at least 6 characters. After the change, only the new password can unlock the vault, so be sure to remember it." },
        { "修改主密码", "Change master password" },
        { "修改主密码…", "Change master password…" },
        { "已创建分类「", "Category created «" },
        { "已存在同名 Key，请换一个名称", "A Key with this name already exists. Choose a different name." },
        { "已导出到 ", "Exported to " },
        { "已复制", "Copied" },
        { "已复制 ", "Copied " },
        { "已更新「", "Updated «" },
        { "已将「", "Set «" },
        { "已解锁", "Unlocked" },
        { "已切换默认 Key", "Default Key changed" },
        { "已切换默认 Key，但保存失败，请检查磁盘后重试", "Default Key changed, but saving failed. Check the disk and try again." },
        { "已切换为浅色模式", "Switched to light mode" },
        { "已切换为深色模式", "Switched to dark mode" },
        { "已删除，但保存失败，请检查磁盘后重试", "Deleted, but saving failed. Check the disk and try again." },
        { "已删除「", "Deleted «" },
        { "已锁定，请输入主密码", "Locked. Enter your master password." },
        { "已添加，但保存失败，请检查磁盘后重试", "Added, but saving failed. Check the disk and try again." },
        { "已添加「", "Added «" },
        { "已修改，但保存失败，请检查磁盘后重试", "Updated, but saving failed. Check the disk and try again." },
        { "已因长时间无操作自动锁定", "Locked automatically after a long period of inactivity." },
        { "有未保存的修改", "Unsaved changes" },
        { "再次输入新密码", "Re-enter new password" },
        { "再次输入主密码", "Re-enter master password" },
        { "粘贴 Key…", "Paste Key…" },
        { "找不到文件夹", "Folder not found" },
        { "这个分类还是空的", "This category is empty" },
        { "这个分类名称已经存在了。", "This category name already exists." },
        { "这个分类已经存在了。", "This category already exists." },
        { "这个条目的用途、项目等", "Purpose, project, etc. for this entry" },
        { "正在保存，保存完成后将自动退出", "Saving. The app will exit automatically when done." },
        { "正在保存，保存完成后自动锁定", "Saving. The vault will lock automatically when done." },
        { "正在创建…", "Creating…" },
        { "正在导出，请稍候", "Exporting, please wait" },
        { "正在导出，完成后将自动退出", "Exporting. The app will exit automatically when done." },
        { "正在更新主密码，请稍候", "Updating master password, please wait" },
        { "正在更新主密码…", "Updating master password…" },
        { "正在解锁…", "Unlocking…" },
        { "正在重试保存，保存完成后自动锁定", "Retrying save. The vault will lock automatically when done." },
        { "知道了", "Got it" },
        { "至少保留一条 Key", "At least one Key must be kept." },
        { "至少填写一条 Key 内容", "Enter content for at least one Key" },
        { "重命名", "Rename" },
        { "重命名分类", "Rename category" },
        { "主密码", "Master password" },
        { "主密码错误，请重试", "Incorrect master password. Try again." },
        { "主密码已更新", "Master password updated" },
        { "自动锁定（无操作超时）", "Auto-lock (idle timeout)" },
        { "AES-256 · 本地存储", "AES-256 · Local storage" },
        { "AES-256 · PBKDF2-SHA256 · 仅保存在本机", "AES-256 · PBKDF2-SHA256 · Stored only on this device" },
        { "Key 列表", "Key list" },
        { "Key 详情", "Key details" },
        { "Key Center 明文导出", "Key Center plaintext export" },
        { "Key Center 已经在运行中。", "Key Center is already running." },
        { "Test#2026 密码", "Test#2026 password" },
        { "立即重启", "Restart now" },
        { "应用语言", "Apply language" },
        { "语言", "Language" },
        { "语言设置在重启应用后生效。是否立即重启？", "The language change takes effect after restarting Key Center. Restart now?" },
        { "语言已保存，重启应用后生效", "Language saved. Restart the app to apply." },
        { "中文", "中文" },
        { "重启后生效", "Applies after restart" },
        { "打开 GitHub 仓库", "Open GitHub repository" },
        { "查看发布版本", "View releases" },
        { "设置尚未保存完成，已取消重启", "Settings are not saved yet; restart canceled" },
        { "无法重启", "Cannot restart" },
        { "保存未完成，请检查磁盘后重试。", "The save did not complete. Check the disk and try again." },
};
    }
}

