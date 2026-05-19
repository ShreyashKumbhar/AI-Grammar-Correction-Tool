/* ─────────────────────────────────────────────────────────────────────────────
   Grammar Corrector — Frontend Application
   Handles: text input, API calls, result rendering, highlight & tooltips
───────────────────────────────────────────────────────────────────────────── */

'use strict';

const GRAMMAR_API = '/api/grammar';
const MAX_CHARS = 5000;

// ── State ─────────────────────────────────────────────────────────────────────
let lastResponse = null;
let isLoading    = false;

// ── DOM references (populated on load) ─────────────────────────────────────────
let inputText, charCounter, langSelect, btnCorrect, btnClear, btnCopy;
let resultsSection, originalPanel, correctedPanel, issueList, statsBar;
let errorBanner, errorMsg;
let authOverlay, authUserBar, authGuestBar, usageBadge;

// ── Bootstrap ─────────────────────────────────────────────────────────────────
document.addEventListener('DOMContentLoaded', init);

function init() {
  // Initialize DOM references
  inputText      = document.getElementById('inputText');
  charCounter    = document.getElementById('charCounter');
  langSelect     = document.getElementById('langSelect');
  btnCorrect     = document.getElementById('btnCorrect');
  btnClear       = document.getElementById('btnClear');
  btnCopy        = document.getElementById('btnCopy');
  resultsSection = document.getElementById('resultsSection');
  originalPanel  = document.getElementById('originalPanel');
  correctedPanel = document.getElementById('correctedPanel');
  issueList      = document.getElementById('issueList');
  statsBar       = document.getElementById('statsBar');
  errorBanner    = document.getElementById('errorBanner');
  errorMsg       = document.getElementById('errorMsg');
  authOverlay    = document.getElementById('authOverlay');
  authUserBar    = document.getElementById('authUserBar');
  authGuestBar   = document.getElementById('authGuestBar');
  usageBadge     = document.getElementById('usageBadge');

  console.log('Initializing Grammar Corrector app...');
  console.log('DOM elements found:', {
    inputText: !!inputText,
    btnCorrect: !!btnCorrect,
    btnClear: !!btnClear,
    langSelect: !!langSelect,
    resultsSection: !!resultsSection,
    errorBanner: !!errorBanner
  });

  // Validate all required elements are present
  if (!inputText || !btnCorrect || !btnClear || !langSelect || !resultsSection) {
    console.error('ERROR: Required DOM elements not found!');
    return;
  }

  // Ensure results section is hidden initially
  resultsSection.classList.remove('visible');

  bindEvents();
  updateCharCounter();
  initAuthUI();
  console.log('Initialization complete');
}

// ── Authentication UI ─────────────────────────────────────────────────────────
function initAuthUI() {
  updateAuthUI();

  document.getElementById('btnSignOut')?.addEventListener('click', () => logout());
}

function updateAuthUI() {
  const loggedIn = isAuthenticated();
  const user = getCurrentUser();

  authGuestBar?.classList.toggle('hidden', loggedIn);
  authUserBar?.classList.toggle('hidden', !loggedIn);
  authOverlay?.classList.toggle('hidden', loggedIn);

  if (inputText) inputText.disabled = !loggedIn;
  if (langSelect) langSelect.disabled = !loggedIn;
  if (btnCorrect) btnCorrect.disabled = !loggedIn;
  if (btnClear) btnClear.disabled = !loggedIn;

  if (loggedIn && user) {
    const nameEl = document.getElementById('userDisplayName');
    const tierEl = document.getElementById('userTierBadge');
    if (nameEl) nameEl.textContent = user.fullName || user.email;
    if (tierEl) {
      const tier = user.subscriptionTier || 'Free';
      tierEl.textContent = tier;
      tierEl.className = `tier-badge tier-${tier.toLowerCase()}`;
    }
    loadQuotaStatus();
  } else if (usageBadge) {
    usageBadge.textContent = '';
    usageBadge.classList.add('hidden');
  }
}

