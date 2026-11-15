# Game Design Document (GDD)

> **Status:** Baseline proposal for team discussion. This document consolidates the team's current ideas; it is not a claim that the features are implemented or that open design questions are settled.

## 1. Concept and vision

### One-line pitch

Sky-Felicia is an educational space mission design game in which players plan, build, and operate missions while learning how engineering decisions affect scientific outcomes.

### Problem to solve

1. Space engineering concepts are often presented theoretically, with few opportunities for students to experiment with a mission from concept through operations.
2. Educational tools may focus on isolated aspects of space missions rather than the connections among mission planning, spacecraft design, resource management, spacecraft health, and science.
3. Complex topics can discourage beginners. The game should make them approachable with visualization, clear explanations, and interactive decisions.

### Audience

- Students who have had limited opportunity to experiment with end-to-end mission design.
- Players with varied levels of knowledge about space science and engineering, including beginners.

**To decide:** Specific age ranges, supported languages, and whether difficulty adapts automatically or is selected by the player.

### Design pillars

1. **Understandable tradeoffs:** Show how a choice affects mass, power, cost, risk, and scientific return.
2. **Learn by doing:** Explain concepts in context and let players try again after a mission.
3. **More than one good solution:** Reward sound engineering rather than one prescribed spacecraft configuration.
4. **Accessible participation:** Do not rely on color, sound, or prior technical knowledge as the sole way to understand or play.

### References and inspiration

The team cited *Trailmakers*, *No Man's Sky*, *Spaceflight Simulator*, and submarine-operator games as references for hands-on building, exploration, simulation, and managing a vehicle under operational constraints. These are inspirations, not feature or content requirements.

## 2. Core gameplay

### Mission lifecycle

1. **Choose a mission:** Review the destination, science objective, requirements, constraints, and difficulty.
2. **Design and plan:** Select a launch vehicle, orbit or trajectory, spacecraft systems, and scientific payload within the available resources.
3. **Validate:** Check mission compatibility and show understandable warnings about unmet requirements or risky tradeoffs.
4. **Operate and simulate:** Advance the mission, monitor spacecraft and mission health, and make decisions when scheduled or unexpected events occur.
5. **Evaluate:** Present objective results, resource use, decisions, failures, and scientific return in a mission report.
6. **Refine:** Preserve the design so players can learn from the outcome and try a revised solution.

The design and operation phases should connect: plans and spacecraft configuration determine what the player can do during the mission.

### Engineering and spacecraft design

Players configure spacecraft systems and choose components from a range of simple to advanced options. Candidate systems include:

- Structure and spacecraft bus.
- Power generation, storage, and consumption.
- Communications and onboard computers.
- Attitude control and trajectory control.
- Scientific instruments and payload.
- Health monitoring, including spacecraft status and, where relevant to the scenario, crew or food status.

For each relevant component, communicate its effects. Candidate properties include mass, power use or generation, cost, data generation, capability, reliability, and scientific value. A component's compatibility with the mission may depend on orbit, lifetime, communications, available power, launch capacity, or other scenario constraints.

**Required behavior:** Significant design decisions must have visible consequences. For example, an additional instrument can increase science capability while also increasing mass, power demand, cost, or data volume.

**To decide:** Whether players can construct launch vehicles from parts, choose from predefined vehicles, or use both; the initial component catalog; and the numerical models for component properties.

### Launch vehicle and orbit selection

- Present launch vehicle options with understandable performance characteristics, such as thrust and payload capacity.
- Present multiple orbit or trajectory options with plain-language explanations of their benefits, constraints, and mission fit.
- Make destination, spacecraft, launch, orbit, communications, and lifetime compatibility visible before launch.
- Include Earth-observation missions as a candidate mission type.

**To decide:** The initial destinations, orbit set, orbital mechanics fidelity, and which orbit parameters are exposed to players.

### Mission operations and events

- Players monitor mission status and make decisions in response to mission conditions.
- Timed events can create operational pressure and simulate real mission scenarios.
- Random or scenario-triggered events can introduce emergencies or unexpected constraints. An asteroid event was mentioned as an example, not a committed event.
- Show spacecraft damage as a percentage if damage is included in the selected simulation model.
- Preserve the ability to make informed responses; an event should explain relevant information rather than produce an unexplained pass/fail result.

**To decide:** Which events are authored versus randomized, whether events are replayable, and how timers and damage affect mission outcomes.

### Collaboration

- Support collaborative play for teams of up to four people.
- **To decide:** Whether collaboration is synchronous, turn-based, or shared through another workflow; how responsibilities are divided; and whether a solo mode is in scope.

### Mission validation and evaluation

