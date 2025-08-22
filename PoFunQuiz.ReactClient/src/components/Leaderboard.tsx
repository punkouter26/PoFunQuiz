import React, { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import QuizApiService from '../services/QuizApiService';

interface LeaderboardEntry {
  id: string;
  playerName: string;
  score: number;
  accuracy: number;
  gamesPlayed: number;
  averageScore: number;
  maxStreak: number;
  lastPlayedDate: string;
  rank: number;
}

interface TimeFilter {
  label: string;
  value: 'all' | 'today' | 'week' | 'month';
}

const Leaderboard: React.FC = () => {
  const navigate = useNavigate();
  const [leaderboard, setLeaderboard] = useState<LeaderboardEntry[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string>('');
  const [timeFilter, setTimeFilter] = useState<'all' | 'today' | 'week' | 'month'>('all');
  const [sortBy, setSortBy] = useState<'score' | 'accuracy' | 'gamesPlayed'>('score');

  const timeFilters: TimeFilter[] = [
    { label: 'All Time', value: 'all' },
    { label: 'Today', value: 'today' },
    { label: 'This Week', value: 'week' },
    { label: 'This Month', value: 'month' }
  ];

  useEffect(() => {
    loadLeaderboard();
  }, [timeFilter, sortBy]);

  const loadLeaderboard = async () => {
    try {
      setIsLoading(true);
      setError('');

      const data = await QuizApiService.getLeaderboard({
        timeFilter,
        sortBy,
        limit: 50
      });

      // Process and rank the data
      const processedData: LeaderboardEntry[] = data.map((entry, index) => ({
        ...entry,
        rank: index + 1,
        accuracy: Math.round(entry.accuracy * 100) / 100,
        averageScore: Math.round(entry.averageScore * 100) / 100
      }));

      setLeaderboard(processedData);
    } catch (error) {
      console.error('Error loading leaderboard:', error);
      setError('Failed to load leaderboard. Please try again.');
    } finally {
      setIsLoading(false);
    }
  };

  const formatDate = (dateString: string): string => {
    const date = new Date(dateString);
    const now = new Date();
    const diffTime = Math.abs(now.getTime() - date.getTime());
    const diffDays = Math.ceil(diffTime / (1000 * 60 * 60 * 24));

    if (diffDays === 1) return 'Today';
    if (diffDays === 2) return 'Yesterday';
    if (diffDays <= 7) return `${diffDays} days ago`;
    return date.toLocaleDateString();
  };

  const getRankIcon = (rank: number): string => {
    switch (rank) {
      case 1: return '🥇';
      case 2: return '🥈';
      case 3: return '🥉';
      default: return `#${rank}`;
    }
  };

  const getRankStyle = (rank: number) => {
    const baseStyle = {
      fontWeight: 'bold',
      fontSize: '18px',
      minWidth: '60px',
      textAlign: 'center' as const
    };

    if (rank <= 3) {
      return {
        ...baseStyle,
        background: rank === 1 ? 'linear-gradient(45deg, #FFD700, #FFA500)' : 
                   rank === 2 ? 'linear-gradient(45deg, #C0C0C0, #808080)' : 
                   'linear-gradient(45deg, #CD7F32, #8B4513)',
        WebkitBackgroundClip: 'text',
        WebkitTextFillColor: 'transparent',
        fontSize: '20px'
      };
    }

    return { ...baseStyle, color: '#6c757d' };
  };

  if (isLoading) {
    return (
      <div style={{ padding: '20px', textAlign: 'center' }}>
        <h2>Loading Leaderboard...</h2>
        <div>Fetching latest scores...</div>
      </div>
    );
  }

  if (error) {
    return (
      <div style={{ padding: '20px', textAlign: 'center' }}>
        <h2>Error</h2>
        <p style={{ color: '#dc3545' }}>{error}</p>
        <button 
          onClick={loadLeaderboard}
          style={{
            padding: '10px 20px',
            backgroundColor: '#007bff',
            color: 'white',
            border: 'none',
            borderRadius: '5px',
            cursor: 'pointer'
          }}
        >
          Try Again
        </button>
      </div>
    );
  }

  return (
    <div style={{ padding: '20px', maxWidth: '1000px', margin: '0 auto' }}>
      <div style={{ textAlign: 'center', marginBottom: '30px' }}>
        <h1>🏆 Leaderboard</h1>
        <p style={{ color: '#6c757d' }}>Top performers in PoFunQuiz</p>
      </div>

      {/* Filters */}
      <div style={{ 
        display: 'flex', 
        justifyContent: 'space-between', 
        alignItems: 'center',
        flexWrap: 'wrap',
        gap: '15px',
        marginBottom: '30px',
        backgroundColor: '#f8f9fa',
        padding: '20px',
        borderRadius: '10px'
      }}>
        <div>
          <label style={{ fontWeight: 'bold', marginRight: '10px' }}>Time Period:</label>
          <select 
            value={timeFilter} 
            onChange={(e) => setTimeFilter(e.target.value as any)}
            style={{
              padding: '8px 12px',
              borderRadius: '5px',
              border: '1px solid #ced4da',
              fontSize: '14px'
            }}
          >
            {timeFilters.map(filter => (
              <option key={filter.value} value={filter.value}>
                {filter.label}
              </option>
            ))}
          </select>
        </div>

        <div>
          <label style={{ fontWeight: 'bold', marginRight: '10px' }}>Sort By:</label>
          <select 
            value={sortBy} 
            onChange={(e) => setSortBy(e.target.value as any)}
            style={{
              padding: '8px 12px',
              borderRadius: '5px',
              border: '1px solid #ced4da',
              fontSize: '14px'
            }}
          >
            <option value="score">Highest Score</option>
            <option value="accuracy">Best Accuracy</option>
            <option value="gamesPlayed">Most Games</option>
          </select>
        </div>

        <button 
          onClick={() => navigate('/')}
          style={{
            padding: '10px 20px',
            backgroundColor: '#28a745',
            color: 'white',
            border: 'none',
            borderRadius: '5px',
            cursor: 'pointer',
            fontWeight: 'bold'
          }}
        >
          🎮 Play Game
        </button>
      </div>

      {leaderboard.length === 0 ? (
        <div style={{ textAlign: 'center', padding: '50px' }}>
          <h3>No Results Found</h3>
          <p style={{ color: '#6c757d' }}>
            No leaderboard entries for the selected time period.
          </p>
          <button 
            onClick={() => navigate('/')}
            style={{
              padding: '12px 30px',
              backgroundColor: '#007bff',
              color: 'white',
              border: 'none',
              borderRadius: '5px',
              cursor: 'pointer',
              marginTop: '20px'
            }}
          >
            Start Playing
          </button>
        </div>
      ) : (
        <>
          {/* Top 3 Podium */}
          {leaderboard.length >= 3 && (
            <div style={{ 
              display: 'flex', 
              justifyContent: 'center', 
              alignItems: 'end',
              marginBottom: '40px',
              gap: '20px'
            }}>
              {/* 2nd Place */}
              <div style={{ 
                textAlign: 'center',
                backgroundColor: '#f8f9fa',
                padding: '20px',
                borderRadius: '10px',
                minWidth: '150px'
              }}>
                <div style={{ fontSize: '48px', marginBottom: '10px' }}>🥈</div>
                <h4 style={{ margin: '0 0 5px 0' }}>{leaderboard[1].playerName}</h4>
                <div style={{ fontWeight: 'bold', fontSize: '18px' }}>{leaderboard[1].score}</div>
                <div style={{ fontSize: '12px', color: '#6c757d' }}>{leaderboard[1].accuracy}% accuracy</div>
              </div>

              {/* 1st Place */}
              <div style={{ 
                textAlign: 'center',
                backgroundColor: 'linear-gradient(135deg, #FFD700, #FFA500)',
                background: 'linear-gradient(135deg, #fff3cd, #ffeaa7)',
                padding: '30px 20px',
                borderRadius: '10px',
                minWidth: '150px',
                border: '2px solid #FFD700'
              }}>
                <div style={{ fontSize: '64px', marginBottom: '10px' }}>🥇</div>
                <h3 style={{ margin: '0 0 5px 0' }}>{leaderboard[0].playerName}</h3>
                <div style={{ fontWeight: 'bold', fontSize: '24px', color: '#856404' }}>{leaderboard[0].score}</div>
                <div style={{ fontSize: '14px', color: '#856404' }}>{leaderboard[0].accuracy}% accuracy</div>
              </div>

              {/* 3rd Place */}
              <div style={{ 
                textAlign: 'center',
                backgroundColor: '#f8f9fa',
                padding: '20px',
                borderRadius: '10px',
                minWidth: '150px'
              }}>
                <div style={{ fontSize: '48px', marginBottom: '10px' }}>🥉</div>
                <h4 style={{ margin: '0 0 5px 0' }}>{leaderboard[2].playerName}</h4>
                <div style={{ fontWeight: 'bold', fontSize: '18px' }}>{leaderboard[2].score}</div>
                <div style={{ fontSize: '12px', color: '#6c757d' }}>{leaderboard[2].accuracy}% accuracy</div>
              </div>
            </div>
          )}

          {/* Full Leaderboard Table */}
          <div style={{ 
            backgroundColor: 'white',
            borderRadius: '10px',
            boxShadow: '0 2px 10px rgba(0,0,0,0.1)',
            overflow: 'hidden'
          }}>
            <table style={{ width: '100%', borderCollapse: 'collapse' }}>
              <thead>
                <tr style={{ backgroundColor: '#f8f9fa' }}>
                  <th style={{ padding: '15px', textAlign: 'center', borderBottom: '1px solid #dee2e6' }}>Rank</th>
                  <th style={{ padding: '15px', textAlign: 'left', borderBottom: '1px solid #dee2e6' }}>Player</th>
                  <th style={{ padding: '15px', textAlign: 'center', borderBottom: '1px solid #dee2e6' }}>Score</th>
                  <th style={{ padding: '15px', textAlign: 'center', borderBottom: '1px solid #dee2e6' }}>Accuracy</th>
                  <th style={{ padding: '15px', textAlign: 'center', borderBottom: '1px solid #dee2e6' }}>Games</th>
                  <th style={{ padding: '15px', textAlign: 'center', borderBottom: '1px solid #dee2e6' }}>Avg Score</th>
                  <th style={{ padding: '15px', textAlign: 'center', borderBottom: '1px solid #dee2e6' }}>Max Streak</th>
                  <th style={{ padding: '15px', textAlign: 'center', borderBottom: '1px solid #dee2e6' }}>Last Played</th>
                </tr>
              </thead>
              <tbody>
                {leaderboard.map((entry) => (
                  <tr 
                    key={entry.id}
                    style={{ 
                      backgroundColor: entry.rank <= 3 ? '#f8f9fa' : 'white',
                      borderBottom: '1px solid #dee2e6'
                    }}
                  >
                    <td style={{ padding: '15px', textAlign: 'center' }}>
                      <div style={getRankStyle(entry.rank)}>
                        {getRankIcon(entry.rank)}
                      </div>
                    </td>
                    <td style={{ padding: '15px', fontWeight: entry.rank <= 3 ? 'bold' : 'normal' }}>
                      {entry.playerName}
                    </td>
                    <td style={{ padding: '15px', textAlign: 'center', fontWeight: 'bold' }}>
                      {entry.score.toLocaleString()}
                    </td>
                    <td style={{ padding: '15px', textAlign: 'center' }}>
                      {entry.accuracy}%
                    </td>
                    <td style={{ padding: '15px', textAlign: 'center' }}>
                      {entry.gamesPlayed}
                    </td>
                    <td style={{ padding: '15px', textAlign: 'center' }}>
                      {entry.averageScore.toLocaleString()}
                    </td>
                    <td style={{ padding: '15px', textAlign: 'center' }}>
                      {entry.maxStreak}
                    </td>
                    <td style={{ padding: '15px', textAlign: 'center', fontSize: '14px', color: '#6c757d' }}>
                      {formatDate(entry.lastPlayedDate)}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>

          {leaderboard.length >= 50 && (
            <div style={{ textAlign: 'center', marginTop: '20px', color: '#6c757d' }}>
              Showing top 50 players
            </div>
          )}
        </>
      )}
    </div>
  );
};

export default Leaderboard;