async function loadQuotaStatus() {
  if (!usageBadge || !isAuthenticated()) return;

  try {
    const res = await authenticatedFetch(`${API_BASE}/subscription/quota-status`);
    if (!res?.ok) return;

    const data = await res.json();
    if (data.isUnlimited) {
      usageBadge.textContent = 'Unlimited';
    } else {
      usageBadge.textContent = `${data.used ?? 0} / ${data.limit ?? 500} this month`;
    }
    usageBadge.classList.remove('hidden');

    if (data.isExceeded) {
      usageBadge.classList.add('quota-exceeded');
      if (btnCorrect) btnCorrect.disabled = true;
    } else {
      usageBadge.classList.remove('quota-exceeded');
      if (btnCorrect && !isLoading) btnCorrect.disabled = false;
    }
  } catch (err) {
    console.warn('Could not load quota status:', err);
  }
}

function requireAuth() {
  if (isAuthenticated()) return true;

  showError('Please sign in or create an account to use the grammar checker.');
  authOverlay?.classList.remove('hidden');
  return false;
}

// ── Event bindings ────────────────────────────────────────────────────────────
function bindEvents() {
  inputText.addEventListener('input', onInput);
  inputText.addEventListener('keydown', e => {
    // Cmd/Ctrl+Enter triggers correction
    if ((e.metaKey || e.ctrlKey) && e.key === 'Enter') {
      e.preventDefault();
      triggerCheck();
    }
  });

  btnCorrect.addEventListener('click', triggerCheck);
  btnClear.addEventListener('click', clearAll);
  btnCopy?.addEventListener('click', copyResult);
}

// ── Input handling ────────────────────────────────────────────────────────────
function onInput() {
  updateCharCounter();
}

function updateCharCounter() {
  const len = inputText.value.length;
  charCounter.textContent = `${len.toLocaleString()} / ${MAX_CHARS.toLocaleString()}`;
  charCounter.className = 'char-counter';
  if (len > MAX_CHARS * 0.85) charCounter.classList.add('warn');
  if (len > MAX_CHARS)        charCounter.classList.add('over');
}

// ── API call ──────────────────────────────────────────────────────────────────
async function triggerCheck() {
  if (isLoading) return;
  if (!requireAuth()) return;

  const text = inputText.value.trim();
  if (!text) {
    inputText.focus();
    shakeElement(inputText);
    return;
  }
  if (text.length > MAX_CHARS) {
    showError(`Text exceeds ${MAX_CHARS.toLocaleString()} characters.`);
    return;
  }

  setLoading(true);
  hideError();
  showSkeletonResults();

  try {
    console.log('Making API call to:', `${GRAMMAR_API}/check`);
    console.log('Payload:', { text, language: langSelect.value });

    const res = await authenticatedFetch(`${GRAMMAR_API}/check`, {
      method:  'POST',
      body:    JSON.stringify({ text, language: langSelect.value }),
      signal:  AbortSignal.timeout(20000)
    });

    if (!res) return;

    console.log('Response status:', res.status);

    if (!res.ok) {
      let errorMsg = `HTTP ${res.status}`;
      try {
        const err = await res.json();
        errorMsg = err.error || err.message || errorMsg;
      } catch (parseErr) {
        // Failed to parse error response as JSON
        try {
          const textErr = await res.text();
          console.error('Response text:', textErr);
          if (textErr) {
            errorMsg = textErr.substring(0, 200); // Limit error message length
          }
        } catch (textErr) {
          console.error('Failed to read response:', textErr);
        }
      }
      throw new Error(errorMsg);
    }

    lastResponse = await res.json();
    console.log('API Response:', lastResponse);
    renderResults(lastResponse);
    loadQuotaStatus();

  } catch (err) {
    console.error('API Error:', err);
    hideSkeletonResults();
    if (err.name === 'TimeoutError') {
      showError('The request timed out. Please try again.');
    } else {
      showError(err.message || 'An unexpected error occurred.');
    }
  } finally {
    setLoading(false);
  }
}

