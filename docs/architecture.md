# Architecture

> **Status:** Proposed logical architecture. The repository does not yet identify an implementation stack or contain application source code. Confirm choices with the team before treating this document as an implementation specification.

## Goals

- Keep mission content, simulation rules, presentation, and evaluation understandable and testable.
- Make consequences of a player's design choices explainable.
- Allow missions and components to grow without hiding their constraints or effects.
- Support accessible presentation of mission status and feedback.

## Technology stack

**To decide:** Game engine or framework, programming language, rendering approach, persistence mechanism, hosting target, and test tooling. Choose these based on the team's skills, hackathon platform requirements, and the desired collaboration experience. Update this section when the team agrees.

## Proposed logical components

| Component | Responsibility |
|---|---|
| User interface | Mission selection, spacecraft configuration, validation feedback, operations controls, and reports. Presents status through more than color or sound alone. |
| Mission content | Defines objectives, destinations, constraints, available components, event options, and difficulty for each scenario. |
| Design and validation | Tracks the selected vehicle, orbit, spacecraft, and payload; checks compatibility and resource limits; explains warnings. |
| Mission simulation | Advances mission state and applies the effects of configuration, player decisions, and events. |
| Event handling | Selects or schedules mission events and presents relevant decisions. The selection and randomness model are **To decide**. |
| Evaluation and reporting | Compares outcomes with objectives, computes performance, and explains how decisions influenced results. |
| Persistence | Saves mission designs and results so players can review and refine them. Storage format and synchronization are **To decide**. |
| Collaboration | Coordinates shared mission work for teams of up to four. Interaction model and authority over state are **To decide**. |

These are logical responsibilities, not a requirement to create a separate service or package for each one.

## Proposed data concepts

Keep the initial domain model small and explicit. Candidate concepts:

- **Mission:** identifier, description, science objective, requirements, constraints, and available options.
- **Component:** type, capabilities, mass, power effects, cost, data generation, and any relevant compatibility or scientific value.
- **Vehicle:** selected launch option and its performance limits.
- **Spacecraft design:** selected components, payload, orbit, and computed resource totals.
- **Mission state:** current phase, remaining resources, spacecraft health, objective progress, and event history.
- **Event:** trigger conditions, player choices, and state effects.
- **Result:** objective outcomes, resource use, score details, decisions, failures, and report content.

Use units explicitly for physical quantities. Validate mission content and player designs at the boundary where they enter the simulation. Avoid duplicating derived totals as independent editable values unless there is a clear consistency rule.

## Data flow

1. Load a mission definition and show its objective and constraints.
2. Collect the player's design selections.
3. Validate selections against the mission, vehicle, orbit, and component constraints; return actionable explanations.
4. Initialize mission state from the validated design.
5. Advance the simulation in response to time, events, and player decisions.
6. Evaluate completed or interrupted state against the mission objectives.
7. Save the design and result, and present an explainable mission report.

## Simulation and explainability

- Keep mission state changes explicit so results can be traced to design choices, events, or player actions.
- Make resource and compatibility calculations consistent between design validation and simulation.
- Define event behavior and probability only after the team chooses how random events should work.
- If randomized events are used, consider recording the event sequence (and a reproducible seed if appropriate) to support debugging and fair result review.
- Make scoring components visible in the report; exact formulas remain a product decision.

## Quality attributes

- **Accessibility:** Essential information must not be conveyed by color or sound alone.
- **Clarity:** Use consistent units and explain warnings, failures, and scoring effects.
- **Reliability:** Invalid configurations should produce actionable validation errors rather than silently proceeding.
- **Extensibility:** Mission and component definitions should be addable without rewriting unrelated presentation logic.
- **Testability:** Resource calculations, compatibility rules, event effects, and evaluation should be testable independently of the interface where the chosen stack permits.

## Open technical decisions

- Target platform(s), runtime, engine/framework, and supported devices.
- Offline versus networked operation and whether collaboration is real-time.
- Persistence format, ownership, synchronization, and privacy expectations.
- Simulation time model, physical fidelity, and numerical units.
- Mission and component authoring format and validation workflow.
- Accessibility target and input methods.
- Automated test, build, and release process.

Record the team's decisions here, including rationale when they constrain later work.
