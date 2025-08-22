import React, { useEffect, useState, useCallback } from 'react';
import { useNavigate } from 'react-router-dom';
import { useGameState } from '../contexts/GameStateContext';
import QuizApiService from '../services/QuizApiService';

const GameBoard: React.FC = () => {
  const navigate = useNavigate();
  const { gameState, updateGameState } = useGameState();
  const [currentQuestionIndex, setCurrentQuestionIndex] = useState(0);
  const [timeRemaining, setTimeRemaining] = useState(30);
  const [isAnswered, setIsAnswered] = useState(false);
  const [selectedAnswer, setSelectedAnswer] = useState<string>('');
  const [showFeedback, setShowFeedback] = useState(false);
  const [playerStats, setPlayerStats] = useState<{[playerId: string]: {score: number, streak: number}}>({});
  const [gameTimer, setGameTimer] = useState<NodeJS.Timeout | null>(null);
  const [questionStartTime, setQuestionStartTime] = useState<number>(0);

  const currentQuestion = gameState.questions[currentQuestionIndex];
  const isGameFinished = currentQuestionIndex >= gameState.questions.length;

  useEffect(() => {
    if (!gameState.sessionId || gameState.questions.length === 0) {
      navigate('/setup');
      return;
    }

    startQuestionTimer();
    setQuestionStartTime(Date.now());

    return () => {
      if (gameTimer) clearInterval(gameTimer);
    };
  }, [currentQuestionIndex]);

  useEffect(() => {
    const handleKeyPress = (event: KeyboardEvent) => {
      if (isAnswered || showFeedback) return;

      const key = event.key.toLowerCase();
      const answerKeys = ['a', 'b', 'c', 'd'];
      const keyIndex = answerKeys.indexOf(key);

      if (keyIndex !== -1 && keyIndex < currentQuestion?.options.length) {
        handleAnswerSelect(currentQuestion.options[keyIndex]);
      }
    };

    window.addEventListener('keydown', handleKeyPress);
    return () => window.removeEventListener('keydown', handleKeyPress);
  }, [isAnswered, showFeedback, currentQuestion]);

  const startQuestionTimer = () => {
    if (gameTimer) clearInterval(gameTimer);
    
    setTimeRemaining(30);
    setIsAnswered(false);
    setSelectedAnswer('');
    setShowFeedback(false);

    const timer = setInterval(() => {
      setTimeRemaining((prev) => {
        if (prev <= 1) {
          handleTimeUp();
          return 0;
        }
        return prev - 1;
      });
    }, 1000);

    setGameTimer(timer);
  };

  const handleAnswerSelect = async (answer: string) => {
    if (isAnswered || showFeedback) return;

    setSelectedAnswer(answer);
    setIsAnswered(true);

    const responseTime = Date.now() - questionStartTime;
    const isCorrect = answer === currentQuestion.correctAnswer;

    // Show immediate feedback
    setShowFeedback(true);

    try {
      // Submit answer to backend
      await QuizApiService.submitAnswer({
        sessionId: gameState.sessionId!,
        questionId: currentQuestion.id,
        playerId: gameState.currentPlayerId || 'player1', // Assuming single player for now
        answer: answer,
        isCorrect: isCorrect,
        responseTime: responseTime
      });

      // Update local player stats
      updatePlayerStats(gameState.currentPlayerId || 'player1', isCorrect, responseTime);

      // Auto-advance after showing feedback
      setTimeout(() => {
        nextQuestion();
      }, 2000);

    } catch (error) {
      console.error('Error submitting answer:', error);
    }
  };

  const handleTimeUp = () => {
    if (!isAnswered) {
      handleAnswerSelect(''); // Submit empty answer for timeout
    }
  };

  const updatePlayerStats = (playerId: string, isCorrect: boolean, responseTime: number) => {
    setPlayerStats(prev => {
      const currentStats = prev[playerId] || { score: 0, streak: 0 };
      const scoreIncrement = isCorrect ? calculateScore(responseTime, currentStats.streak) : 0;
      const newStreak = isCorrect ? currentStats.streak + 1 : 0;

      return {
        ...prev,
        [playerId]: {
          score: currentStats.score + scoreIncrement,
          streak: newStreak
        }
      };
    });
  };

  const calculateScore = (responseTime: number, currentStreak: number): number => {
    let baseScore = 100;
    
    // Speed bonus (faster = more points)
    const speedBonus = Math.max(0, Math.floor((30000 - responseTime) / 1000) * 5);
    
    // Streak bonus
    const streakBonus = currentStreak * 10;
    
    return baseScore + speedBonus + streakBonus;
  };

  const nextQuestion = () => {
    if (gameTimer) clearInterval(gameTimer);

    if (currentQuestionIndex + 1 >= gameState.questions.length) {
      // Game finished
      finishGame();
    } else {
      setCurrentQuestionIndex(prev => prev + 1);
    }
  };

  const finishGame = async () => {
    try {
      await QuizApiService.endGame(gameState.sessionId!);
      updateGameState({ isGameActive: false });
      navigate('/results');
    } catch (error) {
      console.error('Error finishing game:', error);
    }
  };

  const getAnswerButtonStyle = (option: string) => {
    const baseStyle = {
      padding: '15px 20px',
      margin: '5px',
      border: 'none',
      borderRadius: '5px',
      cursor: isAnswered ? 'default' : 'pointer',
      fontSize: '16px',
      fontWeight: 'bold',
      transition: 'all 0.3s ease',
      width: '100%',
      textAlign: 'left' as const,
      minHeight: '60px',
      display: 'flex',
      alignItems: 'center'
    };

    if (!showFeedback) {
      return {
        ...baseStyle,
        backgroundColor: selectedAnswer === option ? '#007bff' : '#f8f9fa',
        color: selectedAnswer === option ? 'white' : '#333',
        opacity: isAnswered ? 0.7 : 1
      };
    }

    // Show feedback colors
    if (option === currentQuestion.correctAnswer) {
      return { ...baseStyle, backgroundColor: '#28a745', color: 'white' };
    } else if (option === selectedAnswer) {
      return { ...baseStyle, backgroundColor: '#dc3545', color: 'white' };
    } else {
      return { ...baseStyle, backgroundColor: '#6c757d', color: 'white', opacity: 0.6 };
    }
  };

  const getTimerColor = () => {
    if (timeRemaining > 20) return '#28a745';
    if (timeRemaining > 10) return '#ffc107';
    return '#dc3545';
  };

  if (isGameFinished) {
    navigate('/results');
    return null;
  }

  if (!currentQuestion) {
    return (
      <div style={{ padding: '20px', textAlign: 'center' }}>
        <h2>Loading...</h2>
      </div>
    );
  }

  const currentPlayerStats = playerStats[gameState.currentPlayerId || 'player1'] || { score: 0, streak: 0 };

  return (
    <div style={{ padding: '20px', maxWidth: '800px', margin: '0 auto' }}>
      {/* Game Header */}
      <div style={{ 
        display: 'flex', 
        justifyContent: 'space-between', 
        alignItems: 'center',
        marginBottom: '20px',
        backgroundColor: '#f8f9fa',
        padding: '15px',
        borderRadius: '10px'
      }}>
        <div>
          <h3 style={{ margin: 0 }}>Question {currentQuestionIndex + 1} of {gameState.questions.length}</h3>
          <div style={{ fontSize: '14px', color: '#6c757d' }}>
            Score: {currentPlayerStats.score} | Streak: {currentPlayerStats.streak}
          </div>
        </div>
        <div style={{ textAlign: 'center' }}>
          <div style={{ 
            fontSize: '32px', 
            fontWeight: 'bold', 
            color: getTimerColor(),
            margin: '0'
          }}>
            {timeRemaining}s
          </div>
          <div style={{ fontSize: '12px', color: '#6c757d' }}>Time Remaining</div>
        </div>
      </div>

      {/* Progress Bar */}
      <div style={{ 
        width: '100%', 
        backgroundColor: '#e9ecef', 
        borderRadius: '5px', 
        height: '8px',
        marginBottom: '30px'
      }}>
        <div style={{ 
          width: `${((currentQuestionIndex + 1) / gameState.questions.length) * 100}%`,
          backgroundColor: '#007bff',
          height: '100%',
          borderRadius: '5px',
          transition: 'width 0.3s ease'
        }}></div>
      </div>

      {/* Question */}
      <div style={{ 
        backgroundColor: 'white',
        padding: '30px',
        borderRadius: '10px',
        boxShadow: '0 2px 10px rgba(0,0,0,0.1)',
        marginBottom: '20px'
      }}>
        <h2 style={{ 
          fontSize: '24px', 
          marginBottom: '30px',
          lineHeight: '1.4',
          textAlign: 'center'
        }}>
          {currentQuestion.question}
        </h2>

        {/* Answer Options */}
        <div style={{ display: 'grid', gap: '10px' }}>
          {currentQuestion.options.map((option, index) => (
            <button
              key={index}
              onClick={() => handleAnswerSelect(option)}
              disabled={isAnswered}
              style={getAnswerButtonStyle(option)}
            >
              <span style={{ 
                backgroundColor: 'rgba(0,0,0,0.1)', 
                borderRadius: '50%', 
                width: '30px', 
                height: '30px', 
                display: 'flex', 
                alignItems: 'center', 
                justifyContent: 'center',
                marginRight: '15px',
                fontWeight: 'bold'
              }}>
                {String.fromCharCode(65 + index)}
              </span>
              {option}
            </button>
          ))}
        </div>

        {/* Keyboard Hint */}
        {!isAnswered && (
          <div style={{ 
            textAlign: 'center', 
            marginTop: '20px', 
            fontSize: '14px', 
            color: '#6c757d' 
          }}>
            💡 Use keyboard: A, B, C, D to answer quickly
          </div>
        )}

        {/* Feedback */}
        {showFeedback && (
          <div style={{
            marginTop: '20px',
            padding: '15px',
            borderRadius: '5px',
            backgroundColor: selectedAnswer === currentQuestion.correctAnswer ? '#d4edda' : '#f8d7da',
            border: `1px solid ${selectedAnswer === currentQuestion.correctAnswer ? '#c3e6cb' : '#f5c6cb'}`,
            textAlign: 'center'
          }}>
            <strong>
              {selectedAnswer === currentQuestion.correctAnswer 
                ? '✅ Correct!' 
                : `❌ Incorrect. The answer was: ${currentQuestion.correctAnswer}`
              }
            </strong>
            {selectedAnswer === currentQuestion.correctAnswer && (
              <div style={{ marginTop: '5px', fontSize: '14px' }}>
                +{calculateScore(Date.now() - questionStartTime, currentPlayerStats.streak - (selectedAnswer === currentQuestion.correctAnswer ? 1 : 0))} points
              </div>
            )}
          </div>
        )}
      </div>
    </div>
  );
};

export default GameBoard;
