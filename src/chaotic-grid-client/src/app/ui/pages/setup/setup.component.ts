import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { firstValueFrom } from 'rxjs';

import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';

import { ApiService } from '../../../core/services/api.service';
import { AuthService } from '../../../core/auth/auth.service';

@Component({
  selector: 'app-setup',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    MatCardModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    MatProgressSpinnerModule
  ],
  template: `
    <div class="page">
      <mat-card class="card">
        <mat-card-header>
          <mat-card-title>Initial Setup</mat-card-title>
          <mat-card-subtitle>
            Check <code>/app_data/setup-token.txt</code> for the setup token.
          </mat-card-subtitle>
        </mat-card-header>

        <mat-card-content>
          <form [formGroup]="form" (ngSubmit)="onSubmit()">
            <mat-form-field appearance="outline" class="field">
              <mat-label>Setup token</mat-label>
              <input matInput formControlName="token" autocomplete="off" />
            </mat-form-field>

            <mat-form-field appearance="outline" class="field">
              <mat-label>Nickname</mat-label>
              <input matInput formControlName="nickname" autocomplete="off" />
            </mat-form-field>

            @if (error()) {
              <div class="error">{{ error() }}</div>
            }

            <div class="actions">
              <button mat-raised-button color="primary" type="submit" [disabled]="busy()">
                Setup
              </button>
              @if (busy()) {
                <mat-progress-spinner diameter="20" mode="indeterminate"></mat-progress-spinner>
              }
            </div>
          </form>
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
        max-width: 520px;
      }

      .field {
        width: 100%;
      }

      .actions {
        margin-top: 12px;
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
export class SetupComponent {
  private readonly api = inject(ApiService);
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private readonly fb = inject(FormBuilder);

  readonly busy = signal(false);
  readonly error = signal<string | null>(null);

  readonly form = this.fb.nonNullable.group({
    token: ['', [Validators.required]],
    nickname: ['', [Validators.required, Validators.maxLength(80)]]
  });

  async onSubmit(): Promise<void> {
    this.error.set(null);

    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    try {
      this.busy.set(true);
      const res = await firstValueFrom(
        this.api.setup({
          token: this.form.controls.token.value.trim(),
          nickname: this.form.controls.nickname.value.trim()
        })
      );

      this.auth.setJwt(res.jwt);
      await this.router.navigate(['/']);
    } catch (e) {
      this.error.set(e instanceof Error ? e.message : 'Setup failed.');
    } finally {
      this.busy.set(false);
    }
  }
}
