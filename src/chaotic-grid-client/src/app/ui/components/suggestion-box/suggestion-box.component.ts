import { ChangeDetectionStrategy, Component, Input, computed, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { firstValueFrom } from 'rxjs';

import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';

import { ApiService } from '../../../core/services/api.service';
import { AuthService } from '../../../core/auth/auth.service';
import { GamePermission } from '../../../core/models/permissions.enum';
import { GameStore } from '../../../core/store/game.store';

@Component({
  selector: 'app-suggestion-box',
  standalone: true,
  imports: [ReactiveFormsModule, MatCardModule, MatFormFieldModule, MatInputModule, MatButtonModule],
  template: `
    @if (canSuggest()) {
      <mat-card class="card">
        <mat-card-header>
          <mat-card-title>Suggest a tile</mat-card-title>
        </mat-card-header>

        <mat-card-content>
          <form [formGroup]="form" (ngSubmit)="submit()">
            <mat-form-field appearance="outline" class="field">
              <mat-label>Tile text</mat-label>
              <input matInput formControlName="text" />
            </mat-form-field>

            <div class="actions">
              <button mat-raised-button color="primary" type="submit" [disabled]="busy()">Submit</button>
            </div>
          </form>

          @if (error()) {
            <div class="error">{{ error() }}</div>
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

      .field {
        width: 100%;
      }

      .actions {
        margin-top: 8px;
      }

      .error {
        color: #b00020;
        margin-top: 8px;
      }
    `
  ],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class SuggestionBoxComponent {
  private readonly api = inject(ApiService);
  private readonly auth = inject(AuthService);
  private readonly store = inject(GameStore);
  private readonly fb = inject(FormBuilder);

  @Input({ required: true }) boardId!: string;

  readonly busy = signal(false);
  readonly error = signal<string | null>(null);

  readonly canSuggest = computed(() => this.auth.hasPermission(GamePermission.SuggestTile));

  readonly form = this.fb.nonNullable.group({
    text: ['', [Validators.required, Validators.maxLength(200)]]
  });

  async submit(): Promise<void> {
    this.error.set(null);

    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    try {
      this.busy.set(true);
      const tile = await firstValueFrom(
        this.api.suggestTile({
          boardId: this.boardId,
          text: this.form.controls.text.value.trim()
        })
      );

      this.store.upsertTile(tile);
      this.form.reset();
    } catch (e) {
      this.error.set(e instanceof Error ? e.message : 'Failed to suggest tile.');
    } finally {
      this.busy.set(false);
    }
  }
}
