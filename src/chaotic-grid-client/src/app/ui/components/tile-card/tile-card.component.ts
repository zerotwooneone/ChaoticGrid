import { ChangeDetectionStrategy, Component, EventEmitter, Input, Output } from '@angular/core';
import { MatCardModule } from '@angular/material/card';
import { TileDto } from '../../../domain/models';

@Component({
  selector: 'app-tile-card',
  standalone: true,
  imports: [MatCardModule],
  template: `
    <mat-card class="tile" [class.missing]="!tile" [class.approved]="tile?.isApproved" [class.confirmed]="tile?.isConfirmed" (click)="clicked.emit()">
      <mat-card-content>
        @if (tile) {
          {{ tile.text }}
        } @else {
          <span class="placeholder">—</span>
        }
      </mat-card-content>
    </mat-card>
  `,
  styles: [
    `
      .tile {
        height: 100%;
        display: flex;
        align-items: center;
        justify-content: center;
        padding: 8px;
        text-align: center;
      }

      .tile.approved {
        outline: 2px solid rgba(76, 175, 80, 0.6);
      }

      .tile.confirmed {
        outline: 3px solid rgba(33, 150, 243, 0.7);
      }

      .tile.missing {
        opacity: 0.5;
      }

      .placeholder {
        opacity: 0.6;
      }
    `
  ],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class TileCardComponent {
  @Input() tile: TileDto | null = null;

  @Output() readonly clicked = new EventEmitter<void>();
}