// ── Render results ────────────────────────────────────────────────────────────
function renderResults(data) {
  // Stats bar
  renderStats(data);

  // Annotated original text
  renderAnnotatedOriginal(data.originalText, data.matches);

  // Auto-corrected text
  renderCorrectedText(data.correctedText, data.originalText, data.matches);

  // Sidebar issues
  renderIssueList(data.matches);

  // Show section
  hideSkeletonResults();
  resultsSection.classList.add('visible');

  // Scroll into view
  setTimeout(() => {
    resultsSection.scrollIntoView({ behavior: 'smooth', block: 'start' });
  }, 100);
}

// ── Stats bar ─────────────────────────────────────────────────────────────────
function renderStats(data) {
  statsBar.innerHTML = '';

  if (!data.hasErrors) {
    statsBar.innerHTML = `
      <span class="stat-chip perfect">
        <span class="dot" style="background:var(--style); box-shadow:0 0 8px var(--style)"></span>
        No issues found
      </span>`;
    return;
  }

  if (data.spellingErrorCount > 0) {
    statsBar.appendChild(makeStatChip(
      'spelling',
      `${data.spellingErrorCount} spelling ${plural(data.spellingErrorCount, 'error')}`
    ));
  }
  if (data.grammarErrorCount > 0) {
    statsBar.appendChild(makeStatChip(
      'grammar',
      `${data.grammarErrorCount} grammar ${plural(data.grammarErrorCount, 'issue')}`
    ));
  }
  if (data.styleIssueCount > 0) {
    statsBar.appendChild(makeStatChip(
      'style',
      `${data.styleIssueCount} style ${plural(data.styleIssueCount, 'suggestion')}`
    ));
  }

  const sep = document.createElement('div');
  sep.className = 'stat-separator';
  statsBar.appendChild(sep);

  const total = data.matches.length;
  const note = document.createElement('span');
  note.style.cssText = 'font-size:.78rem; color:var(--text-muted)';
  note.textContent = `${total} ${plural(total, 'issue')} detected`;
  statsBar.appendChild(note);
}

function makeStatChip(type, label) {
  const chip = document.createElement('span');
  chip.className = `stat-chip ${type}`;
  chip.innerHTML = `<span class="dot"></span>${label}`;
  return chip;
}

// ── Annotated original text ───────────────────────────────────────────────────
function renderAnnotatedOriginal(text, matches) {
  if (!matches || matches.length === 0) {
    originalPanel.textContent = text;
    return;
  }

  // Sort matches by offset ascending, remove overlaps
  const sorted = deduplicateMatches([...matches].sort((a, b) => a.offset - b.offset));

  let html = '';
  let cursor = 0;

  for (const match of sorted) {
    // Text before this match
    if (match.offset > cursor) {
      html += escHtml(text.slice(cursor, match.offset));
    }

    const word   = text.slice(match.offset, match.offset + match.length);
    const type   = match.issueType || 'grammar';
    const sugg   = match.suggestions?.[0] ?? '';
    const msg    = escHtml(match.message);
    const shortM = escHtml(match.shortMessage || match.message);

    html += `
      <span class="error-highlight ${type}" data-offset="${match.offset}">
        ${escHtml(word)}
        <span class="tooltip">
          <div class="tooltip-type ${type}">${capitalize(type)}</div>
          <div class="tooltip-msg">${msg}</div>
          ${sugg ? `<span class="tooltip-suggestion">${escHtml(sugg)}</span>` : ''}
        </span>
      </span>`;

    cursor = match.offset + match.length;
  }

  // Remaining text
  if (cursor < text.length) {
    html += escHtml(text.slice(cursor));
  }

  originalPanel.innerHTML = html;
}

