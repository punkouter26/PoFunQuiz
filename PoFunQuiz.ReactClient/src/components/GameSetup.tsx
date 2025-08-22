import React, { useState, useEffect, useRef } from 'react';
import { useNavigate, useSearchParams } from 'react-router-dom';
import { useGameState } from '../contexts/GameStateContext';
import QuizApiService from '../services/QuizApiService';
import { GameSession, Player, QuestionDifficulty } from '../models/types';

const GameSetup: React.FC = () => {
  const [searchParams] = useSearchParams();
  const navigate = useNavigate();
  const { gameState, setCurrentGame } = useGameState();
  const [isLoading, setIsLoading] = useState(false);
  const [isCountingDown, setIsCountingDown] = useState(false);
  const [countdownValue, setCountdownValue] = useState(3);
  const [player1Ready, setPlayer1Ready] = useState(false);
  const [player2Ready, setPlayer2Ready] = useState(false);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);
  const containerRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    // Initialize game from URL parameters
    const player1 = searchParams.get('player1');
    const player2 = searchParams.get('player2');
    const topic = searchParams.get('topic');

    if (player1 && player2 && topic) {
      const newGame: GameSession = {
        gameId: generateGameId(),
        player1: { initials: player1 },
        player2: { initials: player2 },
        player1Initials: player1,
        player2Initials: player2,
        player1Questions: [],
        player2Questions: [],
        player1BaseScore: 0,
        player2BaseScore: 0,
        player1StreakBonus: 0,
        player2StreakBonus: 0,
        player1SpeedBonus: 0,
        player2SpeedBonus: 0,
        player1TimeBonus: 0,
        player2TimeBonus: 0,
        player1Streak: 0,
        player2Streak: 0,
        player1MaxStreak: 0,
        player2MaxStreak: 0,
        player1Score: 0,
        player2Score: 0,
        selectedCategories: [topic],
        gameDifficulty: QuestionDifficulty.Medium,
        isComplete: false,
        isTie: false
      };
      setCurrentGame(newGame);
    } else {
      setErrorMessage('Game session not found.');
    }

    // Focus container for keyboard events
    if (containerRef.current) {
      containerRef.current.focus();
    }
  }, [searchParams, setCurrentGame]);

  useEffect(() => {
    if (player1Ready && player2Ready && !isLoading && !isCountingDown) {
      generateQuestionsAndStart();
    }
  }, [player1Ready, player2Ready]);

  const generateGameId = () => {
    return Math.random().toString(36).substr(2, 9);
  };

  const generateQuestionsAndStart = async () => {
    if (!gameState.currentGame) return;

    setIsLoading(true);
    setErrorMessage(null);

    try {
      const category = gameState.currentGame.selectedCategories[0];
      const questionCount = 10;

      const [player1Questions, player2Questions] = await Promise.all([
        QuizApiService.generateQuestions(questionCount),
        QuizApiService.generateQuestions(questionCount)
      ]);

      const updatedGame = {
        ...gameState.currentGame,
        player1Questions,
        player2Questions,
        startTime: new Date()
      };

      setCurrentGame(updatedGame);
      setIsLoading(false);
      startCountdown();
    } catch (error) {
      setIsLoading(false);
      setErrorMessage('Failed to generate questions. Please try again.');
      console.error('Error generating questions:', error);
    }
  };

  const startCountdown = () => {
    setIsCountingDown(true);
    setCountdownValue(3);

    const countdownInterval = setInterval(() => {
      setCountdownValue(prev => {
        if (prev <= 1) {
          clearInterval(countdownInterval);
          setIsCountingDown(false);
          navigate('/game-board');
          return 0;
        }
        return prev - 1;
      });
    }, 1000);
  };

  const setPlayerReady = (playerNumber: number) => {
    if (playerNumber === 1) {
      setPlayer1Ready(true);
    } else {
      setPlayer2Ready(true);
    }
  };

  const handleKeyDown = (event: React.KeyboardEvent) => {
    const key = event.key;
    
    if (!player1Ready && (key === '1' || key === '2' || key === '3' || key === '4')) {
      setPlayer1Ready(true);
    } else if (!player2Ready && (key === '6' || key === '7' || key === '8' || key === '9')) {
      setPlayer2Ready(true);
    }
  };

  const retrySetup = () => {
    setErrorMessage(null);
    setPlayer1Ready(false);
    setPlayer2Ready(false);
    setIsLoading(false);
    setIsCountingDown(false);
  };

  if (!gameState.currentGame) {
    return (
      <div className="game-setup-container">
        <div className="game-setup-content">
          <h4>Game session not found.</h4>
          <button onClick={() => navigate('/')} className="button primary">
            Return to Main Menu
          </button>
        </div>
      </div>
    );
  }

  return (
    <div 
      className="game-setup-container"
      onKeyDown={handleKeyDown}
      tabIndex={0}
      ref={containerRef}
    >
      <div className="game-setup-content">
        {isLoading ? (
          <>
            <h5>Generating Questions...</h5>
            <div className="progress-bar">
              <div className="progress-bar-fill"></div>
            </div>
          </>
        ) : isCountingDown ? (
          <>
            <h5 className="text-primary">Get Ready!</h5>
            <h1 className="countdown-text">{countdownValue}</h1>
          </>
        ) : errorMessage ? (
          <>
            <div className="alert alert-danger">{errorMessage}</div>
            <button onClick={retrySetup} className="button primary">
              Retry
            </button>
            <button onClick={() => navigate('/')} className="button secondary">
              Back to Main Menu
            </button>
          </>
        ) : (
          <>
            <div className="game-info">
              <div className="info-card">
                <h6>Topic: {gameState.currentGame.selectedCategories.join(', ')}</h6>
                <p>Difficulty: {QuestionDifficulty[gameState.currentGame.gameDifficulty]}</p>
              </div>
            </div>

            <div className="players-container">
              <div className="player-card">
                <h5 className="text-primary">Player 1</h5>
                <div className="avatar primary">{gameState.currentGame.player1.initials}</div>
                <p>Use keys 1-4</p>
                {!player1Ready ? (
                  <button 
                    onClick={() => setPlayerReady(1)}
                    className="button primary"
                  >
                    I'm Ready
                  </button>
                ) : (
                  <div className="ready-badge">Ready! ✓</div>
                )}
              </div>

              <div className="player-card">
                <h5 className="text-secondary">Player 2</h5>
                <div className="avatar secondary">{gameState.currentGame.player2.initials}</div>
                <p>Use keys 6-9</p>
                {!player2Ready ? (
                  <button 
                    onClick={() => setPlayerReady(2)}
                    className="button secondary"
                  >
                    I'm Ready
                  </button>
                ) : (
                  <div className="ready-badge">Ready! ✓</div>
                )}
              </div>
            </div>
          </>
        )}
      </div>
    </div>
  );
};

export default GameSetup;
