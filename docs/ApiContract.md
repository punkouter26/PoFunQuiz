# ApiContract — API Specs + Error Handling Policy

## Base URL

- **Local:** `http://localhost:5000` / `https://localhost:5001`
- **Production:** `https://app-pofunquiz-{token}.azurewebsites.net`
- **OpenAPI UI (Scalar):** `/scalar/v1`
- **OpenAPI JSON:** `/openapi/v1.json`

---

## REST Endpoints

### GET `/api/quiz/questions`

Generates AI-powered quiz questions via Azure OpenAI GPT-4o.

**Query Parameters**

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `count` | `int` | Yes | Number of questions to generate (must be > 0) |
| `category` | `string` | No | Quiz category. Defaults to `"general knowledge"` |

**Valid categories:** `General`, `Science`, `History`, `Geography`, `Technology`, `Sports`, `Entertainment`, `Arts`

**Response 200 OK**
```json
[
  {
    "id": "3f2a1b4c-...",
    "question": "What is the speed of light?",
    "options": ["299,792 km/s", "150,000 km/s", "1,000 km/s", "500,000 km/s"],
    "correctOptionIndex": 0,
    "category": "Science",
    "difficulty": "Medium",
    "basePoints": 2
  }
]
```

**Response 400 Bad Request**
```json
{
  "title": "Invalid Request",
  "detail": "Count must be a positive number.",
  "status": 400
}
```

**Caching:** Output-cached for 60 seconds, keyed on `count + category`. Cache tag: `"QuizQuestions"`.

**Telemetry:** OpenTelemetry activity `"QuizGeneration"` with tags: `quiz.question_count`, `quiz.category`, `quiz.generated_count`, `quiz.duration_ms`.

---

### GET `/api/leaderboard`

Returns top 10 scores for a given category.

**Query Parameters**

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `category` | `string` | No | Filter by category. Defaults to `"General"` |

**Response 200 OK**
```json
[
  {
    "partitionKey": "Science",
    "rowKey": "a1b2c3d4-...",
    "playerName": "Alice",
    "score": 1450,
    "maxStreak": 5,
    "category": "Science",
    "datePlayed": "2026-02-18T10:30:00Z",
    "wins": 3,
    "losses": 1
  }
]
```

---

### POST `/api/leaderboard`

Submits a player score to the leaderboard.

**Request Body**
```json
{
  "playerName": "Alice",
  "score": 1450,
  "maxStreak": 5,
  "category": "Science",
  "wins": 1,
  "losses": 0
}
```

**Validation Rules**
- `playerName`: required, max 20 characters, HTML tags stripped
- `score`: must be ≥ 0, clamped to 10,000
- `category`: if not in allowed list, defaults to `"General"`

**Response 201 Created**
```json
{
  "partitionKey": "Science",
  "rowKey": "new-guid",
  "playerName": "Alice",
  "score": 1450,
  ...
}
```

**Response 400 Bad Request**
```json
"PlayerName is required."
```

---

### GET `/health`

Returns the health status of all registered health checks.

**Response 200 OK** (or **503** if any check fails)
```json
{
  "status": "Healthy",
  "timestamp": "2026-02-18T10:00:00Z",
  "checks": [
    {
      "name": "openai",
      "status": "Healthy",
      "description": "OpenAI is reachable",
      "duration": 142.3,
      "exception": null
    },
    {
      "name": "table-storage",
      "status": "Healthy",
      "description": "Table storage is reachable",
      "duration": 23.1,
      "exception": null
    }
  ]
}
```

---

### GET `/diag`

Returns masked configuration values for debugging. **Only expose in non-production or behind auth.**

**Response 200 OK**
```json
{
  "environment": "Development",
  "timestamp": "2026-02-18T10:00:00Z",
  "connections": {
    "tableStorage": "Defa****rage=true",
    "azureSignalR": "(not set)",
    "applicationInsights": "(not set)",
    "keyVault": "(not set)"
  },
  "azureOpenAI": {
    "endpoint": "https****net/",
    "apiKey": "****",
    "deploymentName": "gpt-4o"
  },
  "settings": {
    "urls": "http://0.0.0.0:5000;https://0.0.0.0:5001",
    "contentRoot": "C:\\...\\PoFunQuiz.Web"
  }
}
```

---

## SignalR Hub Contract

**Hub URL:** `/gamehub`

### Client → Server Methods

| Method | Parameters | Returns | Description |
|--------|-----------|---------|-------------|
| `CreateGame` | `playerName: string` | `string` (gameId) | Creates a new game session, returns 4-char Game ID |
| `JoinGame` | `dto: JoinGameDto` | `JoinGameResult` | Join existing session |
| `StartGame` | `gameId: string` | `void` | Host-only: starts the game |
| `UpdateScore` | `gameId, playerNumber (1\|2), score` | `void` | Update a player's score |
| `EndGame` | `gameId: string` | `void` | Mark game as completed |

### Server → Client Events

| Event | Payload | Triggered When |
|-------|---------|---------------|
| `GameUpdated` | `GameStateDto` | Any session state changes |
| `PlayerJoined` | `playerName: string` | A new player joins |
| `GameStarted` | `GameStateDto` | Host starts the game |
| `ScoreUpdated` | `GameStateDto` | Any score update |
| `GameEnded` | `GameStateDto` | Game is ended |

### JoinGameDto
```json
{ "playerName": "Bob", "gameId": "ABCD" }
```

### JoinGameResult
```json
{ "success": true, "failReason": "" }
```

**Fail reasons:** `"not_found"` | `"already_started"` | `"already_full"`

---

## Error Handling Policy

| Scenario | HTTP Status | Response |
|----------|------------|---------|
| Invalid question count | 400 | ProblemDetails JSON |
| Missing player name | 400 | Plain string message |
| Negative score | 400 | Plain string message |
| Unhandled server exception | 500 | ProblemDetails (GlobalExceptionHandlerMiddleware) |
| SignalR auth failure | HubException | Exception message as string |
| Health check failure | 503 | JSON with failed check details |

### GlobalExceptionHandlerMiddleware Policy
- Catches all unhandled exceptions
- Logs full exception with correlation ID via Serilog
- Returns `application/problem+json` with:
  - `status: 500`
  - `title: "An unexpected error occurred"`
  - Correlation ID in `extensions.correlationId`
  - Exception message **not exposed** in production

---

## .http Test File

See [src/PoFunQuiz.Web/PoFunQuiz.http](../src/PoFunQuiz.Web/PoFunQuiz.http) for ready-to-use HTTP request tests for all endpoints.
