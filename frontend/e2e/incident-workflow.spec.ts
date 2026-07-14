import { expect, test } from '@playwright/test'

test('records and acknowledges a tenant incident through the UI', async ({ page }) => {
  await page.goto('/')
  await page.getByLabel('Tenant ID').fill('demo')
  await page.getByLabel('API key').fill('demo-key')
  await page.getByRole('button', { name: 'Connect' }).click()
  await expect(page.getByText('No incidents recorded for this tenant.')).toBeVisible()

  await page.getByLabel('Device ID').fill('panel-e2e-01')
  await page.getByLabel('Event type').selectOption('Alarm')
  await page.getByLabel('Message').fill('E2E smoke detector alarm')
  await page.getByRole('button', { name: 'Record event' }).click()

  await expect(page.getByText('panel-e2e-01')).toBeVisible()
  await expect(page.getByText('E2E smoke detector alarm')).toBeVisible()
  await expect(page.getByText('Critical')).toBeVisible()

  await page.getByRole('button', { name: 'Acknowledge' }).click()
  await expect(page.getByText('Acknowledged')).toBeVisible()
})

