# ComponentMap — Component Tree + Service Dependencies

> **Full version** — Blazor component hierarchy and all service wiring.

## Blazor Component Tree

```mermaid
graph TB
    subgraph "Components/App.razor (Root)"
        APP[App.razor]
        ROUTES[Routes.razor]
        APP --> ROUTES
    end

    subgraph "Layout"
        ML[MainLayout.razor]
        NAV[NavMenu.razor]
        ML --> NAV
    end

    subgraph "Pages"
        HOME[Home.razor<br/>Route: /]
        SETUP[GameSetup.razor<br/>Route: /game-setup]
        BOARD[GameBoard.razor<br/>Route: /game]
        RESULTS[Results.razor<br/>Route: /results]
        LEADER[Leaderboard.razor<br/>Route: /leaderboard]
        LOBBY[MultiplayerLobby.razor<br/>Route: /multiplayer]
        DIAG[Diag.razor<br/>Route: /diag]
        ERR[Error.razor]
    end

    subgraph "Game Components"
        TIMER[GameTimer.razor<br/>countdown + events]
        PBOARD[PlayerBoard.razor<br/>per-player Q+A UI]
        SCORE[ScoreBoard.razor<br/>live score display]
    end

    ROUTES --> ML
    ML --> HOME
    ML --> SETUP
    ML --> BOARD
    ML --> RESULTS
    ML --> LEADER
    ML --> LOBBY
    ML --> DIAG

    BOARD --> TIMER
    BOARD --> PBOARD
    BOARD --> SCORE
```

## Service Dependency Map

```mermaid
graph LR
    subgraph "Blazor Pages"
        HOME[Home]
        SETUP[GameSetup]
        BOARD[GameBoard]
        RESULTS[Results]
        LEADER[Leaderboard]
        LOBBY[MultiplayerLobby]
    end

    subgraph "Application Services"
        GS[GameState\nScoped]
        OAI[IOpenAIService\nScoped → OpenAIService]
        GCS[GameClientService\nScoped]
        MLS[MultiplayerLobbyService\nSingleton]
        LR[ILeaderboardRepository\nScoped → LeaderboardRepository]
        SRS[SessionReaperService\nHostedService]
        TSI[TableStorageInitializer\nHostedService]
    end

    subgraph "Infrastructure"
        TC[TableClient\nScoped]
        TSC[TableServiceClient\nSingleton]
        AZ_OAI[AzureOpenAIClient]
        HUB[GameHub\nSignalR]
    end

    subgraph "External"
        OPENAI_API[Azure OpenAI API]
        TABLE_API[Azure Table Storage]
        SR_SVC[Azure SignalR Service]
    end

    SETUP --> GS
    SETUP --> OAI
    BOARD --> GS
    BOARD --> GCS
    RESULTS --> GS
    RESULTS --> LR
    LEADER --> LR
    LOBBY --> GCS

    OAI --> AZ_OAI --> OPENAI_API
    LR --> TC --> TSC --> TABLE_API
    GCS --> HUB
    HUB --> MLS
    MLS --> SRS
    TSC --> TSI
    TSI --> TABLE_API
    HUB --> SR_SVC
```

## Vertical Slice Feature Map

```mermaid
graph TB
    subgraph "Features/Quiz"
        QE[QuizEndpoints.cs<br/>GET /api/quiz/questions]
        OAI_SVC[OpenAIService.cs<br/>IOpenAIService]
        QD[QuizQuestionDeserializers.cs]
        QE --> OAI_SVC
        OAI_SVC --> QD
    end

    subgraph "Features/Leaderboard"
        GL[GetLeaderboard.cs<br/>GET /api/leaderboard]
        SS[SubmitScore.cs<br/>POST /api/leaderboard]
        ILR[ILeaderboardRepository]
        LR_IMPL[LeaderboardRepository.cs]
        GL --> ILR
        SS --> ILR
        ILR --> LR_IMPL
    end

    subgraph "Features/Multiplayer"
        GH[GameHub.cs<br/>SignalR /gamehub]
        MLS2[MultiplayerLobbyService.cs]
        GCS2[GameClientService.cs]
        DTOS[MultiplayerDtos.cs]
        SRS2[SessionReaperService.cs]
        GH --> MLS2
        GCS2 --> GH
        SRS2 --> MLS2
    end

    subgraph "Features/Storage"
        TSI2[TableStorageInitializer.cs]
    end
```

---

## Simplified Component Map

```mermaid
graph TB
    APP[App Root] --> LAYOUT[Layout]
    LAYOUT --> HOME & SETUP & BOARD & RESULTS & LEADER & LOBBY

    BOARD --> TIMER[Timer] & PBOARD[PlayerBoard] & SCORE[ScoreBoard]

    SETUP -->|calls| QUIZ_API[Quiz API]
    LOBBY -->|SignalR| GAMEHUB[GameHub]
    RESULTS -->|submits| LB_API[Leaderboard API]
    LEADER -->|reads| LB_API

    QUIZ_API -->|GPT-4o| OPENAI[Azure OpenAI]
    LB_API -->|read/write| TABLES[Table Storage]
    GAMEHUB -->|scale| SIGNALR[Azure SignalR]
```
