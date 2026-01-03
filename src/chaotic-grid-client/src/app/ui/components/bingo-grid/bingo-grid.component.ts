import { ChangeDetectionStrategy, Component, Input, computed, signal } from '@angular/core';
import { MatGridListModule } from '@angular/material/grid-list';
import { TileDto } from '../../../domain/models';
import { TileCardComponent } from '../tile-card/tile-card.component';

@Component({
  selector: 'app-bingo-grid',
  standalone: true,
  imports: [MatGridListModule, TileCardComponent],
  template: `
    <mat-grid-list cols="5" rowHeight="1:1" gutterSize="8">
      @for (cell of cells(); track $index) {
        <mat-grid-tile>
          <app-tile-card [tile]="cell"></app-tile-card>
        </mat-grid-tile>
      }
    </mat-grid-list>
  `,
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class BingoGridComponent {
  private readonly gridTileIdsInternal = signal<string[]>([]);
  private readonly tilesByIdInternal = signal<Map<string, TileDto>>(new Map());

  @Input({ required: true })
  set gridTileIds(value: string[]) {
    this.gridTileIdsInternal.set(value ?? []);
  }

  @Input({ required: true })
  set tilesById(value: Map<string, TileDto>) {
    this.tilesByIdInternal.set(value ?? new Map());
  }

  readonly cells = computed(() => {
    const ids = this.gridTileIdsInternal();
    const tiles = this.tilesByIdInternal();

    const result: Array<TileDto | null> = [];
    for (let i = 0; i < 25; i++) {
      const id = ids[i];
      result.push(id ? tiles.get(id) ?? null : null);
    }

    return result;
  });
}
