import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';

import { ApiService } from '../../../core/services/api.service';
import { SignalRService } from '../../../core/services/signalr.service';
import { GameStore } from '../../../core/store/game.store';

@Component({
  selector: 'app-lobby',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    MatCardModule,
    MatFormFieldModule,
    MatInputModule,
    MatCheckboxModule,
    MatButtonModule,
    MatProgressSpinnerModule
  ],
  templateUrl: './lobby.component.html',
  styleUrl: './lobby.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class LobbyComponent {
  private readonly api = inject(ApiService);
  private readonly signalr = inject(SignalRService);
  private readonly store = inject(GameStore);
  private readonly router = inject(Router);
  private readonly fb = inject(FormBuilder);

  readonly isBusy = signal(false);
  readonly error = signal<string | null>(null);

  readonly suggestedPlayerId = computed(() => crypto.randomUUID());

  readonly createForm = this.fb.nonNullable.group({
    boardName: ['', [Validators.required, Validators.maxLength(200)]],
    displayName: ['', [Validators.required, Validators.maxLength(80)]],
    seed: [undefined as number | undefined]
  });

  readonly joinForm = this.fb.nonNullable.group({
    boardId: ['', [Validators.required]],
    displayName: ['', [Validators.required, Validators.maxLength(80)]],
    seed: [undefined as number | undefined]
  });

  async createBoard(): Promise<void> {
    this.error.set(null);
    if (this.createForm.invalid) {
      this.createForm.markAllAsTouched();
      return;
    }

    const boardName = this.createForm.controls.boardName.value;
    const displayName = this.createForm.controls.displayName.value;
    const seed = this.createForm.controls.seed.value;
    const playerId = this.suggestedPlayerId();

    try {
      this.isBusy.set(true);

      const state = await this.api.createBoard(boardName).toPromise();
      if (!state) {
        throw new Error('Server did not return a board.');
      }

      this.store.setBoardState(state);
      this.store.setLocalPlayer(playerId);

      await this.signalr.joinBoard(state.boardId, playerId, displayName, true, seed);

      await this.router.navigate(['/board', state.boardId]);
    } catch (e) {
      this.error.set(e instanceof Error ? e.message : 'Failed to create board.');
    } finally {
      this.isBusy.set(false);
    }
  }

  async joinBoard(): Promise<void> {
    this.error.set(null);
    if (this.joinForm.invalid) {
      this.joinForm.markAllAsTouched();
      return;
    }

    const boardId = this.joinForm.controls.boardId.value.trim();
    const displayName = this.joinForm.controls.displayName.value;
    const seed = this.joinForm.controls.seed.value;
    const playerId = this.suggestedPlayerId();

    try {
      this.isBusy.set(true);

      await this.signalr.joinBoard(boardId, playerId, displayName, false, seed);

      await this.router.navigate(['/board', boardId]);
    } catch (e) {
      this.error.set(e instanceof Error ? e.message : 'Failed to join board.');
    } finally {
      this.isBusy.set(false);
    }
  }
}
