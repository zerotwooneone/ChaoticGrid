import { TestBed } from '@angular/core/testing';

import { GameStore } from './game.store';
import { CompletionVoteStartedDto } from '../../domain/models';
import { ApiService } from '../services/api.service';

describe('GameStore', () => {
  it('should add pending completion vote when VoteRequested is received', () => {
    TestBed.configureTestingModule({
      providers: [
        {
          provide: ApiService,
          useValue: {
            getBoardState: () => {
              throw new Error('Not used in this test.');
            }
          }
        }
      ]
    });

    const store = TestBed.inject(GameStore);

    const vote: CompletionVoteStartedDto = {
      proposerId: crypto.randomUUID(),
      tileId: crypto.randomUUID()
    };

    expect(store.pendingCompletionVotes()).toEqual([]);

    store.onCompletionVoteRequested(vote);

    expect(store.pendingCompletionVotes()).toEqual([vote]);
  });
});
