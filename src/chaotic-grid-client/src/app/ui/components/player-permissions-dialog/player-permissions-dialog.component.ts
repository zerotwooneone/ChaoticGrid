import { ChangeDetectionStrategy, Component, Inject } from '@angular/core';
import { MAT_DIALOG_DATA, MatDialogModule } from '@angular/material/dialog';

import { PlayerContextInspectorComponent } from '../player-context-inspector/player-context-inspector.component';

@Component({
  selector: 'app-player-permissions-dialog',
  standalone: true,
  imports: [MatDialogModule, PlayerContextInspectorComponent],
  template: `
    <h2 mat-dialog-title>My Permissions</h2>
    <div mat-dialog-content>
      <app-player-context-inspector [boardId]="data.boardId" [inDialog]="true"></app-player-context-inspector>
    </div>
  `,
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class PlayerPermissionsDialogComponent {
  constructor(@Inject(MAT_DIALOG_DATA) public readonly data: { boardId: string }) {}
}
