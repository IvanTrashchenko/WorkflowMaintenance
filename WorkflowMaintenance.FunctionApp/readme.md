# WorkflowMaintenance.FunctionApp

A self-running Azure Function that every **3 minutes** lists and cancels up to ~50 “Running” workflow runs in a Logic App (Standard) instance before hitting 429 throttling limits.

## Prerequisites
- An Azure AD App Registration with **Client Secret**, assigned the **Logic Apps Standard Operator** role on the target Logic App resource.  