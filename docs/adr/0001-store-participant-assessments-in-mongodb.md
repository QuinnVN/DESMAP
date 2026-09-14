# Store participant assessments in MongoDB

The application stores each completed initial assessment in MongoDB after saving it in the participant's browser. MongoDB enforces one assessment per normalized email address and uses the assessment identifier to make retries idempotent. The browser keeps a pending, synced, or error status so a database outage does not prevent the participant from viewing the locally saved result.
