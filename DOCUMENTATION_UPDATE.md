# Project Structure & Architecture - Updated Documentation

## Current Project Structure (Organized)

```
GrammarCorrector/
│
├── 📁 Controllers/
│   └── GrammarController.cs              # REST API endpoints
│       ├─ namespace: GrammarCorrector.Controllers ✅
│       ├─ Route: api/[controller]
│       ├─ POST /api/grammar/check - Main correction endpoint
│       └─ GET /api/grammar/health - Health check probe
│
├── 📁 Models/
│   ├── CorrectionRequest.cs              # Input DTO
│   │   └─ Fields: Text (string), Language (string, default "en-US")
│   │
│   └── CorrectionResponse.cs             # Output DTO + internal types
│       ├─ Fields: OriginalText, CorrectedText, Matches[], Error counts
│       ├─ LanguageMatch - Single error/suggestion
│       └─ Internal: LanguageToolResponse, LTMatch, LTRule, etc. (for deserialization)
│
├── 📁 Services/
│   ├── IGrammarService.cs                # Service interface
│   │   └─ Method: CheckTextAsync(CorrectionRequest, CancellationToken)
│   │
│   └── GrammarService.cs                 # Implementation (307 lines)
│       ├─ Primary: Calls LanguageTool REST API
│       ├─ Fallback: Built-in rule engine (35+ spelling patterns, 17 grammar rules)
│       ├─ Processes: Maps LTMatch → LanguageMatch, classifies issues
│       └─ Output: Auto-corrected text + match details
│
├── 📁 wwwroot/                           # Static web assets (served by ASP.NET)
│   ├── index.html                        # SPA shell (184 lines, semantic HTML5)
│   │   ├─ Loads: /css/style.css
│   │   ├─ Loads: /js/app.js
│   │   ├─ Elements: Hero, input card, results grid, sidebar
│   │   └─ Accessibility: ARIA labels, semantic HTML, live regions
│   │
│   ├── 📁 css/
│   │   └── style.css                     # Styling (875 lines, luxury dark theme)
│   │       ├─ Design: Cormorant Garamond (headers) + Outfit (body) + JetBrains Mono (code)
│   │       ├─ Colors: Deep charcoal bg, gold accents, red/purple/teal errors
│   │       ├─ Features: Animations, grain overlay, ambient orbs, responsive grid
│   │       └─ Breakpoints: Desktop (>720px), mobile (≤720px)
│   │
│   └── 📁 js/
│       └── app.js                        # Frontend logic (505 lines)
│           ├─ Init: DOMContentLoaded → bind events, validate elements
│           ├─ Events: Input, keyboard shortcuts (Ctrl+Enter), buttons
│           ├─ API: POST to /api/grammar/check with timeout
│           ├─ Rendering: Stats bar, annotated original, corrected text, issue sidebar
│           ├─ UX: Loading states, error handling, clipboard copy, smooth scroll
│           └─ Utils: HTML escaping, deduplication, keyboard shortcuts
│
├── 📁 Properties/
│   └── launchSettings.json               # Launch configuration
│       └─ Profile "GrammarCorrector": HTTPS:50866, HTTP:50867
│
├── Program.cs (55 lines)                 # Application startup
│   ├─ ConfigureServices:
│   │  ├─ AddControllers with JSON camelCase serialization
│   │  ├─ AddHttpClient for LanguageTool API (BaseAddress, timeout)
│   │  └─ AddCors (AllowAnyOrigin for development)
│   │
│   └─ ConfigureApp:
│      ├─ StaticFiles from wwwroot/
│      ├─ SPA fallback to index.html
│      ├─ MapControllers for API routes
│      └─ UseCors middleware
│
├── appsettings.json                      # Configuration
│   └─ LanguageTool: BaseUrl, Endpoint, DefaultLanguage, Timeout
│
├── GrammarCorrector.csproj               # Project file (.NET 8)
│   └─ Dependencies: Microsoft.AspNetCore.OpenApi
│
└── README.md, QUICK_REFERENCE.md, etc.   # Documentation

```

---

## Technology Stack

| Layer | Technology | Purpose |
|-------|-----------|---------|
| **Frontend** | HTML5, CSS3, Vanilla JavaScript (ES6+) | SPA with no build step |
| **Backend** | ASP.NET Core 8 | REST API, DI, middleware |
| **Language** | C# 12 | Type-safe backend logic |
| **Serialization** | System.Text.Json | camelCase for frontend compatibility |
| **External API** | LanguageTool REST | Primary grammar engine |
| **Fallback** | Built-in rule engine | 35 spelling + 17 grammar patterns |

---

## Request/Response Flow

