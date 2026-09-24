// Client-side behaviours for the app shell and forms. No bundler: this file is served as-is.

// Auto-dismisses the flash messages rendered by the layout after a short delay, and lets the user
// close them immediately through the Bootstrap alert button.
(function () {
  var AUTO_DISMISS_MS = 5000;

  document.addEventListener('DOMContentLoaded', function () {
    document.querySelectorAll('.js-auto-dismiss').forEach(function (alert) {
      window.setTimeout(function () {
        if (window.bootstrap && window.bootstrap.Alert) {
          window.bootstrap.Alert.getOrCreateInstance(alert).close();
        } else {
          alert.remove();
        }
      }, AUTO_DISMISS_MS);
    });
  });
})();

// Toggles the sidebar. On large screens it collapses to an icon rail and the choice is remembered
// in localStorage; on small screens it opens the off-canvas panel over a backdrop.
(function () {
  var STORAGE_KEY = 'stockflow.sidebar';
  var DESKTOP_QUERY = '(min-width: 992px)';

  document.addEventListener('DOMContentLoaded', function () {
    var sidebar = document.getElementById('appSidebar');
    var backdrop = document.querySelector('[data-sidebar-backdrop]');
    var buttons = document.querySelectorAll('[data-sidebar-toggle]');
    var desktop = window.matchMedia(DESKTOP_QUERY);

    if (!sidebar || buttons.length === 0) {
      return;
    }

    function isDesktop() {
      return desktop.matches;
    }

    function isCollapsed() {
      return document.documentElement.classList.contains('app-sidebar-collapsed');
    }

    function isOpen() {
      return sidebar.classList.contains('show');
    }

    function syncButtons() {
      var expanded = isDesktop() ? !isCollapsed() : isOpen();

      buttons.forEach(function (button) {
        button.setAttribute('aria-expanded', expanded ? 'true' : 'false');

        var label = expanded
          ? button.getAttribute('data-label-collapse')
          : button.getAttribute('data-label-expand');

        if (label) {
          button.setAttribute('aria-label', label);
          button.setAttribute('title', label);
        }
      });
    }

    function setCollapsed(collapsed) {
      document.documentElement.classList.toggle('app-sidebar-collapsed', collapsed);

      try {
        window.localStorage.setItem(STORAGE_KEY, collapsed ? 'collapsed' : 'expanded');
      } catch (error) {
        // Ignore unavailable storage: the toggle still works for the current page.
      }
    }

    function closeOverlay() {
      sidebar.classList.remove('show');
      if (backdrop) {
        backdrop.classList.remove('show');
      }
      syncButtons();
    }

    function toggle() {
      if (isDesktop()) {
        setCollapsed(!isCollapsed());
      } else {
        sidebar.classList.toggle('show');
        if (backdrop) {
          backdrop.classList.toggle('show', isOpen());
        }
      }

      syncButtons();
    }

    buttons.forEach(function (button) {
      button.addEventListener('click', toggle);
    });

    if (backdrop) {
      backdrop.addEventListener('click', closeOverlay);
    }

    // Escape closes the off-canvas panel on small screens.
    document.addEventListener('keydown', function (event) {
      if (event.key === 'Escape' && !isDesktop() && isOpen()) {
        closeOverlay();
      }
    });

    // The rail only applies on desktop, so drop the overlay when crossing the breakpoint.
    desktop.addEventListener('change', closeOverlay);

    syncButtons();
  });
})();

// Formats the identity document input as the user types: DUI 00000000-0 and NIT 0000-000000-000-0
// (14 digits) or 00000000-0 (9 digits, homologated DUI). Passport and other stay free.
(function () {
  function onlyDigits(value) {
    return (value.match(/\d/g) || []).join('');
  }

  function formatDocument(type, value) {
    var digits = onlyDigits(value);

    if (type === 'Dui') {
      digits = digits.slice(0, 9);
      return digits.length > 8 ? digits.slice(0, 8) + '-' + digits.slice(8) : digits;
    }

    if (type === 'Nit') {
      if (digits.length <= 9) {
        digits = digits.slice(0, 9);
        return digits.length > 8 ? digits.slice(0, 8) + '-' + digits.slice(8) : digits;
      }

      digits = digits.slice(0, 14);
      var parts = [digits.slice(0, 4), digits.slice(4, 10), digits.slice(10, 13), digits.slice(13, 14)]
        .filter(function (part) { return part.length > 0; });
      return parts.join('-');
    }

    return value;
  }

  document.addEventListener('DOMContentLoaded', function () {
    var typeSelect = document.getElementById('Input_DocumentType');
    var documentInput = document.getElementById('Input_TaxId');
    if (!typeSelect || !documentInput) {
      return;
    }

    function apply() {
      documentInput.value = formatDocument(typeSelect.value, documentInput.value);
    }

    documentInput.addEventListener('input', apply);
    typeSelect.addEventListener('change', apply);
  });
})();

