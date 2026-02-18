# DataModel — Database Schema + State Transitions

> **Full version** — Entity Relationship Diagram, in-memory models, and lifecycle state machines.

## Entity Relationship Diagram

```mermaid
erDiagram
    LeaderboardEntry {
        string PartitionKey PK "= Category (e.g. 'General')"
        string RowKey PK "= Guid (unique per score)"
        string PlayerName "max 20 chars, HTML-sanitized"
        int Score "0 – 10,000"
        int MaxStreak "highest consecutive correct answers"
        string Category "General | Science | History | ..."
        datetime DatePlayed "UTC"
        int Wins "multiplayer wins"
        int Losses "multiplayer losses"
        DateTimeOffset Timestamp "Azure Table auto-set"
        ETag ETag "optimistic concurrency"
    }

    QuizQuestion {
        string Id PK "Guid"
        string Question "question text"
        string_array Options "4 answer choices"
        int CorrectOptionIndex "0-3"
        string Category "matches QuestionCategories.All"
        enum Difficulty "Easy=1pt | Medium=2pt | Hard=3pt"
    }

    GameSession {
        string GameId PK "Guid"
        Player Player1 "name, initials"
        Player Player2 "name, initials"
        QuizQuestion_array Player1Questions "per-player question set"
        QuizQuestion_array Player2Questions "per-player question set"
        datetime StartTime "UTC"
        datetime EndTime "UTC (null = in-progress)"
        int Player1BaseScore
        int Player2BaseScore
        int Player1StreakBonus
        int Player2StreakBonus
        int Player1SpeedBonus
        int Player2SpeedBonus
        int Player1TimeBonus
        int Player2TimeBonus
        int Player1Streak "current streak"
        int Player2Streak "current streak"
        int Player1MaxStreak
        int Player2MaxStreak
        int Player1CorrectCount
        int Player2CorrectCount
        string_array SelectedCategories
        enum GameDifficulty "Easy | Medium | Hard"
    }

    Player {
        string Name PK
        string Initials "derived from Name"
    }

    GameSession ||--|| Player : "Player1"
    GameSession ||--|| Player : "Player2"
    GameSession ||--o{ QuizQuestion : "Player1Questions"
    GameSession ||--o{ QuizQuestion : "Player2Questions"
    LeaderboardEntry }o--|| QuizQuestion : "Category references"
```

## Scoring Formula

```mermaid
graph LR
    BS[Base Score<br/>Easy=1 Med=2 Hard=3] --> TS
    STK[Streak Bonus<br/>+streak pts per correct] --> TS
    SPD[Speed Bonus<br/>faster = more pts] --> TS
    TIM[Time Bonus<br/>remaining time] --> TS
    TS[Total Score<br/>Player1Score / Player2Score]
```

## GameSession State Machine

```mermaid
stateDiagram-v2
    [*] --> Pending : CreateSession (Host joins)
    Pending --> WaitingForPlayer2 : Host connected
    WaitingForPlayer2 --> Ready : Player 2 joins (max 2 players)
    Ready --> InProgress : Host calls StartGame<br/>StartTime = UtcNow
    InProgress --> Complete : All questions answered<br/>EndTime = UtcNow
    Complete --> [*] : Session reaped after 30 min TTL

    InProgress --> Abandoned : Player disconnects<br/>SessionReaperService cleans up
    Abandoned --> [*]

    note right of Complete
        IsComplete = true
        Winner computed from scores
        IsTie if scores equal
    end note
```

## Azure Table Storage Schema

```mermaid
graph TB
    subgraph "Azure Table Storage Account"
        subgraph "Table: PoFunQuizPlayers"
            R1["PartitionKey='General' | RowKey='{guid}' | PlayerName | Score | MaxStreak | ..."]
            R2["PartitionKey='Science' | RowKey='{guid}' | PlayerName | Score | MaxStreak | ..."]
            R3["PartitionKey='History' | RowKey='{guid}' | ..."]
        end
    end

    note["Partition = Category<br/>Enables efficient per-category leaderboard queries<br/>Top-10 by Score per partition"]
    R1 -.-> note
```

---

## Simplified Data Model

```mermaid
classDiagram
    class LeaderboardEntry {
        +string PlayerName
        +int Score
        +string Category
        +int MaxStreak
        +DateTime DatePlayed
    }

    class GameSession {
        +string GameId
        +Player Player1
        +Player Player2
        +int Player1Score
        +int Player2Score
        +bool IsComplete
        +Player? Winner
        +bool IsTie
    }

    class QuizQuestion {
        +string Question
        +List~string~ Options
        +int CorrectOptionIndex
        +string Category
        +QuestionDifficulty Difficulty
        +int BasePoints
    }

    class Player {
        +string Name
        +string Initials
    }

    GameSession --> Player : has 2
    GameSession --> QuizQuestion : has many
```
