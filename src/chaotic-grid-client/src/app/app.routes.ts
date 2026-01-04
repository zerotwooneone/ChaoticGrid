import { Routes } from '@angular/router';

import { ActiveBoardComponent } from './ui/pages/game-board/active-board.component';
import { LobbyComponent } from './ui/pages/lobby/lobby.component';
import { authGuard } from './core/auth/auth.guard';

export const routes: Routes = [
  { path: '', component: LobbyComponent },
  { path: 'lobby/:boardId', loadComponent: () => import('./ui/pages/lobby/draft-lobby.component').then(m => m.DraftLobbyComponent), canActivate: [authGuard] },
  { path: 'setup', loadComponent: () => import('./ui/pages/setup/setup.component').then(m => m.SetupComponent) },
  { path: 'invite/:token', loadComponent: () => import('./ui/pages/invite/invite.component').then(m => m.InviteComponent) },
  { path: 'invite', loadComponent: () => import('./ui/pages/invite/invite.component').then(m => m.InviteComponent) },
  { path: 'board/:boardId', component: ActiveBoardComponent, canActivate: [authGuard] },
  { path: '**', redirectTo: '' }
];
