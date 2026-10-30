# 安全策略 · Security Policy

## 支持范围 / Supported versions

Key Center 以最新公开版本为安全支持对象。请先升级到 [最新版本](https://github.com/verneuil/KeyCenter/releases/latest) 再报告问题。
Security support targets the latest public release. Please upgrade to the [latest version](https://github.com/verneuil/KeyCenter/releases/latest) before reporting.

## 报告安全问题 / Reporting a vulnerability

请不要在公开 Issue 中粘贴真实 API Key、主密码、`keycenter.dat` 或任何个人数据。
Please do **not** paste real API keys, master passwords, `keycenter.dat`, or any personal data into a public issue.

推荐通过 GitHub Security Advisories 私密报告：
The preferred channel is a private GitHub Security Advisory:

- https://github.com/verneuil/KeyCenter/security/advisories/new

如果无法使用该入口，可以开一个不含敏感信息的 Issue，说明“需要私密渠道”。
If that is not available, open an issue containing **no sensitive information** and ask for a private channel.

## 设计边界 / Design boundaries

- 保险箱数据仅在本地加密保存；用户主动导出的明文文件除外，程序不发起网络请求。
- 主密码不保存、不上传；忘记后无法恢复。
- Key Center 不是完整密码管理器，不应作为唯一的高价值凭据存储。
- The vault data is encrypted and stored locally; user-initiated plaintext exports are the exception. The application makes no network requests.
- The master password is never stored or uploaded and cannot be recovered.
- Key Center is not a full password manager and should not be the only store for high-value credentials.

## 已接受风险 / Accepted risks

- **明文导出**是用户主动操作：导出前必须重新输入主密码，界面会明确提示导出文件是明文。保险箱文件本身仍使用 AES/HMAC 加密；导出的 `.txt` 文件不加密，用户需自行保护。
  Code scanning 会将此行为识别为 CWE-312（cleartext storage），这是导出功能的预期设计，已按 “Won't fix” 记录为已接受风险。
- **Plaintext export** is user-initiated. The user must re-enter the master password, and the UI explicitly warns that the exported file is plaintext. The vault file itself remains AES/HMAC encrypted; the exported `.txt` is intentionally not encrypted and must be protected by the user.
  Code scanning flags this behavior as CWE-312 (cleartext storage). It is an intentional product trade-off and is recorded as “Won't fix”.
