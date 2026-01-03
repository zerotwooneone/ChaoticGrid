import { Routes } from '@angular/router';

import { GameBoardComponent } from './ui/pages/game-board/game-board.component';
import { LobbyComponent } from './ui/pages/lobby/lobby.component';
import { authGuard } from './core/auth/auth.guard';

export const routes: Routes = [
  { path: '', component: LobbyComponent },
  { path: 'setup', loadComponent: () => import('./ui/pages/setup/setup.component').then(m => m.SetupComponent) },
  { path: 'invite/:token', loadComponent: () => import('./ui/pages/invite/invite.component').then(m => m.InviteComponent) },
  { path: 'invite', loadComponent: () => import('./ui/pages/invite/invite.component').then(m => m.InviteComponent) },
  { path: 'board/:boardId', component: GameBoardComponent, canActivate: [authGuard] },
  { path: '**', redirectTo: '' }
];
