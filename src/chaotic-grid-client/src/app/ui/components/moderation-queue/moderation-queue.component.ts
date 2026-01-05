import { ChangeDetectionStrategy, Component, Input, computed, inject, signal } from '@angular/core';
import { firstValueFrom } from 'rxjs';

import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';

import { ApiService } from '../../../core/services/api.service';
import { GameStore } from '../../../core/store/game.store';
import { BoardPermission, TileStatus } from '../../../domain/models';

@Component({
  selector: 'app-moderation-queue',
  standalone: true,
  imports: [MatCardModule, MatButtonModule],
  template: `
    @if (canApprove()) {
      <mat-card class="card">
        <mat-card-header>
          <mat-card-title>Moderation Queue</mat-card-title>
          <mat-card-subtitle>Pending: {{ pendingTiles().length }}</mat-card-subtitle>
        </mat-card-header>

        <mat-card-content>
          @if (error()) {
            <div class="error">{{ error() }}</div>
          }

          @for (tile of pendingTiles(); track tile.id) {
            <div class="row">
              <div class="text">{{ tile.text }}</div>
              <div class="actions">
                <button mat-stroked-button color="primary" (click)="approve(tile.id)" [disabled]="busy()">Approve</button>
                <button mat-stroked-button color="warn" (click)="reject(tile.id)" [disabled]="busy()">Reject</button>
              </div>
            </div>
          }

          @if (!pendingTiles().length) {
            <div class="empty">No pending tiles.</div>
          }
        </mat-card-content>
      </mat-card>
    }
  `,
  styles: [
    `
      .card {
        margin-top: 12px;
      }

      .row {
        display: flex;
        justify-content: space-between;
        align-items: center;
        gap: 12px;
        padding: 8px 0;
        border-bottom: 1px solid rgba(0, 0, 0, 0.08);
      }

      .text {
        flex: 1;
      }

      .actions {
        display: flex;
        gap: 8px;
      }

      .empty {
        opacity: 0.7;
        padding: 8px 0;
      }

      .error {
        color: #b00020;
        margin-bottom: 8px;
      }
    `
  ],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class ModerationQueueComponent {
  private readonly api = inject(ApiService);
  private readonly store = inject(GameStore);

  @Input({ required: true }) boardId!: string;

  readonly busy = signal(false);
  readonly error = signal<string | null>(null);

  readonly canApprove = computed(() => {
    const ctx = this.store.playerContext();
    if (!ctx) {
      return false;
    }

    return (ctx.effectivePermissions & BoardPermission.ApproveTile) === BoardPermission.ApproveTile;
  });

  readonly pendingTiles = computed(() => this.store.tiles().filter(t => t.status === TileStatus.Pending));

  async approve(tileId: string): Promise<void> {
    await this.moderate(tileId, TileStatus.Approved);
  }

  async reject(tileId: string): Promise<void> {
    await this.moderate(tileId, TileStatus.Rejected);
  }

  private async moderate(tileId: string, status: TileStatus): Promise<void> {
    this.error.set(null);

    try {
      this.busy.set(true);
      const tile = await firstValueFrom(this.api.moderateTile(tileId, { boardId: this.boardId, status }));
      this.store.upsertTile(tile);
    } catch (e) {
      this.error.set(e instanceof Error ? e.message : 'Failed to moderate tile.');
    } finally {
      this.busy.set(false);
    }
  }
}
