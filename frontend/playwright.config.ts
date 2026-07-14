import { defineConfig } from '@playwright/test'

export default defineConfig({
  testDir: './e2e',
  use: {
    baseURL: 'http://127.0.0.1:4173',
    trace: 'retain-on-failure',
  },
  webServer: [
    {
      command: '$HOME/.dotnet/dotnet run --project ../src/FireSystemEventMonitor.Api/FireSystemEventMonitor.Api.csproj',
      url: 'http://127.0.0.1:5080/health',
      reuseExistingServer: true,
      env: {
        ASPNETCORE_URLS: 'http://127.0.0.1:5080',
        DatabaseProvider: 'Sqlite',
        ConnectionStrings__Default: 'Data Source=/tmp/fire-system-event-monitor-e2e.db',
        Tenants__demo: 'demo-key',
      },
    },
    {
      command: 'npm run build && npm run preview -- --host 127.0.0.1',
      url: 'http://127.0.0.1:4173',
      reuseExistingServer: true,
    },
  ],
})

