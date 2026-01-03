import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { firstValueFrom } from 'rxjs';

import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';

import { ApiService } from '../../../core/services/api.service';
import { GameStore } from '../../../core/store/game.store';

@Component({
  selector: 'app-invite',
  standalone: true,
  imports: [MatCardModule, MatButtonModule, MatProgressSpinnerModule],
  template: `
    <div class="page">
      <mat-card class="card">
        <mat-card-header>
          <mat-card-title>Invite</mat-card-title>
          <mat-card-subtitle>Accept invite to join a board.</mat-card-subtitle>
        </mat-card-header>

        <mat-card-content>
          <div class="token">
            <div class="label">Token</div>
            <code>{{ token() }}</code>
          </div>

          @if (error()) {
            <div class="error">{{ error() }}</div>
          }

          <div class="actions">
            <button mat-raised-button color="primary" (click)="accept()" [disabled]="busy() || !token()">
              Accept & Join
            </button>
            @if (busy()) {
              <mat-progress-spinner diameter="20" mode="indeterminate"></mat-progress-spinner>
            }
          </div>
        </mat-card-content>
      </mat-card>
    </div>
  `,
  styles: [
    `
      .page {
        display: flex;
        justify-content: center;
        padding: 24px;
      }

      .card {
        width: 100%;
        max-width: 720px;
      }

      .token {
        margin-top: 8px;
        display: grid;
        gap: 4px;
      }

      .label {
        opacity: 0.8;
      }

      .actions {
        margin-top: 16px;
        display: flex;
        align-items: center;
        gap: 12px;
      }

      .error {
        color: #b00020;
        margin-top: 8px;
      }
    `
  ],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class InviteComponent {
  private readonly api = inject(ApiService);
  private readonly store = inject(GameStore);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);

  readonly busy = signal(false);
  readonly error = signal<string | null>(null);

  readonly token = computed(() => this.route.snapshot.paramMap.get('token') ?? this.route.snapshot.queryParamMap.get('token') ?? '');

  async accept(): Promise<void> {
    this.error.set(null);

    const token = this.token();
    if (!token) {
      this.error.set('Missing invite token.');
      return;
    }

    try {
      this.busy.set(true);
      const state = await firstValueFrom(this.api.joinByInvite(token));
      this.store.setBoardState(state);
      await this.router.navigate(['/board', state.boardId]);
    } catch (e) {
      this.error.set(e instanceof Error ? e.message : 'Failed to accept invite.');
    } finally {
      this.busy.set(false);
    }
  }
}
