# GrammarCorrector Project Structure Map

## Final Organized Structure

```
GrammarCorrector/
│
├── 📁 Controllers/
│   └── GrammarController.cs              # REST API endpoints (POST /api/grammar/check, GET health)
│
├── 📁 Models/
│   ├── CorrectionRequest.cs              # Input DTO (Text, Language)
│   └── CorrectionResponse.cs             # Output DTO + internal LanguageTool types
│
├── 📁 Services/
│   ├── IGrammarService.cs                # Service interface
│   └── GrammarService.cs                 # LanguageTool integration + fallback rules
│
├── 📁 wwwroot/                           # Static web assets (served from this dir)
│   ├── index.html                        # SPA entry point
│   ├── 📁 css/
│   │   └── style.css                     # Luxury dark theme, 875 lines
│   └── 📁 js/
│       └── app.js                        # Frontend logic, API calls, rendering
│
├── 📁 Properties/
│   └── launchSettings.json               # Dev launch config (ports 50866/50867)
│
├── 📁 Documentation/
│   ├── PROJECT_STRUCTURE_REORGANIZATION.md
│   ├── QUICK_REFERENCE.md
│   ├── LINUX_COMPATIBILITY_AUDIT.md
│   ├── CHANGES_MADE.md
│   ├── FINAL_AUDIT_REPORT.md
│   ├── FINAL_AUDIT_COMPLETE.md
│   ├── README_FIXES.md
│   ├── MASTER_CHECKLIST.md
│   └── README.md
│
├── Program.cs                            # DI, middleware config, static file setup
├── appsettings.json                      # LanguageTool endpoint config
└── GrammarCorrector.csproj              # .NET 8 project file

```

---

## Architecture Overview

### Request Flow
```
┌─────────────────────────────────────────────────────────────────┐
│                          CLIENT (Browser)                       │
│  ┌──────────────────────────────────────────────────────────┐   │
│  │ wwwroot/index.html                                      │   │
│  │ - UI markup, layout, semantic HTML                      │   │
│  │ - Loading from /css/style.css                           │   │
│  │ - Executing /js/app.js                                  │   │
│  └────────────────┬─────────────────────────────────────────┘   │
│                   │                                              │
│  wwwroot/js/app.js (Frontend Logic)                             │
│  ┌──────────────────────────────────────────────────────────┐   │
│  │ • Listen to input/button events                         │   │
│  │ • POST text to /api/grammar/check                       │   │
│  │ • Render results with highlighting                      │   │
│  │ • Show stats, issues, suggestions                       │   │
│  └────────────────┬─────────────────────────────────────────┘   │
│                   │ fetch(/api/grammar/check)                    │
└───────────────────┼──────────────────────────────────────────────┘
					│
					↓
	┌───────────────────────────────────────────────────────────┐
	│              SERVER (ASP.NET Core 8)                      │
	│                                                           │
	│  Program.cs                                             │
	│  ├─ Startup config                                     │
	│  ├─ Static files from wwwroot/                         │
	│  ├─ DI registration                                    │
	│  └─ Middleware pipeline                                │
	│                                                           │
	│  Controllers/GrammarController.cs                       │
	│  └─ POST /api/grammar/check                            │
	│     └─ Calls IGrammarService.CheckTextAsync()          │
	│                                                           │
	│  Services/GrammarService.cs                             │
	│  ├─ 1) Try LanguageTool API (https://api.languagetool.org)
	│  │  └─ POST with { text, language, enabledOnly }      │
	│  │  └─ Returns matches with offsets, suggestions       │
	│  │                                                      │
	│  ├─ 2) Fallback: Built-in rules                        │
	│  │  ├─ Common spelling errors dict (35+ patterns)     │
	│  │  └─ Grammar rules (17 subject-verb patterns)        │
	│  │                                                      │
	│  ├─ 3) Apply corrections                               │
	│  │  └─ Auto-correct with best suggestions (right-to-left)
	│  │                                                      │
	│  └─ Returns CorrectionResponse                         │
	│                                                           │
	│  Models/                                                 │
	│  ├─ CorrectionRequest (input DTO)                     │
	│  ├─ CorrectionResponse (output DTO)                   │
	│  ├─ LanguageMatch (match details)                     │
	│  └─ LanguageTool types (internal)                     │
	│                                                           │
	└────────────────┬──────────────────────────────────────────┘
					 │ JSON response (camelCase)
					 │
					 ↓
	┌────────────────────────────────────────────────────────┐
	│ Frontend receives CorrectionResponse                  │
	│ {                                                     │
	│   "originalText": "...",                             │
	│   "correctedText": "...",                            │
	│   "matches": [...],                                  │
	│   "spellingErrorCount": 2,                           │
	│   "grammarErrorCount": 1,                            │
	│   "styleIssueCount": 0,                              │
	│   "hasErrors": true                                  │
	│ }                                                     │
	│                                                       │
	│ Renders:                                             │
	│ • Stats bar (error counts)                          │
	│ • Annotated original (highlighted errors)            │
	│ • Auto-corrected text (with corrections highlighted) │
	│ • Issue sidebar (clickable cards)                    │
	│ • Tooltips with suggestions                         │
	└──────────────────────────────────────────────────────┘
```

