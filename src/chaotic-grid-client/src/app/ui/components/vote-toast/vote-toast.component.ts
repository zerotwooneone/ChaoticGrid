import { ChangeDetectionStrategy, Component, Input } from '@angular/core';
import { MatCardModule } from '@angular/material/card';

@Component({
  selector: 'app-vote-toast',
  standalone: true,
  imports: [MatCardModule],
  template: `
    <mat-card class="toast">
      <mat-card-content>
        <div class="title">Vote</div>
        <div class="message">{{ message }}</div>
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
    `
  ],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class VoteToastComponent {
  @Input({ required: true }) message = '';
}