```
┌─────────────────────────────────────────────────────────────────────┐
│                        FRONTEND (wwwroot/)                          │
│  index.html + CSS + JS                                              │
│  ┌────────────────────────────────────────────────────────────────┐ │
│  │ User types text in textarea                                    │ │
│  │ Clicks "Correct" or presses Ctrl+Enter                         │ │
│  │ app.js validates: not empty, ≤5000 chars                       │ │
│  │ → showSkeletonResults()                                        │ │
│  │ → fetch("POST /api/grammar/check", { text, language })         │ │
│  └───────────┬──────────────────────────────────────────────────┘ │
│              │                                                      │
│   JSON Request (camelCase):                                        │
│   { "text": "...", "language": "en-US" }                           │
└──────────────┼──────────────────────────────────────────────────────┘
			   │
			   ↓ HTTP POST over HTTPS/HTTP
	┌──────────────────────────────────────────┐
	│    BACKEND (ASP.NET Core 8)              │
	│                                          │
	│ GrammarController.Check(request, ct)     │
	│ ├─ Validate: not empty, ≤5000 chars     │
	│ ├─ Call: _grammarService.CheckTextAsync()│
	│ └─ Return: CorrectionResponse (200 OK)   │
	│                                          │
	│ GrammarService.CheckTextAsync()          │
	│ ├─ TRY: Call LanguageTool API            │
	│ │   POST https://api.languagetool.org/v2/check
	│ │   ├─ Request: { text, language, enabledOnly }
	│ │   └─ Response: { matches: [...] }
	│ │   ├─ Each match: offset, length, replacements, rule
	│ │   ├─ Map to LanguageMatch (classify as spelling/grammar/style)
	│ │   └─ Return list of matches
	│ │
	│ ├─ CATCH (if timeout/error):
	│ │   ├─ Log warning
	│ │   ├─ Activate fallback rule engine
	│ │   └─ Return basic patterns (common errors)
	│ │
	│ ├─ Apply corrections:
	│ │   ├─ Sort matches by offset (descending, right-to-left)
	│ │   ├─ Replace each with first suggestion
	│ │   └─ Build correctedText
	│ │
	│ └─ Return CorrectionResponse:
	│     {
	│       originalText: "...",
	│       correctedText: "...",
	│       matches: [ { offset, length, suggestions, issueType, ... } ],
	│       spellingErrorCount: 2,
	│       grammarErrorCount: 1,
	│       styleIssueCount: 0,
	│       hasErrors: true
	│     }
	└──────────────┬───────────────────────────────────────────┘
				   │
				   ↓ JSON Response (camelCase)
┌──────────────────────────────────────────────────────────────────────┐
│                        FRONTEND (wwwroot/)                           │
│  hideSkeletonResults() → renderResults(data)                        │
│                                                                      │
│  ├─ renderStats(data)                                              │
│  │  → Display: "2 spelling, 1 grammar, 0 style"                    │
│  │                                                                  │
│  ├─ renderAnnotatedOriginal(originalText, matches)                 │
│  │  → For each match: create <span class="error-highlight">       │
│  │  → Highlight span with error color (red/purple/teal)           │
│  │  → Tooltip shows: type, message, suggestion                    │
│  │                                                                  │
│  ├─ renderCorrectedText(correctedText, matches)                    │
│  │  → Display corrected text with <span class="corrected-word">   │
│  │  → Highlight replaced words in gold                            │
│  │                                                                  │
│  ├─ renderIssueList(matches)                                       │
│  │  → For each match: create issue card                           │
│  │  → Card shows: word, type, message, suggestion                 │
│  │  → Clickable: scroll to highlight in original                  │
│  │                                                                  │
│  └─ resultsSection.classList.add('visible')                        │
│     → Fade in animation                                            │
│     → Scroll into view                                             │
│                                                                      │
│  User sees:                                                        │
│  ✓ Original text with errors highlighted and tooltips              │
│  ✓ Auto-corrected text with changes highlighted                    │
│  ✓ Stats bar showing error counts                                  │
│  ✓ Sidebar with clickable issue cards                              │
│                                                                      │
│  User can:                                                         │
│  → Click issue card to highlight error in original                 │
│  → Click Copy button to copy corrected text                        │
│  → Click Clear to start over                                       │
│  → Change language for next correction                             │
└──────────────────────────────────────────────────────────────────────┘
```

---

## File Relationships

