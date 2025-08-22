export interface Player {
  initials: string;
}

export interface QuizQuestion {
  question: string;
  options: string[];
  correctAnswer: string;
  explanation?: string;
}

export enum QuestionDifficulty {
  Easy = 0,
  Medium = 1,
  Hard = 2
}

export interface GameSession {
  gameId: string;
  player1: Player;
  player2: Player;
  player1Initials: string;
  player2Initials: string;
  player1Questions: QuizQuestion[];
  player2Questions: QuizQuestion[];
  startTime?: Date;
  endTime?: Date;
  player1BaseScore: number;
  player2BaseScore: number;
  player1StreakBonus: number;
  player2StreakBonus: number;
  player1SpeedBonus: number;
  player2SpeedBonus: number;
  player1TimeBonus: number;
  player2TimeBonus: number;
  player1Streak: number;
  player2Streak: number;
  player1MaxStreak: number;
  player2MaxStreak: number;
  player1Score: number;
  player2Score: number;
  selectedCategories: string[];
  gameDifficulty: QuestionDifficulty;
  isComplete: boolean;
  winner?: Player;
  isTie: boolean;
}

export interface GameState {
  currentGame?: GameSession;
}
