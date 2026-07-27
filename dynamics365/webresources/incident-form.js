"use strict";

var FireSystem = typeof FireSystem === "undefined" ? {} : FireSystem;

FireSystem.IncidentForm = (function () {
  var HIGH_PRIORITY = 1;
  var FOLLOW_UP_NOTIFICATION_ID = "fsm_high_priority_follow_up";
  var FOLLOW_UP_MESSAGE =
    "Set a follow-up deadline before saving this high-priority case.";

  function getFormContext(executionContext) {
    if (
      !executionContext ||
      typeof executionContext.getFormContext !== "function"
    ) {
      return null;
    }

    return executionContext.getFormContext();
  }

  function updateFollowUpRequirement(executionContext) {
    var formContext = getFormContext(executionContext);
    if (!formContext) {
      return false;
    }

    var priorityAttribute = formContext.getAttribute("prioritycode");
    var followUpAttribute = formContext.getAttribute("followupby");
    var followUpControl = formContext.getControl("followupby");
    if (!priorityAttribute || !followUpAttribute || !followUpControl) {
      return false;
    }

    var isHighPriority = priorityAttribute.getValue() === HIGH_PRIORITY;
    followUpAttribute.setRequiredLevel(isHighPriority ? "required" : "none");

    if (isHighPriority && !followUpAttribute.getValue()) {
      followUpControl.setNotification(
        FOLLOW_UP_MESSAGE,
        FOLLOW_UP_NOTIFICATION_ID,
      );
    } else {
      followUpControl.clearNotification(FOLLOW_UP_NOTIFICATION_ID);
    }

    return isHighPriority;
  }

  function onLoad(executionContext) {
    updateFollowUpRequirement(executionContext);
  }

  function onPriorityChange(executionContext) {
    updateFollowUpRequirement(executionContext);
  }

  return {
    onLoad: onLoad,
    onPriorityChange: onPriorityChange,
    updateFollowUpRequirement: updateFollowUpRequirement,
  };
})();

if (typeof module !== "undefined" && module.exports) {
  module.exports = FireSystem.IncidentForm;
}
