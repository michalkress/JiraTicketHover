# 🐙 JiraTicketHover

**Visual Studio extension that shows Jira ticket titles on mouse hover.**

[![Visual Studio](https://img.shields.io/badge/Visual%20Studio-2026-blue)]()
[![Platform](https://img.shields.io/badge/platform-Windows-lightgrey)]()

---

## ✨ What it does

Working in code with ticket references like `PROJ-123`? Just hover over it:

| Before | After |
|--------|-------|
| `// fixed in JIRA-456` | 💡 **Tooltip:** *"Fix login error on production"* |

**No more switching to Jira just to remember what a ticket is about.**

---

## 🚀 Features

| Feature | Description |
|---------|-------------|
| 🔍 **Automatic detection** | Finds `PROJECT-NUMBER` patterns in code comments |
| 💡 **Tooltip with title** | Shows ticket summary fetched from Jira API |
| 🔗 **One-click navigation** | Opens full ticket in your default browser |
| 🔐 **OAuth 2.0** | Secure authentication with Atlassian |
| ⚙️ **Fully configurable** | Project keys, cache timeout, enable/disable |
| 📁 **Multi-language** | Works with C#, C++, TypeScript, text files |

---

## 📦 Installation

### From Visual Studio Marketplace (coming soon)

`Extensions` → `Manage Extensions` → search *"JiraTicketHover"* → **Download**

### Build from source

```bash
git clone https://github.com/yourusername/JiraTicketHover.git
cd JiraTicketHover
open src/JiraTicketHover.sln
# Press F5 to build and run experimental instance
