# ApplicationFlow — Auth Flow + User Journey

> **Full version** — Security flows and complete user journeys through all app screens.

## Authentication & Security Flow

```mermaid
sequenceDiagram
    autonumber
    actor Player
    participant Browser
    participant BlazorServer as PoFunQuiz.Web<br/>(Blazor Server)
    participant KeyVault as Azure Key Vault<br/>(PoShared)
    participant ManagedIdentity as Managed Identity
    participant OpenAI as Azure OpenAI
    participant TableStorage as Table Storage

    Note over BlazorServer: Application Startup
    BlazorServer->>ManagedIdentity: Request access token (DefaultAzureCredential)
    ManagedIdentity-->>BlazorServer: Token granted
    BlazorServer->>KeyVault: Fetch secrets (OpenAI key, SignalR conn str, AppInsights)
    KeyVault-->>BlazorServer: Secrets injected into IConfiguration

    Note over Player,Browser: Player arrives (no login required — anonymous play)
    Player->>Browser: Navigate to https://app-pofunquiz.azurewebsites.net
    Browser->>BlazorServer: GET / (HTTPS)
    BlazorServer-->>Browser: Blazor Server HTML + SignalR handshake
    Browser->>BlazorServer: WebSocket established (/_blazor)

    Note over Player,BlazorServer: CSP header enforced on every response
    BlazorServer-->>Browser: Content-Security-Policy: default-src 'self'; connect-src 'self' wss:
```

## Solo Quiz User Journey

```mermaid
sequenceDiagram
    autonumber
    actor Player
    participant UI as Blazor UI
    participant API as /api/quiz/questions
    participant OpenAI as Azure OpenAI GPT-4o
    participant TableStorage as /api/leaderboard

    Player->>UI: Home page → "Play Solo"
    UI->>UI: GameSetup.razor — choose category + difficulty
    Player->>UI: Select "Science / Hard" → "Start Game"
    UI->>API: GET /api/quiz/questions?count=10&category=Science
    API->>OpenAI: POST /chat/completions (GPT-4o prompt for 10 questions)
    OpenAI-->>API: JSON array of questions
    API-->>UI: 200 OK [QuizQuestion[]]  (output-cached 60s)
    UI->>UI: GameBoard.razor — render question 1 of 10
    loop Each Question
        Player->>UI: Select answer (A/B/C/D)
        UI->>UI: Evaluate answer, update score<br/>streak bonus, speed bonus, time bonus
        UI->>UI: Advance to next question
    end
    UI->>UI: Results.razor — show final scores + breakdown
    Player->>UI: "Submit Score" 
    UI->>TableStorage: POST /api/leaderboard {PlayerName, Score, Category}
    TableStorage-->>UI: 201 Created
    UI->>UI: Leaderboard.razor — show top 10
```

## Multiplayer User Journey

```mermaid
sequenceDiagram
    autonumber
    actor Host
    actor Guest
    participant HubConn as SignalR Hub<br/>/gamehub
    participant LobbyService as MultiplayerLobbyService
    participant API as /api/quiz/questions

    Host->>HubConn: CreateGame("Alice")
    HubConn->>LobbyService: CreateSession("Alice", connId)
    LobbyService-->>HubConn: GameSession {GameId: "ABCD"}
    HubConn-->>Host: "ABCD" (gameId)

    Host->>Host: MultiplayerLobby.razor — share game code "ABCD"
    Guest->>HubConn: JoinGame({PlayerName:"Bob", GameId:"ABCD"})
    HubConn->>LobbyService: TryJoinSession("ABCD","Bob")
    LobbyService-->>HubConn: Success
    HubConn-->>Host: GameUpdated(GameStateDto)
    HubConn-->>Guest: PlayerJoined("Bob")

    Host->>HubConn: StartGame("ABCD")
    Note over HubConn: Auth check: only host can start
    HubConn->>LobbyService: GetSession("ABCD")
    HubConn-->>Host: GameStarted(session)
    HubConn-->>Guest: GameStarted(session)

    par Both players quiz simultaneously
        Host->>API: GET /api/quiz/questions
        Guest->>API: GET /api/quiz/questions
    end

    loop Score updates
        Host->>HubConn: UpdateScore("ABCD", 1, 450)
        HubConn-->>Host: ScoreUpdated(GameStateDto)
        HubConn-->>Guest: ScoreUpdated(GameStateDto)
    end

    Host->>HubConn: EndGame("ABCD")
    HubConn-->>Host: GameEnded(final GameStateDto)
    HubConn-->>Guest: GameEnded(final GameStateDto)
```

## Page Navigation Map

```mermaid
stateDiagram-v2
    [*] --> Home : Load app

    Home --> GameSetup : Play Solo
    Home --> MultiplayerLobby : Multiplayer
    Home --> Leaderboard : View Scores

    GameSetup --> GameBoard : Start Game
    MultiplayerLobby --> GameBoard : Game Started (SignalR)

    GameBoard --> Results : All questions answered

    Results --> Leaderboard : Submit Score
    Results --> Home : Play Again

    Leaderboard --> Home : Back

    state GameBoard {
        [*] --> Question
        Question --> Scored : Answer selected
        Scored --> Question : Next question
        Scored --> [*] : Last question
    }
```

---

## Simplified Flow

```mermaid
flowchart LR
    A([Home]) -->|Solo| B([Game Setup])
    A -->|Multiplayer| C([Lobby])
    A -->|Scores| D([Leaderboard])
    B -->|Start| E([Game Board])
    C -->|All joined + host starts| E
    E -->|Done| F([Results])
    F -->|Submit| D
    F -->|Again| A
```