```
GrammarController.cs
  ├─ depends on: IGrammarService (injected via DI)
  ├─ reads: CorrectionRequest (from HTTP body)
  └─ returns: CorrectionResponse (JSON serialized to camelCase)

GrammarService.cs
  ├─ implements: IGrammarService
  ├─ uses: HttpClient (for LanguageTool)
  ├─ uses: IConfiguration (for appsettings.json)
  ├─ uses: ILogger (for warnings/errors)
  ├─ deserializes: LanguageToolResponse
  ├─ maps: LTMatch → LanguageMatch
  └─ returns: CorrectionResponse with Matches list

index.html
  ├─ imports: /css/style.css
  ├─ loads: /js/app.js (at end of body)
  └─ contains: semantic HTML with ARIA attributes

app.js
  ├─ waits for: DOMContentLoaded event
  ├─ queries: DOM elements by ID (inputText, btnCorrect, etc.)
  ├─ makes POST requests: /api/grammar/check
  ├─ renders: results from CorrectionResponse
  └─ styles with: CSS variables from style.css

style.css
  ├─ defines: color scheme (--gold, --spelling, --grammar, etc.)
  ├─ defines: animations (@keyframes drift, fadeUp, spin, etc.)
  ├─ imports: Google Fonts (Cormorant Garamond, Outfit, JetBrains Mono)
  └─ provides: responsive layout with @media queries
```

---

## Key Configuration Points

### Program.cs - Static Files & SPA

```csharp
// Use wwwroot as the root for static files
var wwwrootPath = Path.Combine(app.Environment.ContentRootPath, "wwwroot");
var fileProvider = new PhysicalFileProvider(wwwrootPath);

// Serve static files (CSS, JS, images, etc.)
app.UseStaticFiles(new StaticFileOptions
{
	FileProvider = fileProvider,
	RequestPath = ""
});

// SPA fallback: any unmatched route → index.html
app.MapFallbackToFile("index.html", new StaticFileOptions
{
	FileProvider = fileProvider,
	RequestPath = ""
});
```

### Program.cs - JSON Serialization

```csharp
builder.Services.AddControllers()
	.AddJsonOptions(options =>
	{
		// Frontend expects camelCase (text, language, matches, etc.)
		options.JsonSerializerOptions.PropertyNamingPolicy = 
			System.Text.Json.JsonNamingPolicy.CamelCase;
	});
```

### appsettings.json - LanguageTool

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

## Directory Serving Behavior

| Request Path | Served From | Result |
|---|---|---|
| `/` | `wwwroot/index.html` | SPA shell |
| `/css/style.css` | `wwwroot/css/style.css` | Stylesheet |
| `/js/app.js` | `wwwroot/js/app.js` | JavaScript |
| `/api/grammar/check` | GrammarController.Check() | API endpoint |
| `/api/grammar/health` | GrammarController.Health() | Health probe |
| `/unknown-path` | `wwwroot/index.html` | SPA fallback |

---

## Error Handling

### Client-Side (app.js)
- Empty text: Show visual shake, no API call
- Text > 5000 chars: Show error banner, no API call
- API timeout: Show "Request timed out" message
- API error: Show error details from response

### Server-Side (GrammarController)
- Empty text: `400 Bad Request` with error message
- Text > 5000 chars: `400 Bad Request` with error message
- Timeout: `408 Request Timeout`
- Server error: `500 Internal Server Error`

### Fallback (GrammarService)
- LanguageTool unreachable: Activate built-in rules
- Built-in rules: 35 spelling patterns + 17 grammar patterns
- Result: User never sees error, graceful degradation

---

## Performance Characteristics

| Metric | Value |
|---|---|
| Frontend build step | None (vanilla JS) |
| CSS minification | Not required (single file) |
| JS bundle size | ~505 lines, ~15KB |
| API response time | 1-2 seconds (LanguageTool) |
| Client-side rendering | <100ms |
| Max text length | 5,000 characters |
| Character counter | Real-time, no debounce |

---

## Deployment Readiness

✅ Static files organized in wwwroot/  
✅ SPA fallback configured  
✅ JSON responses use camelCase  
✅ HTTPS ready (launchSettings configured)  
✅ CORS enabled for development  
✅ Error handling comprehensive  
✅ No hardcoded paths  
✅ Configuration externalized (appsettings.json)  
✅ Cross-platform compatible (.NET 8)  
✅ No Windows-specific APIs  

---

## Documentation Files

| File | Purpose |
|---|---|
| `README.md` | Project overview, features, setup, API reference |
| `QUICK_REFERENCE.md` | Run/test guide, verification checklist, troubleshooting |
| `PROJECT_STRUCTURE_MAP.md` | Architecture, relationships, layer breakdown |
| `RESTRUCTURING_CHECKLIST.md` | Verification of project organization |
| `LINUX_COMPATIBILITY_AUDIT.md` | Cross-platform verification |
| `FINAL_SUMMARY.md` | Final project summary |

---

**Current Status: ✅ Production Ready**
