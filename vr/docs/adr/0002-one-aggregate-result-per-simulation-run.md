---
status: accepted
---

# Use one aggregate result per simulation run

Each completed Clinic, Doctor, Lawyer, or Sales simulation produces one immutable JSON aggregate. The backend builds it through atomic local draft updates, then upserts the complete document to MongoDB under the run identifier. Synchronization state stays in a separate local sidecar. This keeps MongoDB free of partially assembled runs and lets the operator assign one participant to one completed simulation.

## Considered options

- Storing one MongoDB document for each patient, case, defense, or Sales part would make results available earlier, but consumers would need to join documents and detect incomplete runs.
- Updating one lifetime document for each participant would combine sessions and make retries, concurrent edits, duplicate real names, and historical immutability harder to manage.
- Writing result fragments directly to MongoDB would reduce local storage, but an outage could lose or partially publish a run.

## Consequences

MongoDB receives no data for aborted runs. A Sales Part 1 assessment remains local when Part 2 does not finish. Completed results are unavailable remotely until the terminal result and all required processing complete. The backend keeps drafts, aggregates, synchronization records, and WAV files indefinitely, so local storage can grow without a bound.

The participant clears only after the aggregate synchronizes. Three failed automatic attempts leave the participant active until `db sync` succeeds or the operator clears the assignment manually. The aggregate has no schema version or model and rubric provenance, so research deployments must keep their analysis configuration fixed and coordinate structural changes outside the document itself.
