(function () {
  function jsonToKv(json) {
    if (!json) return '';
    try {
      const obj = JSON.parse(json);
      if (!obj || typeof obj !== 'object' || Array.isArray(obj)) return '';
      const keys = Object.keys(obj);
      keys.sort();
      return keys.map(k => `${k}=${obj[k] ?? ''}`).join('\n');
    } catch {
      return '';
    }
  }

  function init() {
    const sel = document.getElementById('integrationTarget');
    const txt = document.getElementById('parametersText');
    if (!sel || !txt) return;

    function applyTemplate() {
      const opt = sel.options[sel.selectedIndex];
      const tpl = opt?.getAttribute('data-template');
      if (!tpl) return;
      txt.value = jsonToKv(tpl);
    }

    sel.addEventListener('change', applyTemplate);

    // Optional reset link (Edit page)
    const reset = document.getElementById('resetFromTemplate');
    if (reset) {
      reset.addEventListener('click', (ev) => {
        ev.preventDefault();
        applyTemplate();
      });
    }

    // EventType template support
    const evtSel = document.getElementById('subscribedEventType');
    const evtTxt = document.getElementById('eventTypeParametersText');
    function applyEventTypeTemplate() {
      if (!evtSel || !evtTxt) return;
      const opt = evtSel.options[evtSel.selectedIndex];
      const tpl = opt?.getAttribute('data-template');
      if (!tpl) return;
      evtTxt.value = jsonToKv(tpl);
    }

    if (evtSel && evtTxt) {
      evtSel.addEventListener('change', applyEventTypeTemplate);
      const evtReset = document.getElementById('resetEventTypeFromTemplate');
      if (evtReset) {
        evtReset.addEventListener('click', (ev) => {
          ev.preventDefault();
          applyEventTypeTemplate();
        });
      }
    }
  }

  if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', init);
  } else {
    init();
  }
})();
