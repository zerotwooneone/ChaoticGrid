import { ChangeDetectionStrategy, Component, computed, effect, inject, signal, untracked } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatToolbarModule } from '@angular/material/toolbar';
import { firstValueFrom } from 'rxjs';

import { ApiService } from '../../../core/services/api.service';
import { GameStore } from '../../../core/store/game.store';
import { BingoGridComponent } from '../../components/bingo-grid/bingo-grid.component';

@Component({
  selector: 'app-game-board',
  standalone: true,
  imports: [MatToolbarModule, MatButtonModule, MatCardModule, RouterLink, BingoGridComponent],
  templateUrl: './game-board.component.html',
  styleUrl: './game-board.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class GameBoardComponent {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly api = inject(ApiService);
  private readonly store = inject(GameStore);

  readonly boardId = signal<string>(this.route.snapshot.paramMap.get('boardId') ?? '');

  readonly state = this.store.boardState;

  readonly gridTileIds = this.store.gridTileIds;

  readonly tilesById = computed(() => {
    const tiles = this.store.tiles();
    const map = new Map<string, (typeof tiles)[number]>();
    for (const t of tiles) {
      map.set(t.id, t);
    }
    return map;
  });

  constructor() {
    effect(() => {
      const id = this.boardId();
      if (!id) {
        void this.router.navigate(['/']);
        return;
      }

      const current = this.store.boardId();
      if (current === id) {
        return;
      }

      void untracked(async () => {
        const state = await firstValueFrom(this.api.getBoardState(id));
        this.store.setBoardState(state);
      });
    });
  }
}
