import { ApplicationConfig, provideZoneChangeDetection } from '@angular/core';
import { provideHttpClient, withFetch } from '@angular/common/http';
import { provideRouter } from '@angular/router';
import { HubConnectionBuilder, LogLevel } from '@microsoft/signalr';

import { routes } from './app.routes';
import { HUB_CONNECTION_FACTORY } from './core/services/hub-connection-factory';

export const appConfig: ApplicationConfig = {
  providers: [
    provideZoneChangeDetection({ eventCoalescing: true }),
    provideRouter(routes),
    provideHttpClient(withFetch()),
    {
      provide: HUB_CONNECTION_FACTORY,
      useFactory: () => {
        return () =>
          new HubConnectionBuilder()
            .withUrl(`${window.location.origin}/hubs/game`)
            .withAutomaticReconnect([0, 1000, 2000, 5000, 10000])
            .configureLogging(LogLevel.Information)
            .build();
      }
    }
  ]
};
