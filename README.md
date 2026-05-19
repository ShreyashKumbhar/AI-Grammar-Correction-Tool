# ✍ Prose — AI Grammar Corrector

A full-stack **grammar and spell-checking web application** with user authentication, subscription management, and payment processing. Built with **ASP.NET Core 10** (C#) backend and modern **HTML/CSS/JavaScript** frontend.

---

## ✨ Key Features

- **Spelling & Grammar Detection** – Powered by LanguageTool's open-source engine
- **Style Suggestions** – Redundancy, clarity, and wordiness recommendations
- **Multi-language Support** – 10+ language options via LanguageTool
- **Offline Fallback** – Built-in rule engine for when LanguageTool API is unavailable
- **Secure Authentication** – JWT-based with Bcrypt password hashing
- **Subscription Tiers** – Free (500 corrections/month) and Unlimited ($9.99/month)
- **Payment Processing** – Stripe integration with Payment Intent API
- **Usage Analytics** – Track corrections and monitor monthly quota
- **User Dashboard** – Manage account, subscriptions, and settings
- **Responsive Design** – Optimized for desktop and mobile devices

---

## 🚀 Quick Start

### Prerequisites
- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
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
├── Controllers/              # API endpoints
│   ├── AuthController.cs
│   ├── GrammarController.cs
│   ├── SubscriptionController.cs
│   └── PaymentController.cs
├── Services/                 # Business logic
│   ├── IGrammarService.cs
│   ├── IAuthService.cs
│   └── [Implementation files]
├── Models/                   # Data models
├── Data/                     # Entity Framework DbContext
├── wwwroot/                  # Static web assets
│   ├── index.html
│   ├── auth.html
│   ├── dashboard.html
│   ├── css/style.css
│   └── js/app.js
└── Program.cs                # Startup configuration
```

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

---

## 🔧 Configuration

Key settings in `appsettings.json`:

| Setting | Description |
|---------|-------------|
| `ConnectionStrings.DefaultConnection` | SQL Server connection |
| `Jwt.SecretKey` | JWT signing key (min 32 chars) |
| `Stripe.SecretKey` | Stripe secret API key |
| `Stripe.PublishableKey` | Stripe publishable key |

### LanguageTool
The app uses LanguageTool for grammar checking:
- **Base URL**: `https://api.languagetool.org`
- **Timeout**: 15 seconds
- **Fallback**: Built-in rules engine if API unavailable

---

## 🛠️ Technologies

| Layer | Technology |
|-------|-----------|
| **Backend** | ASP.NET Core 10, Entity Framework Core 10 |
| **Database** | SQL Server (LocalDB/Express) |
| **Authentication** | JWT, BCrypt.Net-Next |
| **Payments** | Stripe.net |
| **Grammar Engine** | LanguageTool API |
| **Frontend** | HTML5, CSS3, Vanilla JavaScript |

---

## 🔐 Security

- **Passwords** – Bcrypt hashing with salt
- **API Authentication** – JWT tokens with 24-hour expiration
- **Payment Security** – PCI-compliant Stripe integration
- **HTTPS** – Enforced for all connections
- **CORS** – Configured for same-origin requests

---

## 📋 Requirements for Development

- .NET 10 SDK or later
- Visual Studio 2022/2026 or VS Code
- SQL Server (LocalDB included with Visual Studio)
- Git

---

## Contributing

Contributions are welcome via pull requests and GitHub issues.

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

---

## 📝 License

This project is [MIT licensed](LICENSE) – feel free to use, modify, and distribute.
