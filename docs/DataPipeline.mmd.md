# DataPipeline — Data Workflow + User Workflow

> **Full version** — CRUD flows, question generation pipeline, and leaderboard persistence.

## Question Generation Pipeline

```mermaid
flowchart TD
    REQ[Client requests questions<br/>GET /api/quiz/questions?count=10&category=Science]
    CACHE{Output Cache<br/>60s TTL<br/>keyed on count+category}
    HIT[Return cached response]
    MISS[Cache miss]
    OTEL[Start OpenTelemetry Activity<br/>'QuizGeneration']
    PROMPT[Build GPT-4o system prompt<br/>category + count + difficulty + format]
    CALL[POST Azure OpenAI<br/>/chat/completions]
    PARSE[Deserialize JSON<br/>QuizQuestionDeserializers]
    VALIDATE{Valid questions?}
    RETRY[Retry / fallback]
    RETURN[Return QuizQuestion[]]
    LOG[Log: count + duration_ms<br/>activity tags set]

    REQ --> CACHE
    CACHE -->|Hit| HIT
    CACHE -->|Miss| MISS
    MISS --> OTEL --> PROMPT --> CALL --> PARSE --> VALIDATE
    VALIDATE -->|Yes| RETURN
    VALIDATE -->|No| RETRY --> CALL
    RETURN --> LOG
    RETURN --> CACHE
```

## Score Submission Pipeline

```mermaid
flowchart TD
    PLAY[Player finishes game<br/>Results.razor]
    BUILD[Build LeaderboardEntry<br/>PlayerName, Score, Category, MaxStreak]
    POST[POST /api/leaderboard]
    VAL{Validation}
    VAL_FAIL[400 Bad Request<br/>PlayerName empty or Score < 0]
    SANITIZE[Sanitize HTML tags<br/>Clamp name to 20 chars<br/>Clamp score to 10,000]
    CAT_CHECK{Category valid in<br/>QuestionCategories.All?}
    DEFAULT_CAT[Default to 'General']
    SET_KEY[Set PartitionKey = Category<br/>RowKey = new Guid<br/>Timestamp = UtcNow]
    WRITE[LeaderboardRepository.AddScoreAsync<br/>TableClient.AddEntityAsync]
    TABLE[(Azure Table Storage<br/>Table: PoFunQuizPlayers)]
    RESP[201 Created + entity]

    PLAY --> BUILD --> POST --> VAL
    VAL -->|Invalid| VAL_FAIL
    VAL -->|Valid| SANITIZE --> CAT_CHECK
    CAT_CHECK -->|Unknown| DEFAULT_CAT --> SET_KEY
    CAT_CHECK -->|Known| SET_KEY
    SET_KEY --> WRITE --> TABLE --> RESP
```

## Leaderboard Read Pipeline

```mermaid
flowchart TD
    REQ2[GET /api/leaderboard?category=Science]
    QUERY[LeaderboardRepository.GetTopScoresAsync<br/>TableClient.QueryAsync<br/>filter: PartitionKey eq 'Science']
    TABLE2[(Azure Table Storage<br/>PoFunQuizPlayers)]
    SORT[Sort by Score DESC]
    LIMIT[Take top 10]
    RETURN2[Return LeaderboardEntry[]]
    RENDER[Leaderboard.razor renders table]

    REQ2 --> QUERY --> TABLE2
    TABLE2 --> SORT --> LIMIT --> RETURN2 --> RENDER
```

## Multiplayer Score Sync Pipeline

```mermaid
sequenceDiagram
    participant P1 as Player 1 (Browser)
    participant P2 as Player 2 (Browser)
    participant Hub as GameHub (SignalR)
    participant Lobby as MultiplayerLobbyService (in-memory)

    P1->>Hub: UpdateScore("ABCD", playerNumber=1, score=450)
    Hub->>Lobby: GetSession("ABCD")
    Lobby-->>Hub: GameSession {Player1BaseScore=450}
    Hub->>Lobby: session.Player1BaseScore = 450
    Hub-->>P1: ScoreUpdated(GameStateDto)
    Hub-->>P2: ScoreUpdated(GameStateDto)

    Note over Lobby: In-memory only — no DB write during game
    Note over Lobby: SessionReaperService purges sessions > 30min old

    P1->>Hub: EndGame("ABCD")
    Hub-->>P1: GameEnded(final scores)
    Hub-->>P2: GameEnded(final scores)
    P1->>P1: Results.razor → POST /api/leaderboard
    P2->>P2: Results.razor → POST /api/leaderboard
```

## Full CRUD Matrix

```mermaid
graph LR
    subgraph "Create"
        C1[POST /api/leaderboard → AddEntityAsync]
        C2[CreateGame via SignalR → in-memory session]
    end

    subgraph "Read"
        R1[GET /api/quiz/questions → OpenAI generation]
        R2[GET /api/leaderboard → TableClient.QueryAsync]
        R3[GET /health → health checks]
        R4[GET /diag → masked config values]
    end

    subgraph "Update"
        U1[UpdateScore via SignalR → session mutation]
        U2[StartGame via SignalR → session.StartTime = UtcNow]
    end

    subgraph "Delete"
        D1[SessionReaperService → removes expired sessions]
    end
```

---

## Simplified Data Pipeline

```mermaid
flowchart LR
    U([User]) -->|choose category| S[GameSetup]
    S -->|GET questions| OAI[OpenAI GPT-4o]
    OAI -->|QuizQuestion array| G[GameBoard]
    G -->|answer events| SC[Scoring Engine]
    SC -->|final score| R[Results]
    R -->|POST score| TB[Table Storage]
    TB -->|top 10| LB[Leaderboard]
```
