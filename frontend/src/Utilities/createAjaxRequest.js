import $ from 'jquery';
import { recordApiResult } from 'Diagnostics/diagnosticsEvents';

const absUrlRegex = /^(https?:)?\/\//i;
const apiRoot = window.Readarr.apiRoot;

function isRelative(ajaxOptions) {
  return !absUrlRegex.test(ajaxOptions.url);
}

function addRootUrl(ajaxOptions) {
  ajaxOptions.url = apiRoot + ajaxOptions.url;
}

function addApiKey(ajaxOptions) {
  ajaxOptions.headers = ajaxOptions.headers || {};
  ajaxOptions.headers['X-Api-Key'] = window.Readarr.apiKey;
}

function addContentType(ajaxOptions) {
  if (
    ajaxOptions.contentType == null &&
    ajaxOptions.dataType === 'json' &&
    (ajaxOptions.method === 'PUT' || ajaxOptions.method === 'POST' || ajaxOptions.method === 'DELETE')) {
    ajaxOptions.contentType = 'application/json';
  }
}

export default function createAjaxRequest(originalAjaxOptions) {
  const requestXHR = new window.XMLHttpRequest();
  let aborted = false;
  let complete = false;
  const startTime = Date.now();

  function abortRequest() {
    if (!complete) {
      aborted = true;
      requestXHR.abort();
    }
  }

  const ajaxOptions = { ...originalAjaxOptions };

  if (isRelative(ajaxOptions)) {
    addRootUrl(ajaxOptions);
    addApiKey(ajaxOptions);
    addContentType(ajaxOptions);
  }

  const method = (ajaxOptions.method || ajaxOptions.type || 'GET').toUpperCase();
  const shouldRecord = !ajaxOptions.skipDiagnostics;
  const urlForLog = ajaxOptions.url;

  const request = $.ajax({
    xhr: () => requestXHR,
    ...ajaxOptions
  }).done((data, textStatus, xhr) => {
    if (shouldRecord) {
      recordApiResult({
        method,
        url: urlForLog,
        status: xhr.status,
        ok: true,
        durationMs: Date.now() - startTime
      });
    }
  }).fail((xhr) => {
    if (shouldRecord) {
      recordApiResult({
        method,
        url: urlForLog,
        status: xhr.status,
        ok: false,
        durationMs: Date.now() - startTime
      });
    }
  }).then(null, (xhr, textStatus, errorThrown) => {
    xhr.aborted = aborted;

    return $.Deferred().reject(xhr, textStatus, errorThrown).promise();
  }).always(() => {
    complete = true;
  });

  return {
    request,
    abortRequest
  };
}
