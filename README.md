# ✍ Prose — AI Grammar Corrector

A full-stack **grammar and spell-checking web application** with user authentication, subscription management, and payment processing. Built with **ASP.NET Core 8** (C#) backend and modern **HTML/CSS/JavaScript** frontend.

**Live Demo:** [Add live URL if available]

---

## ✨ Key Features

### Core Grammar Checking
- **Spelling & Grammar Detection** – Powered by LanguageTool's open-source engine
- **Grammar Rules** – Subject-verb agreement, articles, tense issues, and more
- **Style Suggestions** – Redundancy, clarity, and wordiness recommendations
- **Multi-language Support** – 10 language options via LanguageTool
- **Offline Fallback** – Built-in rule engine works when LanguageTool API is unavailable

### User & Subscription System
- **Secure Authentication** – JWT-based with Bcrypt password hashing
- **Subscription Tiers**
  - *Free*: 500 corrections/month
  - *Unlimited*: Unlimited corrections for $9.99/month
- **Payment Processing** – Stripe integration with Payment Intent API
- **Usage Analytics** – Track corrections and monitor monthly quota
- **User Dashboard** – Manage account, subscriptions, and settings

### User Interface
- **Inline Error Highlights** – Color-coded by error type with hover tooltips
- **Auto-corrected Text** – Changed words highlighted for easy review
- **Issue Sidebar** – Click error cards to jump to context in text
- **Responsive Design** – Optimized for desktop and mobile devices
- **Dark Theme UI** – Professional, luxury editorial interface

---

## 🚀 Quick Start

### Prerequisites
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [SQL Server](https://www.microsoft.com/sql-server/sql-server-downloads) (LocalDB or Express)
- [Stripe Account](https://stripe.com) (for payment integration)

### Installation & Setup

1. **Clone the repository**
   ```bash
   git clone https://github.com/ShreyashKumbhar/AI-Grammar-Correction-Tool.git
   cd GrammarCorrector
   ```

2. **Configure appsettings.json**
   ```json
   {
     "ConnectionStrings": {
       "DefaultConnection": "Server=.;Database=GrammarCorrectorDb;Trusted_Connection=true;TrustServerCertificate=true;"
     },
     "Jwt": {
       "SecretKey": "your-secret-key-minimum-32-characters-long",
       "Issuer": "GrammarCorrector",
       "Audience": "GrammarCorrectorUsers",
       "ExpirationMinutes": 1440
     },
     "Stripe": {
       "SecretKey": "sk_test_YOUR_KEY",
       "PublishableKey": "pk_test_YOUR_KEY",
       "WebhookSecret": "whsec_YOUR_SECRET"
     }
   }
   ```

3. **Restore dependencies and create database**
   ```bash
   dotnet restore
   dotnet ef database update
   ```

4. **Run the application**
   ```bash
   dotnet run
   ```

5. **Open in browser**
   Navigate to `https://localhost:50866`

---

## 📁 Project Structure

```
GrammarCorrector/
├── Controllers/              # API controllers
│   ├── AuthController.cs
│   ├── GrammarController.cs
│   ├── SubscriptionController.cs
│   └── PaymentController.cs
├── Services/                 # Business logic
│   ├── IGrammarService.cs
│   ├── IAuthService.cs
│   ├── ISubscriptionService.cs
│   └── [Implementation files]
├── Models/                   # Data models
│   ├── User.cs
│   ├── CorrectionRequest.cs
│   └── Subscription.cs
├── Data/                     # Entity Framework DbContext
├── wwwroot/                  # Static web assets
│   ├── index.html
│   ├── auth.html
│   ├── dashboard.html
│   ├── css/style.css
│   └── js/app.js
├── Properties/launchSettings.json
└── Program.cs                # Startup configuration
```

---

## 🔧 Configuration

### Environment Variables
Key configuration settings in `appsettings.json`:

| Setting | Description | Example |
|---------|-------------|---------|
| `ConnectionStrings.DefaultConnection` | SQL Server connection | `Server=.;Database=GrammarCorrectorDb;...` |
| `Jwt.SecretKey` | JWT signing key (min 32 chars) | `your-secret-key-...` |
| `Stripe.SecretKey` | Stripe secret API key | `sk_test_...` |
| `Stripe.PublishableKey` | Stripe publishable key | `pk_test_...` |

### LanguageTool API
The app uses LanguageTool for grammar checking. Default settings:
- **Base URL**: `https://api.languagetool.org`
- **Endpoint**: `/v2/check`
- **Timeout**: 15 seconds
- **Fallback**: Built-in rules engine if API unavailable

---

## 📚 API Endpoints

### Authentication
- `POST /api/auth/signup` – Register new user
- `POST /api/auth/login` – User login (returns JWT token)
- `POST /api/auth/change-password` – Change user password

### Grammar Checking
- `POST /api/grammar/check` – Check text for grammar/spelling errors

### Subscriptions
- `GET /api/subscription/status` – Get user subscription status
- `POST /api/subscription/upgrade` – Upgrade to Unlimited tier
- `POST /api/subscription/downgrade` – Downgrade to Free tier

### Payments
- `POST /api/payment/create-payment-intent` – Initiate Stripe payment
- `GET /api/payment/history` – Get payment transaction history

### Analytics
- `GET /api/analytics/usage` – Get monthly usage statistics

---

## 🔐 Security

- **Passwords** – Bcrypt hashing with salt
- **API Authentication** – JWT tokens with 24-hour expiration
- **Payment Security** – PCI-compliant Stripe integration
- **HTTPS** – Enforced for all connections
- **CORS** – Configured for same-origin requests

---

## 🛠️ Technologies

| Layer | Technology |
|-------|-----------|
| **Backend** | ASP.NET Core 8, Entity Framework Core 8 |
| **Database** | SQL Server (LocalDB/Express) |
| **Authentication** | JWT (System.IdentityModel.Tokens.Jwt) |
| **Password Hashing** | BCrypt.Net-Next |
| **Payments** | Stripe.net |
| **Grammar Engine** | LanguageTool API |
| **Frontend** | HTML5, CSS3, Vanilla JavaScript |

---

## 📋 Requirements for Development

- .NET 8 SDK or later
- Visual Studio 2022/2026 or VS Code
- SQL Server (LocalDB included with Visual Studio)
- Git

---

## 📝 License

This project is [MIT licensed](LICENSE) – feel free to use, modify, and distribute.

---

## 🤝 Contributing

Contributions are welcome! Please follow these guidelines:

1. Fork the repository
2. Create a feature branch: `git checkout -b feature/your-feature`
3. Commit changes: `git commit -m 'Add your feature'`
4. Push to the branch: `git push origin feature/your-feature`
5. Submit a Pull Request

For major changes, please open an issue first to discuss proposed changes.

---

## 💬 Support

For questions or issues:
- Open a [GitHub Issue](https://github.com/ShreyashKumbhar/AI-Grammar-Correction-Tool/issues)
- Check existing documentation in the repository

---

## 👨‍💻 Author

**Shreyash Kumbhar** – [GitHub Profile](https://github.com/ShreyashKumbhar)

---

## 🙏 Acknowledgments

- [LanguageTool](https://languagetool.org/) – Grammar checking engine
- [Stripe](https://stripe.com/) – Payment processing
- [ASP.NET Core](https://dotnet.microsoft.com/) – Framework
- [Entity Framework Core](https://docs.microsoft.com/ef/) – ORM

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
