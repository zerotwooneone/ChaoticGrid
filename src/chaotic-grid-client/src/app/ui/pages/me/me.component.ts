import { ChangeDetectionStrategy, Component, computed, effect, inject, signal, untracked } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { firstValueFrom } from 'rxjs';

import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatToolbarModule } from '@angular/material/toolbar';

import { ApiService } from '../../../core/services/api.service';
import { AuthService } from '../../../core/auth/auth.service';
import { BoardPermission, RoleTemplateDto } from '../../../domain/models';
import { SystemPermission } from '../../../core/models/permissions.enum';

@Component({
  selector: 'app-me',
  standalone: true,
  imports: [
    RouterLink,
    ReactiveFormsModule,
    MatToolbarModule,
    MatButtonModule,
    MatCardModule,
    MatFormFieldModule,
    MatInputModule,
    MatCheckboxModule,
    MatProgressSpinnerModule
  ],
  template: `
    <mat-toolbar color="primary" class="toolbar">
      <div class="title">Profile</div>
      <span class="spacer"></span>
      <a mat-button routerLink="/">Home</a>
    </mat-toolbar>

    <div class="page">
      <mat-card class="card">
        <mat-card-header>
          <mat-card-title>System Context</mat-card-title>
          <mat-card-subtitle>Your global identity and permissions</mat-card-subtitle>
        </mat-card-header>

        <mat-card-content>
          @if (busyContext()) {
            <mat-progress-spinner diameter="20" mode="indeterminate"></mat-progress-spinner>
          } @else if (contextError()) {
            <div class="error">{{ contextError() }}</div>
          } @else {
            <div class="row"><span class="label">Nickname:</span> <span>{{ nickname() }}</span></div>
            <div class="row"><span class="label">System mask:</span> <code>{{ systemPermissions() }}</code></div>
          }
        </mat-card-content>
      </mat-card>

      @if (canManageTemplates()) {
        <mat-card class="card">
          <mat-card-header>
            <mat-card-title>Role Template Editor</mat-card-title>
            <mat-card-subtitle>Applies to future boards you create</mat-card-subtitle>
          </mat-card-header>

          <mat-card-content>
            @if (busyTemplates()) {
              <mat-progress-spinner diameter="20" mode="indeterminate"></mat-progress-spinner>
            } @else if (templatesError()) {
              <div class="error">{{ templatesError() }}</div>
            }

            <form [formGroup]="createForm" (ngSubmit)="create()" class="section">
              <h3>Create Template</h3>

              <mat-form-field appearance="outline" class="field">
                <mat-label>Name</mat-label>
                <input matInput formControlName="name" />
              </mat-form-field>

              <div class="perm-list">
                @for (p of permissions(); track p.flag) {
                  <mat-checkbox
                    [checked]="hasFlag(createMask(), p.flag)"
                    (change)="setCreateMask(toggleMask(createMask(), p.flag, $event.checked))">
                    {{ p.label }}
                  </mat-checkbox>
                }
              </div>

              <div class="actions">
                <button mat-raised-button color="primary" type="submit" [disabled]="busyTemplates()">Create</button>
              </div>
            </form>

            <div class="section">
              <h3>Existing Templates</h3>

              @if (!templates().length) {
                <div class="muted">No templates yet.</div>
              } @else {
                <div class="template-list">
                  @for (t of templates(); track t.id) {
                    <div class="template">
                      <div class="template-header">
                        <div>
                          <div class="template-name">{{ t.name }}</div>
                          <div class="muted">Mask: <code>{{ t.defaultBoardPermissions }}</code></div>
                        </div>
                        <div class="template-actions">
                          <button mat-stroked-button color="primary" (click)="beginEdit(t)" [disabled]="busyTemplates()">
                            {{ editingId() === t.id ? 'Cancel' : 'Edit' }}
                          </button>
                          <button mat-stroked-button color="warn" (click)="remove(t)" [disabled]="busyTemplates()">Delete</button>
                        </div>
                      </div>

                      @if (editingId() === t.id) {
                        <form [formGroup]="editForm" (ngSubmit)="saveEdit()" class="edit">
                          <mat-form-field appearance="outline" class="field">
                            <mat-label>Name</mat-label>
                            <input matInput formControlName="name" />
                          </mat-form-field>

                          <div class="perm-list">
                            @for (p of permissions(); track p.flag) {
                              <mat-checkbox
                                [checked]="hasFlag(editMask(), p.flag)"
                                (change)="setEditMask(toggleMask(editMask(), p.flag, $event.checked))">
                                {{ p.label }}
                              </mat-checkbox>
                            }
                          </div>

                          <div class="actions">
                            <button mat-raised-button color="primary" type="submit" [disabled]="busyTemplates() || editForm.invalid">Save</button>
                          </div>
                        </form>
                      }
                    </div>
                  }
                </div>
              }
            </div>
          </mat-card-content>
        </mat-card>
      } @else {
        <mat-card class="card">
          <mat-card-content>
            <div class="muted">Role templates are available only to users with CreateBoard permission.</div>
          </mat-card-content>
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

      .page {
        padding: 16px;
        display: flex;
        flex-direction: column;
        gap: 16px;
        align-items: center;
      }

      .card {
        width: 100%;
        max-width: 900px;
      }

      .row {
        display: flex;
        gap: 8px;
        margin: 4px 0;
        align-items: baseline;
      }

      .label {
        font-weight: 600;
        min-width: 120px;
      }

      .error {
        margin-top: 8px;
        padding: 12px;
        border-radius: 8px;
        background: rgba(244, 67, 54, 0.1);
        color: #b71c1c;
      }

      .muted {
        opacity: 0.8;
      }

      .section {
        margin-top: 16px;
      }

      .field {
        width: 100%;
      }

      .perm-list {
        display: flex;
        flex-direction: column;
        gap: 6px;
        margin-top: 8px;
      }

      .actions {
        margin-top: 12px;
        display: flex;
        gap: 8px;
      }

      .template-list {
        display: flex;
        flex-direction: column;
        gap: 12px;
        margin-top: 8px;
      }

      .template {
        padding: 12px;
        border: 1px solid rgba(0, 0, 0, 0.12);
        border-radius: 8px;
      }

      .template-header {
        display: flex;
        justify-content: space-between;
        gap: 12px;
        align-items: start;
      }

      .template-name {
        font-weight: 600;
      }

      .template-actions {
        display: flex;
        gap: 8px;
        flex-wrap: wrap;
        justify-content: end;
      }

      .edit {
        margin-top: 12px;
        padding-top: 12px;
        border-top: 1px solid rgba(0, 0, 0, 0.12);
      }
    `
  ],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class MeComponent {
  private readonly api = inject(ApiService);
  private readonly auth = inject(AuthService);
  private readonly fb = inject(FormBuilder);

  readonly busyContext = signal(false);
  readonly contextError = signal<string | null>(null);
  readonly nickname = signal<string>('');
  readonly systemPermissions = signal<number>(0);

  readonly busyTemplates = signal(false);
  readonly templatesError = signal<string | null>(null);
  readonly templates = signal<RoleTemplateDto[]>([]);

  readonly editingId = signal<string | null>(null);

  readonly createMask = signal<number>(0);
  readonly editMask = signal<number>(0);

  readonly createForm = this.fb.nonNullable.group({
    name: ['', [Validators.required, Validators.maxLength(100)]]
  });

  readonly editForm = this.fb.nonNullable.group({
    name: ['', [Validators.required, Validators.maxLength(100)]]
  });

  readonly canManageTemplates = computed(() => this.auth.hasPermission(SystemPermission.CreateBoard));

  readonly permissions = computed(() =>
    [
      { flag: BoardPermission.SuggestTile, label: 'SuggestTile' },
      { flag: BoardPermission.CastVote, label: 'CastVote' },
      { flag: BoardPermission.ApproveTile, label: 'ApproveTile' },
      { flag: BoardPermission.ModifyBoardSettings, label: 'ModifyBoardSettings' },
      { flag: BoardPermission.ManageBoardRoles, label: 'ManageBoardRoles' },
      { flag: BoardPermission.ForceCompleteTile, label: 'ForceCompleteTile' },
      { flag: BoardPermission.SelfCompleteTile, label: 'SelfCompleteTile' },
      { flag: BoardPermission.ModifyOwnPermissions, label: 'ModifyOwnPermissions' }
    ] as const
  );

  constructor() {
    effect(() => {
      void untracked(async () => {
        await this.loadSystemContext();
        await this.loadTemplates();
      });
    });
  }

  hasFlag(mask: number, flag: number): boolean {
    return (mask & flag) === flag;
  }

  toggleMask(mask: number, flag: number, checked: boolean): number {
    return checked ? mask | flag : mask & ~flag;
  }

  setCreateMask(mask: number): void {
    this.createMask.set(mask);
  }

  setEditMask(mask: number): void {
    this.editMask.set(mask);
  }

  private async loadSystemContext(): Promise<void> {
    this.contextError.set(null);
    try {
      this.busyContext.set(true);
      const ctx = await firstValueFrom(this.api.getMySystemContext());
      this.nickname.set(ctx.nickname);
      this.systemPermissions.set(ctx.systemPermissions);
    } catch (e) {
      this.contextError.set(e instanceof Error ? e.message : 'Failed to load system context.');
    } finally {
      this.busyContext.set(false);
    }
  }

  private async loadTemplates(): Promise<void> {
    if (!this.canManageTemplates()) {
      return;
    }

    this.templatesError.set(null);
    try {
      this.busyTemplates.set(true);
      const list = await firstValueFrom(this.api.getMyRoleTemplates());
      this.templates.set(list);
    } catch (e) {
      this.templatesError.set(e instanceof Error ? e.message : 'Failed to load role templates.');
    } finally {
      this.busyTemplates.set(false);
    }
  }

  async create(): Promise<void> {
    this.templatesError.set(null);
    if (this.createForm.invalid) {
      this.createForm.markAllAsTouched();
      return;
    }

    try {
      this.busyTemplates.set(true);
      await firstValueFrom(
        this.api.createRoleTemplate({
          name: this.createForm.controls.name.value.trim(),
          defaultBoardPermissions: this.createMask()
        })
      );
      this.createForm.reset();
      this.createMask.set(0);
      await this.loadTemplates();
    } catch (e) {
      this.templatesError.set(e instanceof Error ? e.message : 'Failed to create role template.');
    } finally {
      this.busyTemplates.set(false);
    }
  }

  beginEdit(t: RoleTemplateDto): void {
    if (this.editingId() === t.id) {
      this.editingId.set(null);
      return;
    }

    this.editingId.set(t.id);
    this.editForm.controls.name.setValue(t.name);
    this.editMask.set(t.defaultBoardPermissions);
  }

  async saveEdit(): Promise<void> {
    const id = this.editingId();
    if (!id) {
      return;
    }

    this.templatesError.set(null);
    if (this.editForm.invalid) {
      this.editForm.markAllAsTouched();
      return;
    }

    try {
      this.busyTemplates.set(true);
      await firstValueFrom(
        this.api.updateRoleTemplate(id, {
          name: this.editForm.controls.name.value.trim(),
          defaultBoardPermissions: this.editMask()
        })
      );
      this.editingId.set(null);
      await this.loadTemplates();
    } catch (e) {
      this.templatesError.set(e instanceof Error ? e.message : 'Failed to update role template.');
    } finally {
      this.busyTemplates.set(false);
    }
  }

  async remove(t: RoleTemplateDto): Promise<void> {
    this.templatesError.set(null);
    try {
      this.busyTemplates.set(true);
      await firstValueFrom(this.api.deleteRoleTemplate(t.id));
      if (this.editingId() === t.id) {
        this.editingId.set(null);
      }
      await this.loadTemplates();
    } catch (e) {
      this.templatesError.set(e instanceof Error ? e.message : 'Failed to delete role template.');
    } finally {
      this.busyTemplates.set(false);
    }
  }
}
