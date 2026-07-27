"use strict";

const test = require("node:test");
const assert = require("node:assert/strict");
const incidentForm = require("../incident-form.js");

function formHarness({ priority = 2, followUp = null } = {}) {
  const events = [];
  const priorityAttribute = {
    getValue: () => priority,
  };
  const followUpAttribute = {
    getValue: () => followUp,
    setRequiredLevel: (level) => events.push(["required", level]),
  };
  const followUpControl = {
    setNotification: (message, id) =>
      events.push(["notification", message, id]),
    clearNotification: (id) => events.push(["clear", id]),
  };
  const formContext = {
    getAttribute: (name) => {
      if (name === "prioritycode") return priorityAttribute;
      if (name === "followupby") return followUpAttribute;
      return null;
    },
    getControl: (name) => (name === "followupby" ? followUpControl : null),
  };

  return {
    events,
    executionContext: {
      getFormContext: () => formContext,
    },
  };
}

test("high-priority case requires follow-up and shows guidance", () => {
  const harness = formHarness({ priority: 1 });

  const result = incidentForm.updateFollowUpRequirement(
    harness.executionContext,
  );

  assert.equal(result, true);
  assert.deepEqual(harness.events[0], ["required", "required"]);
  assert.equal(harness.events[1][0], "notification");
  assert.match(harness.events[1][1], /follow-up deadline/);
});

test("high-priority case with a follow-up clears stale guidance", () => {
  const harness = formHarness({
    priority: 1,
    followUp: new Date("2026-07-27T12:00:00Z"),
  });

  incidentForm.updateFollowUpRequirement(harness.executionContext);

  assert.deepEqual(harness.events, [
    ["required", "required"],
    ["clear", "fsm_high_priority_follow_up"],
  ]);
});

test("normal-priority case makes follow-up optional", () => {
  const harness = formHarness({ priority: 2 });

  const result = incidentForm.updateFollowUpRequirement(
    harness.executionContext,
  );

  assert.equal(result, false);
  assert.deepEqual(harness.events, [
    ["required", "none"],
    ["clear", "fsm_high_priority_follow_up"],
  ]);
});

test("onLoad applies the same rule", () => {
  const harness = formHarness({ priority: 1 });

  incidentForm.onLoad(harness.executionContext);

  assert.equal(harness.events[0][1], "required");
});

test("onPriorityChange applies the same rule", () => {
  const harness = formHarness({ priority: 2 });

  incidentForm.onPriorityChange(harness.executionContext);

  assert.equal(harness.events[0][1], "none");
});

test("missing execution context is handled without a form error", () => {
  assert.equal(incidentForm.updateFollowUpRequirement(null), false);
});

test("missing form columns are handled without a form error", () => {
  const executionContext = {
    getFormContext: () => ({
      getAttribute: () => null,
      getControl: () => null,
    }),
  };

  assert.equal(incidentForm.updateFollowUpRequirement(executionContext), false);
});
