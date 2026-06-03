(() => {
  const form = document.getElementById("analysis-form");
  if (!form) {
    return;
  }

  const submitButton = document.getElementById("analysis-submit");
  const runningState = document.getElementById("analysis-running");

  form.addEventListener("submit", () => {
    if (window.jQuery) {
      const validator = window.jQuery(form);
      if (typeof validator.valid === "function" && !validator.valid()) {
        return;
      }
    }

    if (submitButton instanceof HTMLButtonElement) {
      submitButton.disabled = true;
      submitButton.textContent = "Analyzing...";
    }

    if (runningState instanceof HTMLElement) {
      runningState.hidden = false;
    }
  });
})();
