import { ChangeDetectionStrategy, Component, computed, effect, inject, signal, untracked } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { firstValueFrom } from 'rxjs';

import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatDialog } from '@angular/material/dialog';
import { MatIconModule } from '@angular/material/icon';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatToolbarModule } from '@angular/material/toolbar';

import { ApiService } from '../../../core/services/api.service';
import { GameStore } from '../../../core/store/game.store';
import { BoardPermission } from '../../../domain/models';
import { SuggestionBoxComponent } from '../../components/suggestion-box/suggestion-box.component';
import { ModerationQueueComponent } from '../../components/moderation-queue/moderation-queue.component';
import { PlayerPermissionsDialogComponent } from '../../components/player-permissions-dialog/player-permissions-dialog.component';

@Component({
  selector: 'app-draft-lobby',
  standalone: true,
  imports: [MatToolbarModule, MatButtonModule, MatCardModule, MatIconModule, MatTooltipModule, RouterLink, SuggestionBoxComponent, ModerationQueueComponent],
  template: `
    <mat-toolbar color="primary" class="toolbar">
      <div class="title">Draft Lobby</div>
      <span class="spacer"></span>

      <button mat-icon-button matTooltip="My permissions" (click)="openPermissions()">
        <mat-icon>shield</mat-icon>
      </button>

      <a mat-button routerLink="/">Home</a>
    </mat-toolbar>

    <div class="content">
      @if (state(); as s) {
        <mat-card class="card">
          <mat-card-header>
            <mat-card-title>{{ s.name }}</mat-card-title>
            <mat-card-subtitle>Status: {{ s.status }}</mat-card-subtitle>
          </mat-card-header>

          <mat-card-content>
            <div class="stats">
              <div>Approved tiles: {{ approvedCount() }}</div>
              <div>Pending tiles: {{ pendingCount() }}</div>
              <div>Rejected tiles: {{ rejectedCount() }}</div>
            </div>

            <app-suggestion-box [boardId]="boardId()"></app-suggestion-box>

            <app-moderation-queue [boardId]="boardId()"></app-moderation-queue>

            <div class="actions">
              @if (canStart()) {
                <button mat-raised-button color="primary" (click)="start()" [disabled]="busy()">
                  Start Game
                </button>
              }
              <a mat-raised-button color="primary" [routerLink]="['/board', boardId()]">Go to Board</a>
            </div>
          </mat-card-content>
        </mat-card>
      } @else {
        <mat-card class="card">
          <mat-card-content>Loading...</mat-card-content>
        </mat-card>
      }
    </div>
  `,
  styles: [
    `
      .toolbar {
        position: sticky;
        top: 0;
        z-index: 10;
      }

      .title {
        font-weight: 600;
      }

      .spacer {
        flex: 1;
      }

      .content {
        padding: 16px;
        display: flex;
        justify-content: center;
      }

      .card {
        width: 100%;
        max-width: 900px;
      }

      .stats {
        display: grid;
        grid-template-columns: repeat(3, minmax(0, 1fr));
        gap: 12px;
        margin-bottom: 16px;
        opacity: 0.9;
      }

      .actions {
        margin-top: 16px;
      }
    `
  ],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class DraftLobbyComponent {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly api = inject(ApiService);
  private readonly store = inject(GameStore);
  private readonly dialog = inject(MatDialog);

  readonly boardId = signal(this.route.snapshot.paramMap.get('boardId') ?? '');

  readonly state = this.store.boardState;

  readonly approvedCount = computed(() => this.store.tiles().filter(t => t.status === 1).length);
  readonly pendingCount = computed(() => this.store.tiles().filter(t => t.status === 0).length);
  readonly rejectedCount = computed(() => this.store.tiles().filter(t => t.status === 2).length);

  readonly busy = signal(false);

  readonly canStart = computed(() => {
    const s = this.state();
    if (!s) {
      return false;
    }

    if (s.status !== 0) {
      return false;
    }

    const ctx = this.store.playerContext();
    if (!ctx) {
      return false;
    }

    const canModify = (ctx.effectivePermissions & BoardPermission.ModifyBoardSettings) === BoardPermission.ModifyBoardSettings;
    return canModify && this.approvedCount() >= s.minimumApprovedTilesToStart;
  });

  constructor() {
    effect(() => {
      const id = this.boardId();
      if (!id) {
        void this.router.navigate(['/']);
        return;
      }

      const current = this.store.boardId();
      if (current === id) {
        return;
      }

      void untracked(async () => {
        const state = await firstValueFrom(this.api.getBoardState(id));
        this.store.setBoardState(state);

        await this.store.syncPlayerContext(id);

        // Pull tiles from /tiles so ApproveTile users get pending tiles too.
        const tiles = await firstValueFrom(this.api.getTiles(id));
        for (const t of tiles) {
          this.store.upsertTile(t);
        }
      });
    });
  }

  openPermissions(): void {
    const id = this.boardId();
    if (!id) {
      return;
    }

    this.dialog.open(PlayerPermissionsDialogComponent, { data: { boardId: id } });
  }

  async start(): Promise<void> {
    const id = this.boardId();
    if (!id) {
      return;
    }

    try {
      this.busy.set(true);
      const state = await firstValueFrom(this.api.startBoard(id));
      this.store.setBoardState(state);
      await this.router.navigate(['/board', id]);
    } finally {
      this.busy.set(false);
    }
  }
}
