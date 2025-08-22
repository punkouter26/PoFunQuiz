import React, { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { useGameState } from '../contexts/GameStateContext';
import { Player } from '../models/types';
import QuizApiService from '../services/QuizApiService';

interface PlayerResult extends Player {
  finalScore: number;
  accuracy: number;
  maxStreak: number;
  averageResponseTime: number;
  rank: number;
}

const Results: React.FC = () => {
  const navigate = useNavigate();
  const { gameState, resetGame } = useGameState();
  const [playerResults, setPlayerResults] = useState<PlayerResult[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [winner, setWinner] = useState<PlayerResult | null>(null);

  useEffect(() => {
    if (!gameState.sessionId) {
      navigate('/');
      return;
    }

    calculateResults();
  }, [gameState.sessionId]);

  const calculateResults = async () => {
    try {
      setIsLoading(true);
      
      // Get final game session data from API
      const sessionData = await QuizApiService.getGameSession(gameState.sessionId!);
      
      const results: PlayerResult[] = sessionData.players.map((player) => {
        // Calculate final score with bonuses
        const baseScore = player.score || 0;
        const streakBonus = (player.maxStreak || 0) * 50; // 50 points per max streak
        const speedBonus = Math.floor((player.averageResponseTime || 0) < 3000 ? 100 : 0); // Speed bonus for fast answers
        const finalScore = baseScore + streakBonus + speedBonus;

        // Calculate accuracy
        const totalAnswered = player.correctAnswers + player.incorrectAnswers;
        const accuracy = totalAnswered > 0 ? (player.correctAnswers / totalAnswered) * 100 : 0;

        return {
          ...player,
          finalScore,
          accuracy: Math.round(accuracy * 100) / 100,
          maxStreak: player.maxStreak || 0,
          averageResponseTime: player.averageResponseTime || 0,
          rank: 0 // Will be set below
        };
      });

      // Sort by final score and assign ranks
      results.sort((a, b) => b.finalScore - a.finalScore);
      results.forEach((result, index) => {
        result.rank = index + 1;
      });

      setPlayerResults(results);
      setWinner(results[0] || null);
      
      // Update leaderboard
      if (results.length > 0) {
        await QuizApiService.updateLeaderboard({
          sessionId: gameState.sessionId!,
          winners: results.slice(0, 3), // Top 3 players
          gameDate: new Date().toISOString(),
          totalQuestions: gameState.questions.length
        });
      }
    } catch (error) {
      console.error('Error calculating results:', error);
    } finally {
      setIsLoading(false);
    }
  };

  const handlePlayAgain = () => {
    resetGame();
    navigate('/');
  };

  const formatTime = (milliseconds: number): string => {
    return `${(milliseconds / 1000).toFixed(1)}s`;
  };

  if (isLoading) {
    return (
      <div style={{ padding: '20px', textAlign: 'center' }}>
        <h2>Calculating Results...</h2>
        <div>Processing game data...</div>
      </div>
    );
  }

  return (
    <div style={{ padding: '20px', maxWidth: '800px', margin: '0 auto' }}>
      <h1 style={{ textAlign: 'center', marginBottom: '30px' }}>🎉 Game Results 🎉</h1>
      
      {winner && (
        <div style={{ 
          backgroundColor: '#fff3cd', 
          border: '1px solid #ffeaa7', 
          borderRadius: '10px', 
          padding: '20px', 
          marginBottom: '30px',
          textAlign: 'center'
        }}>
          <h2 style={{ color: '#856404', marginBottom: '10px' }}>
            🏆 Winner: {winner.name}
          </h2>
          <p style={{ fontSize: '24px', fontWeight: 'bold', color: '#856404' }}>
            Final Score: {winner.finalScore} points
          </p>
        </div>
      )}

      <div style={{ marginBottom: '30px' }}>
        <h3>Final Standings</h3>
        <table style={{ width: '100%', borderCollapse: 'collapse', marginTop: '15px' }}>
          <thead>
            <tr style={{ backgroundColor: '#f8f9fa' }}>
              <th style={{ padding: '12px', border: '1px solid #dee2e6', textAlign: 'left' }}>Rank</th>
              <th style={{ padding: '12px', border: '1px solid #dee2e6', textAlign: 'left' }}>Player</th>
              <th style={{ padding: '12px', border: '1px solid #dee2e6', textAlign: 'center' }}>Final Score</th>
              <th style={{ padding: '12px', border: '1px solid #dee2e6', textAlign: 'center' }}>Accuracy</th>
              <th style={{ padding: '12px', border: '1px solid #dee2e6', textAlign: 'center' }}>Max Streak</th>
              <th style={{ padding: '12px', border: '1px solid #dee2e6', textAlign: 'center' }}>Avg Response</th>
            </tr>
          </thead>
          <tbody>
            {playerResults.map((result) => (
              <tr key={result.id}>
                <td style={{ padding: '12px', border: '1px solid #dee2e6' }}>
                  {result.rank === 1 && '🥇'}
                  {result.rank === 2 && '🥈'}
                  {result.rank === 3 && '🥉'}
                  {result.rank > 3 && `#${result.rank}`}
                </td>
                <td style={{ padding: '12px', border: '1px solid #dee2e6', fontWeight: 'bold' }}>
                  {result.name}
                </td>
                <td style={{ padding: '12px', border: '1px solid #dee2e6', textAlign: 'center', fontWeight: 'bold' }}>
                  {result.finalScore}
                </td>
                <td style={{ padding: '12px', border: '1px solid #dee2e6', textAlign: 'center' }}>
                  {result.accuracy}%
                </td>
                <td style={{ padding: '12px', border: '1px solid #dee2e6', textAlign: 'center' }}>
                  {result.maxStreak}
                </td>
                <td style={{ padding: '12px', border: '1px solid #dee2e6', textAlign: 'center' }}>
                  {formatTime(result.averageResponseTime)}
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      <div style={{ marginBottom: '30px' }}>
        <h3>Game Summary</h3>
        <div style={{ 
          backgroundColor: '#f8f9fa', 
          padding: '15px', 
          borderRadius: '5px',
          display: 'grid',
          gridTemplateColumns: 'repeat(auto-fit, minmax(200px, 1fr))',
          gap: '15px'
        }}>
          <div>
            <strong>Total Questions:</strong> {gameState.questions.length}
          </div>
          <div>
            <strong>Game Duration:</strong> {formatTime(gameState.gameDuration || 0)}
          </div>
          <div>
            <strong>Players:</strong> {playerResults.length}
          </div>
          <div>
            <strong>Completion Rate:</strong> 100%
          </div>
        </div>
      </div>

      <div style={{ textAlign: 'center' }}>
        <button 
          onClick={handlePlayAgain}
          style={{ 
            padding: '12px 30px', 
            margin: '10px',
            backgroundColor: '#28a745',
            color: 'white',
            border: 'none',
            borderRadius: '5px',
            cursor: 'pointer',
            fontSize: '16px',
            fontWeight: 'bold'
          }}
        >
          🎮 Play Again
        </button>
        <button 
          onClick={() => navigate('/leaderboard')} 
          style={{ 
            padding: '12px 30px', 
            margin: '10px',
            backgroundColor: '#17a2b8',
            color: 'white',
            border: 'none',
            borderRadius: '5px',
            cursor: 'pointer',
            fontSize: '16px',
            fontWeight: 'bold'
          }}
        >
          📊 View Leaderboard
        </button>
      </div>
    </div>
  );
};

export default Results;
