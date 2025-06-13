# PoFunQuiz Mermaid Diagrams

This folder contains the complete set of Mermaid diagrams for the PoFunQuiz application, including both the source markdown files and their converted SVG representations.

## Verified Diagrams

All diagrams have been verified for valid Mermaid syntax and successfully converted to SVG format using the Mermaid CLI.

### 1. Flowchart - Game Flow Diagram
- **Source**: `game-flow.md`
- **SVG**: `game-flow-1.svg`
- **Type**: Flowchart (flowchart TD)
- **Description**: Shows the complete game flow from player entry through game completion

### 2. Sequence Diagram - Game Interaction
- **Source**: `sequence-diagram.md`
- **SVG**: `sequence-diagram-1.svg`
- **Type**: Sequence Diagram
- **Description**: Illustrates the interaction between User, Client (Blazor), Server (API), Database, and OpenAI API

### 3. Class Diagram - System Architecture
- **Source**: `class-diagram.md`
- **SVG**: `class-diagram-1.svg`
- **Type**: Class Diagram
- **Description**: Shows the main classes and their relationships in the system

### 4. Entity-Relationship (ER) Diagram - Data Model
- **Source**: `er-diagram.md`
- **SVG**: `er-diagram-1.svg`
- **Type**: ER Diagram
- **Description**: Defines the data entities and their relationships for Azure Table Storage

### 5. State Diagram - Game State Management
- **Source**: `state-diagram.md`
- **SVG**: `state-diagram-1.svg`
- **Type**: State Diagram (stateDiagram-v2)
- **Description**: Shows the various states and transitions in the game lifecycle

## Conversion Details

All diagrams were converted using the Mermaid CLI:
```bash
npx @mermaid-js/mermaid-cli -i [source].md -o [output].svg
```

## File Sizes
- class-diagram-1.svg: 36,881 bytes
- er-diagram-1.svg: 100,295 bytes  
- game-flow-1.svg: 26,911 bytes
- sequence-diagram-1.svg: 30,373 bytes
- state-diagram-1.svg: 46,451 bytes

## Usage

The SVG files can be:
- Embedded in documentation
- Viewed in web browsers
- Included in presentations
- Used in project wikis or README files

## Syntax Validation

All Mermaid syntax has been validated through successful conversion to SVG format. The diagrams follow Mermaid best practices and use current syntax standards.
