import { TestBed } from '@angular/core/testing';

import { GameStore } from './game.store';
import { VoteRequest } from '../../domain/models';

describe('GameStore', () => {
  it('should add pending vote when VoteRequested is received', () => {
    TestBed.configureTestingModule({});

    const store = TestBed.inject(GameStore);

    const vote: VoteRequest = {
      playerId: crypto.randomUUID(),
      tileId: crypto.randomUUID()
    };

    expect(store.pendingVotes()).toEqual([]);

    store.onVoteRequested(vote);

    expect(store.pendingVotes()).toEqual([vote]);
  });
});