// ── Corrected text ────────────────────────────────────────────────────────────
function renderCorrectedText(corrected, original, matches) {
  if (!matches || matches.length === 0) {
    correctedPanel.textContent = corrected;
    return;
  }

  // Build a set of corrected substrings so we can highlight them in the output
  // We compare char-by-char to find replaced spans (simple diff)
  correctedPanel.innerHTML = escHtml(corrected); // plain fallback

  // Attempt a token-level highlight of changed words
  try {
    correctedPanel.innerHTML = highlightCorrectedWords(original, corrected, matches);
  } catch (_) {
    correctedPanel.textContent = corrected;
  }
}

function highlightCorrectedWords(original, corrected, matches) {
  // Build spans of changes using match offsets and suggestions
  const sorted   = deduplicateMatches([...matches].sort((a, b) => a.offset - b.offset));
  let corrCursor = 0;
  let origCursor = 0;
  let html       = '';

  for (const match of sorted) {
    if (match.suggestions.length === 0) continue;
    const suggestion = match.suggestions[0];

    // Advance corrected cursor by the delta
    const beforeOrig = original.slice(origCursor, match.offset);
    html += escHtml(corrected.slice(corrCursor, corrCursor + beforeOrig.length));
    corrCursor += beforeOrig.length;
    origCursor  = match.offset;

    // Highlight the suggestion
    html += `<span class="corrected-word">${escHtml(suggestion)}</span>`;
    corrCursor += suggestion.length;
    origCursor += match.length;
  }

  // Remainder
  html += escHtml(corrected.slice(corrCursor));
  return html;
}

