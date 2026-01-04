import { TestBed } from '@angular/core/testing';
import { ActivatedRoute, Router, convertToParamMap } from '@angular/router';
import { RouterTestingModule } from '@angular/router/testing';
import { of } from 'rxjs';

import { ActiveBoardComponent } from './active-board.component';
import { ApiService } from '../../../core/services/api.service';
import { GameStore } from '../../../core/store/game.store';
import { HUB_CONNECTION_FACTORY } from '../../../core/services/hub-connection-factory';

describe('ActiveBoardComponent', () => {
  it('should redirect to /lobby/:boardId when board is Draft', async () => {
    const apiMock = {
      getBoardState: jasmine.createSpy('getBoardState').and.returnValue(
        of({
          boardId: 'b',
          name: 'n',
          status: 0,
          minimumApprovedTilesToStart: 24,
          tiles: [],
          players: []
        })
      )
    };

    TestBed.configureTestingModule({
      imports: [RouterTestingModule, ActiveBoardComponent],
      providers: [
        { provide: ApiService, useValue: apiMock },
        {
          provide: HUB_CONNECTION_FACTORY,
          useValue: () => ({
            state: 0,
            on: () => {},
            onreconnected: () => {},
            start: async () => {},
            stop: async () => {},
            invoke: async () => {}
          })
        },
        { provide: ActivatedRoute, useValue: { snapshot: { paramMap: convertToParamMap({ boardId: 'b' }) } } }
      ]
    });

    const fixture = TestBed.createComponent(ActiveBoardComponent);
    const store = TestBed.inject(GameStore);
    const router = TestBed.inject(Router);
    spyOn(router, 'navigate').and.resolveTo(true);

    // Ensure the store isn't already set to this boardId so the component loads via ApiService.
    store.clear();

    fixture.detectChanges();

    await fixture.whenStable();

    expect(router.navigate).toHaveBeenCalledWith(['/lobby', 'b']);
  });

  it('should not redirect when board is Active', async () => {
    const apiMock = {
      getBoardState: jasmine.createSpy('getBoardState').and.returnValue(
        of({
          boardId: 'b',
          name: 'n',
          status: 1,
          minimumApprovedTilesToStart: 24,
          tiles: [],
          players: []
        })
      )
    };

    TestBed.configureTestingModule({
      imports: [RouterTestingModule, ActiveBoardComponent],
      providers: [
        { provide: ApiService, useValue: apiMock },
        {
          provide: HUB_CONNECTION_FACTORY,
          useValue: () => ({
            state: 0,
            on: () => {},
            onreconnected: () => {},
            start: async () => {},
            stop: async () => {},
            invoke: async () => {}
          })
        },
        { provide: ActivatedRoute, useValue: { snapshot: { paramMap: convertToParamMap({ boardId: 'b' }) } } }
      ]
    });

    const fixture = TestBed.createComponent(ActiveBoardComponent);
    const store = TestBed.inject(GameStore);
    const router = TestBed.inject(Router);
    spyOn(router, 'navigate').and.resolveTo(true);
    store.clear();

    fixture.detectChanges();

    await fixture.whenStable();

    expect(router.navigate).not.toHaveBeenCalledWith(['/lobby', 'b']);
  });
});
