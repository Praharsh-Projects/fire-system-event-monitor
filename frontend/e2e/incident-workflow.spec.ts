import { expect, test } from '@playwright/test'

test('records and acknowledges a tenant incident through the UI', async ({ page }) => {
  const deviceId = `panel-e2e-${Date.now()}`

  await page.goto('/')
  await page.getByLabel('Tenant ID').fill('demo')
  await page.getByLabel('API key').fill('demo-key')
  await page.getByRole('button', { name: 'Connect' }).click()
  await expect(page.getByRole('heading', { name: 'Record device event' })).toBeVisible()

  await page.getByLabel('Device ID').fill(deviceId)
  await page.getByLabel('Event type').selectOption('Alarm')
  await page.getByLabel('Message').fill('E2E smoke detector alarm')
  await page.getByRole('button', { name: 'Record event' }).click()

  const incident = page.getByRole('listitem').filter({ hasText: deviceId })
  await expect(incident).toBeVisible()
  await expect(incident).toContainText('E2E smoke detector alarm')
  await expect(incident).toContainText('Critical')

  await incident.getByRole('button', { name: 'Acknowledge' }).click()
  await expect(incident).toContainText('Acknowledged')
})
