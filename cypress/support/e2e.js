import './commands';

// Prevent Cypress from failing on React-internal errors that can be triggered
// during page navigation (e.g. cleanup effects running on a detached DOM node).
// Test assertions still catch any real functional regressions.
Cypress.on('uncaught:exception', (err) => {
  if (
    err.message.includes('Cannot read properties of null') ||
    err.message.includes('ResizeObserver loop limit exceeded')
  ) {
    return false;
  }
});
