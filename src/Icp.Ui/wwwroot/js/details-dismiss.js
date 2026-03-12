(function () {
  function init() {
    const detailsList = document.querySelectorAll('details.icp-dropdown');
    if (!detailsList.length) return;

    document.addEventListener('click', (ev) => {
      for (const d of detailsList) {
        if (!d.open) continue;
        if (d.contains(ev.target)) continue;
        d.open = false;
      }
    });

    document.addEventListener('keydown', (ev) => {
      if (ev.key !== 'Escape') return;
      for (const d of detailsList) {
        if (d.open) d.open = false;
      }
    });
  }

  if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', init);
  } else {
    init();
  }
})();
