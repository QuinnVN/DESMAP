# Career minigames

Practice scenarios in which the player performs tasks from a profession.

## Language

### Shared results

**Game result**:
A durable outcome from one meaningful unit of a simulation, such as a patient, case, round, closing defense, persuasion attempt, or completed customer session. Routine gameplay telemetry is not a game result.
_Avoid_: Telemetry event, activity log

**Result unit**:
The game-specific scope evaluated by one game result. Its type distinguishes values that must not be compared directly, such as a clinic patient score and a sales persuasion percentage.
_Avoid_: Event, generic score

### Lawyer

**Interview wave**:
One of four consecutive stages in the defendant interview. Each wave presents several authored questions, and the player permanently chooses one question for that interview attempt.
_Avoid_: Follow-up group, round

**Interview restart**:
The player's choice to repeat all four interview waves after reaching the insufficient-evidence outcome. It retains the initial clues but clears interview choices, interview-unlocked clues, and evidence-board progress.
_Avoid_: Scene reload, technical retry

**Strong clue**:
An authored clue that counts toward the evidence required to continue from the interview to the evidence board.
_Avoid_: High score, guaranteed proof

**Weak clue**:
An authored clue that offers limited support. One weak clue may accompany three strong clues without blocking progress to the evidence board.
_Avoid_: Medium evidence

**Noise clue**:
An authored clue that does not help establish the defense. A chosen noise clue makes the interview evidence insufficient.
_Avoid_: Weak clue, misleading proof

**Clue**:
A case fact available to the player from the initial brief or a chosen interview question.
_Avoid_: Evidence, proof

**Evidence link**:
A relationship the player creates between two clues on the evidence board. The player may create links in any action order, while the completed set represents an authored logical order.
_Avoid_: Clue selection, question order

**Evidence chain**:
The case-authored logical sequence represented by a completed set of evidence links. Its ordering is defined by clue content and configuration, not by visible numbers or the order in which the player creates links.
_Avoid_: Link order, numbered cards

**Defense conclusion**:
The case's authored conclusion shown to the player before and during the closing defense. The player is expected to explain and support it, not discover a different conclusion.
_Avoid_: Player verdict, hidden answer

**Closing defense**:
The player's spoken explanation of the defense conclusion using the evidence and reasoning established during the case.
_Avoid_: Recording, final answer

**Defense assessment**:
A backend evaluation of how well the closing defense uses the provided evidence, connects it logically, stays faithful to the defense conclusion, and communicates the argument clearly.
_Avoid_: Verdict, case solution

### Sales

**Recommended shoe**:
The shoe selected by the player as the best match for the customer's needs.

**Best-fit shoe**:
The shoe designated by the scenario as the strongest match for the customer's budget and needs.

**Comparison alternative**:
Another shoe from the earlier comparison panel that the customer mentions when questioning the recommendation. It may be cheaper or more expensive than the recommended shoe.

**Customer objection**:
A predetermined customer remark that challenges the recommended shoe's price, appearance, or suitability for the customer's needs.

**Persuasion response**:
The player's spoken answer to the customer objection, defending the recommended shoe or changing the recommendation.
_Avoid_: Defense, rebuttal

**Persuasion assessment**:
A score and brief feedback evaluating the player's persuasion response.

**Dissatisfied returning customer**:
A customer who returns after an earlier purchase with a legitimate unresolved problem.
_Avoid_: Difficult customer

**Returning customer profile**:
The fixed identity, purchase history, product condition, needs, and speaking style that constrain the AI customer's responses throughout Part 2.
_Avoid_: Prompt

**Presenting complaint**:
The problem as the dissatisfied returning customer first describes it. It may not identify the actual cause.
_Avoid_: Actual cause

**Actual cause**:
The single scenario-defined issue that explains the customer's dissatisfaction and must be discovered through conversation.
_Avoid_: Presenting complaint

**Trust-rebuilding response**:
The player's spoken attempt to acknowledge the problem, offer a suitable resolution, and restore the customer's confidence in the business.

**Part 2 assessment**:
A backend record produced from the completed conversation transcript. It contains the customer trust state and flags for emotional handling, cause identification, solution suitability, and trust rebuilding.
_Avoid_: Score, written feedback

**Customer trust state**:
The returning customer's confidence at the end of the conversation. It is restored, partially restored, or lost.

**Manager escalation**:
The player's decision to transfer the complaint to a manager. In this scenario it immediately ends Part 2 with customer trust lost.

**Conversation objective**:
The current skill task in Part 2. The objectives are emotional handling, cause identification, solution proposal, and trust rebuilding, and the player must complete them in that order.
_Avoid_: Round, answer choice

**Sales outcome display**:
The final VR display that shows only the customer trust state after the returning-customer conversation. Detailed assessments remain in the backend.
_Avoid_: Sales results summary, combined score, aggregate score
