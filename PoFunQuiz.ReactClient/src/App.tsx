import React from 'react';
import { BrowserRouter as Router, Routes, Route } from 'react-router-dom';
import { GameStateProvider } from './contexts/GameStateContext';
import Home from './components/Home';
import GameSetup from './components/GameSetup';
import GameBoard from './components/GameBoard';
import Results from './components/Results';
import Leaderboard from './components/Leaderboard';
import './App.css';

function App() {
  return (
    <GameStateProvider>
      <Router>
        <div className="App">
          <header className="App-header">
            <nav className="navbar">
              <h1 className="app-title">PoFunQuiz</h1>
            </nav>
          </header>
          <main className="App-main">
            <Routes>
              <Route path="/" element={<Home />} />
              <Route path="/gamesetup" element={<GameSetup />} />
              <Route path="/game-board" element={<GameBoard />} />
              <Route path="/results" element={<Results />} />
              <Route path="/leaderboard" element={<Leaderboard />} />
            </Routes>
          </main>
        </div>
      </Router>
    </GameStateProvider>
  );
}

export default App;