---

## Layer Breakdown

### Presentation Layer (wwwroot/)
- **index.html** - Semantic, accessible HTML5
- **css/style.css** - Luxury dark theme with animations
- **js/app.js** - DOM manipulation, API orchestration, UX logic

### API Layer (Controllers/)
- **GrammarController.cs** - RESTful endpoints with validation

### Business Logic Layer (Services/)
- **IGrammarService** - Contract definition
- **GrammarService** - Core correction logic, external API calls, fallback engine

### Data Models (Models/)
- **Request/Response DTOs** - Shape of data exchanged
- **Domain objects** - Match, suggestion, issue types
- **External API types** - LanguageTool JSON deserialization

---

## Configuration & Entry Point

### Program.cs Responsibilities
1. **Dependency Injection**
   - Register HttpClient for LanguageTool
   - Register IGrammarService → GrammarService

2. **Middleware Pipeline**
   - Static file serving from wwwroot/
   - CORS policy
   - Controller routing
   - SPA fallback to index.html

3. **JSON Serialization**
   - Configure camelCase output for frontend

### appsettings.json
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

---

## File Relationships

```
GrammarController
  ├─ depends on → IGrammarService
  ├─ reads → CorrectionRequest (from body)
  └─ returns → CorrectionResponse

GrammarService
  ├─ implements → IGrammarService
  ├─ uses → HttpClient (for LanguageTool API)
  ├─ uses → IConfiguration (for endpoints)
  ├─ uses → ILogger
  ├─ deserializes → LanguageToolResponse
  ├─ maps → LTMatch → LanguageMatch
  └─ returns → CorrectionResponse (with Matches list)

index.html
  ├─ imports → css/style.css
  ├─ loads → js/app.js
  └─ calls → /api/grammar/check (via fetch)

app.js
  ├─ makes requests → POST /api/grammar/check
  ├─ renders from → CorrectionResponse
  ├─ styles with → css variables from style.css
  └─ displays → LanguageMatch objects
```

---

## Static File Serving

```
Request: GET /css/style.css
		 ↓
		 wwwroot/ (root provider)
		 ├─ css/style.css ✓ Found
		 └─ Served with 200 OK

Request: GET /js/app.js
		 ↓
		 wwwroot/ (root provider)
		 ├─ js/app.js ✓ Found
		 └─ Served with 200 OK

Request: GET /unknown-path
		 ↓
		 wwwroot/ (root provider)
		 ├─ unknown-path ✗ Not found
		 └─ Fallback → index.html (SPA)
			└─ Let app.js handle routing
```

---

## Namespace Organization

```
GrammarCorrector
├─ .Controllers
│  └─ GrammarController
│
├─ .Models
│  ├─ CorrectionRequest
│  ├─ CorrectionResponse
│  ├─ LanguageMatch
│  ├─ LanguageToolResponse (internal)
│  ├─ LTMatch (internal)
│  ├─ LTReplacement (internal)
│  ├─ LTRule (internal)
│  └─ LTCategory (internal)
│
└─ .Services
   ├─ IGrammarService
   └─ GrammarService
```

---

## Compilation & Runtime

```
BUILD PHASE
┌─────────────────────────────────────────┐
│ 1. C# compiler checks namespaces        │
│    • Controllers/... → GrammarCorrector │
│    • Services/... → GrammarCorrector    │
│    • Models/... → GrammarCorrector      │
│                                         │
│ 2. Project references resolved          │
│    • wwwroot/ marked as content files   │
│                                         │
│ 3. Output: GrammarCorrector.dll         │
└─────────────────────────────────────────┘
			  ↓
RUNTIME PHASE
┌─────────────────────────────────────────┐
│ 1. Program.cs runs                      │
│    • DI container configured            │
│    • Static file provider: wwwroot/     │
│                                         │
│ 2. First request to /                   │
│    • Matched to fallback                │
│    • Serves wwwroot/index.html          │
│                                         │
│ 3. Browser requests /css/style.css      │
│    • Matched to static files            │
│    • Served from wwwroot/css/           │
│                                         │
│ 4. Browser requests /js/app.js          │
│    • Matched to static files            │
│    • Served from wwwroot/js/            │
│                                         │
│ 5. API requests to /api/grammar/check   │
│    • Routed to GrammarController        │
│    • DI provides IGrammarService        │
└─────────────────────────────────────────┘
```

---

## Summary

✅ **Well-organized, scalable structure**  
✅ **Follows ASP.NET Core conventions**  
✅ **Clear separation of concerns**  
✅ **Easy to maintain and extend**  
✅ **Professional directory layout**  

Perfect for team collaboration and future development! 🎯
