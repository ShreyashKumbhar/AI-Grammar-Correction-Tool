# Quick Reference - Run & Test Guide

## 🚀 Quick Start (Copy & Paste)

```powershell
# Navigate to project
cd C:\Users\Admin\source\repos\GrammarCorrector\

# Build
dotnet build

# Run
dotnet run
```

Then open: **https://localhost:50866/**

---

## ✅ Verification Checklist

### 1. Application Starts
- [ ] No build errors
- [ ] Server listens on port 50866
- [ ] Browser opens to https://localhost:50866/

### 2. Page Loads
- [ ] "Prose — AI Grammar Corrector" title visible
- [ ] Dark theme UI displays correctly
- [ ] Textarea for input is visible
- [ ] "Correct" button is visible
- [ ] Open F12 console shows no errors

### 3. Console Shows Initialization
In browser Developer Console (F12), you should see:
```
Initializing Grammar Corrector app...
DOM elements found: { inputText: true, btnCorrect: true, ... }
Initialization complete
```

### 4. Test Grammar Correction
**Input:** Type in the textarea:
```
She go to school every day
```

**Action:** Press `Ctrl+Enter` or click the "Correct" button

**Console Should Show:**
```
Making API call to: /api/grammar/check
Payload: { text: "She go to school every day", language: "en-US" }
Response status: 200
API Response: { originalText: "...", correctedText: "She goes to school every day", ... }
```

**Result Should Display:**
- [ ] Original text with "go" highlighted in purple (grammar error)
- [ ] Corrected text showing "goes"
- [ ] Stats showing "1 grammar issue detected"
- [ ] Sidebar showing the error with suggestion

### 5. Test Error Handling
**Input:** Leave textarea empty

**Action:** Click "Correct" button

**Result:**
- [ ] Input field shakes (visual feedback)
- [ ] No API call is made (check console)

### 6. Test Character Counter
**Input:** Type several words

**Result:**
- [ ] Character count updates in real-time
- [ ] Format: "X / 5,000"
- [ ] Warning at 85% (yellow)
- [ ] Error at 100% (red)

---

## 🔧 Key Files Modified

| File | Change | Why |
|------|--------|-----|
| Program.cs | Added JSON camelCase config | Frontend expects camelCase properties |
| Program.cs | Added static files from project root | CSS/JS files are in project root, not wwwroot |
| index.html | Fixed `/css/style.css` → `/style.css` | File is in root, not in css/ subdirectory |
| index.html | Fixed `/js/app.js` → `/app.js` | File is in root, not in js/ subdirectory |
| app.js | Refactored DOM initialization | Wait for page load before accessing DOM |
| app.js | Added console logging | Debug API calls and initialization |

---

## 🐛 Troubleshooting

### Problem: "localhost page can't be found"
**Solution:** Check if dotnet run is still active. You should see:
```
Now listening on: https://localhost:50866
```

### Problem: Page loads but nothing happens when clicking "Correct"
**Steps:**
1. Open browser Developer Console (F12)
2. Look for error messages
3. Check Network tab to see if API request was made
4. Share any red errors from console

### Problem: Style/theme looks wrong
**Solution:**
1. Hard refresh: `Ctrl+Shift+R` (Windows) or `Cmd+Shift+R` (Mac)
2. Check that `/style.css` loads in Network tab
3. Should show Status 200

### Problem: "API Response" doesn't appear in console
**Steps:**
1. Check Network tab for `/api/grammar/check` request
2. Click on the request to see response
3. Look for status code (should be 200 for success)

### Problem: Results appear but are empty/blank
**Solution:**
1. Check Network tab response for `/api/grammar/check`
2. Response should show camelCase JSON:
   - `originalText` (not `OriginalText`)
   - `matches` (not `Matches`)
   - `correctedText` (not `CorrectedText`)
3. If still showing PascalCase, Program.cs JSON config wasn't applied

---

## 📊 Expected Behavior Summary

| Action | Expected Result |
|--------|-----------------|
| **Page Load** | Hero section displays, textarea ready for input |
| **Type Text** | Character counter updates live |
| **Click Correct** | Skeleton loader appears, then results |
| **No Errors in Text** | "No issues found" message with checkmark |
| **Grammar Error** | Error highlighted in purple, suggestion shown |
| **Spelling Error** | Error highlighted in red, suggestion shown |
| **Style Issue** | Error highlighted in teal, suggestion shown |
| **Empty Input** | Textarea shakes, no API call made |
| **Text > 5000 chars** | Error message shown, no API call made |
| **Change Language** | Next correction uses selected language |
| **Click Clear** | Input cleared, results hidden, ready for new input |
| **Click Copy** | "Copied!" message, text in clipboard |
| **API Timeout** | "Request timed out" error message |
| **API Down** | Built-in rules activate, basic errors detected |