// Selects the whole value of numeric fields when they receive focus, so typing replaces the current
// value instead of appending to it (for example typing "2" over a "0"). Uses focusin so it also
// covers markup injected later. The selection is applied on the next tick so the click that gave
// focus does not collapse it.
(function () {
  document.addEventListener('focusin', function (event) {
    var input = event.target;
    if (!input.matches || !input.matches('.js-numeric')) {
      return;
    }

    window.setTimeout(function () {
      input.select();
    }, 0);
  });
})();

// Turns the category selectors into searchable comboboxes. Tom Select keeps the underlying select
// in sync, so htmx and normal form posts keep working.
(function () {
  document.addEventListener('DOMContentLoaded', function () {
    if (typeof TomSelect === 'undefined') {
      return;
    }

    document.querySelectorAll('select.js-searchable').forEach(function (element) {
      var noResults = element.getAttribute('data-no-results') || '';

      new TomSelect(element, {
        allowEmptyOption: true,
        plugins: ['dropdown_input'],
        render: {
          no_results: function () {
            var div = document.createElement('div');
            div.className = 'no-results';
            div.textContent = noResults;
            return div;
          }
        }
      });
    });
  });
})();

// Product form: when the purchase unit equals the base unit the conversion factor is always 1, so
// the field is fixed and made read-only (read-only, not disabled, so it is still posted). Also shows
// the cost of one base unit (purchase price divided by the factor) when the units differ. The
// server validates the same rules.
(function () {
  function parseNumber(value) {
    var number = parseFloat(String(value).replace(',', '.'));
    return isNaN(number) ? null : number;
  }

  function formatCost(value) {
    return value >= 0.01 ? value.toFixed(2) : value.toFixed(4);
  }

  document.addEventListener('DOMContentLoaded', function () {
    var baseUnit = document.getElementById('Input_BaseUnit');
    var purchaseUnit = document.getElementById('Input_PurchaseUnit');
    var factor = document.getElementById('Input_PurchaseUnitFactor');
    if (!baseUnit || !purchaseUnit || !factor) {
      return;
    }

    var price = document.getElementById('Input_PurchasePrice');
    var hint = document.getElementById('unit-cost-hint');

    function updateHint() {
      if (!hint || !price) {
        return;
      }

      var priceValue = parseNumber(price.value);
      var factorValue = parseNumber(factor.value);
      var show = baseUnit.value !== purchaseUnit.value && priceValue !== null && factorValue > 0;

      hint.hidden = !show;
      if (show) {
        renderHint(priceValue, factorValue);
      }
    }

    // Builds "<template>" replacing {0} price, {1} factor, {2} unit cost (bold) and {3} unit name.
    // Nodes are created with textContent so no markup from the template or inputs is interpreted.
    function renderHint(priceValue, factorValue) {
      var currency = hint.getAttribute('data-currency') || '';
      var values = {
        '0': currency + priceValue.toFixed(2),
        '1': String(factorValue),
        '2': currency + formatCost(priceValue / factorValue),
        '3': baseUnit.options[baseUnit.selectedIndex].text.toLowerCase()
      };

      hint.textContent = '';
      hint.getAttribute('data-template').split(/(\{[0-3]\})/).forEach(function (part) {
        var match = /^\{([0-3])\}$/.exec(part);
        if (!match) {
          hint.appendChild(document.createTextNode(part));
          return;
        }

        var node = document.createElement(match[1] === '2' ? 'strong' : 'span');
        node.textContent = values[match[1]];
        hint.appendChild(node);
      });
    }

    function applyUnits() {
      var sameUnit = baseUnit.value === purchaseUnit.value;
      if (sameUnit) {
        factor.value = '1';
      }
      factor.readOnly = sameUnit;
      updateHint();
    }

    baseUnit.addEventListener('change', applyUnits);
    purchaseUnit.addEventListener('change', applyUnits);
    factor.addEventListener('input', updateHint);
    if (price) {
      price.addEventListener('input', updateHint);
    }

    applyUnits();
  });
})();

// Supplier products form: the agreed price is per purchase unit, so the help text names the unit of
// the selected product ("per box") and shows the product's general purchase price as a reference.
// Texts come from localized data attributes and are set with textContent.
(function () {
  document.addEventListener('DOMContentLoaded', function () {
    var select = document.querySelector('select.js-supplier-product');
    var help = document.getElementById('supplier-price-help');
    var reference = document.getElementById('supplier-price-reference');
    if (!select || !help || !reference) {
      return;
    }

    var defaultText = help.textContent;

    function apply() {
      var option = select.options[select.selectedIndex];
      var unit = option ? option.getAttribute('data-unit') : null;
      var price = option ? option.getAttribute('data-price') : null;

      help.textContent = unit
        ? help.getAttribute('data-template').replace('{0}', unit.toLowerCase())
        : defaultText;

      reference.hidden = !price;
      reference.textContent = price
        ? reference.getAttribute('data-template').replace('{0}', price)
        : '';
    }

    select.addEventListener('change', apply);
    apply();
  });
})();