// ── Issue sidebar ─────────────────────────────────────────────────────────────
function renderIssueList(matches) {
  issueList.innerHTML = '';

  if (!matches || matches.length === 0) {
    issueList.innerHTML = `
      <div class="perfect-banner">
        <div class="perfect-icon" aria-hidden="true"></div>
        <div class="perfect-text">
          <strong>No issues found</strong>
          <span>Your text looks great!</span>
        </div>
      </div>`;
    return;
  }

  for (let i = 0; i < matches.length; i++) {
    const m    = matches[i];
    const type = m.issueType || 'grammar';
    const sugg = m.suggestions?.[0] ?? '';

    const card = document.createElement('div');
    card.className = `issue-card ${type}`;
    card.dataset.offset = m.offset;

    card.innerHTML = `
      <div class="issue-header">
        <span class="issue-word ${type}">${escHtml(m.originalText || `#${i+1}`)}</span>
        <span class="issue-tag ${type}">${capitalize(type)}</span>
      </div>
      <div class="issue-msg">${escHtml(m.shortMessage || m.message)}</div>
      ${sugg ? `
      <div class="issue-fix">
        Suggestion:
        <span class="issue-fix-word">${escHtml(sugg)}</span>
      </div>` : ''}`;

    // Click scrolls to and pulses the highlight in original panel
    card.addEventListener('click', () => scrollToHighlight(m.offset));
    issueList.appendChild(card);
  }
}

// ── Highlight interaction ─────────────────────────────────────────────────────
function scrollToHighlight(offset) {
  const el = originalPanel.querySelector(`[data-offset="${offset}"]`);
  if (!el) return;
  el.scrollIntoView({ behavior: 'smooth', block: 'center' });
  el.style.outline = '2px solid var(--gold)';
  el.style.outlineOffset = '2px';
  setTimeout(() => { el.style.outline = ''; el.style.outlineOffset = ''; }, 1600);
}

// ── Copy to clipboard ─────────────────────────────────────────────────────────
async function copyResult() {
  const text = lastResponse?.correctedText;
  if (!text) return;

  try {
    await navigator.clipboard.writeText(text);
    btnCopy.classList.add('copied');
    btnCopy.innerHTML = `
      <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
        <polyline points="20 6 9 17 4 12"/>
      </svg> Copied!`;
    setTimeout(() => {
      btnCopy.classList.remove('copied');
      btnCopy.innerHTML = `
        <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
          <rect x="9" y="9" width="13" height="13" rx="2"/><path d="M5 15H4a2 2 0 0 1-2-2V4a2 2 0 0 1 2-2h9a2 2 0 0 1 2 2v1"/>
        </svg> Copy corrected`;
    }, 2200);
  } catch (_) {
    // Fallback for browsers without clipboard API
    const ta = document.createElement('textarea');
    ta.value = text;
    ta.style.position = 'fixed';
    ta.style.opacity = '0';
    document.body.appendChild(ta);
    ta.select();
    document.execCommand('copy');
    document.body.removeChild(ta);
  }
}

// ── Clear ─────────────────────────────────────────────────────────────────────
function clearAll() {
  if (!isAuthenticated()) {
    requireAuth();
    return;
  }

  inputText.value = '';
  updateCharCounter();
  resultsSection.classList.remove('visible');
  lastResponse = null;
  hideError();
  inputText.focus();
}

// ── Loading state ─────────────────────────────────────────────────────────────
function setLoading(on) {
  isLoading = on;
  btnCorrect.disabled = on || !isAuthenticated();

  if (on) {
    btnCorrect.innerHTML = `
      <div class="spinner"></div>
      Analysing…`;
  } else {
    btnCorrect.innerHTML = `
      Correct
      <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5">
        <path d="M5 12h14M12 5l7 7-7 7"/>
      </svg>`;
  }
}

// ── Skeleton loader ───────────────────────────────────────────────────────────
function showSkeletonResults() {
  resultsSection.classList.add('visible');
  originalPanel.innerHTML = `
    <div class="skeleton" style="width:90%;height:14px"></div>
    <div class="skeleton" style="width:75%;height:14px"></div>
    <div class="skeleton" style="width:60%;height:14px"></div>`;
  correctedPanel.innerHTML = `
    <div class="skeleton" style="width:88%;height:14px"></div>
    <div class="skeleton" style="width:70%;height:14px"></div>`;
  issueList.innerHTML = `
    <div class="skeleton" style="height:80px;border-radius:12px"></div>
    <div class="skeleton" style="height:80px;border-radius:12px"></div>`;
  statsBar.innerHTML = `<div class="skeleton" style="width:200px;height:24px;border-radius:100px"></div>`;
}

function hideSkeletonResults() {
  // content will be replaced by real data; just ensure section is visible
}

// ── Error banner ──────────────────────────────────────────────────────────────
function showError(msg) {
  errorMsg.textContent = msg;
  errorBanner.style.display = 'flex';
}

function hideError() {
  errorBanner.style.display = 'none';
}

// ── Utilities ─────────────────────────────────────────────────────────────────
function escHtml(str) {
  return String(str)
    .replace(/&/g, '&amp;')
    .replace(/</g, '&lt;')
    .replace(/>/g, '&gt;')
    .replace(/"/g, '&quot;');
}

function capitalize(str) {
  return str ? str[0].toUpperCase() + str.slice(1) : '';
}

function plural(n, word) {
  return n === 1 ? word : word + 's';
}

function shakeElement(el) {
  el.style.animation = 'none';
  el.offsetHeight; // reflow
  el.style.animation = 'shake .4s ease';
  setTimeout(() => { el.style.animation = ''; }, 400);
}

// Remove overlapping matches (keep the first / longest)
function deduplicateMatches(sorted) {
  const result = [];
  let lastEnd  = -1;
  for (const m of sorted) {
    if (m.offset >= lastEnd) {
      result.push(m);
      lastEnd = m.offset + m.length;
    }
  }
  return result;
}

// ── Inline shake keyframe (injected once) ─────────────────────────────────────
(function injectShakeStyle() {
  const s = document.createElement('style');
  s.textContent = `
    @keyframes shake {
      0%,100%{transform:translateX(0)}
      20%{transform:translateX(-6px)}
      40%{transform:translateX(6px)}
      60%{transform:translateX(-4px)}
      80%{transform:translateX(4px)}
    }`;
  document.head.appendChild(s);
})();
