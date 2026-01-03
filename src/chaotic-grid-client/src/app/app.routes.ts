import { Routes } from '@angular/router';

import { GameBoardComponent } from './ui/pages/game-board/game-board.component';
import { LobbyComponent } from './ui/pages/lobby/lobby.component';

export const routes: Routes = [
  { path: '', component: LobbyComponent },
  { path: 'board/:boardId', component: GameBoardComponent },
  { path: '**', redirectTo: '' }
];
