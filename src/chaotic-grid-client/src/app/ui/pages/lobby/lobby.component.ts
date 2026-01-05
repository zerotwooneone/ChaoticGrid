import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { firstValueFrom } from 'rxjs';

import { ApiService } from '../../../core/services/api.service';
import { AuthService } from '../../../core/auth/auth.service';
import { SignalRService } from '../../../core/services/signalr.service';
import { GameStore } from '../../../core/store/game.store';

@Component({
  selector: 'app-lobby',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    RouterLink,
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
  private readonly auth = inject(AuthService);
  private readonly signalr = inject(SignalRService);
  private readonly store = inject(GameStore);
  private readonly router = inject(Router);
  private readonly fb = inject(FormBuilder);

  readonly isAuthed = this.auth.currentUser;

  readonly isBusy = signal(false);
  readonly error = signal<string | null>(null);

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

    try {
      this.isBusy.set(true);

      const state = await firstValueFrom(this.api.createBoard(boardName));
      if (!state) {
        throw new Error('Server did not return a board.');
      }

      this.store.setBoardState(state);

      await this.signalr.joinBoard(state.boardId, displayName, true, seed);

      await this.router.navigate(['/lobby', state.boardId]);
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

    try {
      this.isBusy.set(true);

      await this.signalr.joinBoard(boardId, displayName, false, seed);

      await this.router.navigate(['/lobby', boardId]);
    } catch (e) {
      this.error.set(e instanceof Error ? e.message : 'Failed to join board.');
    } finally {
      this.isBusy.set(false);
    }
  }
}
