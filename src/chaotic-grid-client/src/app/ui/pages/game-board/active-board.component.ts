import { ChangeDetectionStrategy, Component, computed, effect, inject, signal, untracked } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatDialog } from '@angular/material/dialog';
import { MatIconModule } from '@angular/material/icon';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatToolbarModule } from '@angular/material/toolbar';
import { firstValueFrom } from 'rxjs';

import { ApiService } from '../../../core/services/api.service';
import { GameStore } from '../../../core/store/game.store';
import { SignalRService } from '../../../core/services/signalr.service';
import { BingoGridComponent } from '../../components/bingo-grid/bingo-grid.component';
import { VoteToastComponent } from '../../components/vote-toast/vote-toast.component';
import { PlayerPermissionsDialogComponent } from '../../components/player-permissions-dialog/player-permissions-dialog.component';

@Component({
  selector: 'app-active-board',
  standalone: true,
  imports: [MatToolbarModule, MatButtonModule, MatCardModule, MatIconModule, MatTooltipModule, RouterLink, BingoGridComponent, VoteToastComponent],
  template: `
    <mat-toolbar color="primary" class="toolbar">
      <div class="title">
        @if (state()) {
          {{ state()!.name }}
        } @else {
          Loading...
        }
      </div>

      <span class="spacer"></span>

      <button mat-icon-button matTooltip="My permissions" (click)="openPermissions()">
        <mat-icon>shield</mat-icon>
      </button>

      <a mat-button [routerLink]="['/lobby', boardId()]">Lobby</a>
    </mat-toolbar>

    <div class="content">
      @if (state(); as s) {
        <mat-card class="card">
          <mat-card-header>
            <mat-card-title>Board</mat-card-title>
            <mat-card-subtitle>Status: {{ s.status }}</mat-card-subtitle>
          </mat-card-header>
          <mat-card-content>
            <app-bingo-grid
              [gridTileIds]="gridTileIds()"
              [tilesById]="tilesById()"
              (tileClicked)="onTileClicked($event)"
            ></app-bingo-grid>
          </mat-card-content>
        </mat-card>

        @if (activeVoteTile()) {
          <app-vote-toast
            class="vote-toast"
            [tileText]="activeVoteTile()!.text"
            (voted)="onVoted($event)"
          ></app-vote-toast>
        }
      } @else {
        <mat-card class="card">
          <mat-card-content>Loading board state...</mat-card-content>
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

      .vote-toast {
        position: fixed;
        right: 16px;
        bottom: 16px;
      }
    `
  ],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class ActiveBoardComponent {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly api = inject(ApiService);
  private readonly store = inject(GameStore);
  private readonly signalr = inject(SignalRService);
  private readonly dialog = inject(MatDialog);

  readonly boardId = signal<string>(this.route.snapshot.paramMap.get('boardId') ?? '');

  readonly state = this.store.boardState;

  readonly gridTileIds = this.store.gridTileIds;

  readonly tilesById = computed(() => {
    const tiles = this.store.tiles();
    const map = new Map<string, (typeof tiles)[number]>();
    for (const t of tiles) {
      map.set(t.id, t);
    }
    return map;
  });

  readonly activeVote = computed(() => this.store.pendingCompletionVotes()[0] ?? null);

  readonly activeVoteTile = computed(() => {
    const vote = this.activeVote();
    if (!vote) {
      return null;
    }

    return this.tilesById().get(vote.tileId) ?? null;
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
        const s = this.store.boardState();
        if (s?.status === 0) {
          void this.router.navigate(['/lobby', id]);
        }
        return;
      }

      void untracked(async () => {
        const state = await firstValueFrom(this.api.getBoardState(id));
        this.store.setBoardState(state);

        await this.store.syncPlayerContext(id);

        if (state.status === 0) {
          void this.router.navigate(['/lobby', id]);
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

  async onTileClicked(tileId: string | null): Promise<void> {
    if (!tileId) {
      return;
    }

    const state = this.store.boardState();
    if (!state || state.status !== 1) {
      return;
    }

    const tile = this.tilesById().get(tileId);
    if (!tile || tile.isConfirmed) {
      return;
    }

    await this.signalr.proposeCompletion(this.boardId(), tileId);
  }

  async onVoted(isYes: boolean): Promise<void> {
    const vote = this.activeVote();
    if (!vote) {
      return;
    }

    await this.signalr.castCompletionVote(this.boardId(), { tileId: vote.tileId, isYes });
  }
}
