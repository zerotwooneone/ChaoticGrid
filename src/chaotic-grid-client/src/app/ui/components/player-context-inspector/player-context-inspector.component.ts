import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, Inject, Input, computed, effect, inject, signal, untracked } from '@angular/core';
import { firstValueFrom } from 'rxjs';

import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MAT_DIALOG_DATA, MatDialog, MatDialogModule } from '@angular/material/dialog';
import { MatChipsModule } from '@angular/material/chips';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';

import { ApiService } from '../../../core/services/api.service';
import { BoardPermission, PlayerContextDto } from '../../../domain/models';

@Component({
  selector: 'app-confirm-permission-override-dialog',
  standalone: true,
  imports: [MatButtonModule, MatDialogModule],
  template: `
    <h2 mat-dialog-title>Confirm</h2>
    <div mat-dialog-content>
      {{ data.text }}
    </div>
    <div mat-dialog-actions align="end">
      <button mat-button [mat-dialog-close]="false">Cancel</button>
      <button mat-raised-button color="primary" [mat-dialog-close]="true">Save</button>
    </div>
  `,
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class ConfirmPermissionOverrideDialogComponent {
  constructor(@Inject(MAT_DIALOG_DATA) public readonly data: { text: string }) {}
}

@Component({
  selector: 'app-player-context-inspector',
  standalone: true,
  imports: [CommonModule, MatCardModule, MatButtonModule, MatProgressSpinnerModule, MatCheckboxModule, MatDialogModule, MatChipsModule, MatTooltipModule],
  template: `
    @if (!inDialog) {
      <mat-card class="card">
        <mat-card-header>
          <mat-card-title>My Board Context</mat-card-title>
          <mat-card-subtitle>Role + effective permissions</mat-card-subtitle>
        </mat-card-header>

        <mat-card-content>
          <ng-container [ngTemplateOutlet]="content"></ng-container>
        </mat-card-content>
      </mat-card>
    } @else {
      <ng-container [ngTemplateOutlet]="content"></ng-container>
    }

    <ng-template #content>
      @if (busy()) {
        <mat-progress-spinner diameter="20" mode="indeterminate"></mat-progress-spinner>
      } @else if (error()) {
        <div class="error">{{ error() }}</div>
      } @else {
        @if (ctx()) {
          <div class="row"><span class="label">Role:</span> <span>{{ ctx()!.roleName ?? 'None' }}</span></div>

          <div class="cap-grid">
            @for (p of permissions(); track p.flag) {
              <mat-chip [matTooltip]="p.tooltip" [ngClass]="chipClass(p.flag)">
                {{ p.label }}
              </mat-chip>
            }
          </div>

          @if (canModifyOwnPermissions()) {
            <div class="actions">
              <button mat-stroked-button color="primary" (click)="refresh()" [disabled]="busy()">Refresh</button>
              <button mat-raised-button color="primary" (click)="toggleEditor()" [disabled]="busy()">
                {{ showEditor() ? 'Close' : 'Modify My Permissions' }}
              </button>
            </div>

            @if (showEditor()) {
              <div class="editor">
                <div class="hint">
                  You are editing your effective permission set. Changes are stored as allow/deny overrides relative to your Role.
                </div>

                <div class="perm-list">
                  @for (p of permissions(); track p.flag) {
                    <mat-checkbox [checked]="isDesiredEffective(p.flag)" (change)="setDesiredEffective(p.flag, $event.checked)">
                      {{ p.label }}
                      <span class="source">{{ getPermissionSourceText(p.flag) }}</span>
                    </mat-checkbox>
                  }
                </div>

                <div class="actions">
                  <button mat-raised-button color="primary" (click)="save()" [disabled]="busy()">Save</button>
                </div>
              </div>
            }
          } @else {
            <div class="actions">
              <button mat-stroked-button color="primary" (click)="refresh()" [disabled]="busy()">Refresh</button>
            </div>
          }
        } @else {
          <div>Not loaded.</div>
        }
      }
    </ng-template>
  `,
  styles: [
    `
      .card {
        margin-top: 16px;
      }

      .row {
        display: flex;
        gap: 8px;
        margin: 4px 0;
        align-items: baseline;
      }

      .label {
        font-weight: 600;
        min-width: 90px;
      }

      .actions {
        margin-top: 12px;
        display: flex;
        gap: 8px;
      }

      .cap-grid {
        margin-top: 12px;
        display: flex;
        flex-wrap: wrap;
        gap: 8px;
      }

      .chip-inherited {
        background: rgba(46, 125, 50, 0.18);
        color: #1b5e20;
      }

      .chip-override {
        background: rgba(245, 124, 0, 0.18);
        color: #e65100;
      }

      .chip-masked {
        background: rgba(97, 97, 97, 0.18);
        color: #424242;
        text-decoration: line-through;
      }

      .chip-inactive {
        opacity: 0.55;
      }

      .editor {
        margin-top: 12px;
        padding-top: 12px;
        border-top: 1px solid rgba(0, 0, 0, 0.12);
      }

      .perm-list {
        display: flex;
        flex-direction: column;
        gap: 6px;
        margin-top: 8px;
      }

      .hint {
        opacity: 0.8;
      }

      .tag {
        margin-left: 6px;
        opacity: 0.7;
        font-size: 12px;
      }

      .error {
        color: #b00020;
      }
    `
  ],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class PlayerContextInspectorComponent {
  private readonly api = inject(ApiService);
  private readonly dialog = inject(MatDialog);

  @Input({ required: true }) boardId!: string;
  @Input() inDialog = false;

  readonly busy = signal(false);
  readonly error = signal<string | null>(null);
  readonly ctx = signal<PlayerContextDto | null>(null);

  readonly showEditor = signal(false);
  readonly desiredEffective = signal<number>(0);

  readonly canModifyOwnPermissions = computed(() => {
    const c = this.ctx();
    if (!c) {
      return false;
    }

    return (c.effectivePermissions & BoardPermission.ModifyOwnPermissions) === BoardPermission.ModifyOwnPermissions;
  });

  constructor() {
    effect(() => {
      const id = this.boardId;
      if (!id) {
        return;
      }

      void untracked(() => this.refresh());
    });
  }

  readonly permissions = computed(() =>
    [
      { flag: BoardPermission.SuggestTile, label: 'SuggestTile', tooltip: 'Submit new tiles.' },
      { flag: BoardPermission.CastVote, label: 'CastVote', tooltip: 'Participate in consensus.' },
      { flag: BoardPermission.ApproveTile, label: 'ApproveTile', tooltip: 'Moderate tile suggestions.' },
      { flag: BoardPermission.ModifyBoardSettings, label: 'ModifyBoardSettings', tooltip: 'Edit board title/settings.' },
      { flag: BoardPermission.ManageBoardRoles, label: 'ManageBoardRoles', tooltip: 'Assign roles to others.' },
      { flag: BoardPermission.ForceCompleteTile, label: 'ForceCompleteTile', tooltip: 'Force-complete tiles.' },
      { flag: BoardPermission.SelfCompleteTile, label: 'SelfCompleteTile', tooltip: 'Self-complete tiles.' },
      { flag: BoardPermission.ModifyOwnPermissions, label: 'ModifyOwnPermissions', tooltip: 'Modify your own permission overrides.' }
    ] as const
  );

  chipClass(flag: BoardPermission): string {
    const c = this.ctx();
    if (!c) {
      return 'chip-inactive';
    }

    const role = (c.rolePermissions & flag) === flag;
    const allow = (c.allowOverrides & flag) === flag;
    const deny = (c.denyOverrides & flag) === flag;
    const effective = (c.effectivePermissions & flag) === flag;

    if (deny && role && !effective) {
      return 'chip-masked';
    }

    if (effective && allow && !role) {
      return 'chip-override';
    }

    if (effective && role) {
      return 'chip-inherited';
    }

    return 'chip-inactive';
  }

  async refresh(): Promise<void> {
    this.error.set(null);

    if (!this.boardId) {
      return;
    }

    try {
      this.busy.set(true);
      const ctx = await firstValueFrom(this.api.getMyBoardContext(this.boardId));
      this.ctx.set(ctx);
      this.desiredEffective.set(ctx.effectivePermissions);
    } catch (e) {
      this.error.set(e instanceof Error ? e.message : 'Failed to load context.');
    } finally {
      this.busy.set(false);
    }
  }

  toggleEditor(): void {
    const next = !this.showEditor();
    this.showEditor.set(next);
    if (next) {
      this.resetEdits();
    }
  }

  resetEdits(): void {
    const c = this.ctx();
    this.desiredEffective.set(c?.effectivePermissions ?? 0);
  }

  hasEdits(): boolean {
    const c = this.ctx();
    if (!c) {
      return false;
    }

    return this.desiredEffective() !== c.effectivePermissions;
  }

  isRoleGranted(flag: BoardPermission): boolean {
    const c = this.ctx();
    if (!c) {
      return false;
    }

    return (c.rolePermissions & flag) === flag;
  }

  isDesired(flag: BoardPermission): boolean {
    return (this.desiredEffective() & flag) === flag;
  }

  onToggleDesired(flag: BoardPermission, checked: boolean): void {
    const current = this.desiredEffective();
    const next = checked ? current | flag : current & ~flag;
    this.desiredEffective.set(next);
  }

  tagFor(flag: BoardPermission): string | null {
    const role = this.isRoleGranted(flag);
    const desired = this.isDesired(flag);

    if (role && desired) {
      return 'role';
    }

    if (role && !desired) {
      return 'masked';
    }

    if (!role && desired) {
      return 'extended';
    }

    return null;
  }

  isDesiredEffective(flag: BoardPermission): boolean {
    return this.isDesired(flag);
  }

  setDesiredEffective(flag: BoardPermission, checked: boolean): void {
    this.onToggleDesired(flag, checked);
  }

  getPermissionSourceText(flag: BoardPermission): string {
    const tag = this.tagFor(flag);
    return tag ? `(${tag})` : '';
  }

  async save(): Promise<void> {
    await this.saveEdits();
  }

  async saveEdits(): Promise<void> {
    this.error.set(null);
    if (!this.boardId) {
      return;
    }

    const ok = await firstValueFrom(
      this.dialog
        .open(ConfirmPermissionOverrideDialogComponent, {
          data: {
            text: 'You are modifying your specific permission set. This overrides your assigned Role defaults.'
          }
        })
        .afterClosed()
    );

    if (!ok) {
      return;
    }

    try {
      this.busy.set(true);
      const c = this.ctx();
      const role = c?.rolePermissions ?? 0;
      const desired = this.desiredEffective();

      const allow = desired & ~role;
      const deny = (~desired) & role;

      const updated = await firstValueFrom(
        this.api.updateMyBoardPermissionOverrides(this.boardId, { allowOverrideMask: allow, denyOverrideMask: deny })
      );
      this.ctx.set(updated);
      this.desiredEffective.set(updated.effectivePermissions);
      this.showEditor.set(false);
    } catch (e) {
      this.error.set(e instanceof Error ? e.message : 'Failed to save permission overrides.');
    } finally {
      this.busy.set(false);
    }
  }
}
