import { ChangeDetectionStrategy, Component, EventEmitter, Input, Output } from '@angular/core';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';

@Component({
  selector: 'app-vote-toast',
  standalone: true,
  imports: [MatCardModule, MatButtonModule],
  template: `
    <mat-card class="toast">
      <mat-card-content>
        <div class="title">Is this true?</div>
        <div class="message">{{ tileText }}</div>

        <div class="actions">
          <button mat-raised-button color="primary" (click)="voted.emit(true)">Yes</button>
          <button mat-raised-button color="warn" (click)="voted.emit(false)">No</button>
        </div>
      </mat-card-content>
    </mat-card>
  `,
  styles: [
    `
      .toast {
        width: 320px;
      }

      .title {
        font-weight: 600;
        margin-bottom: 4px;
      }

      .message {
        opacity: 0.85;
      }

      .actions {
        display: flex;
        gap: 8px;
        margin-top: 12px;
      }
    `
  ],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class VoteToastComponent {
  @Input({ required: true }) tileText = '';

  @Output() readonly voted = new EventEmitter<boolean>();
}
