# Career minigames

Practice scenarios in which the player performs tasks from a profession.

## Language

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