- Each mission defines minimum objectives required to pass and optional objectives that can earn bonus points.
- Evaluate objective completion, scientific value, resource management, engineering choices, and successful collaboration where applicable.
- Explain how decisions affected performance; do not reduce the report to an unexplained success/failure label.
- Save failed and incomplete designs so players can refine them.
- Allow multiple viable configurations for the same mission.
- A mission report summarizes objectives, outcomes, resource usage, important decisions, events, failures, and areas to improve.
- Initial resources and rewards may fund later attempts or missions. The amount, economy, and consequences of failure are **To decide**.
- Leaderboards are a possible future feature, not a launch requirement.

### Education and difficulty

- Provide a short, fixed explanation for important concepts and decisions.
- Offer optional deeper explanations or learning resources for players who want them.
- Use visualizations and indicators to help players understand mission status and tradeoffs.
- Provide different knowledge or difficulty levels, mission types, and scenarios. More advanced levels may introduce more capable spacecraft components and more demanding constraints.
- Adaptive difficulty based on age bracket is a creative idea. **To decide:** whether to collect age information, how adaptation works, and whether player-selected difficulty is preferable.

## 3. Functional requirements

| ID | Requirement | Acceptance intent |
|---|---|---|
| FR-1 | Define mission objectives and requirements before design. | Players can identify the science objective, required outcomes, and constraints. |
| FR-2 | Configure a spacecraft and select a launch option and orbit. | The design exposes relevant component capabilities and resource effects. |
| FR-3 | Validate a proposed design against mission constraints. | Players see unmet requirements, incompatibilities, and meaningful risks before simulation. |
| FR-4 | Simulate and manage the mission. | The mission reflects the selected design and presents operational decisions. |
| FR-5 | Respond to failures and events. | Players receive enough information to make a reasoned decision and see its consequences. |
| FR-6 | Evaluate performance and explain outcomes. | A report covers required and optional objectives, resource use, science, and notable decisions. |
| FR-7 | Preserve designs for iteration. | A player can revisit and refine a design after an unsuccessful or completed attempt. |
| FR-8 | Teach concepts in context. | Key concepts have concise explanations, with optional additional detail. |
| FR-9 | Support team play for up to four people. | Collaboration behavior and its completion criteria are **To decide** before implementation. |
| FR-10 | Communicate status accessibly. | No essential status or instruction is conveyed by color or sound alone. |

These requirements are a starting point. Prioritize a small, testable first mission before expanding the catalog.

## 4. Rules and constraints

Missions may impose limits on:

- Budget and available starting resources.
- Spacecraft mass and launch payload capacity.
- Power generation, storage, and consumption.
- Communications capacity and operational windows.
- Orbit or trajectory suitability.
- Mission lifetime and component capability.
- Scientific objectives and required instruments.
- Unexpected events and operational decisions.

Exact values and formulas are mission-specific and **To decide**. The game should surface constraints before players commit to a design and explain the effects of exceeding or approaching a limit.

### Success and scoring

The goal is to complete the mission with the strongest overall performance, not merely to maximize one statistic. Scoring should balance required objectives, optional objectives, scientific return, and efficient use of constrained resources. Weights, thresholds, tie handling, and any reward economy remain **To decide**.

## 5. Accessibility and player experience

- Do not use color as the only way to distinguish states; pair colors with labels, icons, patterns, or other cues.
- Music or sound cues may reinforce color-coded information, but must be optional and must not be the only way to receive essential information.
- Keep explanations clear for beginners and offer optional depth rather than forcing specialist terminology.
- **To decide:** input methods, screen-reader support, motion and animation controls, subtitles, and supported accessibility standards.

## 6. Creative notes and future ideas

Ideas to explore after the core mission loop is validated:

- Adaptive difficulty and age-appropriate learning paths.
- More destinations, orbits, and mission types.
- Additional random events and emergency response scenarios.
- Expanded health monitoring, including crew-health systems in missions where crew are present.
- Leaderboards and progression rewards.
- More advanced components and user-built launch vehicles.
- Additional mission report and learning-resource formats.

These ideas are not commitments. Prioritize them based on the hackathon scope, learning value, accessibility, and implementation effort.

## 7. Open questions

Resolve these with the team before relying on them as implementation requirements:

1. What is the first playable mission, and what is its minimum pass condition?
2. Which platform and technology stack are being used?
3. What is the simulation's intended level of physical fidelity?
4. Is spacecraft and launch-vehicle design part-based, selection-based, or a mix?
5. How do teams of up to four collaborate in the chosen platform?
6. Which constraints, event types, and scoring formula are in the first version?
7. Is difficulty player-selected, adaptive, or both, and what role (if any) does age play?
8. Which accessibility needs and assistive technologies are in the initial scope?
9. Are starting resources and mission rewards part of the first playable version?
