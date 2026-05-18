# ✍ Prose — AI Grammar Corrector

A **full-stack grammar and spell-checking web app** built with ASP.NET Core 8 (C#) for the backend and a hand-crafted luxury editorial frontend (HTML/CSS/JS).

---

## 📸 Features

| Feature | Detail |
|---|---|
| **Spelling detection** | Powered by LanguageTool's open-source engine |
| **Grammar checking** | Subject-verb agreement, articles, tense issues, etc. |
| **Style suggestions** | Redundancy, clarity, wordiness |
| **Inline highlights** | Errors colour-coded by type with hover tooltips |
| **Auto-corrected text** | Changed words highlighted in gold |
| **Issue sidebar** | Click a card to jump to the error in context |
| **Multi-language** | 10 language options via LanguageTool |
| **Offline fallback** | Built-in rule engine if LanguageTool is unreachable |
| **Responsive** | Fully mobile and desktop optimised |

---

## 🚀 Getting Started

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- Internet connection (for LanguageTool API — optional, fallback engine works offline)

### 1. Clone / extract the project

```bash
cd GrammarCorrector
```

### 2. Restore and run

```bash
dotnet restore
dotnet run
```

The app starts on **http://localhost:5000** (or the port shown in the terminal).

### 3. Open in browser

Navigate to `http://localhost:5000` — the frontend is served directly from `wwwroot/`.

---

## 🏗 Project Structure

```
GrammarCorrector/
├── Controllers/
│   └── GrammarController.cs      # REST API: POST /api/grammar/check
├── Models/
│   ├── CorrectionRequest.cs      # Input DTO
│   └── CorrectionResponse.cs     # Output DTO + internal LanguageTool models
├── Services/
│   ├── IGrammarService.cs        # Service interface
│   └── GrammarService.cs         # LanguageTool API + fallback rule engine
├── wwwroot/
│   ├── index.html                # Single-page app shell
│   ├── css/style.css             # Luxury dark theme, animations
│   └── js/app.js                 # API calls, DOM rendering, UX logic
├── Program.cs                    # App bootstrap + DI registration
├── appsettings.json              # Config (LanguageTool URL, timeout)
└── GrammarCorrector.csproj
```

---

## 🔌 API Reference

### `POST /api/grammar/check`

**Request body** (`application/json`):
```json
{
  "text": "She go to school every day.",
  "language": "en-US"
}
```

**Response** (`200 OK`):
```json
{
  "originalText": "She go to school every day.",
  "correctedText": "She goes to school every day.",
  "matches": [
    {
      "offset": 4,
      "length": 2,
      "originalText": "go",
      "message": "Did you mean 'goes'? The verb 'go' must be conjugated as 'goes' for the third person singular.",
      "shortMessage": "Subject-verb agreement",
      "suggestions": ["goes"],
      "issueType": "grammar",
      "ruleId": "HE_VERB_AGR",
      "category": "Grammar"
    }
  ],
  "spellingErrorCount": 0,
  "grammarErrorCount": 1,
  "styleIssueCount": 0,
  "hasErrors": true
}
```

### `GET /api/grammar/health`

Returns `{ "status": "healthy", "timestamp": "..." }` — useful as a liveness probe.

---

## ⚙️ How the Correction Engine Works

### Primary: LanguageTool REST API

The app calls `https://api.languagetool.org/v2/check` with your text and language code.

LanguageTool returns an array of **matches** — each containing:
- `offset` + `length` — exact position of the error in the string  
- `replacements` — ranked list of suggestions  
- `rule` — the rule that fired (category, issueType, ID)

`GrammarService` maps these to the internal `LanguageMatch` model and classifies
each as `"spelling"`, `"grammar"`, or `"style"` using the rule's `issueType` and
`category.id` fields.

**Auto-correction** is applied by processing matches in **reverse offset order**
(right-to-left), replacing each erroneous span with the top suggestion. This
preserves the validity of earlier offsets.

### Fallback: Built-in Rule Engine

If LanguageTool is unreachable (timeout, no internet), the service automatically
falls back to a deterministic rule engine that covers:

- **~35 common spelling mistakes** (lookup table, case-preserving)
- **Subject-verb agreement** (she/he/it + present tense verbs)
- **Article choice** (a vs. an before vowel/consonant sounds)

This ensures the app is always functional.

---

## 🎨 UI Design Notes

The frontend follows a **luxury editorial dark** aesthetic:

| Element | Choice |
|---|---|
| Background | Deep charcoal (`#080810`) with film-grain overlay |
| Accent | Warm gold (`#c8a96e`) for CTAs, highlights, corrections |
| Spelling errors | Coral red (`#e85d75`) underline + background tint |
| Grammar errors | Soft violet (`#7b6cf6`) underline + background tint |
| Style suggestions | Teal (`#44c5a6`) underline + background tint |
| Display font | Cormorant Garamond (editorial serif) |
| Body font | Outfit (modern geometric sans) |
| Code/mono | JetBrains Mono (corrected text output) |
| Animations | CSS `fadeUp` on load, ambient orb `drift`, button hover glow |

---

## 🔧 Configuration

Edit `appsettings.json` to adjust:

```json
{
  "LanguageTool": {
    "BaseUrl": "https://api.languagetool.org",
    "Endpoint": "/v2/check",
    "DefaultLanguage": "en-US",
    "TimeoutSeconds": 15
  }
}
```

To use a **self-hosted LanguageTool** instance (no rate limits), change `BaseUrl`
to your server address: e.g. `"http://localhost:8081"`.

### Self-hosting LanguageTool (optional, Docker)

```bash
docker run -d -p 8081:8010 silviof/docker-languagetool
```

Then update `appsettings.json`:
```json
{ "LanguageTool": { "BaseUrl": "http://localhost:8081", "Endpoint": "/v2/check" } }
```

---

## 📝 Example Input / Output

| Input | Corrected | Issues |
|---|---|---|
| `She go to school every day.` | `She goes to school every day.` | 1 grammar |
| `Thier house is big.` | `Their house is big.` | 1 spelling |
| `I recieve a email from he.` | `I receive an email from him.` | 2 spelling + 1 grammar |
| `This is a very very good idea.` | `This is a very good idea.` | 1 style |

---

## 📜 License

MIT — free for personal and commercial use.
