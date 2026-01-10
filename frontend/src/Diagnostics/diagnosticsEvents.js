const maxBufferSize = 500;
const flushThreshold = 200;
const flushIntervalMs = 15000;
const maxTextLength = 160;
let buffer = [];
let flushTimer = null;
let isFlushing = false;

function isDevelopBranch() {
  return window.Readarr && window.Readarr.branch === 'develop';
}

function getPath() {
  const urlBase = window.Readarr.urlBase;
  const pathname = window.location.pathname;
  return urlBase && pathname.startsWith(urlBase) ? pathname.substring(urlBase.length) || '/' : pathname;
}

function safeText(value) {
  if (!value) {
    return null;
  }

  const trimmed = value.trim().replace(/\s+/g, ' ');

  if (!trimmed.length) {
    return null;
  }

  return trimmed.slice(0, maxTextLength);
}

function redactKey(key) {
  return /password|token|apikey|secret|key/i.test(key);
}

function redactObject(obj, depth = 0) {
  if (!obj || typeof obj !== 'object' || depth > 3) {
    return obj;
  }

  if (Array.isArray(obj)) {
    return obj.slice(0, 10).map((item) => redactObject(item, depth + 1));
  }

  return Object.keys(obj).reduce((acc, key) => {
    if (redactKey(key)) {
      acc[key] = 'REDACTED';
    } else {
      acc[key] = redactObject(obj[key], depth + 1);
    }

    return acc;
  }, {});
}

function scheduleFlush() {
  if (flushTimer || isFlushing) {
    return;
  }

  flushTimer = window.setTimeout(() => {
    flushTimer = null;
    flushEvents();
  }, flushIntervalMs);
}

function pushEvent(event) {
  if (!isDevelopBranch()) {
    return;
  }

  buffer.push(event);

  if (buffer.length > maxBufferSize) {
    buffer = buffer.slice(buffer.length - maxBufferSize);
  }

  if (buffer.length >= flushThreshold) {
    flushEvents();
  } else {
    scheduleFlush();
  }
}

export function recordEvent(type, data) {
  pushEvent({
    type,
    timestamp: new Date().toISOString(),
    path: getPath(),
    data: redactObject(data)
  });
}

export function recordApiResult({ method, url, status, ok, durationMs }) {
  recordEvent('api', {
    method,
    url,
    status,
    ok,
    durationMs
  });
}

export function recordMessage(message) {
  recordEvent('message', message);
}

function describeTarget(target) {
  if (!target || !target.tagName) {
    return null;
  }

  const tag = target.tagName.toLowerCase();
  const type = target.getAttribute('type');
  const isFormInput = tag === 'input' || tag === 'textarea' || tag === 'select';
  const label = isFormInput ? null : safeText(target.textContent);

  return {
    tag,
    id: target.id || null,
    name: target.getAttribute('name') || null,
    type,
    role: target.getAttribute('role') || null,
    ariaLabel: target.getAttribute('aria-label') || null,
    title: target.getAttribute('title') || null,
    text: label
  };
}

export function initDiagnostics(store, history) {
  if (!isDevelopBranch()) {
    return;
  }

  document.addEventListener('click', (event) => {
    recordEvent('click', {
      target: describeTarget(event.target)
    });
  }, true);

  document.addEventListener('submit', (event) => {
    recordEvent('submit', {
      target: describeTarget(event.target),
      action: event.target?.getAttribute?.('action') || null
    });
  }, true);

  if (history?.listen) {
    history.listen((location) => {
      recordEvent('route', {
        pathname: location.pathname,
        search: location.search
      });
    });
  }

  if (store?.subscribe) {
    let previousCount = 0;

    store.subscribe(() => {
      const state = store.getState();
      const items = state?.app?.messages?.items || [];

      if (items.length !== previousCount) {
        const latest = items[items.length - 1];
        if (latest) {
          recordMessage({
            id: latest.id,
            kind: latest.kind,
            message: latest.message,
            title: latest.title
          });
        }

        previousCount = items.length;
      }
    });
  }
}

export async function flushEvents() {
  if (!isDevelopBranch() || isFlushing || buffer.length === 0) {
    return;
  }

  isFlushing = true;
  const payload = buffer.slice(0);
  buffer = [];

  try {
    await fetch(`${window.Readarr.apiRoot}/diagnostics/ui-events`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        'X-Api-Key': window.Readarr.apiKey
      },
      body: JSON.stringify(payload)
    });
  } catch (error) {
    buffer = payload.concat(buffer);
  } finally {
    isFlushing = false;
  }
}

if (window.Readarr) {
  window.ReadarrDiagnostics = {
    flush: flushEvents,
    recordEvent
  };
}