---

## 🎯 API Endpoint Details

**Endpoint:** `POST /api/grammar/check`

**Request Headers:**
```
Content-Type: application/json
```

**Request Body:**
```json
{
  "text": "She go to school",
  "language": "en-US"
}
```

**Success Response (200):**
```json
{
  "originalText": "She go to school",
  "correctedText": "She goes to school",
  "matches": [
	{
	  "offset": 4,
	  "length": 2,
	  "originalText": "go",
	  "message": "Subject–verb agreement: use 'goes' with 'she'.",
	  "shortMessage": "Grammar issue",
	  "suggestions": ["goes"],
	  "issueType": "grammar",
	  "ruleId": "FALLBACK_GRAMMAR",
	  "category": "Grammar"
	}
  ],
  "spellingErrorCount": 0,
  "grammarErrorCount": 1,
  "styleIssueCount": 0,
  "hasErrors": true
}
```

**Error Response (400):**
```json
{
  "error": "Text must not be empty."
}
```

---

## 🌍 Supported Languages

- `en-US` - English (US) - default
- `en-GB` - English (UK)
- `en-AU` - English (Australia)
- `de-DE` - Deutsch (German)
- `fr` - Français (French)
- `es` - Español (Spanish)
- `pt-BR` - Português (Portuguese Brazil)
- `nl` - Nederlands (Dutch)
- `it` - Italiano (Italian)
- `pl-PL` - Polski (Polish)

---

## 📈 Performance Notes

- API timeout: 20 seconds (frontend), 15 seconds (backend)
- Max text length: 5,000 characters
- Character counter updates in real-time
- Results section animates in smoothly
- Skeleton loader shows during API call

---

## 🔐 Error Fallback

If LanguageTool API is unavailable:
- Built-in rule engine activates automatically
- Detects 35+ common spelling mistakes
- Detects 10+ common grammar patterns
- No error shown to user
- Seamless experience

---

## ✨ Keyboard Shortcuts

- `Ctrl+Enter` - Correct text (Windows)
- `Cmd+Return` - Correct text (Mac)
- `Tab` - Focus next element
- `Shift+Tab` - Focus previous element

---

## 📱 Responsive Design

- **Desktop (>720px):** Two-column layout (text + sidebar)
- **Tablet (≤720px):** Single column (text stacks above sidebar)
- **Mobile:** Optimized spacing and font sizes

---

## 🎨 Color Scheme

- **Spelling Errors:** Red (#e85d75)
- **Grammar Issues:** Purple (#7b6cf6)
- **Style Issues:** Teal (#44c5a6)
- **Accent:** Gold (#c8a96e)
- **Background:** Deep charcoal (#080810)

---

## 📞 Quick Diagnostics

Run these in browser console to verify setup:

```javascript
// Check if app is initialized
console.log('App initialized:', typeof inputText !== 'undefined' && inputText !== null);

// Check API endpoint
console.log('API Base:', API_BASE); // Should output: /api/grammar

// Check if results section exists
console.log('Results section:', resultsSection); // Should not be null

// Make a test API call
fetch('/api/grammar/check', {
  method: 'POST',
  headers: { 'Content-Type': 'application/json' },
  body: JSON.stringify({ text: 'test', language: 'en-US' })
})
.then(r => r.json())
.then(d => console.log('API Works:', d))
.catch(e => console.error('API Error:', e));
```

---

## ✅ Final Checklist Before Deployment

- [ ] `dotnet build` completes without errors
- [ ] Browser console shows "Initialization complete"
- [ ] CSS loads correctly (Network tab shows `/style.css` 200)
- [ ] JavaScript loads correctly (Network tab shows `/app.js` 200)
- [ ] JSON responses use camelCase (check Network tab)
- [ ] Correction works with at least one test
- [ ] Error handling works (empty input test)
- [ ] Language selection works
- [ ] Copy button works
- [ ] Clear button works
- [ ] Responsive on mobile (use F12 device mode)

---

**All systems go! 🚀**
